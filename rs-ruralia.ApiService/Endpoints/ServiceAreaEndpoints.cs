using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class ServiceAreaEndpoints
{
    public static IEndpointRouteBuilder MapServiceAreaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/service-areas")
            .WithTags("ServiceAreas");

        group.MapGet("/", async (ServiceAreaService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetServiceAreas");

        group.MapGet("/{id:int}", async (int id, ServiceAreaService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetServiceArea");

        group.MapGet("/{id:int}/history", async (int id, ServiceAreaService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetServiceAreaHistory");

        group.MapGet("/{id:int}/history/at/{asOfDate:datetime}", async (int id, DateTime asOfDate, ServiceAreaService service) =>
        {
            var result = await service.GetHistoryByIdAtPointInTimeAsync(id, asOfDate);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetServiceAreaHistoryAtPointInTime");

        group.MapGet("/{id:int}/history/between", async (int id, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, ServiceAreaService service) =>
        {
            var result = await service.GetHistoryBetweenDatesAsync(id, startDate, endDate);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetServiceAreaHistoryBetween");

        group.MapGet("/history", async (ServiceAreaService service) =>
        {
            var result = await service.GetAllHistoryAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetAllServiceAreaHistory");

        group.MapPost("/", async (ServiceArea serviceArea, ServiceAreaService service) =>
        {
            var result = await service.CreateAsync(serviceArea);
            return result.IsSuccess 
                ? Results.Created($"/service-areas/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateServiceArea");

        group.MapPut("/{id:int}", async (int id, ServiceArea serviceArea, ServiceAreaService service) =>
        {
            serviceArea.Id = id;
            var result = await service.UpdateAsync(serviceArea);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateServiceArea");

        group.MapDelete("/{id:int}", async (int id, ServiceAreaService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteServiceArea");

        return app;
    }
}