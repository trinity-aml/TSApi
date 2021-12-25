using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System.Net;
using System.Text.RegularExpressions;
using TSApi.Models;

namespace TSApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string appfolder = "/opt/TSApi";

            foreach (var line in args)
            {
                var g = new Regex("-([^=]+)=([^\n\r]+)").Match(line).Groups;
                string comand = g[1].Value;
                string value = g[2].Value;

                switch (comand)
                {
                    case "d":
                        {
                            // -d=/opt/TSApi
                            appfolder = value;
                            break;
                        }
                }
            }

            if (System.IO.File.Exists($"{appfolder}/settings.json"))
            {
                try
                {
                    Startup.settings = JsonConvert.DeserializeObject<Setting>(System.IO.File.ReadAllText($"{appfolder}/settings.json"));
                    Startup.settings.appfolder = appfolder;
                }
                catch { }
            }
            else
            {
                Startup.settings.appfolder = appfolder;
                System.IO.File.WriteAllText($"{appfolder}/settings.json", JsonConvert.SerializeObject(Startup.settings, Formatting.Indented));
            }

            CreateHostBuilder(null).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(op => op.Listen(Startup.settings.IPAddressAny ? IPAddress.Any : IPAddress.Parse("127.0.0.1"), Startup.settings.port));
                    webBuilder.UseStartup<Startup>();
                });
    }
}
