using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace rs_ruralia.Web.Helpers;

/// <summary>
/// Provides helper methods for authentication and user information
/// </summary>
public static class AuthenticationHelper
{
    /// <summary>
    /// Gets the current user's email from the authentication state
    /// </summary>
    public static async Task<string?> GetCurrentUserEmailAsync(AuthenticationStateProvider authStateProvider)
    {
        var authState = await authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        
        return user.Identity?.Name 
            ?? user.Claims.FirstOrDefault(c => c.Type.Contains("email", StringComparison.OrdinalIgnoreCase))?.Value;
    }

    /// <summary>
    /// Sets the ModifiedBy field on an entity using the current user's email
    /// </summary>
    public static async Task<string?> SetModifiedByAsync<TEntity>(
        TEntity entity, 
        AuthenticationStateProvider authStateProvider,
        Action<TEntity, string> setModifiedBy)
        where TEntity : class
    {
        var userEmail = await GetCurrentUserEmailAsync(authStateProvider);
        
        if (!string.IsNullOrEmpty(userEmail))
        {
            setModifiedBy(entity, userEmail);
        }

        return userEmail;
    }
}
