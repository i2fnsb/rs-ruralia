using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using rs_ruralia.Web.Data;

namespace rs_ruralia.Web.Components.Account;

internal static class IdentityComponentsEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var accountGroup = endpoints.MapGroup("/Account");

        accountGroup.MapPost("/PerformExternalLogin", (HttpContext context, [FromForm] string provider, [FromForm] string returnUrl) =>
        {
            IEnumerable<KeyValuePair<string, string?>> items = [new("ReturnUrl", returnUrl)];
            var redirectUrl = UriHelper.BuildRelative(
                context.Request.PathBase,
                "/Account/ExternalLogin",
                QueryString.Create(items));

            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return TypedResults.Challenge(properties, [provider]);
        });

        accountGroup.MapGet("/PerformSignIn", async (
            HttpContext context,
            [FromQuery] string userId,
            [FromQuery] string returnUrl,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromServices] UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return Results.Redirect("/Account/Login");
            }

            await signInManager.SignInAsync(user, isPersistent: false);
            return Results.Redirect(returnUrl);
        });

        return accountGroup;
    }
}

