using Microsoft.AspNetCore.SignalR;
using Serilog;
using System;
using System.Text.Json;
using System.Threading.Tasks;

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

        public async Task ProxyConfigurationReport(string connectionId, object config)
        {
            await Task.Yield();

            try
            {
                var cfg = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented=true});
                Log.Information($"From proxy client with connection ID {connectionId} proxy configuration is{Environment.NewLine}{cfg}");
            }
            catch (Exception ex) 
            {
                string msg = ex.Message;
            }
        }

        public override async Task OnConnectedAsync()
        {
            await Task.Yield();

            Log.Information($"Client connecting to this hub: connection ID {Context.ConnectionId}");
        }

    }
}
