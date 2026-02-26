using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class CommissionSeatClassEndpoints
{
    public static IEndpointRouteBuilder MapCommissionSeatClassEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/commission-seat-classes")
            .WithTags("CommissionSeatClasses");

        group.MapGet("/", async (CommissionSeatClassService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCommissionSeatClasses");

        group.MapGet("/{id:int}", async (int id, CommissionSeatClassService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCommissionSeatClass");

        group.MapGet("/{id:int}/history", async (int id, CommissionSeatClassService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCommissionSeatClassHistory");

        group.MapPost("/", async (CommissionSeatClass entity, CommissionSeatClassService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/commission-seat-classes/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateCommissionSeatClass");

        group.MapPut("/{id:int}", async (int id, CommissionSeatClass entity, CommissionSeatClassService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateCommissionSeatClass");

        group.MapDelete("/{id:int}", async (int id, CommissionSeatClassService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteCommissionSeatClass");

        return app;
    }
}
