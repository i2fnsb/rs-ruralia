using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class OrdinanceTypeEndpoints
{
    public static void MapOrdinanceTypeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/ordinance-types").WithTags("Ordinance Types");

        group.MapGet("/", async (OrdinanceTypeService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        });

        group.MapGet("/{id:int}", async (int id, OrdinanceTypeService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        });

        group.MapGet("/{id:int}/history", async (int id, OrdinanceTypeService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        });

        group.MapPost("/", async ([FromBody] OrdinanceType ordinanceType, OrdinanceTypeService service) =>
        {
            var result = await service.CreateAsync(ordinanceType);
            return result.IsSuccess
                ? Results.Created($"/ordinance-types/{result.Data!.Id}", result.Data)
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        });

        group.MapPut("/{id:int}", async (int id, [FromBody] OrdinanceType ordinanceType, OrdinanceTypeService service) =>
        {
            ordinanceType.Id = id;
            var result = await service.UpdateAsync(ordinanceType);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        });

        group.MapDelete("/{id:int}", async (int id, OrdinanceTypeService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        });
    }
}