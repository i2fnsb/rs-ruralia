using rs_ruralia.Shared.Enums;

namespace rs_ruralia.Web.Services;

public class ViewModeService
{
    private ViewMode _currentViewMode = ViewMode.Public;
    private List<ViewMode> _availableViewModes = new();

    public event Action? OnViewModeChanged;

    public ViewMode CurrentViewMode => _currentViewMode;
    public IReadOnlyList<ViewMode> AvailableViewModes => _availableViewModes.AsReadOnly();

    public void SetAvailableViewModes(List<ViewMode> modes)
    {
        _availableViewModes = modes;
        
        // If current mode is not available, switch to highest available mode
        if (!_availableViewModes.Contains(_currentViewMode))
        {
            _currentViewMode = _availableViewModes.Any() 
                ? _availableViewModes.OrderByDescending(m => m).First() 
                : ViewMode.Public;
        }
        
        OnViewModeChanged?.Invoke();
    }

    public void SetViewMode(ViewMode mode)
    {
        if (_availableViewModes.Contains(mode))
        {
            _currentViewMode = mode;
            OnViewModeChanged?.Invoke();
        }
    }

    public bool IsAtLeast(ViewMode mode) => _currentViewMode >= mode;
    public bool IsExactly(ViewMode mode) => _currentViewMode == mode;
}
