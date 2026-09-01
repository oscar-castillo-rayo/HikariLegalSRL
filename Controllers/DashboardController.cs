using Microsoft.AspNetCore.Mvc;

namespace HikariLegalSRL.Controllers
{
    public class DashboardController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}
