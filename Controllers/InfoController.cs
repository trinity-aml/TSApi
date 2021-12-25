using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Linq;
using TSApi.Engine.Middlewares;

namespace TSApi.Controllers
{
    public class InfoController : Controller
    {
        [Route("torinfo")]
        public string TorInfo()
        {
            return JsonConvert.SerializeObject(TorAPI.db.Select(i => i.Value), Formatting.Indented);
        }
    }
}
