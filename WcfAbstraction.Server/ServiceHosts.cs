using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Collections.Specialized;
using System.ServiceModel.Description;
using System.ServiceModel.Extensions;
using WcfAbstraction.Server.Services.Proxies;

namespace WcfAbstraction.Server
{
    /// <summary>
    /// Providers a container for <see cref="ServiceHost"/>s.
    /// </summary>
    public class ServiceHosts 
    {
        private List<ServiceHost> serviceHosts = new List<ServiceHost>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceHosts"/> class.
        /// </summary>
        public ServiceHosts()
        {
        }

        /// <summary>
        /// Gets the service names.
        /// </summary>
        /// <value>The service names.</value>
        public string[] ServiceNames
        {
            get
            {
                return serviceHosts.ConvertAll(serviceHost => serviceHost.Description.Name).ToArray();
            }
        }

        /// <summary>
        /// Starts all <see cref="ServiceHost"/>s and initiate the cache
        /// </param>
        /// </summary>
        public void Start()
        {
            //initializing the unity static ctor
            var unityRegistry = UnityRegistry.Instance;

            //load cache is needed
            LoadCache();

            // add the services
            InitServices();

            ////TODO more initialization work here if needed
            ////...
        }

        /// <summary>
        /// Stops all <see cref="ServiceHost"/>s.
        /// </summary>
        public void Stop()
        {
            foreach (ServiceHost serviceHost in serviceHosts)
            {
                serviceHost.Abort();
            }

            UnloadCache();
        }

        /// <summary>
        /// Initiate all WCF services according to app configuration.
        /// </summary>
        private void InitServices()
        {
            var res = (NameValueCollection)System.Configuration.ConfigurationManager.GetSection(@"servicesToLoad");
            foreach (string service in res.AllKeys)
            {
                try
                {
                    Type type = Type.GetType(res[service], true);
                    AddService(type);
                }
                catch (Exception ex)
                {
                    //Logger.WriteError(EventCategory.info, "Error loading service {0}: {1}", service, ex);
                    throw; //we don't want to start the server if we have initialization exceptions
                }
            }

            // start the services
            foreach (ServiceHost serviceHost in serviceHosts)
            {
                serviceHost.Open();
            }
        }

        /// <summary>
        /// Adds a service to be started when <see cref="ServiceHosts"/> starts
        /// </summary>
        /// <param name="service">The service type to start</param>
        private void AddService(Type service)
        {
            //validate
            ValidateServiceDecleration(service);

            //service host initialization
            Type serviceType = typeof(ServiceHost<>);
            Type listType = serviceType.MakeGenericType(service);
            ServiceHost serviceHost = Activator.CreateInstance(listType) as ServiceHost;

            //add behavior (we do it in code since otherwise will have to supply the fully qualified asm name in config)
            //AddBehavior<ProductAffinityBehavior>(serviceHost);

            //add service
            if (!serviceHosts.Contains(serviceHost))
            {
                serviceHosts.Add(serviceHost);
            }
        }

        private void AddBehavior<T>(ServiceHost serviceHost) where T : class, new()
        {
            if (typeof(T).GetInterfaces().Contains(typeof(IEndpointBehavior)))
            {
                foreach (var endPoint in serviceHost.Description.Endpoints)
                {
                    T behavior = endPoint.Behaviors.Find<T>();
                    if (behavior == null)
                    {
                        endPoint.Behaviors.Add((IEndpointBehavior)Activator.CreateInstance<T>());
                    }
                }

                return;
            }

            if (typeof(T).GetInterfaces().Contains(typeof(IServiceBehavior)))
            {
                serviceHost.Description.Behaviors.Add((IServiceBehavior)Activator.CreateInstance<T>());
                return;
            }
        }

        /// <summary>
        /// Validates the service declaration to ensure that all service
        /// have proper attributes declaration and fall within 
        /// methodology of service implementation.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        private void ValidateServiceDecleration(Type serviceType)
        {
            //null validation
            if (serviceType == null)
            {
                throw new ArgumentNullException("Type of service to load cannot be null");
            }

            ValidateContractAttributesDecleration(serviceType);
            ValidateServiceAttributesDecleration(serviceType);

            //attributes validation
            string privateConstructorErrorMsg = "The service [{0}] is missing mandatory private empty constructor.";
            string multipleConstructorErrorMsg = "The service [{0}] has multiple constructor. Use only one default private constructor.";
            string serviceTypeStr = serviceType.ToString();

            //private constructor validation
            if (serviceType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { }, null) == null)
            {
                throw new TypeInitializationException(
                        serviceTypeStr,
                        new Exception(string.Format(privateConstructorErrorMsg, serviceTypeStr)));
            }

            //ensure ONLY private constructor
            if (serviceType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Count() != 1)
            {
                throw new TypeInitializationException(
                        serviceTypeStr,
                        new Exception(string.Format(multipleConstructorErrorMsg, serviceTypeStr)));
            }
        }

        private static void ValidateServiceAttributesDecleration(Type serviceType)
        {
            string attributeErrorMsg = "The service [{0}] is missing mandatory [{1}] attribute.";
            string serviceTypeStr = serviceType.ToString();

            //mandatory attributes list
            List<Type> mandatoryAttributes = new List<Type>();
            mandatoryAttributes.Add(typeof(ServiceBehaviorAttribute));

            //ensure mandatory attributes at service level
            foreach (Type attType in mandatoryAttributes)
            {
                if (!serviceType.IsDefined(attType, false))
                {
                    throw new TypeInitializationException(
                        serviceTypeStr,
                        new NotImplementedException(string.Format(attributeErrorMsg, serviceTypeStr, attType.ToString())));
                }
            }
        }

        private void ValidateContractAttributesDecleration(Type serviceType)
        {
            string serviceTypeStr = serviceType.ToString();
            var serviceInterfaces = serviceType.GetInterfaces();
            
            //ensure the service implements a contract
            if (serviceInterfaces.Length == 0)
            {
                string missingContractMessage = "The service [{0}] does not implement any contract interface";
                throw new TypeInitializationException(
                        serviceTypeStr,
                        new NotImplementedException(string.Format(missingContractMessage, serviceTypeStr)));
            }

            //ensure mandatory attributes at service contract (the interface) level
            foreach (var srvInterface in serviceInterfaces)
            {
                //get the contract interface
                if (srvInterface.GetCustomAttributes(typeof(ServiceContractAttribute), false).Length > 0)
                {
                    //mandatory attributes list
                    List<Type> mandatoryAttributes = new List<Type>();
                    mandatoryAttributes.Add(typeof(ServiceContractAttribute));

                    //ensure attribute according to mandatory list
                    foreach (Type attType in mandatoryAttributes)
                    {
                        if (!srvInterface.IsDefined(attType, false))
                        {
                            string attributeErrorMsg = "The service contract [{0}] is missing mandatory [{1}] attribute.";
                            var interfaceStr = srvInterface.ToString();
                            throw new TypeInitializationException(
                                interfaceStr,
                                new NotImplementedException(string.Format(attributeErrorMsg, interfaceStr, attType.ToString())));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Unloads all the cache objects
        /// </summary>
        private void UnloadCache()
        {
            
        }

        /// <summary>
        /// Initiates all cache objects
        /// <para>
        /// MUST be called before the hosts (proxies) are opened
        /// </para>
        /// </summary>
        private void LoadCache()
        {
            
        }
    }
}
