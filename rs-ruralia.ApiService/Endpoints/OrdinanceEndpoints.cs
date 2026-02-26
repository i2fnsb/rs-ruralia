using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class OrdinanceEndpoints
{
    public static void MapOrdinanceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/ordinances").WithTags("Ordinances");

        group.MapGet("/", async (OrdinanceService service, ILogger<OrdinanceService> logger) =>
        {
            logger.LogInformation("[Endpoint] GET /ordinances - Request received");
            var result = await service.GetAllAsync();
            if (result.IsSuccess)
            {
                logger.LogInformation("[Endpoint] GET /ordinances - Success, returning {Count} ordinances", result.Data?.Count() ?? 0);
                return Results.Ok(result.Data);
            }
            logger.LogWarning("[Endpoint] GET /ordinances - Failed: {Error}", result.ErrorMessage);
            return Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        });

        group.MapGet("/{id:int}", async (int id, OrdinanceService service, ILogger<OrdinanceService> logger) =>
        {
            logger.LogInformation("[Endpoint] GET /ordinances/{Id} - Request received", id);
            var result = await service.GetByIdAsync(id);
            if (result.IsSuccess)
            {
                logger.LogInformation("[Endpoint] GET /ordinances/{Id} - Success", id);
                return Results.Ok(result.Data);
            }
            logger.LogWarning("[Endpoint] GET /ordinances/{Id} - Failed: {Error}", id, result.ErrorMessage);
            return Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        });

        group.MapGet("/service-area/{serviceAreaId:int}", async (int serviceAreaId, OrdinanceService service, ILogger<OrdinanceService> logger) =>
        {
            logger.LogInformation("[Endpoint] GET /ordinances/service-area/{ServiceAreaId} - Request received", serviceAreaId);
            var result = await service.GetByServiceAreaIdAsync(serviceAreaId);
            if (result.IsSuccess)
            {
                logger.LogInformation("[Endpoint] GET /ordinances/service-area/{ServiceAreaId} - Success, returning {Count} ordinances", serviceAreaId, result.Data?.Count() ?? 0);
                return Results.Ok(result.Data);
            }
            logger.LogWarning("[Endpoint] GET /ordinances/service-area/{ServiceAreaId} - Failed: {Error}", serviceAreaId, result.ErrorMessage);
            return Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        });

        group.MapGet("/{id:int}/history", async (int id, OrdinanceService service, ILogger<OrdinanceService> logger) =>
        {
            logger.LogInformation("[Endpoint] GET /ordinances/{Id}/history - Request received", id);
            var result = await service.GetHistoryByIdAsync(id);
            if (result.IsSuccess)
            {
                logger.LogInformation("[Endpoint] GET /ordinances/{Id}/history - Success, returning {Count} history records", id, result.Data?.Count() ?? 0);
                return Results.Ok(result.Data);
            }
            logger.LogWarning("[Endpoint] GET /ordinances/{Id}/history - Failed: {Error}", id, result.ErrorMessage);
            return Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        });

        group.MapPost("/", async ([FromBody] Ordinance ordinance, OrdinanceService service, ILogger<OrdinanceService> logger) =>
        {
            logger.LogInformation("[Endpoint] POST /ordinances - Request received for ordinance {OrdinanceName}", ordinance.OrdinanceName);
            var result = await service.CreateAsync(ordinance);
            if (result.IsSuccess)
            {
                logger.LogInformation("[Endpoint] POST /ordinances - Success, created ordinance ID {Id}", result.Data!.Id);
                return Results.Created($"/ordinances/{result.Data!.Id}", result.Data);
            }
            logger.LogWarning("[Endpoint] POST /ordinances - Failed: {Error}", result.ErrorMessage);
            return Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        });

        group.MapPut("/{id:int}", async (int id, [FromBody] Ordinance ordinance, OrdinanceService service, ILogger<OrdinanceService> logger) =>
        {
            logger.LogInformation("[Endpoint] PUT /ordinances/{Id} - Request received", id);
            ordinance.Id = id;
            var result = await service.UpdateAsync(ordinance);
            if (result.IsSuccess)
            {
                logger.LogInformation("[Endpoint] PUT /ordinances/{Id} - Success", id);
                return Results.NoContent();
            }
            logger.LogWarning("[Endpoint] PUT /ordinances/{Id} - Failed: {Error}", id, result.ErrorMessage);
            return Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        });

        group.MapDelete("/{id:int}", async (int id, OrdinanceService service, ILogger<OrdinanceService> logger) =>
        {
            logger.LogInformation("[Endpoint] DELETE /ordinances/{Id} - Request received", id);
            var result = await service.DeleteAsync(id);
            if (result.IsSuccess)
            {
                logger.LogInformation("[Endpoint] DELETE /ordinances/{Id} - Success", id);
                return Results.NoContent();
            }
            logger.LogWarning("[Endpoint] DELETE /ordinances/{Id} - Failed: {Error}", id, result.ErrorMessage);
            return Results.NotFound(result);
        });
    }
}