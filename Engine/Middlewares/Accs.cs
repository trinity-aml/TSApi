using TSApi.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;

namespace TSApi.Engine.Middlewares
{
    public class Accs
    {
        #region Accs
        private readonly RequestDelegate _next;

        public Accs(RequestDelegate next)
        {
            _next = next;
        }
        #endregion

        public Task Invoke(HttpContext httpContext)
        {
            #region Служебный запрос
            string clientIp = httpContext.Connection.RemoteIpAddress.ToString();

            if (clientIp == "127.0.0.1" || httpContext.Request.Path.Value.StartsWith("/cron") || httpContext.Request.Path.Value.StartsWith("/torinfo") || httpContext.Request.Path.Value.StartsWith("/xrealip") || httpContext.Request.Path.Value.StartsWith("/headers"))
            {
                httpContext.Features.Set(new UserData()
                {
                    login = "service"
                });

                return _next(httpContext);
            }
            #endregion

            if (Startup.settings.AuthorizationRequired)
            {
                #region Авторизация по домену
                string domainid = Regex.Match(httpContext.Request.Host.Value, "^([^\\.]+)\\.").Groups[1].Value;

                UserData _domainUser = Startup.usersDb.FirstOrDefault(i => i.Value.domainid == domainid).Value;
                if (_domainUser != null)
                {
                    httpContext.Features.Set(_domainUser);
                    return _next(httpContext);
                }
                #endregion

                #region Обработка stream потока
                if (httpContext.Request.Method == "GET" && Regex.IsMatch(httpContext.Request.Path.Value, "^/(stream|play)"))
                {
                    if (TorAPI.db.LastOrDefault(i => i.Value.clientIps.Contains(clientIp)).Value is TorInfo info)
                    {
                        httpContext.Features.Set(info.user);
                        return _next(httpContext);
                    }
                    else
                    {
                        httpContext.Response.StatusCode = 404;
                        return Task.CompletedTask;
                    }
                }
                #endregion

                #region Access-Control-Request-Headers
                if (httpContext.Request.Method == "OPTIONS" && httpContext.Request.Headers.TryGetValue("Access-Control-Request-Headers", out var AccessControl) && AccessControl == "authorization")
                {
                    httpContext.Response.StatusCode = 204;
                    return Task.CompletedTask;
                }
                #endregion

                if (httpContext.Request.Headers.TryGetValue("Authorization", out var Authorization))
                {
                    byte[] data = Convert.FromBase64String(Authorization.ToString().Replace("Basic ", ""));
                    string[] decodedString = Encoding.UTF8.GetString(data).Split(":");

                    string login = decodedString[0];
                    string passwd = decodedString[1];

                    if (Startup.usersDb.TryGetValue(login, out UserData _u) && _u.passwd == passwd)
                    {
                        httpContext.Features.Set(_u);
                        return _next(httpContext);
                    }
                }

                if (httpContext.Request.Path.Value.StartsWith("/echo"))
                    return httpContext.Response.WriteAsync("MatriX.API");

                httpContext.Response.StatusCode = 401;
                httpContext.Response.Headers.Add("Www-Authenticate", "Basic realm=Authorization Required");
                return Task.CompletedTask;
            }
            else
            {
                httpContext.Features.Set(new UserData()
                {
                    login = "public",
                    IsShared = true
                });

                return _next(httpContext);
            }
        }
    }
}
