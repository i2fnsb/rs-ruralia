using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class PersonHonorificEndpoints
{
    public static IEndpointRouteBuilder MapPersonHonorificEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/person-honorifics")
            .WithTags("PersonHonorifics");

        group.MapGet("/", async (PersonHonorificService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetPersonHonorifics");

        group.MapGet("/{id:int}", async (int id, PersonHonorificService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetPersonHonorific");

        group.MapGet("/{id:int}/history", async (int id, PersonHonorificService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetPersonHonorificHistory");

        group.MapPost("/", async (PersonHonorific entity, PersonHonorificService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/person-honorifics/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreatePersonHonorific");

        group.MapPut("/{id:int}", async (int id, PersonHonorific entity, PersonHonorificService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdatePersonHonorific");

        group.MapDelete("/{id:int}", async (int id, PersonHonorificService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeletePersonHonorific");

        return app;
    }
}
