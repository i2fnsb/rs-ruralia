using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class VendorVinCodeEndpoints
{
    public static IEndpointRouteBuilder MapVendorVinCodeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/vendor-vin-codes")
            .WithTags("VendorVinCodes");

        group.MapGet("/", async (VendorVinCodeService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetVendorVinCodes");

        group.MapGet("/{id:int}", async (int id, VendorVinCodeService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetVendorVinCode");

        group.MapGet("/{id:int}/history", async (int id, VendorVinCodeService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetVendorVinCodeHistory");

        group.MapPost("/", async (VendorVinCode entity, VendorVinCodeService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/vendor-vin-codes/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateVendorVinCode");

        group.MapPut("/{id:int}", async (int id, VendorVinCode entity, VendorVinCodeService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateVendorVinCode");

        group.MapDelete("/{id:int}", async (int id, VendorVinCodeService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteVendorVinCode");

        return app;
    }
}
