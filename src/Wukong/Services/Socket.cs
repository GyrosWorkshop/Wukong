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
using static System.Threading.Timeout;

namespace Wukong.Services
{
    public delegate void UserIdDelegate(string userId);
    public interface ISocketManager
    {
        event UserIdDelegate ConnectedEvent;
        event UserIdDelegate DisconnectedEvent;
        void SendMessage(IEnumerable<string> userIds, WebSocketEvent obj);
        bool IsConnected(string userId);
        Task AcceptWebsocket(WebSocket webSocket, string userId);
    }

    public class SocketManagerMiddleware
    {
        private readonly RequestDelegate _next;
        public SocketManagerMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task Invoke(HttpContext ctx, ISocketManager socketManager)
        {
            if (ctx.WebSockets.IsWebSocketRequest)
            {
                if (ctx.User?.FindFirst(ClaimTypes.Authentication)?.Value != "true")
                {
                    return;
                }
                var websocket = await ctx.WebSockets.AcceptWebSocketAsync();
                var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                await socketManager.AcceptWebsocket(websocket, userId);
            }
            else
            {
                await _next(ctx);
            }
        }
    }

    public class SocketManager : ISocketManager
    {
        private readonly ILogger Logger;
        public event UserIdDelegate ConnectedEvent;
        public event UserIdDelegate DisconnectedEvent;

        public SocketManager(ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger("SockerManager");
            Logger.LogDebug("SocketManager initialized");
        }

        private readonly Dictionary<string, Timer> disconnectTimer = new Dictionary<string, Timer>();
        private readonly ConcurrentDictionary<string, WebSocket> verifiedSocket = new ConcurrentDictionary<string, WebSocket>();

        public async Task AcceptWebsocket(WebSocket webSocket, string userId)
        {
            ResetTimer(userId);
            verifiedSocket.AddOrUpdate(userId,
                webSocket,
                (key, socket) =>
                {
                    socket.Dispose();
                    return webSocket;
                });
            ConnectedEvent?.Invoke(userId);
            await StartMonitorSocket(userId, webSocket);
        }

        public async void SendMessage(IEnumerable<string> userIds, WebSocketEvent obj)
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
            }
            finally
            {
                Logger.LogDebug("user: " + userId + " socket disposed.");
                WebSocket ws;
                if (verifiedSocket.TryRemove(userId, out ws))
                {
                    socket?.Dispose();
                }
                StartDisconnecTimer(userId);
            }
        }

        private void StartDisconnecTimer(string userId)
        {
            disconnectTimer[userId] = new Timer(Timeout, userId, 15 * 1000, Infinite);
        }

        private void Timeout(object userId)
        {
            var id = (string)userId;
            Logger.LogInformation($"user {id} timeout.");
            ResetTimer(id);
            DisconnectedEvent?.Invoke(id);
        }


        private void ResetTimer(string userId)
        {
            if (userId != null && disconnectTimer.ContainsKey(userId))
            {
                var timer = disconnectTimer[userId];
                disconnectTimer.Remove(userId);
                timer.Change(Infinite, Infinite);
                timer.Dispose();
            }
        }

        public bool IsConnected(string userId)
        {
            return verifiedSocket.Keys.Contains(userId);
        }
    }
}