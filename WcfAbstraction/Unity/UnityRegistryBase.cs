using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Practices.Unity;
using WcfAbstraction.Reflection;

namespace WcfAbstraction.TestTools.Unity
{
    /* Why Singleton & Static together?
     *      1. we want to use this implemenation as base OO (we need it in more places)
     *      1. in the end we want to use UnityRegistry.GetService<myInterface>() and not UnityRegistry.Instance.GetService<myInterface>() (which is longer)
     *         this can be acchived only with satatic.
     *      2. we still want to be able to override curtain method in base
     */

    /// <summary>
    /// Represents implementation of Microsoft Unity for server communication.
    /// 
    /// The UnityRegistry reads all interface/instance configuration from app.config 
    /// By changing the app.config different instances per interface can be introduced (for unittesting purpose for instance).
    /// 
    /// <remarks>
    /// 1. UnityContainer - Resolve, ResolveAll, and BuildUp are all thread safe (http://www.codeplex.com/unity/Thread/View.aspx?ThreadId=27496).
    /// 2. Concrete type will be wrapped with policy injection if any exists
    /// </remarks>
    /// </summary>
    /// <typeparam name="T">The type inheriting from this base class</typeparam>
    public class UnityRegistryBase<T> where T : UnityRegistryBase<T>, new()
    {
        #region Private Members

        private static readonly T unityRegistryBaseInstance = null;

        protected static IUnityContainer _container;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the singleton instance of <typeparamref name="T"/>.
        /// </summary>
        /// <value>The instance.</value>
        public static T Instance
        {
            [DebuggerStepThrough]
            get { return unityRegistryBaseInstance; }
        }

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes static members of the <see cref="UnityRegistryBase{T}"/> class.
        /// Loads the server address and configure the unity container.
        /// </summary>
        static UnityRegistryBase()
        {
            unityRegistryBaseInstance = (T)Activator.CreateInstance(typeof(T), true);
            unityRegistryBaseInstance.Reload(); //ensure lazy initialization just incase
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers the service, mapping between the contract type
        /// and the concrete instance
        /// <remarks>
        /// If the service already exists it will be override
        /// </remarks>
        /// </summary>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="concreteType">Type of the concrete service.</param>
        public virtual void RegisterService(Type contractType, Type concreteType)
        {
            if (contractType == null)
            {
                throw new ArgumentNullException("Type of service contract to load cannot be null", "instance");
            }

            object instance = Activator.CreateInstance(concreteType, true);
            RegisterServiceInstance(contractType, instance);
        }

        /// <summary>
        /// Registers the service, mapping between contract type and instance of the class
        /// implementing it.
        /// </summary>
        /// <param name="contractType">Contract type</param>
        /// <param name="instance">Instance of class implementing contract interface</param>
        public virtual void RegisterServiceInstance(Type contractType, object instance)
        {
            if (_container == null)
            {
                _container = new UnityContainer();
            }

            //register with UnityContainer
            _container.RegisterInstance(contractType, instance);
        }

        /// <summary>
        /// Creates the default instance if no concrete type or mock was defined in configuration.
        /// </summary>
        /// <param name="contractType">Type of the contract.</param>
        /// <returns></returns>
        public virtual object CreateDefaultInstance(Type contractType)
        {
            throw new NotImplementedException("CreateDefaultInstance - must be implemented in sub class only");
        }

        /// <summary>
        /// Gets the service which is a transparent proxie to the server.
        /// </summary>
        /// <typeparam name="S">Service contract type</typeparam>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static S GetService<S>() where S : class
        {
            return (S)_container.Resolve(typeof(S));
        }

        /// <summary>
        /// Reloads this instance by re-reading the app.config, 
        /// re-establishing and registering instances with the unity container.
        /// </summary>
        public virtual void Reload()
        {
            ConfigureUnityContainer();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Configures the unity container.
        /// 
        /// Reads the ServiceToLoad app.config section and creates a 
        /// transparent proxy to all server services. These transparent 
        /// proxies are then registered within the UnityContainer ready 
        /// to be used by the application.
        /// </summary>
        private static void ConfigureUnityContainer()
        {
            _container = new UnityContainer();

            var config = GetConfigMappings();

            foreach (var map in config.Mappings)
            {
                //try-catch because we want developers to continue to work even when
                //they do not have all assemblies. In most cases, they do not need them
                //anyway.
                //On release, we need this method to fail, so we can catch potential
                //issues before it is release to client - hence the #IF-DEF
#if DEBUG
                try
                {
#endif
                    //get the contract type to load
                    Type contractType = Type.GetType(map.ContractType);
                    if (contractType == null)
                    {
                        throw new ArgumentNullException("Type of service contract to load cannot be null");
                    }

                    //get the service type
                    if (!string.IsNullOrEmpty(map.ConcreteType)) //we have a concrete contract implementation
                    {
                        Type concreteType = Type.GetType(map.ConcreteType);

                        if (concreteType == null)
                        {
                            throw new Exception("Your concrete '" + map.ConcreteType + "' type cannot be established");
                        }

                        if (!concreteType.GetInterfaces().Contains(contractType))
                        {
                            throw new Exception("Your concrete object instance '" + map.ConcreteType + "' does not implement contract '" + map.ContractType + "'.");
                        }

                        object concreteInstance = Activator.CreateInstance(concreteType, true);
                        if (concreteInstance == null)
                        {
                            throw new Exception("Your concrete object instance cannot be initialized '" + map.ConcreteType + "'.");
                        }

                        //register with UnityContainer
                        _container.RegisterInstance(contractType, concreteInstance);
                    }
                    else if (!string.IsNullOrEmpty(map.MockType)) //we have mock object using Moq.dll
                    {
                        Type mockType = Type.GetType(map.MockType);

                        if (mockType == null)
                        {
                            throw new Exception("Your mock '" + map.MockType + "' type cannot be esstablished");
                        }

                        if (!mockType.IsSubclassOfRawGeneric(typeof(MoqObject<>)))
                        {
                            throw new Exception("Your mock object instance '" + map.MockType + "' does not implement WcfAbstraction.TestTools.MoqObject abstract class.");
                        }

                        object mockInstance = Activator.CreateInstance(mockType);
                        if (mockInstance == null)
                        {
                            throw new Exception("Your mock object instance cannot be initialized '" + map.MockType + "'.");
                        }

                        PropertyInfo propInfo = mockType.GetProperty("MockObject", contractType);
                        if (propInfo == null)
                        {
                            throw new Exception("Your mock '" + map.MockType + "' does not contain a 'Service' property returning your contract type '" + map.ContractType + ".");
                        }

                        object service = propInfo.GetValue(mockInstance, null);
                        if (service == null)
                        {
                            throw new Exception("Your mock '" + map.MockType + "' 'Service' property returns a null object reference");
                        }

                        //register with UnityContainer
                        _container.RegisterInstance(contractType, service);
                    }
                    else //we have default implementation
                    {
                        //register with UnityContainer
                        _container.RegisterInstance(contractType, unityRegistryBaseInstance.CreateDefaultInstance(contractType));
                    }
#if DEBUG
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error loading contract type " + map.ContractType + ": " + ex.ToString());
                }
#endif
            }
        }

        /// <summary>
        /// Gets the transparent proxies config mappings fron configuration file.
        /// </summary>
        /// <returns></returns>
        protected static UnityMappingSection GetConfigMappings()
        {
            return Configuration.AppConfigHandler.GetSectionByType<UnityMappingSection>();
        }

        #endregion
    }
}
