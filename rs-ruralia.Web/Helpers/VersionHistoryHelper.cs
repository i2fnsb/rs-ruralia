using rs_ruralia.Shared.Models;

namespace rs_ruralia.Web.Helpers;

/// <summary>
/// Provides helper methods for managing entity version history
/// </summary>
public static class VersionHistoryHelper
{
    /// <summary>
    /// Generic handler for restoring a historical version of an entity
    /// </summary>
    public static async Task<(bool Success, string? ErrorMessage, string[]? ErrorDetails)> HandleRestoreAsync<TEntity>(
        TEntity? selectedEntity,
        TEntity historicalRecord,
        Func<TEntity, TEntity> cloneEntity,
        Action<TEntity, int> setEntityId,
        Func<int, TEntity, Task<Result<TEntity>?>> updateApi,
        Func<TEntity, int> getEntityId,
        Func<TEntity, DateTime?> getValidFrom)
        where TEntity : class
    {
        if (selectedEntity == null || historicalRecord == null)
            return (false, "No entity selected", null);

        try
        {
            var restoredData = cloneEntity(historicalRecord);
            var entityId = getEntityId(selectedEntity);
            setEntityId(restoredData, entityId);

            var result = await updateApi(entityId, restoredData);

            if (result?.IsSuccess == true)
            {
                var validFrom = getValidFrom(historicalRecord);
                var message = $"Successfully restored version from {validFrom:g}";
                return (true, message, null);
            }

            return (false, result?.ErrorMessage ?? "Failed to restore historical version", result?.Errors);
        }
        catch (Exception ex)
        {
            return (false, $"Error restoring historical version: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Generic handler for saving entity edits with version history support
    /// </summary>
    public static async Task<(bool Success, string? ErrorMessage, string[]? ErrorDetails, TEntity? UpdatedEntity)> SaveEditAsync<TEntity>(
        TEntity? editModel,
        Func<int, TEntity, Task<Result<TEntity>?>> updateApi,
        Func<TEntity, int> getEntityId,
        string successMessage = "Entity updated successfully!")
        where TEntity : class
    {
        if (editModel == null)
            return (false, "No edit model provided", null, null);

        try
        {
            var entityId = getEntityId(editModel);
            var result = await updateApi(entityId, editModel);

            if (result?.IsSuccess == true && result.Data != null)
            {
                return (true, successMessage, null, result.Data);
            }

            return (false, result?.ErrorMessage ?? "Failed to update entity", result?.Errors, null);
        }
        catch (Exception ex)
        {
            return (false, $"Error updating entity: {ex.Message}", null, null);
        }
    }

    /// <summary>
    /// Loads all versions (current + history) for an entity
    /// </summary>
    public static async Task<(
        IEnumerable<TEntity> AllVersions, 
        TEntity CurrentVersion,
        string? ErrorMessage)> LoadAllVersionsAsync<TEntity>(
        TEntity selectedEntity,
        Func<int, Task<Result<IEnumerable<TEntity>>?>> getHistoryApi,
        Func<TEntity, int> getEntityId,
        Func<TEntity, DateTime?> getValidFrom)
        where TEntity : class
    {
        try
        {
            var entityId = getEntityId(selectedEntity);
            var historyResult = await getHistoryApi(entityId);
            var historyRecords = new List<TEntity>();

            if (historyResult?.IsSuccess == true && historyResult.Data != null)
            {
                historyRecords.AddRange(historyResult.Data);
            }

            var versions = new List<TEntity> { selectedEntity };
            var selectedValidFrom = getValidFrom(selectedEntity);
            versions.AddRange(historyRecords.Where(h => getValidFrom(h) != selectedValidFrom));

            return (versions, selectedEntity, null);
        }
        catch (Exception ex)
        {
            return (new List<TEntity> { selectedEntity }, selectedEntity, $"Error loading versions: {ex.Message}");
        }
    }
}
