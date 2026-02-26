using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class CorrespondenceAddressEndpoints
{
    public static IEndpointRouteBuilder MapCorrespondenceAddressEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/correspondence-addresses")
            .WithTags("CorrespondenceAddresses");

        group.MapGet("/", async (CorrespondenceAddressService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCorrespondenceAddresses");

        group.MapGet("/{id:int}", async (int id, CorrespondenceAddressService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCorrespondenceAddress");

        group.MapGet("/{id:int}/history", async (int id, CorrespondenceAddressService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCorrespondenceAddressHistory");

        group.MapPost("/", async (CorrespondenceAddress entity, CorrespondenceAddressService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/correspondence-addresses/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateCorrespondenceAddress");

        group.MapPut("/{id:int}", async (int id, CorrespondenceAddress entity, CorrespondenceAddressService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateCorrespondenceAddress");

        group.MapDelete("/{id:int}", async (int id, CorrespondenceAddressService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteCorrespondenceAddress");

        return app;
    }
}
