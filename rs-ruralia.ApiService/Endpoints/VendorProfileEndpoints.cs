using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class VendorProfileEndpoints
{
    public static IEndpointRouteBuilder MapVendorProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/vendor-profiles")
            .WithTags("VendorProfiles");

        group.MapGet("/", async (VendorProfileService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetVendorProfiles");

        group.MapGet("/{id:int}", async (int id, VendorProfileService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetVendorProfile");

        group.MapGet("/{id:int}/history", async (int id, VendorProfileService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetVendorProfileHistory");

        group.MapPost("/", async (VendorProfile entity, VendorProfileService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/vendor-profiles/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateVendorProfile");

        group.MapPut("/{id:int}", async (int id, VendorProfile entity, VendorProfileService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateVendorProfile");

        group.MapDelete("/{id:int}", async (int id, VendorProfileService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteVendorProfile");

        return app;
    }
}
