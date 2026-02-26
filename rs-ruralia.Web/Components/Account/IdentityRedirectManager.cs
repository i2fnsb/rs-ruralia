using Microsoft.AspNetCore.Components;

namespace rs_ruralia.Web.Components.Account;

internal sealed class IdentityRedirectManager(NavigationManager navigationManager)
{
    private const string StatusCookieName = "Identity.StatusMessage";

    private static readonly CookieBuilder _statusCookieBuilder = new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(5),
    };

    public void RedirectTo(string? uri)
    {
        uri ??= "";
        if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
        {
            uri = navigationManager.ToBaseRelativePath(uri);
        }

        navigationManager.NavigateTo(uri);
    }

    public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
    {
        var uriWithoutQuery = navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
        var newUri = navigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters);
        
        // Force a full page load to refresh Blazor circuit with new auth state
        navigationManager.NavigateTo(newUri, forceLoad: true);
    }

    public void RedirectToWithStatus(string uri, string message, HttpContext context)
    {
        context.Response.Cookies.Append(StatusCookieName, message, _statusCookieBuilder.Build(context));
        RedirectTo(uri);
    }

    public string? RedirectUri { get; set; }
}
