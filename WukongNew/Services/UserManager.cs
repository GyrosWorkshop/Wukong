﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Wukong.Models;

namespace Wukong.Services
{
    public delegate void UserStateDelegate(User user);

    public interface IUserManager
    {
        User GetAndUpdateUserWithClaims(ClaimsPrincipal claims);
        User GetUser(string userId);
        event UserStateDelegate UserConnected;
        event UserStateDelegate UserDisconnected;
        event UserStateDelegate UserTimeout;
    }

    public interface IUserService
    {
        User User { get; set; }
    }

    public class UserManagerMiddleware
    {
        private readonly RequestDelegate _next;
        public UserManagerMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task Invoke(HttpContext ctx, IUserManager userManager, IUserService userService)
        {
            var user = userManager.GetAndUpdateUserWithClaims(ctx.User);
            userService.User = user;
            await _next(ctx);
        }
    }


    public class UserManager : IUserManager
    {
        private readonly ConcurrentDictionary<string, User> _userMap = new ConcurrentDictionary<string, User>();

        public event UserStateDelegate UserConnected;
        public event UserStateDelegate UserDisconnected;
        public event UserStateDelegate UserTimeout;

        public User GetAndUpdateUserWithClaims(ClaimsPrincipal claims)
        {
            var user = GetOrCreate(claims.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            user?.UpdateFromClaims(claims);
            if (user != null)
            {
                user.UserConnected -= OnUserConnected;
                user.UserDisconnected -= OnUserDisconnected;
                user.UserTimeout -= OnUserTimeout;
                user.UserConnected += OnUserConnected;
                user.UserDisconnected += OnUserDisconnected;
                user.UserTimeout += OnUserTimeout;
            }
            return user;
        }

        private void OnUserConnected(User user) => UserConnected?.Invoke(user);
        private void OnUserDisconnected(User user) => UserDisconnected?.Invoke(user);
        private void OnUserTimeout(User user) => UserTimeout?.Invoke(user);

        private User GetOrCreate(string userId)
        {
            return userId == null ? null : _userMap.GetOrAdd(userId, id => new User(id));
        }

        public User GetUser(string userId)
        {
            return _userMap[userId];
        }
    }


    public class UserService : IUserService
    {
        public User User { get; set; }
    }
}