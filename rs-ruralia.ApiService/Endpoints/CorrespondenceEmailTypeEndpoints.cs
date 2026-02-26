using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class CorrespondenceEmailTypeEndpoints
{
    public static IEndpointRouteBuilder MapCorrespondenceEmailTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/correspondence-email-types")
            .WithTags("CorrespondenceEmailTypes");

        group.MapGet("/", async (CorrespondenceEmailTypeService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCorrespondenceEmailTypes");

        group.MapGet("/{id:int}", async (int id, CorrespondenceEmailTypeService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCorrespondenceEmailType");

        group.MapGet("/{id:int}/history", async (int id, CorrespondenceEmailTypeService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCorrespondenceEmailTypeHistory");

        group.MapPost("/", async (CorrespondenceEmailType entity, CorrespondenceEmailTypeService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/correspondence-email-types/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateCorrespondenceEmailType");

        group.MapPut("/{id:int}", async (int id, CorrespondenceEmailType entity, CorrespondenceEmailTypeService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateCorrespondenceEmailType");

        group.MapDelete("/{id:int}", async (int id, CorrespondenceEmailTypeService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteCorrespondenceEmailType");

        return app;
    }
}
