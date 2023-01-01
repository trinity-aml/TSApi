using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;
using TSApi.Engine.Middlewares;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using TSApi.Models;
using System.Net;

namespace TSApi
{
    public class Startup
    {
        public static ConcurrentBag<IPNetwork> whiteip = new ConcurrentBag<IPNetwork>();

        public static ConcurrentDictionary<string, UserData> usersDb = new ConcurrentDictionary<string, UserData>();

        public static Setting settings = new Setting();

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews().AddJsonOptions(options => {
                //options.JsonSerializerOptions.IgnoreNullValues = true;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();

            #region load usersDb.json
            if (System.IO.File.Exists($"{settings.appfolder}/usersDb.json"))
            {
                usersDb = JsonConvert.DeserializeObject<ConcurrentDictionary<string, UserData>>(System.IO.File.ReadAllText($"{settings.appfolder}/usersDb.json"));
            }
            else
            {
                usersDb = new ConcurrentDictionary<string, UserData>()
                {
                    ["ts"] = new UserData()
                    {
                        login = "ts",
                        passwd = "ts",
                        IsShared = true
                    }
                };

                System.IO.File.WriteAllText($"{settings.appfolder}/usersDb.json", JsonConvert.SerializeObject(usersDb, Formatting.Indented));
            }
            #endregion

            #region load whiteip.txt
            if (System.IO.File.Exists($"{settings.appfolder}/whiteip.txt"))
            {
                foreach (string ip in System.IO.File.ReadAllLines($"{settings.appfolder}/whiteip.txt"))
                {
                    if (string.IsNullOrWhiteSpace(ip))
                        continue;

                    if (ip.Contains("/"))
                    {
                        if (int.TryParse(ip.Split("/")[1], out int prefixLength))
                            whiteip.Add(new IPNetwork(IPAddress.Parse(ip.Split("/")[0]), prefixLength));
                    }
                    else
                    {
                        whiteip.Add(new IPNetwork(IPAddress.Parse(ip), 0));
                    }
                }
            }
            #endregion

            #region IP клиента
            var forwarded = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };

            if (settings.KnownProxies != null && settings.KnownProxies.Count > 0)
            {
                foreach (var k in settings.KnownProxies)
                    forwarded.KnownNetworks.Add(new IPNetwork(IPAddress.Parse(k.ip), k.prefixLength));
            }

            app.UseForwardedHeaders(forwarded);
            #endregion

            app.UseRouting();
            app.UseModHeaders();
            app.UseAccs();
            app.UseIPTables();
            app.UseTorAPI();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
