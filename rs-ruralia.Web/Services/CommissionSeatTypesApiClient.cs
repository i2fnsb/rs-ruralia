using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using System.Net.Http.Json;

namespace rs_ruralia.Web.Services;

public class CommissionSeatTypesApiClient
{
    private readonly HttpClient _httpClient;

    public CommissionSeatTypesApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Result<IEnumerable<CommissionSeatType>>> GetAllCommissionSeatTypesAsync()
    {
        try
        {
            var items = await _httpClient.GetFromJsonAsync<IEnumerable<CommissionSeatType>>("/commission-seat-types");
            return Result<IEnumerable<CommissionSeatType>>.Success(items ?? Enumerable.Empty<CommissionSeatType>());
        }
        catch (HttpRequestException ex)
        {
            return Result<IEnumerable<CommissionSeatType>>.Failure($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CommissionSeatType>>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<CommissionSeatType>> GetCommissionSeatTypeByIdAsync(int id)
    {
        try
        {
            var item = await _httpClient.GetFromJsonAsync<CommissionSeatType>($"/commission-seat-types/{id}");
            return item != null
                ? Result<CommissionSeatType>.Success(item)
                : Result<CommissionSeatType>.Failure("Commission seat type not found");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Result<CommissionSeatType>.Failure("Commission seat type not found");
        }
        catch (Exception ex)
        {
            return Result<CommissionSeatType>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CommissionSeatType>>> GetCommissionSeatTypeHistoryAsync(int id)
    {
        try
        {
            var history = await _httpClient.GetFromJsonAsync<IEnumerable<CommissionSeatType>>($"/commission-seat-types/{id}/history");
            return Result<IEnumerable<CommissionSeatType>>.Success(history ?? Enumerable.Empty<CommissionSeatType>());
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CommissionSeatType>>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<CommissionSeatType>> CreateCommissionSeatTypeAsync(CommissionSeatType commissionSeatType)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/commission-seat-types", commissionSeatType);
            
            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<CommissionSeatType>();
                return created != null
                    ? Result<CommissionSeatType>.Success(created)
                    : Result<CommissionSeatType>.Failure("Failed to parse response");
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            return Result<CommissionSeatType>.Failure($"Failed to create commission seat type: {errorContent}");
        }
        catch (Exception ex)
        {
            return Result<CommissionSeatType>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result<CommissionSeatType>> UpdateCommissionSeatTypeAsync(int id, CommissionSeatType commissionSeatType)
    {
        try
        {
            commissionSeatType.Id = id;
            var response = await _httpClient.PutAsJsonAsync($"/commission-seat-types/{id}", commissionSeatType);
            
            if (response.IsSuccessStatusCode)
            {
                return Result<CommissionSeatType>.Success(commissionSeatType);
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            return Result<CommissionSeatType>.Failure($"Failed to update commission seat type: {errorContent}");
        }
        catch (Exception ex)
        {
            return Result<CommissionSeatType>.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<Result> DeleteCommissionSeatTypeAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/commission-seat-types/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            return Result.Failure($"Failed to delete commission seat type: {errorContent}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error: {ex.Message}");
        }
    }
}