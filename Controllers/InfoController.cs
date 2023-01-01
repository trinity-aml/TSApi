using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System;
using System.Linq;
using TSApi.Engine.Middlewares;
using TSApi.Models;

namespace TSApi.Controllers
{
    public class InfoController : Controller
    {
        IMemoryCache memoryCache;

        public InfoController(IMemoryCache m)
        {
            memoryCache = m;
        }


        [Route("torinfo")]
        public ActionResult TorInfo()
        {
            List<TorInfo> newinfo = new List<TorInfo>();

            foreach (var i in TorAPI.db.Select(i => i.Value))
            {
                var temp = new TorInfo()
                {
                    port = i.port,
                    user = i.user,
                    lastActive = i.lastActive
                };

                if (memoryCache.TryGetValue($"memKeyLocIP:{i.user.login}:{DateTime.Now.Hour}", out HashSet<string> ips))
                    temp.clientIps = ips;

                newinfo.Add(temp);
            }

            return Json(newinfo);
        }


        [Route("xrealip")]
        public string XRealIP()
        {
            return HttpContext.Connection.RemoteIpAddress.ToString();
        }


        [Route("headers")]
        public ActionResult Headers()
        {
            return Json(HttpContext.Request.Headers);
        }
    }
}
