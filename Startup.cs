using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json.Serialization;
using TSApi.Engine.Middlewares;
using System.Collections.Generic;
using Newtonsoft.Json;
using TSApi.Models;

namespace TSApi
{
    public class Startup
    {
        public static Dictionary<string, UserData> usersDb = new Dictionary<string, UserData>();

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
                try
                {
                    usersDb = JsonConvert.DeserializeObject<Dictionary<string, UserData>>(System.IO.File.ReadAllText($"{settings.appfolder}/usersDb.json"));
                }
                catch { }
            }
            else
            {
                usersDb = new Dictionary<string, UserData>()
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

            // IP клиента
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseRouting();
            app.UseModHeaders();
            app.UseAccs();
            app.UseTorAPI();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
