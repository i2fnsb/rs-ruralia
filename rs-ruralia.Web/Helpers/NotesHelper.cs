using MudBlazor;
using rs_ruralia.Shared.Models;
using rs_ruralia.Web.Components.Dialogs;

namespace rs_ruralia.Web.Helpers;

/// <summary>
/// Provides helper methods for managing notes across entity pages
/// </summary>
public static class NotesHelper
{
    /// <summary>
    /// Prompts the user to add a note after editing an entity
    /// </summary>
    public static async Task PromptForEditNoteAsync(
        IDialogService dialogService,
        string entityDisplayName,
        Action<int> onNotesCountChanged,
        Func<int, string, Task> createNote,
        int entityId,
        string? userEmail)
    {
        var parameters = new DialogParameters
        {
            { "EntityName", entityDisplayName }
        };

        var options = new DialogOptions 
        { 
            CloseButton = true, 
            MaxWidth = MaxWidth.Medium, 
            FullWidth = true 
        };

        var dialog = await dialogService.ShowAsync<AddEditNoteDialog>("Add Note About Changes", parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled && result.Data is string noteText && !string.IsNullOrWhiteSpace(noteText))
        {
            await createNote(entityId, noteText);
        }
    }

    /// <summary>
    /// Creates a note for a specific entity
    /// </summary>
    public static async Task<bool> CreateNoteAsync(
        Note note,
        Func<Note, CancellationToken, Task<Result<Note>?>> createNoteApi,
        Action? onSuccess = null)
    {
        try
        {
            var result = await createNoteApi(note, CancellationToken.None);

            if (result?.IsSuccess == true)
            {
                onSuccess?.Invoke();
                return true;
            }

            return false;
        }
        catch (Exception)
        {
            // Silently fail - note creation is optional
            return false;
        }
    }

    /// <summary>
    /// Creates a note object for a road entity
    /// </summary>
    public static Note CreateRoadNote(int roadId, string noteText, string? userEmail)
    {
        return new Note
        {
            NoteText = noteText,
            RoadId = roadId,
            ModifiedBy = userEmail ?? "Unknown User",
            ValidFrom = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a note object for a service area entity
    /// </summary>
    public static Note CreateServiceAreaNote(int serviceAreaId, string noteText, string? userEmail)
    {
        return new Note
        {
            NoteText = noteText,
            ServiceAreaId = serviceAreaId,
            ModifiedBy = userEmail ?? "Unknown User",
            ValidFrom = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a note object for an ordinance entity
    /// </summary>
    public static Note CreateOrdinanceNote(int ordinanceId, string noteText, string? userEmail)
    {
        return new Note
        {
            NoteText = noteText,
            OrdinanceId = ordinanceId,
            ModifiedBy = userEmail ?? "Unknown User",
            ValidFrom = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a note object for a commissioner profile entity
    /// </summary>
    public static Note CreateCommissionerProfileNote(int commissionerProfileId, string noteText, string? userEmail)
    {
        return new Note
        {
            NoteText = noteText,
            CommissionerProfileId = commissionerProfileId,
            ModifiedBy = userEmail ?? "Unknown User",
            ValidFrom = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a note object for a commission seat entity
    /// </summary>
    public static Note CreateCommissionSeatNote(int commissionSeatId, string noteText, string? userEmail)
    {
        return new Note
        {
            NoteText = noteText,
            CommissionSeatId = commissionSeatId,
            ModifiedBy = userEmail ?? "Unknown User",
            ValidFrom = DateTime.UtcNow
        };
    }
}
