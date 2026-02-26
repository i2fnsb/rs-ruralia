using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class SpecificationPayItemTypeEndpoints
{
    public static IEndpointRouteBuilder MapSpecificationPayItemTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/specification-pay-item-types")
            .WithTags("SpecificationPayItemTypes");

        group.MapGet("/", async (SpecificationPayItemTypeService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetSpecificationPayItemTypes");

        group.MapGet("/{id:int}", async (int id, SpecificationPayItemTypeService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetSpecificationPayItemType");

        group.MapGet("/{id:int}/history", async (int id, SpecificationPayItemTypeService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetSpecificationPayItemTypeHistory");

        group.MapPost("/", async (SpecificationPayItemType entity, SpecificationPayItemTypeService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/specification-pay-item-types/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateSpecificationPayItemType");

        group.MapPut("/{id:int}", async (int id, SpecificationPayItemType entity, SpecificationPayItemTypeService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateSpecificationPayItemType");

        group.MapDelete("/{id:int}", async (int id, SpecificationPayItemTypeService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteSpecificationPayItemType");

        return app;
    }
}
