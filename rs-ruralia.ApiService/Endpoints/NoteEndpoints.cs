using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace rs_ruralia.ApiService.Endpoints;

public static class NoteEndpoints
{
    public static IEndpointRouteBuilder MapNoteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/notes")
            .WithTags("Notes");

        // Get notes by entity type and ID
        group.MapGet("/{entityType}/{entityId:int}", async (string entityType, int entityId, NoteService service) =>
        {
            var result = await service.GetByEntityAsync(entityType, entityId);
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetNotesByEntity");

        // Get specific note by ID
        group.MapGet("/{id:int}", async (int id, NoteService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetNote");

        // Get note history
        group.MapGet("/{id:int}/history", async (int id, NoteService service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("GetNoteHistory");

        // Create note
        group.MapPost("/", async (Note note, NoteService service) =>
        {
            var result = await service.CreateAsync(note);
            return result.IsSuccess
                ? Results.Created($"/notes/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("CreateNote");

        // Update note
        group.MapPut("/{id:int}", async (int id, Note note, NoteService service) =>
        {
            note.Id = id;
            var result = await service.UpdateAsync(note);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("UpdateNote");

        // Delete note
        group.MapDelete("/{id:int}", async (int id, NoteService service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("DeleteNote");

        return app;
    }
}
