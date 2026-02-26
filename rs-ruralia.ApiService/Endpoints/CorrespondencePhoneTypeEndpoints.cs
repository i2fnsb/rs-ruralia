using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class CorrespondencePhoneTypeEndpoints
{
    public static IEndpointRouteBuilder MapCorrespondencePhoneTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/correspondence-phone-types")
            .WithTags("CorrespondencePhoneTypes");

        group.MapGet("/", async (CorrespondencePhoneTypeService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCorrespondencePhoneTypes");

        group.MapGet("/{id:int}", async (int id, CorrespondencePhoneTypeService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCorrespondencePhoneType");

        group.MapGet("/{id:int}/history", async (int id, CorrespondencePhoneTypeService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCorrespondencePhoneTypeHistory");

        group.MapPost("/", async (CorrespondencePhoneType entity, CorrespondencePhoneTypeService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/correspondence-phone-types/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateCorrespondencePhoneType");

        group.MapPut("/{id:int}", async (int id, CorrespondencePhoneType entity, CorrespondencePhoneTypeService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateCorrespondencePhoneType");

        group.MapDelete("/{id:int}", async (int id, CorrespondencePhoneTypeService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteCorrespondencePhoneType");

        return app;
    }
}
