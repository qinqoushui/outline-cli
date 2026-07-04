using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OutlineUi.Models;

public class DocumentNode : ViewModelBase
{
    private bool _isSelected;
    private bool _isExpanded = true;
    private bool _isVisible = true;

    public string? Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public NodeType Type { get; set; }

    public ObservableCollection<DocumentNode> Children { get; set; } = [];
    
    public DocumentNode? Parent { get; set; }
    
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }
    
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }
}

public enum NodeType
{
    Collection,
    Document
}
