using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

using System.Diagnostics;
using System.Text.Json;

using System.Collections.Generic;
using System.Text;

namespace ContainerizedAppLib.ProxySupport
{
    public class ReverseProxySessionHub : Hub
    {
        /// <summary>
        /// This method is invoked by name when requested by a connected client
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task OpenNewSession(Uri uri, string token)
        {
            OpenSessionRequest argument = new OpenSessionRequest { InternalUri = uri.AbsoluteUri };
            return Clients.All.SendAsync("NewSessionAuthorized", argument);
        }

    }
}
