using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class OrdinanceEndpoints
{
    public static void MapOrdinanceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/ordinances").WithTags("Ordinances");

        group.MapGet("/", async (OrdinanceService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        });

        group.MapGet("/{id:int}", async (int id, OrdinanceService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        });

        group.MapGet("/service-area/{serviceAreaId:int}", async (int serviceAreaId, OrdinanceService service) =>
        {
            var result = await service.GetByServiceAreaIdAsync(serviceAreaId);
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        });

        group.MapGet("/{id:int}/history", async (int id, OrdinanceService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        });

        group.MapPost("/", async ([FromBody] Ordinance ordinance, OrdinanceService service) =>
        {
            var result = await service.CreateAsync(ordinance);
            return result.IsSuccess
                ? Results.Created($"/ordinances/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        });

        group.MapPut("/{id:int}", async (int id, [FromBody] Ordinance ordinance, OrdinanceService service) =>
        {
            ordinance.Id = id;
            var result = await service.UpdateAsync(ordinance);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        });

        group.MapDelete("/{id:int}", async (int id, OrdinanceService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.NotFound(result);
        });
    }
}