using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class RoadEndpoints
{
    public static IEndpointRouteBuilder MapRoadEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/roads")
            .WithTags("Roads");

        group.MapGet("/", async (RoadService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetRoads");

        group.MapGet("/{id:int}", async (int id, RoadService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetRoad");

        group.MapGet("/{id:int}/history", async (int id, RoadService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetRoadHistory");

        group.MapPost("/", async (Road entity, RoadService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/roads/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateRoad");

        group.MapPut("/{id:int}", async (int id, Road entity, RoadService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateRoad");

        group.MapDelete("/{id:int}", async (int id, RoadService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteRoad");

        group.MapGet("/{id:int}/subdivisions", async (int id, RoadService service) =>
        {
            var result = await service.GetSubdivisionsByRoadIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetSubdivisionsByRoad");

        group.MapPost("/{roadId:int}/subdivisions/{subdivisionId:int}", async (int roadId, int subdivisionId, [FromBody] RoadRoadSubdivision entity, RoadService service) =>
        {
            var result = await service.AddSubdivisionToRoadAsync(roadId, subdivisionId, entity.ModifiedBy);
            return result.IsSuccess 
                ? Results.Created($"/roads/{roadId}/subdivisions/{subdivisionId}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("AddSubdivisionToRoad");

        group.MapDelete("/{roadId:int}/subdivisions/{subdivisionId:int}", async (int roadId, int subdivisionId, RoadService service) =>
        {
            var result = await service.RemoveSubdivisionFromRoadAsync(roadId, subdivisionId);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("RemoveSubdivisionFromRoad");

        return app;
    }
}
