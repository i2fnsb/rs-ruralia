using rs_ruralia.Shared.Models;
using System.Net.Http.Json;

namespace rs_ruralia.Web.Services;

public class CommissionSeatClassesApiClient(HttpClient httpClient)
{
    public async Task<Result<IEnumerable<CommissionSeatClass>>?> GetAllCommissionSeatClassesAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<IEnumerable<CommissionSeatClass>>(
            () => httpClient.GetAsync("/commission-seat-classes", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<CommissionSeatClass>?> GetCommissionSeatClassByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<CommissionSeatClass>(
            () => httpClient.GetAsync($"/commission-seat-classes/{id}", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<IEnumerable<CommissionSeatClass>>?> GetCommissionSeatClassHistoryAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<IEnumerable<CommissionSeatClass>>(
            () => httpClient.GetAsync($"/commission-seat-classes/{id}/history", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<CommissionSeatClass>?> CreateCommissionSeatClassAsync(CommissionSeatClass entity, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<CommissionSeatClass>(
            () => httpClient.PostAsJsonAsync("/commission-seat-classes", entity, cancellationToken),
            cancellationToken);
    }

    public async Task<Result<CommissionSeatClass>?> UpdateCommissionSeatClassAsync(int id, CommissionSeatClass entity, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<CommissionSeatClass>(
            () => httpClient.PutAsJsonAsync($"/commission-seat-classes/{id}", entity, cancellationToken),
            cancellationToken,
            successData: entity);
    }

    public async Task<Result?> DeleteCommissionSeatClassAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            () => httpClient.DeleteAsync($"/commission-seat-classes/{id}", cancellationToken),
            cancellationToken);
    }

    // Generic helper for requests that return data
    private async Task<Result<T>?> ExecuteAsync<T>(
        Func<Task<HttpResponseMessage>> request,
        CancellationToken cancellationToken,
        T? successData = default)
    {
        try
        {
            var response = await request();

            if (response.IsSuccessStatusCode)
            {
                var data = successData ?? await response.Content.ReadFromJsonAsync<T>(cancellationToken);
                return data != null
                    ? Result<T>.Success(data)
                    : Result<T>.Failure("No data returned");
            }

            return await ParseErrorAsync<T>(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return Result<T>.Failure($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result<T>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    // Helper for requests that don't return data (like DELETE)
    private async Task<Result?> ExecuteAsync(
        Func<Task<HttpResponseMessage>> request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await request();

            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            return await ParseErrorAsync(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Unexpected error: {ex.Message}");
        }
    }

    // Parse error response for generic Result<T>
    private async Task<Result<T>> ParseErrorAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var errorResult = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken);
        return Result<T>.Failure(
            errorResult?.Errors ?? new[] { errorResult?.Error ?? $"Request failed with status code {response.StatusCode}" });
    }

    // Parse error response for non-generic Result
    private async Task<Result> ParseErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var errorResult = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken);
        return Result.Failure(
            errorResult?.Errors ?? new[] { errorResult?.Error ?? $"Request failed with status code {response.StatusCode}" });
    }

    private class ErrorResponse
    {
        public string? Error { get; set; }
        public string[]? Errors { get; set; }
    }
}
