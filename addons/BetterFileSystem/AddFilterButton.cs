#if TOOLS
using Godot;

namespace BetterFileSystem;

[Tool]
public partial class AddFilterButton : IconButton
{
    PackedScene filtersScene = GD.Load<PackedScene>("res://addons/BetterFileSystem/Filters.tscn");

    private void OnPressed()
    {
        Plugin.instance.GetEditorInterface().OpenSceneFromPath("res://addons/BetterFileSystem/Filters.tscn");
    }
}

#endif