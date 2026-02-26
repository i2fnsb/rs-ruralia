using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class RoadSubdivisionEndpoints
{
    public static IEndpointRouteBuilder MapRoadSubdivisionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/road-subdivisions")
            .WithTags("RoadSubdivisions");

        group.MapGet("/", async (RoadSubdivisionService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetRoadSubdivisions");

        group.MapGet("/{id:int}", async (int id, RoadSubdivisionService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetRoadSubdivision");

        group.MapGet("/{id:int}/history", async (int id, RoadSubdivisionService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetRoadSubdivisionHistory");

        group.MapPost("/", async (RoadSubdivision entity, RoadSubdivisionService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/road-subdivisions/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateRoadSubdivision");

        group.MapPut("/{id:int}", async (int id, RoadSubdivision entity, RoadSubdivisionService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateRoadSubdivision");

        group.MapDelete("/{id:int}", async (int id, RoadSubdivisionService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteRoadSubdivision");

        return app;
    }
}
