using rs_ruralia.Shared.Models;

namespace rs_ruralia.Web.Helpers;

/// <summary>
/// Provides generic helper methods for loading data from API clients
/// </summary>
public static class DataLoaderHelper
{
    /// <summary>
    /// Generic method to load data from an API endpoint
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="apiCall">The API call function</param>
    /// <param name="fallback">The fallback value if loading fails</param>
    /// <returns>The loaded data or fallback value</returns>
    public static async Task<IEnumerable<TEntity>> LoadDataAsync<TEntity>(
        Func<Task<Result<IEnumerable<TEntity>>?>> apiCall,
        IEnumerable<TEntity>? fallback = null)
    {
        try
        {
            var result = await apiCall();

            if (result?.IsSuccess == true && result.Data != null)
            {
                return result.Data;
            }

            return fallback ?? Array.Empty<TEntity>();
        }
        catch (Exception)
        {
            return fallback ?? Array.Empty<TEntity>();
        }
    }

    /// <summary>
    /// Generic method to load data with error handling and status tracking
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="apiCall">The API call function</param>
    /// <param name="onSuccess">Action to execute on success</param>
    /// <param name="onError">Action to execute on error (receives error message)</param>
    /// <param name="fallback">The fallback value if loading fails</param>
    /// <returns>The loaded data or fallback value</returns>
    public static async Task<IEnumerable<TEntity>> LoadDataWithErrorHandlingAsync<TEntity>(
        Func<Task<Result<IEnumerable<TEntity>>?>> apiCall,
        Action<IEnumerable<TEntity>>? onSuccess = null,
        Action<string?, string[]?>? onError = null,
        IEnumerable<TEntity>? fallback = null)
    {
        try
        {
            var result = await apiCall();

            if (result?.IsSuccess == true && result.Data != null)
            {
                onSuccess?.Invoke(result.Data);
                return result.Data;
            }

            onError?.Invoke(result?.ErrorMessage, result?.Errors);
            return fallback ?? Array.Empty<TEntity>();
        }
        catch (HttpRequestException)
        {
            onError?.Invoke("Network error. Please check your connection and try again.", null);
            return fallback ?? Array.Empty<TEntity>();
        }
        catch (Exception ex)
        {
            onError?.Invoke($"Unexpected error: {ex.Message}", null);
            return fallback ?? Array.Empty<TEntity>();
        }
    }

    /// <summary>
    /// Loads multiple data sources in parallel
    /// </summary>
    public static async Task LoadMultipleAsync(params Func<Task>[] loaders)
    {
        var tasks = loaders.Select(loader => loader()).ToArray();
        await Task.WhenAll(tasks);
    }
}
