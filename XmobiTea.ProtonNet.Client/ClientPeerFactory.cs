﻿using System.Collections.Generic;
using XmobiTea.Bean;
using XmobiTea.Bean.Attributes;
using XmobiTea.Bean.Support;
using XmobiTea.Data.Converter;
using XmobiTea.Linq;
using XmobiTea.Logging;
using XmobiTea.ProtonNet.Client.Services;
using XmobiTea.ProtonNet.Client.Socket;
using XmobiTea.ProtonNet.Client.Socket.Services;
using XmobiTea.ProtonNet.Client.Socket.Types;
using XmobiTea.ProtonNet.Client.Supports;
using XmobiTea.ProtonNet.Client.WebApi;
using XmobiTea.ProtonNetClient.Options;
using XmobiTea.ProtonNetCommon;

namespace XmobiTea.ProtonNet.Client
{
    /// <summary>
    /// Factory class to create and manage different types 
    /// of client peers, including Web API and Socket client peers.
    /// </summary>
    public partial class ClientPeerFactory : IClientPeerFactory
    {
        /// <summary>
        /// Logger instance for logging activities within the factory.
        /// </summary>
        private ILogger logger { get; }

        /// <summary>
        /// A list containing all the client peers that are managed by the factory.
        /// </summary>
        private IList<IClientPeer> clientPeers { get; }

        /// <summary>
        /// The context for managing beans and their dependencies.
        /// </summary>
        public IBeanContext BeanContext { get; }

        /// <summary>
        /// Service provider for initializing client peer requests.
        /// </summary>
        public IInitRequestProviderService InitRequestProviderService { get; }

        /// <summary>
        /// Converter for data transformations between different formats.
        /// </summary>
        public IDataConverter DataConverter { get; }

        /// <summary>
        /// Service for managing RPC (Remote Procedure Call) protocols.
        /// </summary>
        public IRpcProtocolService RpcProtocolService { get; }

        /// <summary>
        /// Service for handling various events within the client peer.
        /// </summary>
        public IEventService EventService { get; }

        /// <summary>
        /// Service for emitting socket session events.
        /// </summary>
        public ISocketSessionEmitService SocketSessionEmitService { get; }

        /// <summary>
        /// Service for managing socket operation models.
        /// </summary>
        public ISocketOperationModelService SocketOperationModelService { get; }

        /// <summary>
        /// Support for debugging within the client peer.
        /// </summary>
        public IDebugSupport DebugSupport { get; }

        /// <summary>
        /// Options for configuring UDP client connections.
        /// </summary>
        public UdpClientOptions UdpClientOptions { get; }

        /// <summary>
        /// Options for configuring TCP client connections.
        /// </summary>
        public TcpClientOptions TcpClientOptions { get; }

        /// <summary>
        /// Ssl options for secure communication in socket connections.
        /// </summary>
        public SslOptions SslOptions { get; }

        /// <summary>
        /// Ssl options specifically for WebSocket communication.
        /// </summary>
        public SslOptions WsSslOptions { get; }

        /// <summary>
        /// The rate at which data is sent, measured in frames per second.
        /// </summary>
        public int SendRate { get; }

        /// <summary>
        /// Private constructor to initialize ClientPeerFactory
        /// with the provided builder.
        /// </summary>
        /// <param name="builder">Builder instance used to configure the factory.</param>
        private ClientPeerFactory(Builder builder)
        {
            this.logger = LogManager.GetLogger(this);
            this.clientPeers = new List<IClientPeer>();

            this.BeanContext = builder.BeanContext ?? this.CreateBeanContext();
            this.BeanContext.SetSingleton(this.BeanContext);

            this.InitRequestProviderService = builder.InitRequestProviderService ?? this.CreateInitRequestProviderService();
            this.BeanContext.SetSingleton(this.InitRequestProviderService);

            this.RpcProtocolService = builder.RpcProtocolService ?? this.CreateRpcProtocolService();
            this.BeanContext.SetSingleton(this.RpcProtocolService);

            this.EventService = builder.EventService ?? this.CreateEventService();
            this.BeanContext.SetSingleton(this.EventService);

            this.SocketSessionEmitService = builder.SocketSessionEmitService ?? this.CreateSocketSessionEmitService();
            this.BeanContext.SetSingleton(this.SocketSessionEmitService);

            this.SocketOperationModelService = builder.SocketOperationModelService ?? this.CreateSocketOperationModelService();
            this.BeanContext.SetSingleton(this.SocketOperationModelService);

            this.DataConverter = builder.DataConverter ?? this.CreateDataConverter();
            this.BeanContext.SetSingleton(this.DataConverter);

            this.DebugSupport = builder.DebugSupport ?? this.CreateDebugSupport();
            this.BeanContext.SetSingleton(this.DebugSupport);

            this.UdpClientOptions = builder.UdpClientOptions ?? this.CreateUdpClientOptions();
            this.TcpClientOptions = builder.TcpClientOptions ?? this.CreateTcpClientOptions();

            this.SslOptions = builder.SslOptions;
            this.WsSslOptions = builder.SslOptions;

            this.SendRate = builder.SendRate > 0 ? builder.SendRate : 60;

            this.SetupSingleton();
        }

        /// <summary>
        /// Sets up singleton classes and binds them within the BeanContext.
        /// </summary>
        private void SetupSingleton()
        {
            this.CreateSingletonClassesAttribute();
            this.AutoBindSingletonObjects();
        }

        /// <summary>
        /// Scans for classes with SingletonAttribute and creates singletons.
        /// </summary>
        private void CreateSingletonClassesAttribute()
        {
            var singletonTypes = this.BeanContext.ScanClassHasCustomAttribute(typeof(SingletonAttribute), false);

            foreach (var singletonType in singletonTypes)
            {
                if (singletonType.IsAbstract || singletonType.IsInterface) continue;

                this.BeanContext.CreateSingleton(singletonType);

                this.logger.Info("BeanContext - auto create SingletonAttribute: " + singletonType.FullName);
            }
        }

        /// <summary>
        /// Automatically binds all singleton objects within the BeanContext.
        /// </summary>
        private void AutoBindSingletonObjects()
        {
            var singletonObjs = this.BeanContext.GetSingletons();
            foreach (var singletonObj in singletonObjs)
            {
                this.BeanContext.AutoBind(singletonObj);
            }

            foreach (var singletonObj in singletonObjs)
            {
                (singletonObj as IFinalAutoBind)?.OnFinalAutoBind();
            }
        }

        /// <summary>
        /// Creates a new Web API client peer.
        /// </summary>
        /// <param name="serverAddress">The address of the server to connect to.</param>
        /// <returns>A new instance of IWebApiClientPeer.</returns>
        public virtual IWebApiClientPeer NewWebApiClientPeer(string serverAddress)
        {
            var answer = new HttpClientClientPeer(serverAddress, this.InitRequestProviderService.NewClientPeerInitRequest(), this.TcpClientOptions);

            this.BeanContext.AutoBind(answer);

            (answer as IFinalAutoBind)?.OnFinalAutoBind();

            answer.SetDebugSupport(this.DebugSupport);

            answer.SetSendRate(this.SendRate);

            this.clientPeers.Add(answer);

            return answer;
        }

        /// <summary>
        /// Creates a new Socket client peer.
        /// </summary>
        /// <param name="serverAddress">The address of the server to connect to.</param>
        /// <param name="protocol">The transport protocol to use (e.g., TCP, UDP).</param>
        /// <returns>A new instance of ISocketClientPeer.</returns>
        public virtual ISocketClientPeer NewSocketClientPeer(string serverAddress, TransportProtocol protocol)
        {
            var answer = new SocketClientPeer(serverAddress, this.InitRequestProviderService.NewClientPeerInitRequest(), this.TcpClientOptions, this.UdpClientOptions, protocol);

            this.BeanContext.AutoBind(answer);

            (answer as IFinalAutoBind)?.OnFinalAutoBind();

            answer.SetDebugSupport(this.DebugSupport);

            answer.SetSendRate(this.SendRate);

            this.clientPeers.Add(answer);

            return answer;
        }

        /// <summary>
        /// Creates a new SSL Socket client peer.
        /// </summary>
        /// <param name="serverAddress">The address of the server to connect to.</param>
        /// <param name="sslProtocol">The SSL transport protocol to use.</param>
        /// <returns>A new instance of ISocketClientPeer with SSL enabled.</returns>
        public virtual ISocketClientPeer NewSslSocketClientPeer(string serverAddress, SslTransportProtocol sslProtocol)
        {
            var answer = new SocketClientPeer(serverAddress, this.InitRequestProviderService.NewClientPeerInitRequest(), this.TcpClientOptions, this.UdpClientOptions, sslProtocol, sslProtocol == SslTransportProtocol.Ssl ? this.SslOptions : this.WsSslOptions);

            this.BeanContext.AutoBind(answer);

            (answer as IFinalAutoBind)?.OnFinalAutoBind();

            answer.SetDebugSupport(this.DebugSupport);

            answer.SetSendRate(this.SendRate);

            this.clientPeers.Add(answer);

            return answer;
        }

        /// <summary>
        /// Retrieves a client peer by its client ID.
        /// </summary>
        /// <param name="clientId">The ID of the client peer to retrieve.</param>
        /// <returns>The client peer with the specified ID.</returns>
        public IClientPeer GetClientPeer(int clientId) => this.clientPeers.Find(x => x.GetClientId() == clientId);

        /// <summary>
        /// Destroys a client peer by its client ID.
        /// </summary>
        /// <param name="clientId">The ID of the client peer to destroy.</param>
        /// <returns>True if the client peer was successfully removed, otherwise false.</returns>
        public bool DestroyClientPeer(int clientId)
        {
            var clientPeer = this.GetClientPeer(clientId);

            if (clientPeer == null) return false;

            return this.clientPeers.Remove(clientPeer);
        }

        /// <summary>
        /// Services all client peers to ensure proper functioning.
        /// </summary>
        public void Service()
        {
            foreach (var clientPeer in this.clientPeers)
                clientPeer.Service();
        }

        /// <summary>
        /// Creates a new builder for constructing ClientPeerFactory instances.
        /// </summary>
        /// <returns>A new instance of the Builder class.</returns>
        public static Builder NewBuilder() => new Builder();

        /// <summary>
        /// Builder class for configuring and constructing instances 
        /// of the ClientPeerFactory.
        /// </summary>
        public class Builder
        {
            /// <summary>
            /// Bean context for managing dependencies and singleton instances.
            /// </summary>
            public IBeanContext BeanContext { get; set; }

            /// <summary>
            /// Service provider for initializing client peer requests.
            /// </summary>
            public IInitRequestProviderService InitRequestProviderService { get; set; }

            /// <summary>
            /// Converter for transforming data between different formats.
            /// </summary>
            public IDataConverter DataConverter { get; set; }

            /// <summary>
            /// Service for managing RPC (Remote Procedure Call) protocols.
            /// </summary>
            public IRpcProtocolService RpcProtocolService { get; set; }

            /// <summary>
            /// Service for handling various events within the client peer.
            /// </summary>
            public IEventService EventService { get; set; }

            /// <summary>
            /// Service for emitting socket session events.
            /// </summary>
            public ISocketSessionEmitService SocketSessionEmitService { get; set; }

            /// <summary>
            /// Service for managing socket operation models.
            /// </summary>
            public ISocketOperationModelService SocketOperationModelService { get; set; }

            /// <summary>
            /// Support for debugging within the client peer.
            /// </summary>
            public IDebugSupport DebugSupport { get; set; }

            /// <summary>
            /// Options for configuring UDP client connections.
            /// </summary>
            public UdpClientOptions UdpClientOptions { get; set; }

            /// <summary>
            /// Options for configuring TCP client connections.
            /// </summary>
            public TcpClientOptions TcpClientOptions { get; set; }

            /// <summary>
            /// Ssl options for secure communication in socket connections.
            /// </summary>
            public SslOptions SslOptions { get; set; }

            /// <summary>
            /// Ssl options specifically for WebSocket communication.
            /// </summary>
            public SslOptions WsSslOptions { get; set; }

            /// <summary>
            /// The rate at which data is sent, measured in frames per second.
            /// </summary>
            public int SendRate { get; set; }

            /// <summary>
            /// Internal constructor to restrict direct instantiation.
            /// </summary>
            internal Builder() { }

            /// <summary>
            /// Sets the bean context for the builder.
            /// </summary>
            /// <param name="beanContext">The IBeanContext to set.</param>
            /// <returns>The current Builder instance.</returns>
            public Builder SetBeanContext(IBeanContext beanContext)
            {
                this.BeanContext = beanContext;
                return this;
            }

            /// <summary>
            /// Sets the init request provider service for the builder.
            /// </summary>
            /// <param name="initRequestProviderService">The IInitRequestProviderService to set.</param>
            /// <returns>The current Builder instance.</returns>
            public Builder SetInitRequestProviderService(IInitRequestProviderService initRequestProviderService)
            {
                this.InitRequestProviderService = initRequestProviderService;
                return this;
            }

            /// <summary>
            /// Sets the data converter for the builder.
            /// </summary>
            /// <param name="dataConverter">The IDataConverter to set.</param>
            /// <returns>The current Builder instance.</returns>
            public Builder SetDataConverter(IDataConverter dataConverter)
            {
                this.DataConverter = dataConverter;
                return this;
            }

            /// <summary>
            /// Sets the RPC protocol service for the builder.
            /// </summary>
            /// <param name="rpcProtocolService">The IRpcProtocolService to set.</param>
            /// <returns>The current Builder instance.</returns>
            public Builder SetRpcProtocolService(IRpcProtocolService rpcProtocolService)
            {
                this.RpcProtocolService = rpcProtocolService;
                return this;
            }

            /// <summary>
            /// Sets the event service for the builder.
            /// </summary>
            /// <param name="eventService">The IEventService to set.</param>
            /// <returns>The current Builder instance.</returns>
            public Builder SetEventService(IEventService eventService)
            {
                this.EventService = eventService;
                return this;
            }

            /// <summary>
            /// Sets the socket session emit service for the builder.
            /// </summary>
            /// <param name="socketSessionEmitService">The ISocketSessionEmitService to set.</param>
            /// <returns>The current Builder instance.</returns>
            public Builder SetSocketSessionEmitService(ISocketSessionEmitService socketSessionEmitService)
            {
                this.SocketSessionEmitService = socketSessionEmitService;
                return this;
            }

            /// <summary>
            /// Sets the socket operation model service for the builder.
            /// </summary>
            /// <param name="socketOperationModelService">The ISocketOperationModelService to set.</param>
            /// <returns>The current Builder instance.</returns>
            public Builder SetSocketOperationModelService(ISocketOperationModelService socketOperationModelService)
            {
                this.SocketOperationModelService = socketOperationModelService;
                return this;
            }

            /// <summary>
            /// Sets the debug support for the builder.
            /// </summary>
            /// <param name="debugSupport">The IDebugSupport to set.</param>
            /// <returns>The current Builder instance.</returns>
            public Builder SetDebugSupport(IDebugSupport debugSupport)
            {
                this.DebugSupport = debugSupport;
                return this;
            }

            /// <summary>
            /// Sets the UDP client options for the builder.
            /// </summary>
            /// <param name="udpClientOptions">The UdpClientOptions to set.</param>
            /// <returns>The current Builder instance.</returns>
            public Builder SetUdpClientOptions(UdpClientOptions udpClientOptions)
            {
                this.UdpClientOptions = udpClientOptions;
                return this;
            }

            /// <summary>
            /// Sets the TCP client options for the builder.
            /// </summary>
            /// <param name="tcpClientOptions">The TcpClientOptions to set.</param>
            /// <returns>The current Builder instance.</returns>
            public Builder SetTcpClientOptions(TcpClientOptions tcpClientOptions)
            {
                this.TcpClientOptions = tcpClientOptions;
                return this;
            }

            /// <summary>
            /// Sets the Ssl options for the builder.
            /// </summary>
            /// <param name="sslOptions">The sslOptions to set.</param>
            /// <returns>The current Builder instance.</returns>
            public Builder SetSslOptions(SslOptions sslOptions)
            {
                this.SslOptions = sslOptions;
                return this;
            }

            /// <summary>
            /// Sets the WebSocket Ssl options for the builder.
            /// </summary>
            /// <param name="sslOptions">The sslOptions to set.</param>
            /// <returns>The current Builder instance.</returns>
            public Builder SetWsSslContext(SslOptions sslOptions)
            {
                this.WsSslOptions = sslOptions;
                return this;
            }

            /// <summary>
            /// Sets the send rate for the builder.
            /// </summary>
            /// <param name="sendRate">The send rate to set.</param>
            /// <returns>The current Builder instance.</returns>
            public Builder SetSendRate(int sendRate)
            {
                this.SendRate = sendRate;
                return this;
            }

            /// <summary>
            /// Builds and returns a new instance of ClientPeerFactory.
            /// </summary>
            /// <returns>A new ClientPeerFactory instance.</returns>
            public ClientPeerFactory Build() => new ClientPeerFactory(this);

        }

    }

}
