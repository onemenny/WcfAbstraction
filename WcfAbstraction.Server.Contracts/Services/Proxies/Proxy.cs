using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using WcfAbstraction.Configuration;
using WcfAbstraction.Validation;

namespace WcfAbstraction.Server.Proxies
{
    /// <summary>
    /// Represents a base class of a proxy for the specified contract, represented by <typeparamref name="TContract"/>.
    /// </summary>
    /// <typeparam name="TContract">The type of the contract.</typeparam>
    public sealed class Proxy<TContract> : IDisposable
        where TContract : class
    {
        #region Fields

        private readonly object callbackInstance;
        private readonly Binding binding;
        private readonly string endpointConfigurationName;
        private readonly EndpointAddress remoteAddress;

        private bool? useServiceModel;
        private IClient client;
        private TContract channel;
        private string _serverAddress;

        #endregion

        #region Client Classes

        private interface IClient : IDisposable
        {
            TContract Channel { get; }

            CommunicationState State { get; }

            ClientCredentials ClientCredentials { get; }

            void Close();

            void Abort();
        }

        private class Client : ClientBase<TContract>, IClient
        {
            private Proxy<TContract> _proxy;

            public Client(Proxy<TContract> proxy)
            {
                Initialize(proxy);
            }

            public Client(Proxy<TContract> proxy, Binding binding, EndpointAddress remoteAddress)
                : base(binding, remoteAddress)
            {
                Initialize(proxy);
            }

            public Client(Proxy<TContract> proxy, string endpointConfigurationName, EndpointAddress remoteAddress)
                : base(endpointConfigurationName, remoteAddress)
            {
                Initialize(proxy);
            }

            private void Initialize(Proxy<TContract> proxy)
            {
                this._proxy = proxy;

                ClientCredentials.Windows.ClientCredential = ServiceProxy.ClientCredential;
            }

            private TContract channel;

            public new TContract Channel
            {
                get { return base.Channel != null ? channel : null; }
            }

            protected override TContract CreateChannel()
            {
                //if (behavior == null)
                //{
                //    this.Endpoint.Behaviors.Add(new MyBehavior());
                //}

                TContract baseChannel = base.CreateChannel();

                ClientProxy proxy = new ClientProxy(this, baseChannel);
                channel = (TContract)proxy.GetTransparentProxy();
                return baseChannel;
            }
        }

        private class DuplexClient : DuplexClientBase<TContract>, IClient
        {
            private Proxy<TContract> proxy;

            public DuplexClient(Proxy<TContract> proxy, object callbackInstance)
                : base(callbackInstance)
            {
                Initialize(proxy);
            }

            public DuplexClient(Proxy<TContract> proxy, object callbackInstance, Binding binding, EndpointAddress remoteAddress)
                : base(callbackInstance, binding, remoteAddress)
            {
                Initialize(proxy);
            }

            public DuplexClient(Proxy<TContract> proxy, object callbackInstance, string endpointConfigurationName, EndpointAddress remoteAddress)
                : base(callbackInstance, endpointConfigurationName, remoteAddress)
            {
                Initialize(proxy);
            }

            private void Initialize(Proxy<TContract> proxy)
            {
                this.proxy = proxy;

                ClientCredentials.Windows.ClientCredential = ServiceProxy.ClientCredential;
            }

            private TContract channel;

            public new TContract Channel
            {
                get { return base.Channel != null ? channel : null; }
            }

            protected override TContract CreateChannel()
            {
                //set the product affinity behavior.
                //ProductAffinityBehavior behavior = this.Endpoint.Behaviors.Find<ProductAffinityBehavior>();
                //if (behavior == null)
                //{
                //    this.Endpoint.Behaviors.Add(new ProductAffinityBehavior());
                //}

                TContract baseChannel = base.CreateChannel();
                
                ClientProxy proxy = new ClientProxy(this, baseChannel);
                channel = (TContract)proxy.GetTransparentProxy();
                return baseChannel;
            }
        }

        private class ClientProxy : RealProxy
        {
            public object Client { get; set; }

            public TContract Channel { get; set; }

            public ClientProxy(object client, TContract channel)
                : base(typeof(TContract))
            {
                Client = client;
                Channel = channel;
            }

            public override IMessage Invoke(IMessage message)
            {
                IMethodCallMessage methodCall = message as IMethodCallMessage;
                if (methodCall == null)
                {
                    throw new ArgumentException("Expected an IMethodCallMessage.", "message");
                }
                else
                {
                    //ServiceProxy.OnCallStarted(Client.proxy);
                    ReturnMessage returnMessage = null;
                    while (returnMessage == null)
                    {
                        try
                        {
                            object[] args = methodCall.Args;

                            object result = methodCall.MethodBase.Invoke(Channel, args);
                            
                            int outArgsCount;
                            object[] outArgs = GetOutArguments(methodCall, args, out outArgsCount);

                            returnMessage = new ReturnMessage(
                                result, 
                                outArgs,
                                outArgsCount, 
                                methodCall.LogicalCallContext, 
                                methodCall);
                        }
                        catch (TargetInvocationException ex)
                        {
                            Exception inner = ex.InnerException;
                            if (inner is SecurityNegotiationException || 
                                inner is FaultException<UnauthorizedAccessException> || 
                                inner is EndpointNotFoundException)
                            {
                                ConnectionErrorEventArgs e = new ConnectionErrorEventArgs(inner);
                                ServiceProxy.OnConnectionError(Client, e);
                                if (!e.Retry)
                                {
                                    returnMessage = new ReturnMessage(inner, methodCall);
                                }
                            }
                            else
                            {
                                returnMessage = new ReturnMessage(inner, methodCall);
                            }
                        }
                        catch (Exception ex)
                        {
                            returnMessage = new ReturnMessage(ex, methodCall);
                        }
                    }

                    //ServiceProxy.OnCallEnded(Client.proxy);
                    return returnMessage;
                }
            }

            private object[] GetOutArguments(IMethodCallMessage methodCall, object[] args, out int count)
            {
                ParameterInfo[] paramInfos = methodCall.MethodBase.GetParameters();
                int countTemp = 0;

                List<object> outParameters = new List<object>();
                for (int i = 0; i < args.Length; ++i)
                {
                    ParameterInfo parameterInfo = paramInfos[i];
                    if (parameterInfo.IsOut || parameterInfo.ParameterType.IsByRef)
                    {
                        ++countTemp;
                        outParameters.Add(args[i]);
                    }
                    else
                    {
                        outParameters.Add(null);
                    }
                }

                count = countTemp;

                return outParameters.ToArray();
            }

            private object[] GetAllArguments(IMethodCallMessage methodCall)
            {
                ParameterInfo[] paramInfos = methodCall.MethodBase.GetParameters();
                return paramInfos.Select((p, i) => new { Param = p, Index = i }).
                                  Select(x => methodCall.Args[x.Index]).
                                  ToArray();
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the channel.
        /// </summary>
        /// <value>The channel.</value>
        public TContract Channel
        {
            get
            {
                if (useServiceModel == true)
                {
                    if (client == null)
                    {
                        //creating the client will set the proper 
                        //binding information as specified in app.config 
                        //foreach end point.
                        //view the binding can be done in the following manner:
                        //((System.ServiceModel.ClientBase<WcfAbstraction.Server.Services.IxxxService>)(client)).Endpoint
                        CreateClient();
                    }
                    else if (client.State == CommunicationState.Faulted ||
                     client.State == CommunicationState.Closing ||
                     client.State == CommunicationState.Closed)
                    {
                        try
                        {
                            client.Dispose();
                        }
                        catch 
                        { 
                        }

                        CreateClient();
                    }

                    return client.Channel;
                }
                else
                {
                    return channel;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is a duplex proxy.
        /// </summary>
        /// <value><c>true</c> if this instance is a duplex proxy; otherwise, <c>false</c>.</value>
        public bool IsDuplex { get; set; }

        /// <summary>
        /// Gets or sets an arbitrary object value that can be used to store custom information about this proxy.
        /// </summary>
        /// <value>The intended value.</value>
        public object Tag { get; set; }

        #endregion

        #region Constructor

        private Proxy(string serverName) : this(null, null, GetDefaultBinding(), CreateEndpointAddress(serverName))
        {
        }

        private Proxy(object callbackInstance, string endpointConfigurationName, Binding binding, EndpointAddress endpointAddress)
        {
            if (endpointAddress != null)
            {
                useServiceModel = true;
            }

            if (endpointAddress == null && string.IsNullOrEmpty(endpointConfigurationName) && !string.IsNullOrEmpty(ServerAddress))
            {
                binding = binding ?? GetDefaultBinding();
                endpointAddress = endpointAddress ?? CreateEndpointAddress(ServerAddress);
            }
            
            this.callbackInstance = callbackInstance;
            this.endpointConfigurationName = endpointConfigurationName;
            this.binding = binding;
            this.remoteAddress = endpointAddress;
                        
            IsDuplex = callbackInstance != null;

            Initialize();
        }

        private Proxy(TContract channel)
        {
            useServiceModel = false;

            this.channel = channel;
        }

        private void Initialize()
        {
            useServiceModel = true;
        }

        private static Binding GetDefaultBinding()
        {
            return new NetTcpBinding("DefaultNetTcpBinding");
        }

        private static EndpointAddress CreateEndpointAddress(string endpointAddress)
        {
            string _typeName = typeof(TContract).Name;
            string _strEndpointAddress = string.Format("net.tcp://{0}/{1}", endpointAddress, _typeName.Substring(1));

            // This strange identity parameter, whose value seems irrelevant somehow helps to 
            // connect to remote computers. Explanation is here:
            // http://whycanfail.wordpress.com/2008/07/17/error-the-target-principal-name-is-incorrect/
            SpnEndpointIdentity identity = new SpnEndpointIdentity("Dummy\\Dummy");
            return new EndpointAddress(new Uri(_strEndpointAddress), identity);
        }

        private string ServerAddress
        {
            get
            {
                if (_serverAddress == null)
                {
                    _serverAddress = string.Format("{0}:{1}", 
                        AppConfigHandler.GetValue<string>("ServerName"),
                        AppConfigHandler.GetValue<string>("PortNumber"));
                }

                return _serverAddress;
            }
        }

        private void CreateClient()
        {
            if (IsDuplex)
            {
                if (endpointConfigurationName != null)
                {
                    client = new DuplexClient(this, callbackInstance, endpointConfigurationName, remoteAddress);
                }
                else if (binding != null)
                {
                    client = new DuplexClient(this, callbackInstance, binding, remoteAddress);
                }
                else
                {
                    client = new DuplexClient(this, callbackInstance);
                }
            }
            else
            {
                if (endpointConfigurationName != null)
                {
                    client = new Client(this, endpointConfigurationName, remoteAddress);
                }
                else if (binding != null)
                {
                    client = new Client(this, binding, remoteAddress);
                }
                else
                {
                    client = new Client(this);
                }
            }

            client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Delegation;
        }

        #endregion

        #region Common Factory Methods

        private static Proxy<TContract> CreateCommon(object callbackInstance, string endpointConfigurationName, Binding binding, EndpointAddress remoteAddress)
        {
            //implement proxy pooling? - no need, unity container used in UI return same proxy every time
            return new Proxy<TContract>(callbackInstance, endpointConfigurationName, binding, remoteAddress);
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a proxy using the provided <paramref name="channel"/>.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns></returns>
        public static Proxy<TContract> Create(TContract channel)
        {
            ArgumentValidator.NotNull(channel, "channel");
            return new Proxy<TContract>(channel);
        }

        /// <summary>
        /// Creates a proxy using an endpoint from configuration.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// The NetTcpBinding (bindingConfiguration specified for the endpoint in app.config)
        /// information will be created when the proxy client (proxy.client) object is created.
        /// This happens in <see cref="ClientProxy"/> CreateClient method.
        /// </remarks>
        public static Proxy<TContract> Create()
        {
            return CreateCommon(null, null, null, null);
        }

        public static Proxy<TContract> Create(string serverName, Binding binding)
        {
            return new Proxy<TContract>(null, null, binding, CreateEndpointAddress(serverName));
        }

        /// <summary>
        /// Creates a proxy using an endpoint from serverName.
        /// </summary>
        /// <returns></returns>
        /// <param name="serverName">server name as string.</param>
        public static Proxy<TContract> Create(string serverName)
        {
            return new Proxy<TContract>(serverName);
        }

        /// <summary>
        /// Creates a proxy using the provided <see cref="Binding"/> and <see cref="EndpointAddress"/>.
        /// </summary>
        /// <param name="binding">The binding.</param>
        /// <param name="remoteAddress">The remote address.</param>
        /// <returns></returns>
        public static Proxy<TContract> Create(Binding binding, EndpointAddress remoteAddress)
        {
            ArgumentValidator.NotNull(binding, "binding");
            ArgumentValidator.NotNull(remoteAddress, "remoteAddress");
            return CreateCommon(null, null, binding, remoteAddress);
        }

        /// <summary>
        /// Creates a proxy using the provided endpoint from configuration and an <see cref="EndpointAddress"/>.
        /// </summary>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        /// <param name="remoteAddress">The remote address.</param>
        /// <returns></returns>
        public static Proxy<TContract> Create(string endpointConfigurationName, EndpointAddress remoteAddress)
        {
            ArgumentValidator.NotNullOrEmptyString(endpointConfigurationName, "endpointConfigurationName");
            ArgumentValidator.NotNull(remoteAddress, "remoteAddress");
            return CreateCommon(null, endpointConfigurationName, null, remoteAddress);
        }

        #endregion

        #region Duplex Factory Methods

        /// <summary>
        /// Creates a duplex proxy using an endpoint from configuration.
        /// </summary>
        /// <param name="callbackInstance">The callback instance.</param>
        /// <returns></returns>
        public static Proxy<TContract> CreateDuplex(object callbackInstance)
        {
            ArgumentValidator.NotNull(callbackInstance, "callbackInstance");
            return CreateCommon(callbackInstance, null, null, null);
        }

        /// <summary>
        /// Creates a duplex proxy using the provided <see cref="Binding"/> and <see cref="EndpointAddress"/>.
        /// </summary>
        /// <param name="callbackInstance">The callback instance.</param>
        /// <param name="binding">The binding.</param>
        /// <param name="remoteAddress">The remote address.</param>
        /// <returns></returns>
        public static Proxy<TContract> CreateDuplex(object callbackInstance, Binding binding, EndpointAddress remoteAddress)
        {
            ArgumentValidator.NotNull(callbackInstance, "callbackInstance");
            ArgumentValidator.NotNull(binding, "binding");
            ArgumentValidator.NotNull(remoteAddress, "remoteAddress");
            return CreateCommon(callbackInstance, null, binding, remoteAddress);
        }

        /// <summary>
        /// Creates a duplex proxy using the provided endpoint from configuration and an <see cref="EndpointAddress"/>.
        /// </summary>
        /// <param name="callbackInstance">The callback instance.</param>
        /// <param name="endpointConfigurationName">Name of the endpoint configuration.</param>
        /// <param name="remoteAddress">The remote address.</param>
        /// <returns></returns>
        public static Proxy<TContract> CreateDuplex(object callbackInstance, string endpointConfigurationName, EndpointAddress remoteAddress)
        {
            ArgumentValidator.NotNull(callbackInstance, "callbackInstance");
            ArgumentValidator.NotNullOrEmptyString(endpointConfigurationName, "endpointConfigurationName");
            ArgumentValidator.NotNull(remoteAddress, "remoteAddress");
            return CreateCommon(callbackInstance, endpointConfigurationName, null, remoteAddress);
        }

        #endregion

        #region IDisposable Members

		/// <summary>
		/// Releases resources used by <see cref="Proxy{T}"/> class instance
		/// </summary>
        public void Dispose()
        {
            try
            {
                if (client != null && client.State != CommunicationState.Faulted)
                {
                    client.Close();
                }
            }
            catch (TimeoutException)
            {
                client.Abort();
            }
        }

        #endregion
    }

    /// <summary>
    /// Used by <see cref="Proxy{T}"/> to store the impersonation credential.
    /// </summary>
    public static class ServiceProxy
    {
        #region Fields

        private const string ImpersonationSectionName = "serviceModelClientImpersonation";
        private static NetworkCredential clientCredential;

        #endregion

        #region Constructor

        static ServiceProxy()
        {
            //TODO menny: delete this
            //ImpersonationSection impersonationSection = ConfigurationManager.GetSection(ImpersonationSectionName) as ImpersonationSection;
            //if (impersonationSection != null)
            //{
            //    clientCredential = new NetworkCredential(impersonationSection.Username, impersonationSection.Password, impersonationSection.Domain);
            //}
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the client credential.
        /// </summary>
        /// <value>The client credential.</value>
        public static NetworkCredential ClientCredential
        {
            get { return clientCredential; }
            set { clientCredential = value; }
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a proxy call has failed.
        /// </summary>
        public static event EventHandler<ConnectionErrorEventArgs> ConnectionError;

        internal static void OnConnectionError(object sender, ConnectionErrorEventArgs e)
        {
            if (ConnectionError != null)
            {
                ConnectionError(sender, e);
            }
        }

        /// <summary>
        /// Occurs when a service proxy call has started.
        /// </summary>
        public static event EventHandler CallStarted;

        internal static void OnCallStarted<TContract>(Proxy<TContract> proxy)
            where TContract : class
        {
            if (CallStarted != null)
            {
                CallStarted(proxy, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Occurs when a service proxy call has ended.
        /// </summary>
        public static event EventHandler CallEnded;

        internal static void OnCallEnded<TContract>(Proxy<TContract> proxy)
            where TContract : class
        {
            if (CallEnded != null)
            {
                CallEnded(proxy, EventArgs.Empty);
            }
        }

        #endregion
    }

    /// <summary>
    /// Provides data for a connection error event.
    /// </summary>
    public class ConnectionErrorEventArgs : EventArgs
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionErrorEventArgs"/> class.
        /// </summary>
        public ConnectionErrorEventArgs()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionErrorEventArgs"/> class.
        /// </summary>
        /// <param name="error">The error.</param>
        public ConnectionErrorEventArgs(Exception error)
        {
            Error = error;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        /// <value>The error.</value>
        public Exception Error { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to retry the operation.
        /// </summary>
        /// <value><c>true</c> if the operation should retry; otherwise, <c>false</c>.</value>
        public bool Retry { get; set; }

        #endregion
    }
}
