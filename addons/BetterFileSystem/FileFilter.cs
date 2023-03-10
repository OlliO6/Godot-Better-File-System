#if TOOLS
using Godot;

namespace BetterFileSystem
{
    [Tool]
    public partial class FileFilter : Node
    {
        [Export] public IncludeType includeType;
        [Export] public FileFilterType filterType;
        [Export] public string filterString = "";
        [Export] public FilterState whenToFilter;
    }
}

#endif