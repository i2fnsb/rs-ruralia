using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class CommissionSeatTypeEndpoints
{
    public static IEndpointRouteBuilder MapCommissionSeatTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/commission-seat-types")
            .WithTags("CommissionSeatTypes");

        group.MapGet("/", async (CommissionSeatTypeService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCommissionSeatTypes");

        group.MapGet("/{id:int}", async (int id, CommissionSeatTypeService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCommissionSeatType");

        group.MapGet("/{id:int}/history", async (int id, CommissionSeatTypeService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCommissionSeatTypeHistory");

        group.MapPost("/", async (CommissionSeatType commissionSeatType, CommissionSeatTypeService service) =>
        {
            var result = await service.CreateAsync(commissionSeatType);
            return result.IsSuccess 
                ? Results.Created($"/commission-seat-types/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateCommissionSeatType");

        group.MapPut("/{id:int}", async (int id, CommissionSeatType commissionSeatType, CommissionSeatTypeService service) =>
        {
            commissionSeatType.Id = id;
            var result = await service.UpdateAsync(commissionSeatType);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateCommissionSeatType");

        group.MapDelete("/{id:int}", async (int id, CommissionSeatTypeService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteCommissionSeatType");

        return app;
    }
}