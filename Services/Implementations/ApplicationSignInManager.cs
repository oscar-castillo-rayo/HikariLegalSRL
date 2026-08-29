using HikariLegalSRL.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HikariLegalSRL.Services.Implementations
{
    public class ApplicationSignInManager : SignInManager<ApplicationUser>
    {
        //Constructor conserva la funcionalidad del SignInManager original y agrega la verificación de la propiedad Activo
        public ApplicationSignInManager(UserManager<ApplicationUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<ApplicationUser>> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<ApplicationUser> confirmation)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
        }

        public override async Task<bool> CanSignInAsync(ApplicationUser user)
        {
            if (!await base.CanSignInAsync(user))
                return false;

            return user.Activo;
        }
    }
}
