using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class RfqProjectScopeEndpoints
{
    public static IEndpointRouteBuilder MapRfqProjectScopeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/rfq-project-scopes")
            .WithTags("RfqProjectScopes");

        group.MapGet("/", async (RfqProjectScopeService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetRfqProjectScopes");

        group.MapGet("/{id:int}", async (int id, RfqProjectScopeService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetRfqProjectScope");

        group.MapGet("/{id:int}/history", async (int id, RfqProjectScopeService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetRfqProjectScopeHistory");

        group.MapPost("/", async (RfqProjectScope entity, RfqProjectScopeService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/rfq-project-scopes/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateRfqProjectScope");

        group.MapPut("/{id:int}", async (int id, RfqProjectScope entity, RfqProjectScopeService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateRfqProjectScope");

        group.MapDelete("/{id:int}", async (int id, RfqProjectScopeService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteRfqProjectScope");

        return app;
    }
}
