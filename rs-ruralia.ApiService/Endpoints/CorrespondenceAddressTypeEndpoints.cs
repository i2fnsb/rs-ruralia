using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class CorrespondenceAddressTypeEndpoints
{
    public static IEndpointRouteBuilder MapCorrespondenceAddressTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/correspondence-address-types")
            .WithTags("CorrespondenceAddressTypes");

        group.MapGet("/", async (CorrespondenceAddressTypeService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCorrespondenceAddressTypes");

        group.MapGet("/{id:int}", async (int id, CorrespondenceAddressTypeService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCorrespondenceAddressType");

        group.MapGet("/{id:int}/history", async (int id, CorrespondenceAddressTypeService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCorrespondenceAddressTypeHistory");

        group.MapPost("/", async (CorrespondenceAddressType entity, CorrespondenceAddressTypeService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/correspondence-address-types/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateCorrespondenceAddressType");

        group.MapPut("/{id:int}", async (int id, CorrespondenceAddressType entity, CorrespondenceAddressTypeService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateCorrespondenceAddressType");

        group.MapDelete("/{id:int}", async (int id, CorrespondenceAddressTypeService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteCorrespondenceAddressType");

        return app;
    }
}
