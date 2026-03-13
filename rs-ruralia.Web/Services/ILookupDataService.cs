using rs_ruralia.Shared.Models;

namespace rs_ruralia.Web.Services;

/// <summary>
/// Service for managing and caching lookup data across the application
/// </summary>
public interface ILookupDataService
{
    // Commission-related lookups
    Task<IEnumerable<CommissionSeatType>> GetCommissionSeatTypesAsync(bool forceRefresh = false);
    Task<IEnumerable<CommissionSeatClass>> GetCommissionSeatClassesAsync(bool forceRefresh = false);
    Task<IEnumerable<CommissionSeatStatus>> GetCommissionSeatStatusesAsync(bool forceRefresh = false);
    Task<IEnumerable<CommissionerStatus>> GetCommissionerStatusesAsync(bool forceRefresh = false);

    // Ordinance-related lookups
    Task<IEnumerable<OrdinanceType>> GetOrdinanceTypesAsync(bool forceRefresh = false);

    // Road-related lookups
    Task<IEnumerable<RoadSurfaceType>> GetRoadSurfaceTypesAsync(bool forceRefresh = false);
    Task<IEnumerable<RoadResponderCode>> GetRoadResponderCodesAsync(bool forceRefresh = false);
    Task<IEnumerable<RoadName>> GetRoadNamesAsync(bool forceRefresh = false);
    Task<IEnumerable<RoadSubdivision>> GetRoadSubdivisionsAsync(bool forceRefresh = false);

    // Service Area-related lookups
    Task<IEnumerable<ServiceAreaCode>> GetServiceAreaCodesAsync(bool forceRefresh = false);

    // Person-related lookups
    Task<IEnumerable<PersonHonorific>> GetPersonHonorificsAsync(bool forceRefresh = false);
    Task<IEnumerable<PersonSuffix>> GetPersonSuffixesAsync(bool forceRefresh = false);

    // RFQ-related lookups
    Task<IEnumerable<RfqIfbType>> GetRfqIfbTypesAsync(bool forceRefresh = false);
    Task<IEnumerable<RfqProjectScope>> GetRfqProjectScopesAsync(bool forceRefresh = false);

    // Invalidate all cached lookups
    void ClearCache();
    
    // Invalidate specific lookup cache
    void ClearCache<T>();
}
