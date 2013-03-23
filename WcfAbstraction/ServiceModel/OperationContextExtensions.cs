using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace WcfAbstraction.ServiceModel
{
    /// <summary>
    /// Operation context extention methods
    /// </summary>
    public static class OperationContextExtensions
    {
        /// <summary>
        /// Gets the client IP
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string GetClientIP(this OperationContext context)
        {            
            MessageProperties messageProperties = context.IncomingMessageProperties;
            var endpointProperty = messageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;

            return endpointProperty.Address;
        }
    }
}
