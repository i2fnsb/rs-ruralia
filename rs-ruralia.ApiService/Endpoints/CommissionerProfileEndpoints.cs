using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class CommissionerProfileEndpoints
{
    public static IEndpointRouteBuilder MapCommissionerProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/commissioner-profiles")
            .WithTags("CommissionerProfiles");

        group.MapGet("/", async (CommissionerProfileService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCommissionerProfiles");

        group.MapGet("/{id:int}", async (int id, CommissionerProfileService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCommissionerProfile");

        group.MapGet("/{id:int}/history", async (int id, CommissionerProfileService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCommissionerProfileHistory");

        group.MapPost("/", async (CommissionerProfile entity, CommissionerProfileService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/commissioner-profiles/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateCommissionerProfile");

        group.MapPut("/{id:int}", async (int id, CommissionerProfile entity, CommissionerProfileService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateCommissionerProfile");

        group.MapDelete("/{id:int}", async (int id, CommissionerProfileService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteCommissionerProfile");

        return app;
    }
}
