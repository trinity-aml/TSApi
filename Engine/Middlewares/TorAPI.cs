using TSApi.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;
using System;
using System.Threading;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using System.Text;

namespace TSApi.Engine.Middlewares
{
    public class TorAPI
    {
        #region TorAPI
        private readonly RequestDelegate _next;

        static Random random = new Random();

        public static ConcurrentDictionary<string, TorInfo> db = new ConcurrentDictionary<string, TorInfo>();

        public TorAPI(RequestDelegate next)
        {
            _next = next;
        }
        #endregion

        #region CheckPort
        async public static ValueTask<bool> CheckPort(int port, HttpContext httpContext)
        {
            try
            {
                bool servIsWork = false;
                DateTime endTimeCheckort = DateTime.Now.AddSeconds(8);

                while (true)
                {
                    try
                    {
                        if (DateTime.Now > endTimeCheckort)
                            break;

                        await Task.Delay(200);

                        using (var client = new HttpClient())
                        {
                            client.Timeout = TimeSpan.FromSeconds(2);

                            var response = await client.GetAsync($"http://127.0.0.1:{port}/echo", httpContext.RequestAborted);
                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                string echo = await response.Content.ReadAsStringAsync();
                                if (echo.StartsWith("MatriX."))
                                {
                                    servIsWork = true;
                                    break;
                                }
                            }
                        }
                    }
                    catch { }
                }

                return servIsWork;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        async public Task InvokeAsync(HttpContext httpContext)
        {
            #region Служебный запрос
            string clientIp = httpContext.Connection.RemoteIpAddress.ToString();

            if (clientIp == "127.0.0.1" || httpContext.Request.Path.Value.StartsWith("/cron") || httpContext.Request.Path.Value.StartsWith("/torinfo") || httpContext.Request.Path.Value.StartsWith("/xrealip") || httpContext.Request.Path.Value.StartsWith("/headers"))
            {
                await _next(httpContext);
                return;
            }
            #endregion

            var userData = httpContext.Features.Get<UserData>();

            string dbKeyOrLogin = userData.login;
            if (userData.IsShared)
                dbKeyOrLogin = $"{userData.login}:{httpContext.Connection.RemoteIpAddress}";

            if (!db.TryGetValue(dbKeyOrLogin, out TorInfo info))
            {
                string inDir = Startup.settings.appfolder;

                string domaintoPath = Regex.Match(httpContext.Request.Host.Value, "^[^\\.]+\\.([0-9]+)\\.").Groups[1].Value;
                string torPath = userData.torPath ?? (File.Exists($"{inDir}/dl/{domaintoPath}/TorrServer-linux-amd64") ? domaintoPath : "master");

                #region TorInfo
                info = new TorInfo()
                {
                    user = userData,
                    port = random.Next(40000, 60000),
                    lastActive = DateTime.Now
                };

                if (!db.TryAdd(dbKeyOrLogin, info))
                {
                    await httpContext.Response.WriteAsync("error: db.TryAdd(dbKeyOrLogin, info)");
                    return;
                }
                #endregion

                #region Создаем папку пользователя
                if (userData.IsShared)
                {
                    string path = $"{inDir}/sandbox/{userData.login}/{httpContext.Connection.RemoteIpAddress.ToString().Replace(".", "").Replace(":", "")}";

                    if (File.Exists($"{path}/config.db"))
                        File.Delete($"{path}/config.db");

                    Directory.CreateDirectory(path);
                    File.Copy($"{inDir}/dl/{torPath}/config.db", $"{path}/config.db");
                }
                else
                {
                    if (!File.Exists($"{inDir}/sandbox/{userData.login}/config.db"))
                    {
                        Directory.CreateDirectory($"{inDir}/sandbox/{userData.login}");
                        File.Copy($"{inDir}/dl/{torPath}/config.db", $"{inDir}/sandbox/{userData.login}/config.db");
                    }
                }
                #endregion

                #region Запускаем TorrServer
                info.thread = new Thread(() =>
                {
                    try
                    {
                        // https://github.com/YouROK/TorrServer
                        string comand = $"{inDir}/dl/{torPath}/TorrServer-linux-amd64 -p {info.port} -d {inDir}/sandbox/{info.user.login} >/dev/null 2>&1";

                        if (userData.IsShared)
                        {
                            string path = $"{inDir}/sandbox/{userData.login}/{httpContext.Connection.RemoteIpAddress.ToString().Replace(".", "").Replace(":", "")}";
                            comand = $"{inDir}/dl/{torPath}/TorrServer-linux-amd64 -p {info.port} -d {path} -r >/dev/null 2>&1";
                        }

                        var processInfo = new ProcessStartInfo();
                        processInfo.FileName = "/bin/bash";
                        processInfo.Arguments = $" -c \"{comand}\"";

                        info.process = Process.Start(processInfo);
                        info.process.WaitForExit();
                    }
                    catch { }

                    info.OnProcessForExit();
                });

                info.thread.Start();
                #endregion

                #region Проверяем доступность сервера
                if (await CheckPort(info.port, httpContext) == false)
                {
                    db.TryRemove(dbKeyOrLogin, out _);
                    info.Dispose();
                    return;
                }
                #endregion

                #region Отслеживанием падение процесса
                info.processForExit += (s, e) =>
                {
                    db.TryRemove(dbKeyOrLogin, out _);
                    info.Dispose();
                };
                #endregion
            }

            // Обновляем IP клиента и время последнего запроса
            info.clientIps.Add(httpContext.Connection.RemoteIpAddress.ToString());
            info.lastActive = DateTime.Now;

            #region settings
            if (httpContext.Request.Path.Value.StartsWith("/settings"))
            {
                if (httpContext.Request.Method != "POST")
                {
                    httpContext.Response.StatusCode = 404;
                    await httpContext.Response.WriteAsync("404 page not found");
                    return;
                }

                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(15);

                    #region Данные запроса
                    MemoryStream mem = new MemoryStream();
                    await httpContext.Request.Body.CopyToAsync(mem);
                    string requestJson = Encoding.UTF8.GetString(mem.ToArray());
                    #endregion

                    #region Актуальные настройки
                    var response = await client.PostAsync($"http://127.0.0.1:{info.port}/settings", new StringContent("{\"action\":\"get\"}", Encoding.UTF8, "application/json"));
                    string settingsJson = await response.Content.ReadAsStringAsync();

                    if (requestJson.Trim() == "{\"action\":\"get\"}")
                    {
                        await httpContext.Response.WriteAsync(settingsJson);
                        return;
                    }

                    if (!userData.allowedToChangeSettings || userData.IsShared)
                    {
                        await httpContext.Response.WriteAsync(string.Empty);
                        return;
                    }
                    #endregion

                    #region Обновляем настройки кеша 
                    string ReaderReadAHead = Regex.Match(requestJson, "\"ReaderReadAHead\":([0-9]+)", RegexOptions.IgnoreCase).Groups[1].Value;
                    string PreloadCache = Regex.Match(requestJson, "\"PreloadCache\":([0-9]+)", RegexOptions.IgnoreCase).Groups[1].Value;

                    settingsJson = Regex.Replace(settingsJson, "\"ReaderReadAHead\":([0-9]+)", $"\"ReaderReadAHead\":{ReaderReadAHead}", RegexOptions.IgnoreCase);
                    settingsJson = Regex.Replace(settingsJson, "\"PreloadCache\":([0-9]+)", $"\"PreloadCache\":{PreloadCache}", RegexOptions.IgnoreCase);
                    settingsJson = "{\"action\":\"set\",\"sets\":" + settingsJson + "}";

                    await client.PostAsync($"http://127.0.0.1:{info.port}/settings", new StringContent(settingsJson, Encoding.UTF8, "application/json"));
                    #endregion

                    // Успех
                    await httpContext.Response.WriteAsync(string.Empty);
                    return;
                }
            }
            #endregion

            #region Отправляем запрос в torrserver
            string pathRequest = Uri.EscapeUriString(httpContext.Request.Path.Value);
            string servUri = $"http://127.0.0.1:{info.port}{pathRequest + httpContext.Request.QueryString.Value}";

            using (var client = new HttpClient())
            {
                var request = CreateProxyHttpRequest(httpContext, new Uri(servUri));
                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, httpContext.RequestAborted);

                await CopyProxyHttpResponse(httpContext, response, info);
            }
            #endregion
        }



        #region CreateProxyHttpRequest
        HttpRequestMessage CreateProxyHttpRequest(HttpContext context, Uri uri)
        {
            var request = context.Request;

            var requestMessage = new HttpRequestMessage();
            var requestMethod = request.Method;
            if (HttpMethods.IsPost(requestMethod))
            {
                var streamContent = new StreamContent(request.Body);
                requestMessage.Content = streamContent;
            }

            foreach (var header in request.Headers)
            {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && requestMessage.Content != null)
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            requestMessage.Headers.Host = context.Request.Host.Value;// uri.Authority;
            requestMessage.RequestUri = uri;
            requestMessage.Method = new HttpMethod(request.Method);

            return requestMessage;
        }
        #endregion

        #region CopyProxyHttpResponse
        async Task CopyProxyHttpResponse(HttpContext context, HttpResponseMessage responseMessage, TorInfo info)
        {
            var response = context.Response;
            response.StatusCode = (int)responseMessage.StatusCode;

            #region UpdateHeaders
            void UpdateHeaders(HttpHeaders headers)
            {
                foreach (var header in headers)
                {
                    if (header.Key.ToLower() is "transfer-encoding" or "etag" or "connection")
                        continue;

                    string value = string.Empty;
                    foreach (var val in header.Value)
                        value += $"; {val}";

                    response.Headers[header.Key] = Regex.Replace(value, "^; ", "");
                    //response.Headers[header.Key] = header.Value.ToArray();
                }
            }
            #endregion

            UpdateHeaders(responseMessage.Headers);
            UpdateHeaders(responseMessage.Content.Headers);

            using (var responseStream = await responseMessage.Content.ReadAsStreamAsync())
            {
                await CopyToAsyncInternal(response.Body, responseStream, context.RequestAborted, info);
                //await responseStream.CopyToAsync(response.Body, context.RequestAborted);
            }
        }
        #endregion


        #region CopyToAsyncInternal
        async Task CopyToAsyncInternal(Stream destination, Stream responseStream, CancellationToken cancellationToken, TorInfo info)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");

            if (!responseStream.CanRead && !responseStream.CanWrite)
                throw new ObjectDisposedException("ObjectDisposed_StreamClosed");

            if (!destination.CanRead && !destination.CanWrite)
                throw new ObjectDisposedException("ObjectDisposed_StreamClosed");

            if (!responseStream.CanRead)
                throw new NotSupportedException("NotSupported_UnreadableStream");

            if (!destination.CanWrite)
                throw new NotSupportedException("NotSupported_UnwritableStream");

            byte[] buffer = new byte[81920];
            int bytesRead;
            while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                info.lastActive = DateTime.Now;
            }
        }
        #endregion
    }
}
