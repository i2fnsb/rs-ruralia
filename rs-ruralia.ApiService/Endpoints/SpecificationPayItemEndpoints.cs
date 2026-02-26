using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class SpecificationPayItemEndpoints
{
    public static IEndpointRouteBuilder MapSpecificationPayItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/specification-pay-items")
            .WithTags("SpecificationPayItems");

        group.MapGet("/", async (SpecificationPayItemService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetSpecificationPayItems");

        group.MapGet("/{id:int}", async (int id, SpecificationPayItemService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetSpecificationPayItem");

        group.MapGet("/{id:int}/history", async (int id, SpecificationPayItemService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetSpecificationPayItemHistory");

        group.MapPost("/", async (SpecificationPayItem entity, SpecificationPayItemService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/specification-pay-items/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateSpecificationPayItem");

        group.MapPut("/{id:int}", async (int id, SpecificationPayItem entity, SpecificationPayItemService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateSpecificationPayItem");

        group.MapDelete("/{id:int}", async (int id, SpecificationPayItemService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteSpecificationPayItem");

        return app;
    }
}
