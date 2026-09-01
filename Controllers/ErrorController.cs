using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HikariLegalSRL.Controllers
{
    [AllowAnonymous]
    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode:int}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            switch (statusCode)
            {
                case StatusCodes.Status401Unauthorized:
                    return RedirectToAction("Login", "Account");

                case StatusCodes.Status403Forbidden:
                    Response.StatusCode = StatusCodes.Status403Forbidden;
                    return View("~/Views/Errors/AccessDenied.cshtml");

                case StatusCodes.Status404NotFound:
                    Response.StatusCode = StatusCodes.Status404NotFound;
                    return View("~/Views/Errors/NotFound.cshtml");

                case StatusCodes.Status500InternalServerError:
                    Response.StatusCode = StatusCodes.Status500InternalServerError;
                    return View("~/Views/Errors/Error.cshtml");

                default:
                    Response.StatusCode = statusCode;
                    return View("~/Views/Errors/Error.cshtml");
            }
        }
    }
}