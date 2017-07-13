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
        void SendMessage(string[] userIds, WebSocketEvent obj);
        bool IsConnected(string userId);
        Task AcceptWebsocket(WebSocket webSocket, string userId);
    }

    public class SocketManagerMiddleware
    {
        private readonly RequestDelegate next;
        public SocketManagerMiddleware(RequestDelegate next)
        {
            this.next = next;
        }
        public async Task Invoke(HttpContext ctx, ISocketManager socketManager, IUserService userService)
        {
            if (ctx.WebSockets.IsWebSocketRequest && ctx.Request.Path.ToString() == "/api/ws")
            {
                if (!ctx.User.Identity.IsAuthenticated)
                {
                    return;
                }
                var websocket = await ctx.WebSockets.AcceptWebSocketAsync();
                await socketManager.AcceptWebsocket(websocket, userService.User.Id);
            }
            else
            {
                await next(ctx);
            }
        }
    }

    public class SocketManager : ISocketManager
    {
        private readonly IUserManager userManager;
        private readonly ILogger logger;

        public SocketManager(ILoggerFactory loggerFactory, IUserManager userManager)
        {
            logger = loggerFactory.CreateLogger("SockerManager");
            logger.LogDebug("SocketManager initialized");
            this.userManager = userManager;
        }

        private readonly ConcurrentDictionary<string, WebSocket> verifiedSocket = new ConcurrentDictionary<string, WebSocket>();

        public async Task AcceptWebsocket(WebSocket webSocket, string userId)
        {
            verifiedSocket.AddOrUpdate(userId,
                webSocket,
                (key, socket) =>
                {
                    var closeAsync = socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None);
                    closeAsync.Wait();
                    return webSocket;
                });
            userManager.GetUser(userId).Connect();
            await StartMonitorSocket(userId, webSocket);
        }

        public async void SendMessage(string[] userIds, WebSocketEvent obj)
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
            foreach (var userId in userIds)
            {
                WebSocket ws;
                if (!verifiedSocket.TryGetValue(userId, out ws)) continue;
                if (ws.State != WebSocketState.Open)
                {
                    await RemoveSocket(userId);
                    continue;
                }
                try
                {
                    await ws.SendAsync(buffer, type, true, token);
                }
                catch (Exception)
                {
                    logger.LogInformation("user: " + userId + " message sent failed.");
                    await RemoveSocket(userId);
                }
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
                    await socket.ReceiveAsync(buffer, token);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
            finally
            {
                logger.LogDebug($"user: {userId} socket disposed.");
                await RemoveSocket(userId);
            }
        }

        private async Task RemoveSocket(string userId)
        {
            WebSocket ws;
            if (verifiedSocket.TryRemove(userId, out ws))
            {
                var closeAsync = ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None);
                if (closeAsync != null)
                    await closeAsync;
            }
            userManager.GetUser(userId)?.Disconnect();
        }

        public bool IsConnected(string userId)
        {
            return verifiedSocket.Keys.Contains(userId);
        }
    }
}