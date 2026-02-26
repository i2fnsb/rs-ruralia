using rs_ruralia.Shared.Models;
using System.Net.Http.Json;

namespace rs_ruralia.Web.Services;

public class ServiceAreaCodesApiClient(HttpClient httpClient)
{
    public async Task<Result<IEnumerable<ServiceAreaCode>>?> GetAllServiceAreaCodesAsync()
    {
        return await httpClient.GetFromJsonAsync<Result<IEnumerable<ServiceAreaCode>>>("/api/service-area-codes");
    }

    public async Task<Result<ServiceAreaCode>?> GetServiceAreaCodeByIdAsync(int id)
    {
        return await httpClient.GetFromJsonAsync<Result<ServiceAreaCode>>($"/api/service-area-codes/{id}");
    }

    public async Task<Result<ServiceAreaCode>?> GetServiceAreaCodeByCodeAsync(string code)
    {
        return await httpClient.GetFromJsonAsync<Result<ServiceAreaCode>>($"/api/service-area-codes/code/{code}");
    }

    public async Task<Result<ServiceAreaCode>?> CreateServiceAreaCodeAsync(ServiceAreaCode serviceAreaCode)
    {
        var response = await httpClient.PostAsJsonAsync("/api/service-area-codes", serviceAreaCode);
        return await response.Content.ReadFromJsonAsync<Result<ServiceAreaCode>>();
    }

    public async Task<Result<ServiceAreaCode>?> UpdateServiceAreaCodeAsync(int id, ServiceAreaCode serviceAreaCode)
    {
        var response = await httpClient.PutAsJsonAsync($"/api/service-area-codes/{id}", serviceAreaCode);
        return await response.Content.ReadFromJsonAsync<Result<ServiceAreaCode>>();
    }

    public async Task<Result?> DeleteServiceAreaCodeAsync(int id)
    {
        var response = await httpClient.DeleteAsync($"/api/service-area-codes/{id}");
        return await response.Content.ReadFromJsonAsync<Result>();
    }
}