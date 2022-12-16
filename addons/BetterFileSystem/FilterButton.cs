#if TOOLS
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace BetterFileSystem;

[Tool]
public partial class FilterButton : IconButton
{
    [Export] public string searchFilter = "";
    [Export] public bool autoDisable = true, autoExpandAll, autoCollapseAll;
    [Export] private string autoSelectPath = "";

    public override void _Ready()
    {
        Toggled += ToggledCallback;
    }

    private void ToggledCallback(bool toggled)
    {
        if (Owner is null || Owner.GetParent() is Viewport || Plugin.instance is null) return;

        if (toggled)
            Activate();
        else
            Deactivate();

        Plugin.instance.ManuelUpdate();
    }

    public void Activate()
    {
        Plugin betterFileSystem = Plugin.instance;

        if (autoDisable) betterFileSystem.clearButton.Clear(this);

        if (searchFilter != "") betterFileSystem.SetSearchFilter(searchFilter);


        GetFilterNodes(out IEnumerable<FileFilter> fileFilters, out IEnumerable<DirectoryFilter> directoryFilters);

        AddAndRemoveFilters(fileFilters, directoryFilters, FilterState.OnEnabled);
    }

    public void Deactivate()
    {
        Plugin betterFileSystem = Plugin.instance;

        GetFilterNodes(out IEnumerable<FileFilter> fileFilters, out IEnumerable<DirectoryFilter> directoryFilters);
        AddAndRemoveFilters(fileFilters, directoryFilters, FilterState.OnDisabled);
    }

    public void Reset()
    {
        ButtonPressed = defaultPressed;
    }

    private void GetFilterNodes(out IEnumerable<FileFilter> fileFilters, out IEnumerable<DirectoryFilter> directoryFilters)
    {
        Godot.Collections.Array<Node> children = GetChildren();

        fileFilters = children.OfType<FileFilter>();
        directoryFilters = children.OfType<DirectoryFilter>();
    }

    private void AddAndRemoveFilters(IEnumerable<FileFilter> fileFilters, IEnumerable<DirectoryFilter> directoryFilters, FilterState whenToAdd)
    {
        Plugin betterFileSystem = Plugin.instance;

        foreach (var filter in fileFilters)
            betterFileSystem.fileFilters.Remove(filter);

        foreach (var filter in directoryFilters)
            betterFileSystem.directoryFilters.Remove(filter);

        var fileFiltersToAdd = fileFilters.Where((FileFilter filter) => { return filter.whenToFilter == whenToAdd; });
        var directoryFiltersToAdd = directoryFilters.Where((DirectoryFilter filter) => { return filter.whenToFilter == whenToAdd; });


        foreach (var filter in fileFiltersToAdd)
            betterFileSystem.fileFilters.Add(filter);

        foreach (var filter in directoryFiltersToAdd)
            betterFileSystem.directoryFilters.Add(filter);
    }
}

#endif