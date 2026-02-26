using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class PersonProfileVendorEndpoints
{
    public static IEndpointRouteBuilder MapPersonProfileVendorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/person-profile-vendors")
            .WithTags("PersonProfileVendors");

        group.MapGet("/", async (PersonProfileVendorService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetPersonProfileVendors");

        group.MapGet("/{id:int}", async (int id, PersonProfileVendorService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetPersonProfileVendor");

        group.MapGet("/{id:int}/history", async (int id, PersonProfileVendorService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetPersonProfileVendorHistory");

        group.MapPost("/", async (PersonProfileVendor entity, PersonProfileVendorService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/person-profile-vendors/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreatePersonProfileVendor");

        group.MapPut("/{id:int}", async (int id, PersonProfileVendor entity, PersonProfileVendorService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdatePersonProfileVendor");

        group.MapDelete("/{id:int}", async (int id, PersonProfileVendorService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeletePersonProfileVendor");

        return app;
    }
}
