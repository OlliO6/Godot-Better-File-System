#if TOOLS
using Godot;

namespace BetterFileSystem;

[Tool]
public partial class IconButton : Button
{
    [Export] public bool defaultPressed;

    private string _icon;
    private bool firstFrame = true;

    [Export]
    private string _EditorIcon
    {
        get => _icon;
        set
        {
            _icon = value;

            if (value is not "")
            {
                if (Plugin.instance is null)
                    Icon = GetThemeIcon(value, Plugin.IconThemeType);
                else
                    Icon = Plugin.instance.EditorTheme.GetIcon(value, Plugin.IconThemeType);
            }
        }
    }


    public override void _Process(double delta)
    {
        if (!firstFrame) return;

        _EditorIcon = _icon;

        firstFrame = false;

        if (ToggleMode)
        {
            SetPressedNoSignal(defaultPressed);
            EmitSignal(SignalName.Toggled, defaultPressed);
            return;
        }

        if (defaultPressed) EmitSignal(SignalName.Pressed);
    }
}

#endif