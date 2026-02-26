using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class CorrespondenceProfileEndpoints
{
    public static IEndpointRouteBuilder MapCorrespondenceProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/correspondence-profiles")
            .WithTags("CorrespondenceProfiles");

        group.MapGet("/", async (CorrespondenceProfileService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCorrespondenceProfiles");

        group.MapGet("/{id:int}", async (int id, CorrespondenceProfileService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCorrespondenceProfile");

        group.MapGet("/{id:int}/history", async (int id, CorrespondenceProfileService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCorrespondenceProfileHistory");

        group.MapPost("/", async (CorrespondenceProfile entity, CorrespondenceProfileService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/correspondence-profiles/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateCorrespondenceProfile");

        group.MapPut("/{id:int}", async (int id, CorrespondenceProfile entity, CorrespondenceProfileService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateCorrespondenceProfile");

        group.MapDelete("/{id:int}", async (int id, CorrespondenceProfileService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteCorrespondenceProfile");

        return app;
    }
}
