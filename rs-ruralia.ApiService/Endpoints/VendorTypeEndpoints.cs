using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class VendorTypeEndpoints
{
    public static IEndpointRouteBuilder MapVendorTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/vendor-types")
            .WithTags("VendorTypes");

        group.MapGet("/", async (VendorTypeService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetVendorTypes");

        group.MapGet("/{id:int}", async (int id, VendorTypeService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetVendorType");

        group.MapGet("/{id:int}/history", async (int id, VendorTypeService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetVendorTypeHistory");

        group.MapPost("/", async (VendorType entity, VendorTypeService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/vendor-types/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateVendorType");

        group.MapPut("/{id:int}", async (int id, VendorType entity, VendorTypeService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateVendorType");

        group.MapDelete("/{id:int}", async (int id, VendorTypeService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteVendorType");

        return app;
    }
}
