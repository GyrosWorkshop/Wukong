using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Net.WebSockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Wukong.Models;
using Microsoft.Extensions.Logging;

namespace Wukong.Services
{
    public delegate void UserDisconnect(string userId);
    public delegate void UserConnect(string userId);
    public class SocketManager
    {
        static readonly Lazy<SocketManager> manage =
            new Lazy<SocketManager>(() => new SocketManager());
        public string PublicKey { set; get; }
        public ILoggerFactory LoggerFactory
        {
            set
            {
                Logger = value.CreateLogger("SocketManager");
            }
        }
        private Dictionary<string, Timer> disconnectTimer = new Dictionary<string, Timer>();
        public event UserDisconnect UserDisconnect;
        public event UserConnect UserConnect;
        private ConcurrentDictionary<string, WebSocket> verifiedSocket = new ConcurrentDictionary<string, WebSocket>();
        private ILogger Logger;

        static public SocketManager Manager
        {
            get
            {
                return manage.Value;
            }
        }

        public async Task WebsocketHandler(HttpContext context, Func<Task> next)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                if (context.User?.FindFirst(ClaimTypes.Authentication)?.Value != "true")
                {
                    return;
                }
                var websocket = await context.WebSockets.AcceptWebSocketAsync();
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                ResetTimer(userId);
                verifiedSocket.AddOrUpdate(userId,
                    websocket,
                    (key, socket) =>
                    {
                        socket.Dispose();
                        return websocket;
                    });
                UserConnect(userId);
                await StartMonitorSocket(userId, websocket);
            }
            else
            {
                await next();
            }
        }

        public async void SendMessage(List<string> userIds, WebSocketEvent obj)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
            };
            var message = JsonConvert.SerializeObject(obj, settings);
            var token = CancellationToken.None;
            var type = WebSocketMessageType.Text;
            var data = Encoding.UTF8.GetBytes(message);
            var buffer = new ArraySegment<Byte>(data);
            try 
            {
                await Task.WhenAll(verifiedSocket.Where(i => (i.Value.State == WebSocketState.Open) && userIds.Contains(i.Key))
                                                .Select(i => i.Value.SendAsync(buffer, type, true, token)));
            }
            catch (Exception)
            {
                Logger.LogInformation("user: " + userIds + " message sent failed.");
            }
        }

        private async Task StartMonitorSocket(string userId, WebSocket socket)
        {
            try
            {
                while (socket != null && socket.State == WebSocketState.Open)
                {
                    var token = CancellationToken.None;
                    var buffer = new ArraySegment<Byte>(new Byte[4096]);
                    var received = await socket.ReceiveAsync(buffer, token);
                    switch (received.MessageType)
                    {
                        case WebSocketMessageType.Close:
                            throw new Exception("socket closed");
                        case WebSocketMessageType.Text:
                        case WebSocketMessageType.Binary:
                            Logger.LogError("user: " + userId + " send message, dropped.");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
                StartDisconnecTimer(userId);
            }
            finally
            {
                Logger.LogDebug("user: " + userId + " socket disposed.");
                WebSocket ws;
                if (verifiedSocket.TryRemove(userId, out ws)) 
                {
                    socket?.Dispose();
                }
            }
        }

        private void StartDisconnecTimer(string userId)
        {
            disconnectTimer[userId] = new Timer(Disconnect, userId, 60 * 1000, 0);
        }

        private void Disconnect(object userId)
        {
            var id = (string)userId;
            UserDisconnect(id);
        }

        private void ResetTimer(string userId)
        {
            if (userId != null && disconnectTimer.ContainsKey(userId))
            {
                var timer = disconnectTimer[userId];
                disconnectTimer.Remove(userId);
                timer.Dispose();
            }
        }
    }
}