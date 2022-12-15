#if TOOLS
using Godot;

namespace BetterFileSystem;

[Tool]
public partial class DirectoryFilter : Node
{
    [Export] public IncludeType includeType;
    [Export] public DirectoryFilterType filterType;
    [Export] public string filterString = "";
    [Export] public FilterState whenToFilter;
}

#endif