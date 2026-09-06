using HikariLegalSRL.Models;
using HikariLegalSRL.ViewModels.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace HikariLegalSRL.Controllers.Account
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Correo,
                model.Contrasena,
                model.RecordarSesion,
                lockoutOnFailure: true
            );

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsNotAllowed)
            {
                ModelState.AddModelError(string.Empty, "Esta cuenta está inactiva. Contacte al administrador.");

                return View(model);
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "La cuenta está bloqueada temporalmente. Intenta nuevamente más tarde.");

                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return RedirectToAction(
                    nameof(ForgotPasswordConfirmation));
            }

            // Genera el token de restablecimiento de contraseña y luego lo codifica en Base64 para que pueda ser enviado en la URL.
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var callbackUrl = Url.Action(
                nameof(ResetPassword),
                "Account", new { email = user.Email, code = encodedToken },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                user.Email!,
                "Restablecimiento de contraseña",
                $"<p>Para restablecer su contraseña haga clic en el siguiente enlace:</p>" +
                $"<p><a href='{callbackUrl}'>Restablecer contraseña</a></p>");

            return RedirectToAction(
                nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string email, string code)
        {
            if (string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(code))
            {
                return BadRequest();
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null || !await IsPasswordResetTokenValidAsync(user, code))
            {
                return BadRequest("El enlace de restablecimiento ya no es válido o ha sido utilizado.");
            }

            var model = new ResetPasswordViewModel
            {
                Email = email,
                Code = code
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            if (!await IsPasswordResetTokenValidAsync(user, model.Code))
            {
                ModelState.AddModelError(string.Empty, "Este enlace ya fue utilizado o ha caducado.");
                return View(model);
            }

            // Decodifica el token de restablecimiento de contraseña desde Base64 antes de usarlo.
            var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));

            var result = await _userManager.ResetPasswordAsync(
                    user,
                    decodedCode,
                    model.Password);

            if (result.Succeeded)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
        private async Task<bool> IsPasswordResetTokenValidAsync(ApplicationUser user, string code)
        {
            if (user == null || string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            try
            {
                var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));

                return await _userManager.VerifyUserTokenAsync(
                    user,
                    TokenOptions.DefaultProvider,
                    "ResetPassword",
                    decodedCode);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public IActionResult AccessDenied()
        {
            return View("~/Views/Errors/AccessDenied.cshtml");
        }
    }
}