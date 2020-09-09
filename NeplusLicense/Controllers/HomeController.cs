using Microsoft.AspNetCore.Mvc;

namespace NeplusLicense.Controllers
{
    [Route(""), Route("License")]
    public class HomeController : Controller
    {
        [HttpGet("{*url}"), ResponseCache(Duration = 21600 /*-- 6 hours --*/)]
        public IActionResult Index() => File("~/index.html", "text/html", true);
    }
}