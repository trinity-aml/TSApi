using Microsoft.AspNetCore.Mvc;
using System.Linq;
using TSApi.Engine.Middlewares;

namespace TSApi.Controllers
{
    public class InfoController : Controller
    {
        [Route("torinfo")]
        public ActionResult TorInfo()
        {
            return Json(TorAPI.db.Select(i => i.Value));
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
