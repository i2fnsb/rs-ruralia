using MudBlazor;
using rs_ruralia.Web.Components.Dialogs;
using Microsoft.AspNetCore.Components;

namespace rs_ruralia.Web.Helpers;

/// <summary>
/// Provides helper methods for create dialogs
/// </summary>
public static class CreateDialogHelper
{
    /// <summary>
    /// Opens a create dialog and handles the result
    /// </summary>
    public static async Task<TEntity?> OpenCreateDialogAsync<TEntity>(
        IDialogService dialogService,
        string entityDisplayName,
        string description,
        Func<TEntity, Task<(bool Success, string? ErrorMessage, string[]? Errors)>> onCreate,
        RenderFragment<TEntity> formContent,
        bool autoSetModifiedBy = true,
        string dialogTitle = "Create New {0}")
        where TEntity : class, new()
    {
        var parameters = new DialogParameters<CreateEntityDialog<TEntity>>
        {
            { x => x.EntityDisplayName, entityDisplayName },
            { x => x.Description, description },
            { x => x.OnCreate, onCreate },
            { x => x.AutoSetModifiedBy, autoSetModifiedBy },
            { x => x.FormContent, formContent }
        };

        var options = new DialogOptions 
        { 
            CloseButton = true, 
            MaxWidth = MaxWidth.Large, 
            FullWidth = true,
            CloseOnEscapeKey = true
        };

        var title = string.Format(dialogTitle, entityDisplayName);
        var dialog = await dialogService.ShowAsync<CreateEntityDialog<TEntity>>(title, parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled && result.Data is TEntity createdEntity)
        {
            return createdEntity;
        }

        return null;
    }

    /// <summary>
    /// Handles post-creation actions: reload data, select item, update URL, show success
    /// </summary>
    public static async Task<TEntity?> HandlePostCreateAsync<TEntity>(
        TEntity? createdEntity,
        Func<Task> reloadData,
        Func<int, TEntity?> findEntityById,
        Func<TEntity, int> getEntityId,
        Func<TEntity, string> getDisplayText,
        Action<TEntity?> setSelectedEntity,
        Func<TEntity, Task>? onEntitySelected = null,
        Action<string, int>? updateUrl = null,
        Action<string>? setSuccessMessage = null)
        where TEntity : class
    {
        if (createdEntity == null)
            return null;

        // Reload data to include the new entity
        await reloadData();

        // Find and select the newly created entity
        var entityId = getEntityId(createdEntity);
        var selectedEntity = findEntityById(entityId);
        
        if (selectedEntity != null)
        {
            setSelectedEntity(selectedEntity);

            // Execute optional post-selection callback
            if (onEntitySelected != null)
            {
                await onEntitySelected(selectedEntity);
            }

            // Update URL if callback provided
            updateUrl?.Invoke("id", entityId);

            // Set success message if callback provided
            var displayText = getDisplayText(createdEntity);
            setSuccessMessage?.Invoke($"'{displayText}' created successfully!");

            return selectedEntity;
        }

        return null;
    }
}
