using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class CommissionerStatusEndpoints
{
    public static IEndpointRouteBuilder MapCommissionerStatusEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/commissioner-statuses")
            .WithTags("CommissionerStatuses");

        group.MapGet("/", async (CommissionerStatusService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCommissionerStatuses");

        group.MapGet("/{id:int}", async (int id, CommissionerStatusService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCommissionerStatus");

        group.MapGet("/{id:int}/history", async (int id, CommissionerStatusService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetCommissionerStatusHistory");

        group.MapPost("/", async (CommissionerStatus entity, CommissionerStatusService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/commissioner-statuses/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateCommissionerStatus");

        group.MapPut("/{id:int}", async (int id, CommissionerStatus entity, CommissionerStatusService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateCommissionerStatus");

        group.MapDelete("/{id:int}", async (int id, CommissionerStatusService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteCommissionerStatus");

        return app;
    }
}
