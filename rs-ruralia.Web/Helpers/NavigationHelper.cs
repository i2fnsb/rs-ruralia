using Microsoft.AspNetCore.Components;

namespace rs_ruralia.Web.Helpers;

/// <summary>
/// Provides helper methods for navigation operations
/// </summary>
public static class NavigationHelper
{
    /// <summary>
    /// Builds a URL with a query parameter
    /// </summary>
    public static string BuildUrlWithQuery(string baseUrl, string paramName, object paramValue)
    {
        return $"{baseUrl}?{paramName}={paramValue}";
    }

    /// <summary>
    /// Navigates to an entity detail page
    /// </summary>
    public static void NavigateToEntity(NavigationManager navigationManager, string basePath, int entityId)
    {
        navigationManager.NavigateTo($"{basePath}?id={entityId}");
    }

    /// <summary>
    /// Updates the current URL with query parameters (for selection state)
    /// </summary>
    public static void UpdateUrlWithSelection(NavigationManager navigationManager, int? entityId)
    {
        if (entityId.HasValue)
        {
            var uri = navigationManager.GetUriWithQueryParameter("id", entityId.Value);
            navigationManager.NavigateTo(uri, replace: true);
        }
    }

    /// <summary>
    /// Navigates to a related entity's detail page
    /// </summary>
    public static void NavigateToRelatedEntity(
        NavigationManager navigationManager, 
        string entityType, 
        int entityId)
    {
        var routes = new Dictionary<string, string>
        {
            { "commissioner", "/commissioners" },
            { "service-area", "/service-areas" },
            { "road", "/roads" },
            { "ordinance", "/ordinances" },
            { "commission-seat", "/commission-seats" },
            { "person", "/persons" },
            { "rfq", "/rfqs" },
            { "vendor", "/vendors" }
        };

        if (routes.TryGetValue(entityType.ToLower(), out var path))
        {
            NavigateToEntity(navigationManager, path, entityId);
        }
    }
}
