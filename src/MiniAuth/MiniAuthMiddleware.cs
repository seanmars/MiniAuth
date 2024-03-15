﻿using JWT.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniAuth.Configs;
using MiniAuth.Managers;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static MiniAuth.Managers.RoleEndpointManager;

namespace MiniAuth
{
    public partial class MiniAuthMiddleware
    {
        private const string EmbeddedFileNamespace = "MiniAuth.wwwroot";
        private readonly RequestDelegate _next;
        private readonly IEnumerable<EndpointDataSource> _endpointSources;
        private readonly IMiniAuthDB _db;
        private readonly StaticFileMiddleware _staticFileMiddleware;
        private readonly MiniAuthOptions _options;
        private readonly IJWTManager _jwtManager;
        private readonly IUserManager _userManer;
        private readonly IRoleEndpointManager _endpointManager;
        private readonly ILogger<MiniAuthMiddleware> _logger;
        private readonly ConcurrentDictionary<string, RoleEndpointEntity> _endpointCache = new ConcurrentDictionary<string, RoleEndpointEntity>();
        private RoleEndpointEntity _routeEndpoint;
        private bool _isMiniAuthPath;

        public MiniAuthMiddleware(RequestDelegate next,
            ILoggerFactory loggerFactory,
            IWebHostEnvironment hostingEnv,
            ILogger<MiniAuthMiddleware> logger,
            IJWTManager jwtManager = null,
            MiniAuthOptions options = null,
            IRoleEndpointManager endpointManager = null,
            IUserManager userManager = null,
            IEnumerable<EndpointDataSource> endpointSources = null,
            IMiniAuthDB db = null
        )
        {
            this._logger = logger;
            this._next = next;
            if (db == null)
                this._db = new MiniAuthDB<SQLiteConnection>("Data Source=miniauth.db;Version=3;");
            if (jwtManager == null)
                _jwtManager = new JWTManager("miniauth", "miniauth", "miniauth.pfx");
            if (options == null)
                _options = new MiniAuthOptions();
            if (userManager == null)
                _userManer = new UserManager(this._db);
            if (endpointManager == null)
                _endpointManager = new RoleEndpointManager(this._db);
            this._endpointSources = endpointSources;
            this._staticFileMiddleware = CreateStaticFileMiddleware(next, loggerFactory, hostingEnv); ;

            {
                var systemEndpoints = _endpointManager.GetEndpointsAsync(_endpointSources).GetAwaiter().GetResult();
                var cache = systemEndpoints.ToDictionary(p => p.Id);
                var endpointCache = new ConcurrentDictionary<string, RoleEndpointEntity>(cache);
                _endpointCache = endpointCache;
            }
        }

        private StaticFileMiddleware CreateStaticFileMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IWebHostEnvironment hostingEnv)
        {
            var staticFileOptions = new StaticFileOptions
            {
                RequestPath = string.IsNullOrEmpty(_options.RoutePrefix) ? string.Empty : $"/{_options.RoutePrefix}",
                FileProvider = new EmbeddedFileProvider(typeof(MiniAuthMiddleware).GetTypeInfo().Assembly, EmbeddedFileNamespace),
            };

            return new StaticFileMiddleware(next, hostingEnv, Options.Create(staticFileOptions), loggerFactory);
        }
        public async Task Invoke(HttpContext context)
        {
            _ = context ?? throw new ArgumentNullException(nameof(context));
            _isMiniAuthPath = context.Request.Path.StartsWithSegments($"/{_options.RoutePrefix}", out PathString subPath);

            this._routeEndpoint = GetEndpoint(context);
            if (_routeEndpoint == null && !_isMiniAuthPath)
            {
                await _next(context);
                return;
            }

            var isAuth = IsAuth(context);
            if (!isAuth)
                return;

            if (_isMiniAuthPath)
            {
                if (context.Request.Path.Equals($"/{_options.RoutePrefix}/login.html"))
                {
                    await _staticFileMiddleware.Invoke(context);
                    return;
                }
                if (context.Request.Path.Equals($"/{_options.RoutePrefix}/login"))
                {
                    if (context.Request.Method == "POST")
                    {
                        var reader = new StreamReader(context.Request.Body);
                        var body = await reader.ReadToEndAsync();
                        var bodyJson = JsonDocument.Parse(body);
                        var root = bodyJson.RootElement;
                        var userName = root.GetProperty("username").GetString();
                        var password = root.GetProperty("password").GetString();
                        if (_userManer.ValidateUser(userName, password))
                        {
                            var roles = _userManer.GetUserRoleIds(userName);
                            var newToken = _jwtManager.GetToken(userName, userName, _options.ExpirationMinuteTime, roles);
                            context.Response.Headers.Add("X-MiniAuth-Token", newToken);
                            context.Response.Cookies.Append("X-MiniAuth-Token", newToken);

                            await ResponseWriteAsync(context, $"{{\"X-MiniAuth-Token\":\"{newToken}\"}}").ConfigureAwait(false);
                            return;
                        }
                        else
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return;
                        }
                    }
                }
                if (context.Request.Path.Equals($"/{_options.RoutePrefix}/logout", StringComparison.OrdinalIgnoreCase))
                {
                    if (context.Request.Method == "GET")
                    {
                        context.Response.Cookies.Delete("X-MiniAuth-Token");
                        context.Response.Redirect($"/{_options.RoutePrefix}/login.html");
                        return;
                    }
                }
                if (context.Request.Path.Value.EndsWith(".js")
                    || context.Request.Path.Value.EndsWith("css")
                    || context.Request.Path.Value.EndsWith(".ico"))
                {
                    await _staticFileMiddleware.Invoke(context);
                    return;
                }
                if (subPath.StartsWithSegments("/api/getAllEnPoints"))
                {
                    await ResponseWriteAsync(context, _endpointCache.Values.Where(w=>w.Type=="system").OrderBy(_=>_.Route).ToJson());
                    return;
                }
                if (context.Request.Path.Value.EndsWith(".html"))
                {
                    await _staticFileMiddleware.Invoke(context);
                    return;
                }
            }
            await _next(context);
            return;
        }
        private bool IsAuth(HttpContext context)
        {
            var isAuth = true;
            if (this._routeEndpoint == null)
                return isAuth;
            var message = string.Empty;
            var messageCode = default(int);
            var token = context.Request.Headers["X-MiniAuth-Token"].FirstOrDefault() ?? context.Request.Cookies["X-MiniAuth-Token"];
            var needCheckAuth = this._routeEndpoint.Enable;
            if (needCheckAuth)
            {
                if (token == null)
                {
                    isAuth = false;
                    messageCode = 401;
                    message = "Unauthorized";
                    goto End;
                }
                try
                {
                    var json = _jwtManager.DecodeToken(token);
                    var sub = JsonDocument.Parse(json).RootElement.GetProperty("sub").GetString();
                    if (sub == null)
                        throw new Exception("sub can't null");
                    var roles = _userManer.GetUserRoleIds(sub);
                    if (this._routeEndpoint.RoleIds != null && !(this._routeEndpoint.RoleIds.Length == 0))
                    {
                        bool hasRole = roles.Any(value => this._routeEndpoint.RoleIds.Contains(value));
                        if (!hasRole)
                        {
                            isAuth = false;
                            messageCode = 401;
                            message = "Unauthorized";
                        }
                    }
                }
                catch (TokenNotYetValidException)
                {
                    isAuth = false;
                    messageCode = 401;
                    message = "Token is not valid yet";
                    goto End;
                }
                catch (TokenExpiredException)
                {
                    isAuth = false;
                    messageCode = 401;
                    message = "Token is expired";
                    goto End;
                }
                catch (SignatureVerificationException)
                {
                    isAuth = false;
                    messageCode = 401;
                    message = "Token signature is not valid";
                    goto End;
                }
            }
        End:
            if (!isAuth)
                DeniedEndpoint(context, new ResponseVo { code = 401, message = "Token signature is not valid" });
            return isAuth;
        }
        private RoleEndpointEntity GetEndpoint(HttpContext context)
        {

            if (_isMiniAuthPath)
            {
                if (_endpointCache.ContainsKey(context.Request.Path.Value.ToLowerInvariant()))
                    return _endpointCache[context.Request.Path.Value.ToLowerInvariant()];
                else
                    return null;
            }
            var ctxEndpoint = context.GetEndpoint();
            if (ctxEndpoint != null)
            {
                return _endpointCache[ctxEndpoint.DisplayName];
            }
            return null;
        }

        private void DeniedEndpoint(HttpContext context, ResponseVo messageInfo, int status = StatusCodes.Status401Unauthorized)
        {
            if (_routeEndpoint == null)
            {
                JsonResponse(context, messageInfo, status);
                return;
            }
            if (_routeEndpoint.RedirectToLoginPage)
            {
                context.Response.Redirect($"/{_options.RoutePrefix}/login.html?returnUrl=" + context.Request.Path);
                return;
            }
            if (_isMiniAuthPath)
            {
                JsonResponse(context, messageInfo, status);
                return;
            }
            JsonResponse(context, messageInfo, status);
        }

        private static void JsonResponse(HttpContext context, ResponseVo messageInfo, int status)
        {
            var message = messageInfo != null ? JsonConvert.SerializeObject(messageInfo) : "Unauthorized";
            if (status == StatusCodes.Status401Unauthorized)
            {
                context.Response.StatusCode = status;
                context.Response.ContentType = "application/json";
                context.Response.ContentLength = Encoding.UTF8.GetByteCount(message);
                context.Response.WriteAsync(message);
            }
        }

        private static async Task ResponseWriteAsync(HttpContext context, string result, string contentType = "application/json")
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = contentType;
            context.Response.ContentLength = result != null ? Encoding.UTF8.GetByteCount(result) : 0;
            await context.Response.WriteAsync(result).ConfigureAwait(false);
        }
    }
}
