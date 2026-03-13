namespace rs_ruralia.Web.Helpers;

/// <summary>
/// Provides common helper methods for entity management pages
/// </summary>
public static class EntityPageHelper
{
    /// <summary>
    /// Clears error and success messages
    /// </summary>
    public static void ClearMessages(ref string? errorMessage, ref string[]? errorDetails, ref string? successMessage)
    {
        errorMessage = null;
        errorDetails = null;
        successMessage = null;
    }

    /// <summary>
    /// Determines if the current version being viewed is the most recent version
    /// </summary>
    public static bool IsViewingCurrentVersion<TEntity>(TEntity? currentVersion, TEntity? selectedEntity, Func<TEntity, DateTime?> getValidFrom)
        where TEntity : class
    {
        if (currentVersion == null || selectedEntity == null)
            return true;

        return getValidFrom(currentVersion) == getValidFrom(selectedEntity);
    }

    /// <summary>
    /// Determines if fields can be edited
    /// </summary>
    public static bool CanEditFields(bool isEditMode, bool isViewingCurrentVersion)
    {
        return isEditMode && isViewingCurrentVersion;
    }

    /// <summary>
    /// Handles edit mode cancellation
    /// </summary>
    public static void HandleEditModeCancelled(bool isEditMode, Action cancelEdit)
    {
        if (isEditMode)
        {
            cancelEdit();
        }
    }

    /// <summary>
    /// Builds a navigation URI with query parameter
    /// </summary>
    public static string BuildNavigationUri(string baseUri, string paramName, int paramValue)
    {
        return $"{baseUri}?{paramName}={paramValue}";
    }
}
