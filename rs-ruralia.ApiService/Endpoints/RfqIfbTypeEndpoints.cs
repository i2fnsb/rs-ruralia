using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class RfqIfbTypeEndpoints
{
    public static IEndpointRouteBuilder MapRfqIfbTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/rfq-ifb-types")
            .WithTags("RfqIfbTypes");

        group.MapGet("/", async (RfqIfbTypeService service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetRfqIfbTypes");

        group.MapGet("/{id:int}", async (int id, RfqIfbTypeService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetRfqIfbType");

        group.MapGet("/{id:int}/history", async (int id, RfqIfbTypeService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetRfqIfbTypeHistory");

        group.MapPost("/", async (RfqIfbType entity, RfqIfbTypeService service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created($"/rfq-ifb-types/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateRfqIfbType");

        group.MapPut("/{id:int}", async (int id, RfqIfbType entity, RfqIfbTypeService service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateRfqIfbType");

        group.MapDelete("/{id:int}", async (int id, RfqIfbTypeService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteRfqIfbType");

        return app;
    }
}
