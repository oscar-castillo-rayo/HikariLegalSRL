using Microsoft.AspNetCore.Mvc;

namespace HikariLegalSRL.Controllers.Dashboard
{
    public class DashboardController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}
