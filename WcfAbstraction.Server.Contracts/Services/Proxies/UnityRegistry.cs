using System;
using System.Linq;
using System.Reflection;
using Microsoft.Practices.Unity;
using WcfAbstraction.Configuration;
using WcfAbstraction.Reflection;
using WcfAbstraction.Server.Proxies;
using WcfAbstraction.TestTools.Unity;

namespace WcfAbstraction.Server.Services.Proxies
{
    /// <summary>
    /// Represents implementation of Microsoft Unity for server communication.
    /// 
    /// The UnityRegistry reads all services/contracts configuration from app.config 
    /// and establishes a transparent proxy to be later used through-out the application.
    /// By changing the app.config different service endpoints can be 
    /// introduced (for unit testing purpose for instance).
    /// 
    /// <remarks>
    /// UnityContainer - Resolve, ResolveAll, and BuildUp are all thread safe (http://www.codeplex.com/unity/Thread/View.aspx?ThreadId=27496).
    /// </remarks>
    /// </summary>
    public class UnityRegistry : UnityRegistryBase<UnityRegistry>
    {
        #region Private Members

        /// <summary>
        /// Ensure thread safe when recreating proxies in fault state
        /// </summary>
        private static readonly object lockObject = new object();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the server address.
        /// </summary>
        /// <value>The server address.</value>
        public static string ServerAddress
        {
            get;
            private set;
        }

        #endregion

        /// <summary>
        /// Registers the service, mapping between the contract type
        /// and the concrete instance
        /// </summary>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="concreteType">Type of the concrete service.</param>
        /// <remarks>
        /// If the service already exists it will be overriden.
        /// </remarks>
        public override void RegisterService(Type contractType, Type concreteType)
        {
            // for validation
            object instance = Activator.CreateInstance(concreteType, true);
            this.RegisterServiceInstance(contractType, instance);
        }

        /// <summary>
        /// Registers the service, mapping between contract type and instance of the class
        /// implementing it.
        /// </summary>
        /// <param name="contractType">Contract type</param>
        /// <param name="instance">Instance of class implementing contract interface</param>
        public override void RegisterServiceInstance(Type contractType, object instance)
        {
            //incase of proxy - make sure it is "real"
            if (instance is System.ServiceModel.ICommunicationObject)
            {
                ((System.ServiceModel.ICommunicationObject)instance).Faulted += new EventHandler(Proxy_Faulted);
            }
            else if (instance.GetType() == typeof(Proxy<>))
            {
                throw new ArgumentException("Type of service contract to load cannot be of Proxy<> type, use transparent (real) proxy instead", "instance");
            }

            base.RegisterServiceInstance(contractType, instance);
        }

        /// <summary>
        /// Creates the default instance if no concrete type or mock was defined in configuration.
        /// The default instance is Transparent-Proxy
        /// </summary>
        /// <param name="contractType">Type of the contract.</param>
        /// <returns></returns>
        public override object CreateDefaultInstance(Type contractType)
        {
            //mimic Proxy<T>.Create().Channel using reflection
            Type proxyType = typeof(Proxy<>);

            Type proxyGenericType = proxyType.MakeGenericType(contractType);
            if (proxyGenericType == null)
            { 
                throw new Exception("Proxy of your contract type '" + contractType.ToString() + "' cannot be established");
            }

            MethodInfo proxyCreateMethod = proxyGenericType.GetMethod(
                "Create",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                null,
                new Type[] { typeof(string) },
                null);
            PropertyInfo proxyChannelProperty = proxyGenericType.GetProperty("Channel");
            MethodInfo proxyChannelPropertyGetMethod = proxyChannelProperty.GetGetMethod();

            //get our transparent proxy
            var returnStaticProxy = proxyCreateMethod.Invoke(null, new object[] { ServerAddress });
            if (returnStaticProxy == null)
            { 
                throw new Exception("Proxy of your contract type '" + contractType.ToString() + "' cannot be invoke with the given server address '" + ServerAddress + "'");
            }

            //make transparent proxy
            var transparentProxy = proxyChannelPropertyGetMethod.Invoke(returnStaticProxy, null);
            if (transparentProxy == null)
            { 
                throw new Exception("Transparent proxy of '" + contractType.ToString() + "' cannot be established");
            }

            //handle fault state 
            var clientField = returnStaticProxy.GetType().GetField("client", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
            var client = (System.ServiceModel.ICommunicationObject)clientField.GetValue(returnStaticProxy);
            client.Faulted += new EventHandler(Proxy_Faulted);

            return transparentProxy;
        }

        /// <summary>
        /// Reloads this instance by re-reading the app.config,
        /// re-establishing and registering instances with the unity container.
        /// </summary>
        public override void Reload()
        {
            SetServerAddress();
            
            //we don't use the base.Reload() since we want to consider the app.config key "Use_Service_Model"
            //in future version where this key will be discarded/obsolete we can call base.Reload() instead
            //of implementing our own ConfigureUnityContainer();
            ConfigureUnityContainer();
        }

        /// <summary>
        /// Sets the server address as stated in app.config ServerConnectionSection.
        /// The address will be used when initializing the various server transparent proxies.
        /// </summary>
        private static void SetServerAddress()
        {

            ServerAddress = string.Format("{0}:{1}", 
                AppConfigHandler.GetValue<string>("ServerName"),
                AppConfigHandler.GetValue<string>("PortNumber"));
        }

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
                RegisterMapping(map);
            }
        }

        private static void RegisterMapping(UnityMappingElement map)
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

                    if (!mockType.IsSubclassOfRawGeneric(typeof(TestTools.MoqObject<>)))
                    {
                        throw new Exception("Your mock object instance '" + map.MockType + "' does not implement WcfAbstraction.TestTools.MoqObject abstract class.");
                    }

                    object mockInstance = Activator.CreateInstance(mockType);

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
                    _container.RegisterInstance(contractType, Instance.CreateDefaultInstance(contractType));
                }
#if DEBUG
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading contract type " + map.ContractType + ": " + ex.ToString());
            }
#endif
        }

        /// <summary>
        /// Gets the transparent proxies config mappings fron configuration file.
        /// </summary>
        /// <returns></returns>
        protected static UnityMappingSection GetConfigMappings()
        {
            return AppConfigHandler.GetSectionByType<UnityMappingSection>();
        }

        #endregion

        #region Event

        /// <summary>
        /// Handles the Faulted event of the Proxy control by re-registering the transparent proxy whit Unity
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private static void Proxy_Faulted(object sender, EventArgs e)
        {
            var contractName = sender.GetType().ToString();

            //UnityContainer - Resolve, ResolveAll, and BuildUp are all thread safe
            //but registering new service is not so we lock the container
            lock (lockObject)
            {
                ((System.ServiceModel.ICommunicationObject)sender).Abort();
                ((System.ServiceModel.ICommunicationObject)sender).Faulted -= Proxy_Faulted;

                var confing = GetConfigMappings();
                foreach (var map in confing.Mappings)
                {
                    //get the contract type to load
                    Type contractType = Type.GetType(map.ContractType);
                    if (contractType != null && contractType == sender.GetType())
                    {
                        //last one you register remains in the container and is returned when you execute the Resolve or ResolveAll method. 
                        UnityRegistry.RegisterMapping(map);

                        break;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine(
                contractName + " was in fault state - therefore it was re-created");
        }

        #endregion
    }
}
