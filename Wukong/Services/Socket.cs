using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Concurrent;
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
        Task AcceptWebsocket(WebSocket webSocket, string userId, string deviceId);
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
                ctx.Request.Query.TryGetValue("deviceId", out var deviceId);
                if (String.IsNullOrEmpty(deviceId))
                {
                    ctx.Request.Headers.TryGetValue("User-Agent", out deviceId);
                }
                await socketManager.AcceptWebsocket(websocket, userService.User.Id, deviceId);
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

        private readonly ConcurrentDictionary<string, Tuple<WebSocket, string>> verifiedSocket =
            new ConcurrentDictionary<string, Tuple<WebSocket, string>>();

        public async Task AcceptWebsocket(WebSocket webSocket, string userId, string deviceId)
        {
            var newTuple = new Tuple<WebSocket, string>(webSocket, deviceId);
            verifiedSocket.AddOrUpdate(userId,
                newTuple,
                (key, tuple) =>
                {
                    var socket = tuple.Item1;
                    SendMessage(socket, new DisconnectEvent {Cause = deviceId}).Wait();
                    socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None).Wait();
                    return newTuple;
                });
            userManager.GetUser(userId).Connect();
            await StartMonitorSocket(userId, webSocket);
        }

        private async Task SendMessage(WebSocket websocket, WebSocketEvent obj)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
            };
            var message = JsonConvert.SerializeObject(obj, settings);
            const WebSocketMessageType type = WebSocketMessageType.Text;
            var token = CancellationToken.None;
            var data = Encoding.UTF8.GetBytes(message);
            var buffer = new ArraySegment<byte>(data);
            await websocket.SendAsync(buffer, type, true, token);
            logger.LogDebug(message);
        }

        public void SendMessage(string[] userIds, WebSocketEvent obj)
        {
            logger.LogDebug("Sending message to " + string.Join(", ", userIds) + " " + obj + " " + obj.EventName);
            var token = CancellationToken.None;
            foreach (var userId in userIds)
            {
                Task.Factory.StartNew(async () =>
                {
                    if (!verifiedSocket.TryGetValue(userId, out var tuple)) return;
                    var ws = tuple.Item1;
                    if (ws.State != WebSocketState.Open)
                    {
                        await RemoveSocket(userId);
                        return;
                    }
                    try
                    {
                        await SendMessage(ws, obj);
                    }
                    catch (Exception)
                    {
                        logger.LogInformation("user: " + userId + " message sent failed.");
                        await RemoveSocket(userId);
                    }
                }, token);
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
                    var result = await socket.ReceiveAsync(buffer, token);
                    if (result.MessageType == WebSocketMessageType.Close) {
                        break;
                    }
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
            if (verifiedSocket.TryRemove(userId, out var tuple))
            {
                var ws = tuple.Item1;
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