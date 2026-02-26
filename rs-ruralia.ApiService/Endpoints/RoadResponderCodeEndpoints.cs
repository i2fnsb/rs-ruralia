using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class RoadResponderCodeEndpoints
{
    public static IEndpointRouteBuilder MapRoadResponderCodeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/road-responder-codes")
            .WithTags("RoadResponderCodes");

        group.MapGet("/", async (RoadResponderCodeService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetRoadResponderCodes");

        group.MapGet("/{id:int}", async (int id, RoadResponderCodeService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetRoadResponderCode");

        group.MapGet("/{id:int}/history", async (int id, RoadResponderCodeService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetRoadResponderCodeHistory");

        group.MapPost("/", async (RoadResponderCode entity, RoadResponderCodeService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/road-responder-codes/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateRoadResponderCode");

        group.MapPut("/{id:int}", async (int id, RoadResponderCode entity, RoadResponderCodeService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateRoadResponderCode");

        group.MapDelete("/{id:int}", async (int id, RoadResponderCodeService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteRoadResponderCode");

        return app;
    }
}
