using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using TSApi.Engine.Middlewares;
using TSApi.Models;

namespace TSApi.Controllers
{
    [Route("cron/[action]")]
    public class CronController : Controller
    {
        #region UpdateUsersDb
        public string UpdateUsersDb()
        {
            if (System.IO.File.Exists($"{Startup.settings.appfolder}/usersDb.json"))
                Startup.usersDb = JsonConvert.DeserializeObject<ConcurrentDictionary<string, UserData>>(System.IO.File.ReadAllText($"{Startup.settings.appfolder}/usersDb.json"));

            if (System.IO.File.Exists($"{Startup.settings.appfolder}/settings.json"))
            {
                var settings = JsonConvert.DeserializeObject<Setting>(System.IO.File.ReadAllText($"{Startup.settings.appfolder}/settings.json"));
                Startup.settings.AuthorizationRequired = settings.AuthorizationRequired;
                Startup.settings.maxiptoIsLockHostOrUser = settings.maxiptoIsLockHostOrUser;
                Startup.settings.worknodetominutes = settings.worknodetominutes;
            }

            #region load whiteip.txt
            if (System.IO.File.Exists($"{Startup.settings.appfolder}/whiteip.txt"))
            {
                ConcurrentBag<IPNetwork> whiteip = new ConcurrentBag<IPNetwork>();
                foreach (string ip in System.IO.File.ReadAllLines($"{Startup.settings.appfolder}/whiteip.txt"))
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

                Startup.whiteip = whiteip;
            }
            #endregion

            return "ok";
        }
        #endregion

        #region CheckingNodes
        static bool workCheckingNodes = false;

        async public Task<string> CheckingNodes()
        {
            if (workCheckingNodes)
                return "work";

            workCheckingNodes = true;

            try
            {
                foreach (var node in TorAPI.db.ToArray())
                {
                    if (node.Value.countError >= 2 || DateTime.Now.AddMinutes(-Startup.settings.worknodetominutes) > node.Value.lastActive)
                    {
                        TorAPI.db.TryRemove(node.Key, out _);
                        node.Value.Dispose();
                    }
                    else
                    {
                        if (await TorAPI.CheckPort(node.Value.port, HttpContext) == false)
                        {
                            node.Value.countError += 1;
                        }
                        else
                        {
                            node.Value.countError = 0;
                        }
                    }
                }
            }
            catch { }

            workCheckingNodes = false;
            return "ok";
        }
        #endregion
    }
}
