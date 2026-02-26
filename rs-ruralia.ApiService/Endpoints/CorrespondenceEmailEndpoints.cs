using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class CorrespondenceEmailEndpoints
{
    public static IEndpointRouteBuilder MapCorrespondenceEmailEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/correspondence-emails")
            .WithTags("CorrespondenceEmails");

        group.MapGet("/", async (CorrespondenceEmailService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCorrespondenceEmails");

        group.MapGet("/{id:int}", async (int id, CorrespondenceEmailService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCorrespondenceEmail");

        group.MapGet("/{id:int}/history", async (int id, CorrespondenceEmailService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCorrespondenceEmailHistory");

        group.MapPost("/", async (CorrespondenceEmail entity, CorrespondenceEmailService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/correspondence-emails/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateCorrespondenceEmail");

        group.MapPut("/{id:int}", async (int id, CorrespondenceEmail entity, CorrespondenceEmailService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateCorrespondenceEmail");

        group.MapDelete("/{id:int}", async (int id, CorrespondenceEmailService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteCorrespondenceEmail");

        return app;
    }
}
