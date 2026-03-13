using rs_ruralia.Shared.Models;
using rs_ruralia.Web.Helpers;
using System.Collections.Concurrent;

namespace rs_ruralia.Web.Services;

/// <summary>
/// Service for managing and caching lookup data across the application
/// Implements in-memory caching to reduce API calls
/// </summary>
public class LookupDataService : ILookupDataService
{
    private readonly ConcurrentDictionary<Type, (DateTime CachedAt, object Data)> _cache = new();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

    // API Clients
    private readonly CommissionSeatTypesApiClient _commissionSeatTypesApi;
    private readonly CommissionSeatClassesApiClient _commissionSeatClassesApi;
    private readonly CommissionSeatStatusesApiClient _commissionSeatStatusesApi;
    private readonly CommissionerStatusesApiClient _commissionerStatusesApi;
    private readonly OrdinanceTypesApiClient _ordinanceTypesApi;
    private readonly RoadSurfaceTypesApiClient _roadSurfaceTypesApi;
    private readonly RoadResponderCodesApiClient _roadResponderCodesApi;
    private readonly RoadNamesApiClient _roadNamesApi;
    private readonly RoadSubdivisionsApiClient _roadSubdivisionsApi;
    private readonly ServiceAreaCodesApiClient _serviceAreaCodesApi;
    private readonly PersonHonorificsApiClient _personHonorificsApi;
    private readonly PersonSuffixesApiClient _personSuffixesApi;
    private readonly RfqIfbTypesApiClient _rfqIfbTypesApi;
    private readonly RfqProjectScopesApiClient _rfqProjectScopesApi;

    public LookupDataService(
        CommissionSeatTypesApiClient commissionSeatTypesApi,
        CommissionSeatClassesApiClient commissionSeatClassesApi,
        CommissionSeatStatusesApiClient commissionSeatStatusesApi,
        CommissionerStatusesApiClient commissionerStatusesApi,
        OrdinanceTypesApiClient ordinanceTypesApi,
        RoadSurfaceTypesApiClient roadSurfaceTypesApi,
        RoadResponderCodesApiClient roadResponderCodesApi,
        RoadNamesApiClient roadNamesApi,
        RoadSubdivisionsApiClient roadSubdivisionsApi,
        ServiceAreaCodesApiClient serviceAreaCodesApi,
        PersonHonorificsApiClient personHonorificsApi,
        PersonSuffixesApiClient personSuffixesApi,
        RfqIfbTypesApiClient rfqIfbTypesApi,
        RfqProjectScopesApiClient rfqProjectScopesApi)
    {
        _commissionSeatTypesApi = commissionSeatTypesApi;
        _commissionSeatClassesApi = commissionSeatClassesApi;
        _commissionSeatStatusesApi = commissionSeatStatusesApi;
        _commissionerStatusesApi = commissionerStatusesApi;
        _ordinanceTypesApi = ordinanceTypesApi;
        _roadSurfaceTypesApi = roadSurfaceTypesApi;
        _roadResponderCodesApi = roadResponderCodesApi;
        _roadNamesApi = roadNamesApi;
        _roadSubdivisionsApi = roadSubdivisionsApi;
        _serviceAreaCodesApi = serviceAreaCodesApi;
        _personHonorificsApi = personHonorificsApi;
        _personSuffixesApi = personSuffixesApi;
        _rfqIfbTypesApi = rfqIfbTypesApi;
        _rfqProjectScopesApi = rfqProjectScopesApi;
    }

    private async Task<IEnumerable<T>> GetCachedDataAsync<T>(
        Func<Task<Result<IEnumerable<T>>?>> apiCall,
        bool forceRefresh = false)
    {
        var cacheKey = typeof(T);

        // Check if we have valid cached data
        if (!forceRefresh && _cache.TryGetValue(cacheKey, out var cached))
        {
            if (DateTime.UtcNow - cached.CachedAt < _cacheExpiration)
            {
                return (IEnumerable<T>)cached.Data;
            }
        }

        // Load fresh data
        var data = await DataLoaderHelper.LoadDataAsync(apiCall);

        // Cache it
        _cache[cacheKey] = (DateTime.UtcNow, data);

        return data;
    }

    // Commission-related lookups
    public Task<IEnumerable<CommissionSeatType>> GetCommissionSeatTypesAsync(bool forceRefresh = false)
        => GetCachedDataAsync(() => _commissionSeatTypesApi.GetAllCommissionSeatTypesAsync(), forceRefresh);

    public Task<IEnumerable<CommissionSeatClass>> GetCommissionSeatClassesAsync(bool forceRefresh = false)
        => GetCachedDataAsync(() => _commissionSeatClassesApi.GetAllCommissionSeatClassesAsync(), forceRefresh);

    public Task<IEnumerable<CommissionSeatStatus>> GetCommissionSeatStatusesAsync(bool forceRefresh = false)
        => GetCachedDataAsync(() => _commissionSeatStatusesApi.GetAllCommissionSeatStatusesAsync(), forceRefresh);

    public Task<IEnumerable<CommissionerStatus>> GetCommissionerStatusesAsync(bool forceRefresh = false)
        => GetCachedDataAsync(() => _commissionerStatusesApi.GetAllCommissionerStatusesAsync(), forceRefresh);

    // Ordinance-related lookups
    public Task<IEnumerable<OrdinanceType>> GetOrdinanceTypesAsync(bool forceRefresh = false)
        => GetCachedDataAsync(() => _ordinanceTypesApi.GetAllOrdinanceTypesAsync(), forceRefresh);

    // Road-related lookups
    public Task<IEnumerable<RoadSurfaceType>> GetRoadSurfaceTypesAsync(bool forceRefresh = false)
        => GetCachedDataAsync(() => _roadSurfaceTypesApi.GetAllRoadSurfaceTypesAsync(), forceRefresh);

    public Task<IEnumerable<RoadResponderCode>> GetRoadResponderCodesAsync(bool forceRefresh = false)
        => GetCachedDataAsync(() => _roadResponderCodesApi.GetAllRoadResponderCodesAsync(), forceRefresh);

    public Task<IEnumerable<RoadName>> GetRoadNamesAsync(bool forceRefresh = false)
        => GetCachedDataAsync(() => _roadNamesApi.GetAllRoadNamesAsync(), forceRefresh);

    public Task<IEnumerable<RoadSubdivision>> GetRoadSubdivisionsAsync(bool forceRefresh = false)
        => GetCachedDataAsync(() => _roadSubdivisionsApi.GetAllRoadSubdivisionsAsync(), forceRefresh);

    // Service Area-related lookups
    public Task<IEnumerable<ServiceAreaCode>> GetServiceAreaCodesAsync(bool forceRefresh = false)
        => GetCachedDataAsync(() => _serviceAreaCodesApi.GetAllServiceAreaCodesAsync(), forceRefresh);

    // Person-related lookups
    public Task<IEnumerable<PersonHonorific>> GetPersonHonorificsAsync(bool forceRefresh = false)
        => GetCachedDataAsync(() => _personHonorificsApi.GetAllPersonHonorificsAsync(), forceRefresh);

    public Task<IEnumerable<PersonSuffix>> GetPersonSuffixesAsync(bool forceRefresh = false)
        => GetCachedDataAsync(() => _personSuffixesApi.GetAllPersonSuffixesAsync(), forceRefresh);

    // RFQ-related lookups
    public Task<IEnumerable<RfqIfbType>> GetRfqIfbTypesAsync(bool forceRefresh = false)
        => GetCachedDataAsync(() => _rfqIfbTypesApi.GetAllRfqIfbTypesAsync(), forceRefresh);

    public Task<IEnumerable<RfqProjectScope>> GetRfqProjectScopesAsync(bool forceRefresh = false)
        => GetCachedDataAsync(() => _rfqProjectScopesApi.GetAllRfqProjectScopesAsync(), forceRefresh);

    // Cache management
    public void ClearCache()
    {
        _cache.Clear();
    }

    public void ClearCache<T>()
    {
        _cache.TryRemove(typeof(T), out _);
    }
}
