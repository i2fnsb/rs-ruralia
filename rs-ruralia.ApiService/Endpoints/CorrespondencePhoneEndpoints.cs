using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class CorrespondencePhoneEndpoints
{
    public static IEndpointRouteBuilder MapCorrespondencePhoneEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/correspondence-phones")
            .WithTags("CorrespondencePhones");

        group.MapGet("/", async (CorrespondencePhoneService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCorrespondencePhones");

        group.MapGet("/{id:int}", async (int id, CorrespondencePhoneService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCorrespondencePhone");

        group.MapGet("/{id:int}/history", async (int id, CorrespondencePhoneService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCorrespondencePhoneHistory");

        group.MapPost("/", async (CorrespondencePhone entity, CorrespondencePhoneService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/correspondence-phones/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateCorrespondencePhone");

        group.MapPut("/{id:int}", async (int id, CorrespondencePhone entity, CorrespondencePhoneService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateCorrespondencePhone");

        group.MapDelete("/{id:int}", async (int id, CorrespondencePhoneService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteCorrespondencePhone");

        return app;
    }
}
