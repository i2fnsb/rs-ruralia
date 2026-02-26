using rs_ruralia.Web.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace rs_ruralia.Web.Components.Account.Endpoints;

public static class PerformSignInEndpoint
{
    public static IEndpointRouteBuilder MapPerformSignInEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/Account/PerformSignIn", async (
            string? userId,
            string? returnUrl,
            bool rememberMe,
            HttpContext context) =>
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.BadRequest("Missing userId");
            }

            var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var signInManager = context.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();

            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return Results.NotFound("User not found");
            }

            // Build authentication properties based on rememberMe
            var authProps = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                IsPersistent = rememberMe
            };

            await signInManager.SignInAsync(user, authProps);

            // Default to home page if no return URL provided
            var targetUrl = !string.IsNullOrWhiteSpace(returnUrl) ? returnUrl : "/";
            
            // Use LocalRedirect for proper HTTP redirect
            return Results.LocalRedirect(targetUrl);
        })
        .AllowAnonymous();

        return endpoints;
    }
}
