using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class RfqVendorDistributionEndpoints
{
    public static IEndpointRouteBuilder MapRfqVendorDistributionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/rfq-vendor-distributions")
            .WithTags("RfqVendorDistributions");

        group.MapGet("/", async (RfqVendorDistributionService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetRfqVendorDistributions");

        group.MapGet("/{id:int}", async (int id, RfqVendorDistributionService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetRfqVendorDistribution");

        group.MapGet("/{id:int}/history", async (int id, RfqVendorDistributionService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetRfqVendorDistributionHistory");

        group.MapPost("/", async (RfqVendorDistribution entity, RfqVendorDistributionService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/rfq-vendor-distributions/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateRfqVendorDistribution");

        group.MapPut("/{id:int}", async (int id, RfqVendorDistribution entity, RfqVendorDistributionService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateRfqVendorDistribution");

        group.MapDelete("/{id:int}", async (int id, RfqVendorDistributionService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteRfqVendorDistribution");

        return app;
    }
}
