using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class CommissionSeatEndpoints
{
    public static IEndpointRouteBuilder MapCommissionSeatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/commission-seats")
            .WithTags("CommissionSeats");

        group.MapGet("/", async (CommissionSeatService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCommissionSeats");

        group.MapGet("/{id:int}", async (int id, CommissionSeatService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCommissionSeat");

        group.MapGet("/service-area/{serviceAreaId:int}", async (int serviceAreaId, CommissionSeatService service) =>
        {
            var result = await service.GetByServiceAreaIdAsync(serviceAreaId);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCommissionSeatsByServiceArea");

        group.MapGet("/{id:int}/history", async (int id, CommissionSeatService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCommissionSeatHistory");

        group.MapPost("/", async (CommissionSeat commissionSeat, CommissionSeatService service) =>
        {
            var result = await service.CreateAsync(commissionSeat);
            return result.IsSuccess 
                ? Results.Created($"/commission-seats/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateCommissionSeat");

        group.MapPut("/{id:int}", async (int id, CommissionSeat commissionSeat, CommissionSeatService service) =>
        {
            commissionSeat.Id = id;
            var result = await service.UpdateAsync(commissionSeat);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateCommissionSeat");

        group.MapDelete("/{id:int}", async (int id, CommissionSeatService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteCommissionSeat");

        return app;
    }
}