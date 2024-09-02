﻿using XmobiTea.ProtonNet.Server.Context;
using XmobiTea.ProtonNet.Server.Services;
using XmobiTea.ProtonNet.Server.WebApi.Services;

namespace XmobiTea.ProtonNet.Server.WebApi.Context
{
    /// <summary>
    /// Represents the context for a Web API server.
    /// </summary>
    public interface IWebApiServerContext : IServerContext
    {
        /// <summary>
        /// Gets the controller service for the Web API.
        /// </summary>
        /// <returns>The controller service.</returns>
        IWebApiControllerService GetControllerService();

    }

    /// <summary>
    /// Provides an implementation of the WebApiServerContext.
    /// </summary>
    sealed class WebApiServerContext : ServerContext, IWebApiServerContext
    {
        private IWebApiControllerService controllerService { get; set; }

        /// <summary>
        /// Gets the controller service for the Web API.
        /// </summary>
        /// <returns>The controller service.</returns>
        public IWebApiControllerService GetControllerService() => this.controllerService;

        /// <summary>
        /// Sets the controller service for the Web API.
        /// </summary>
        /// <param name="controllerService">The controller service to set.</param>
        public void SetControllerService(IWebApiControllerService controllerService) => this.controllerService = controllerService;

        private WebApiServerContext() { }

        /// <summary>
        /// Creates a new instance of the Builder for WebApiServerContext.
        /// </summary>
        /// <returns>The builder instance.</returns>
        public static Builder NewBuilder() => new Builder();

        /// <summary>
        /// Builder class for constructing instances of WebApiServerContext.
        /// </summary>
        public class Builder
        {
            internal Builder() { }

            public IUserPeerSessionService UserPeerSessionService { get; set; }
            public ISessionService SessionService { get; set; }
            public IInitRequestProviderService InitRequestProviderService { get; set; }

            /// <summary>
            /// Sets the user peer session service.
            /// </summary>
            /// <param name="sessionService">The user peer session service to set.</param>
            /// <returns>The builder instance.</returns>
            public Builder SetUserPeerSessionService(IUserPeerSessionService sessionService)
            {
                this.UserPeerSessionService = sessionService;
                return this;
            }

            /// <summary>
            /// Sets the session service.
            /// </summary>
            /// <param name="sessionService">The session service to set.</param>
            /// <returns>The builder instance.</returns>
            public Builder SetSessionService(ISessionService sessionService)
            {
                this.SessionService = sessionService;
                return this;
            }

            /// <summary>
            /// Sets the initialization request provider service.
            /// </summary>
            /// <param name="initRequestProviderService">The initialization request provider service to set.</param>
            /// <returns>The builder instance.</returns>
            public Builder SetInitRequestProviderService(IInitRequestProviderService initRequestProviderService)
            {
                this.InitRequestProviderService = initRequestProviderService;
                return this;
            }

            /// <summary>
            /// Builds a new instance of WebApiServerContext with the specified services.
            /// </summary>
            /// <returns>The constructed WebApiServerContext instance.</returns>
            public WebApiServerContext Build()
            {
                var answer = new WebApiServerContext();

                answer.userPeerSessionService = this.UserPeerSessionService;
                answer.sessionService = this.SessionService;
                answer.initRequestProviderService = this.InitRequestProviderService;

                return answer;
            }

        }

    }

}
