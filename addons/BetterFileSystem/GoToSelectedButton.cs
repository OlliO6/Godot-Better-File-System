#if TOOLS
using Godot;

namespace BetterFileSystem;

[Tool]
public partial class GoToSelectedButton : IconButton
{
    public override void _Ready()
    {
        Pressed += () =>
        {
            Plugin.instance.GoToSelected();
            Plugin.instance.ManuelUpdate();
        };
    }
}

#endif