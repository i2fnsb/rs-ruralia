using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class ServiceAreaCodeEndpoints
{
    public static void MapServiceAreaCodeEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/service-area-codes")
            .WithTags("Service Area Codes");
            //.RequireAuthorization();

        // GET: api/service-area-codes
        group.MapGet("/", async (ServiceAreaCodeService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.BadRequest(result);
        })
        .WithName("GetAllServiceAreaCodes");

        // GET: api/service-area-codes/{id}
        group.MapGet("/{id:int}", async (int id, ServiceAreaCodeService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.NotFound(result);
        })
        .WithName("GetServiceAreaCodeById");

        // GET: api/service-area-codes/code/{code}
        group.MapGet("/code/{code}", async (string code, ServiceAreaCodeService service) =>
        {
            var result = await service.GetByCodeAsync(code);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.NotFound(result);
        })
        .WithName("GetServiceAreaCodeByCode");

        // POST: api/service-area-codes
        group.MapPost("/", async ([FromBody] ServiceAreaCode serviceAreaCode, ServiceAreaCodeService service) =>
        {
            var result = await service.CreateAsync(serviceAreaCode);
            return result.IsSuccess
                ? Results.Created($"/api/service-area-codes/{result.Data?.Id}", result)
                : Results.BadRequest(result);
        })
        .WithName("CreateServiceAreaCode");
        //.RequireAuthorization(policy => policy.RequireRole("fnsbuser", "admin"));

        // PUT: api/service-area-codes/{id}
        group.MapPut("/{id:int}", async (int id, [FromBody] ServiceAreaCode serviceAreaCode, ServiceAreaCodeService service) =>
        {
            if (id != serviceAreaCode.Id)
            {
                return Results.BadRequest(Result<ServiceAreaCode>.Failure("ID mismatch"));
            }

            var result = await service.UpdateAsync(serviceAreaCode);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.BadRequest(result);
        })
        .WithName("UpdateServiceAreaCode");
        //.RequireAuthorization(policy => policy.RequireRole("fnsbuser", "admin"));

        // DELETE: api/service-area-codes/{id}
        group.MapDelete("/{id:int}", async (int id, ServiceAreaCodeService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.BadRequest(result);
        })
        .WithName("DeleteServiceAreaCode");
        //.RequireAuthorization(policy => policy.RequireRole("fnsbuser", "admin"));
    }
}