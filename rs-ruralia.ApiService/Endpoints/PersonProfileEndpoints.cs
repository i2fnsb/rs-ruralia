using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class PersonProfileEndpoints
{
    public static IEndpointRouteBuilder MapPersonProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/person-profiles")
            .WithTags("PersonProfiles");

        group.MapGet("/", async (PersonProfileService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetPersonProfiles");

        group.MapGet("/{id:int}", async (int id, PersonProfileService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetPersonProfile");

        group.MapGet("/{id:int}/history", async (int id, PersonProfileService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetPersonProfileHistory");

        group.MapPost("/", async (PersonProfile entity, PersonProfileService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/person-profiles/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreatePersonProfile");

        group.MapPut("/{id:int}", async (int id, PersonProfile entity, PersonProfileService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdatePersonProfile");

        group.MapDelete("/{id:int}", async (int id, PersonProfileService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeletePersonProfile");

        return app;
    }
}
