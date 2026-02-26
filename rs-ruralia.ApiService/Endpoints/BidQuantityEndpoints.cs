using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class BidQuantityEndpoints
{
    public static IEndpointRouteBuilder MapBidQuantityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bid-quantities")
            .WithTags("BidQuantities");

        group.MapGet("/", async (BidQuantityService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetBidQuantities");

        group.MapGet("/{id:int}", async (int id, BidQuantityService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetBidQuantity");

        group.MapGet("/{id:int}/history", async (int id, BidQuantityService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetBidQuantityHistory");

        group.MapPost("/", async (BidQuantity entity, BidQuantityService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/bid-quantities/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateBidQuantity");

        group.MapPut("/{id:int}", async (int id, BidQuantity entity, BidQuantityService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateBidQuantity");

        group.MapDelete("/{id:int}", async (int id, BidQuantityService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteBidQuantity");

        return app;
    }
}
