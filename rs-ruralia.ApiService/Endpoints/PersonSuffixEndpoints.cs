using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class PersonSuffixEndpoints
{
    public static IEndpointRouteBuilder MapPersonSuffixEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/person-suffixes")
            .WithTags("PersonSuffixes");

        group.MapGet("/", async (PersonSuffixService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetPersonSuffixes");

        group.MapGet("/{id:int}", async (int id, PersonSuffixService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetPersonSuffix");

        group.MapGet("/{id:int}/history", async (int id, PersonSuffixService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetPersonSuffixHistory");

        group.MapPost("/", async (PersonSuffix entity, PersonSuffixService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/person-suffixes/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreatePersonSuffix");

        group.MapPut("/{id:int}", async (int id, PersonSuffix entity, PersonSuffixService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdatePersonSuffix");

        group.MapDelete("/{id:int}", async (int id, PersonSuffixService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeletePersonSuffix");

        return app;
    }
}
