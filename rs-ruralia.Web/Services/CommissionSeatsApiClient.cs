using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using System.Net.Http.Json;

namespace rs_ruralia.Web.Services;

public class CommissionSeatsApiClient
{
    private readonly HttpClient _httpClient;

    public CommissionSeatsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Result<IEnumerable<CommissionSeat>>> GetAllCommissionSeatsAsync()
    {
        try
        {
            var items = await _httpClient.GetFromJsonAsync<IEnumerable<CommissionSeat>>("/commission-seats");
            return Result<IEnumerable<CommissionSeat>>.Success(items ?? Enumerable.Empty<CommissionSeat>());
        }
        catch (HttpRequestException ex)
        {
            return Result<IEnumerable<CommissionSeat>>.Failure($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CommissionSeat>>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<CommissionSeat>> GetCommissionSeatByIdAsync(int id)
    {
        try
        {
            var item = await _httpClient.GetFromJsonAsync<CommissionSeat>($"/commission-seats/{id}");
            return item != null
                ? Result<CommissionSeat>.Success(item)
                : Result<CommissionSeat>.Failure("Commission seat not found");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Result<CommissionSeat>.Failure("Commission seat not found");
        }
        catch (Exception ex)
        {
            return Result<CommissionSeat>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CommissionSeat>>> GetCommissionSeatsByServiceAreaAsync(int serviceAreaId)
    {
        try
        {
            var items = await _httpClient.GetFromJsonAsync<IEnumerable<CommissionSeat>>($"/commission-seats/service-area/{serviceAreaId}");
            return Result<IEnumerable<CommissionSeat>>.Success(items ?? Enumerable.Empty<CommissionSeat>());
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CommissionSeat>>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CommissionSeat>>> GetCommissionSeatHistoryAsync(int id)
    {
        try
        {
            var history = await _httpClient.GetFromJsonAsync<IEnumerable<CommissionSeat>>($"/commission-seats/{id}/history");
            return Result<IEnumerable<CommissionSeat>>.Success(history ?? Enumerable.Empty<CommissionSeat>());
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CommissionSeat>>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<CommissionSeat>> CreateCommissionSeatAsync(CommissionSeat commissionSeat)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/commission-seats", commissionSeat);
            
            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<CommissionSeat>();
                return created != null
                    ? Result<CommissionSeat>.Success(created)
                    : Result<CommissionSeat>.Failure("Failed to parse response");
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            return Result<CommissionSeat>.Failure($"Failed to create commission seat: {errorContent}");
        }
        catch (Exception ex)
        {
            return Result<CommissionSeat>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<CommissionSeat>> UpdateCommissionSeatAsync(int id, CommissionSeat commissionSeat)
    {
        try
        {
            commissionSeat.Id = id;
            var response = await _httpClient.PutAsJsonAsync($"/commission-seats/{id}", commissionSeat);
            
            if (response.IsSuccessStatusCode)
            {
                return Result<CommissionSeat>.Success(commissionSeat);
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            return Result<CommissionSeat>.Failure($"Failed to update commission seat: {errorContent}");
        }
        catch (Exception ex)
        {
            return Result<CommissionSeat>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result> DeleteCommissionSeatAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/commission-seats/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            return Result.Failure($"Failed to delete commission seat: {errorContent}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error: {ex.Message}");
        }
    }
}