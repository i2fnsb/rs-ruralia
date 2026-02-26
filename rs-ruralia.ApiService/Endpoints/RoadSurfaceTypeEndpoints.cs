using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class RoadSurfaceTypeEndpoints
{
    public static IEndpointRouteBuilder MapRoadSurfaceTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/road-surface-types")
            .WithTags("RoadSurfaceTypes");

        group.MapGet("/", async (RoadSurfaceTypeService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetRoadSurfaceTypes");

        group.MapGet("/{id:int}", async (int id, RoadSurfaceTypeService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetRoadSurfaceType");

        group.MapGet("/{id:int}/history", async (int id, RoadSurfaceTypeService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetRoadSurfaceTypeHistory");

        group.MapPost("/", async (RoadSurfaceType entity, RoadSurfaceTypeService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/road-surface-types/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateRoadSurfaceType");

        group.MapPut("/{id:int}", async (int id, RoadSurfaceType entity, RoadSurfaceTypeService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateRoadSurfaceType");

        group.MapDelete("/{id:int}", async (int id, RoadSurfaceTypeService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteRoadSurfaceType");

        return app;
    }
}
