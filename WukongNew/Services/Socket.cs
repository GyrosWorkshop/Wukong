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
    public interface ISocketManager
    {
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
            if (ctx.WebSockets.IsWebSocketRequest && ctx.Request.Path.ToString() == "/api/ws")
            {
                if (ctx.User?.FindFirst(ClaimTypes.Authentication)?.Value != "true")
                {
                    return;
                }
                var websocket = await ctx.WebSockets.AcceptWebSocketAsync();
                var userId = Models.User.GetUserIdentifier(ctx.User.FindFirst(ClaimTypes.AuthenticationMethod).Value,
                    ctx.User.FindFirst(ClaimTypes.NameIdentifier).Value);
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
        private IUserManager _userManager;
        private readonly ILogger Logger;

        public SocketManager(ILoggerFactory loggerFactory, IUserManager userManager)
        {
            Logger = loggerFactory.CreateLogger("SockerManager");
            Logger.LogDebug("SocketManager initialized");
            _userManager = userManager;
        }

        private readonly Dictionary<string, Timer> disconnectTimer = new Dictionary<string, Timer>();
        private readonly ConcurrentDictionary<string, WebSocket> verifiedSocket = new ConcurrentDictionary<string, WebSocket>();

        public async Task AcceptWebsocket(WebSocket webSocket, string userId)
        {
            verifiedSocket.AddOrUpdate(userId,
                webSocket,
                (key, socket) =>
                {
                    socket.Dispose();
                    return webSocket;
                });
            _userManager.GetUser(userId).Connect();
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
                            if (Encoding.UTF8.GetString(buffer.ToArray(), 0, received.Count).StartsWith("ping "))
                            {
                                await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("pong " + DateTime.Now.ToString())), WebSocketMessageType.Text, true, CancellationToken.None);
                            } else
                            {
                                Logger.LogError($"user: {userId} send message, dropped.");
                            }
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
                Logger.LogDebug($"user: {userId} socket disposed.");
                WebSocket ws;
                if (verifiedSocket.TryRemove(userId, out ws))
                {
                    socket?.Dispose();
                }
                _userManager.GetUser(userId)?.Disconnect();
            }
        }

        public bool IsConnected(string userId)
        {
            return verifiedSocket.Keys.Contains(userId);
        }
    }
}