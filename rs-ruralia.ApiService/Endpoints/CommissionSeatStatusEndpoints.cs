using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class CommissionSeatStatusEndpoints
{
    public static IEndpointRouteBuilder MapCommissionSeatStatusEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/commission-seat-statuses")
            .WithTags("CommissionSeatStatuses");

        group.MapGet("/", async (CommissionSeatStatusService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCommissionSeatStatuses");

        group.MapGet("/{id:int}", async (int id, CommissionSeatStatusService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCommissionSeatStatus");

        group.MapGet("/{id:int}/history", async (int id, CommissionSeatStatusService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCommissionSeatStatusHistory");

        group.MapPost("/", async (CommissionSeatStatus entity, CommissionSeatStatusService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/commission-seat-statuses/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateCommissionSeatStatus");

        group.MapPut("/{id:int}", async (int id, CommissionSeatStatus entity, CommissionSeatStatusService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateCommissionSeatStatus");

        group.MapDelete("/{id:int}", async (int id, CommissionSeatStatusService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteCommissionSeatStatus");

        return app;
    }
}
