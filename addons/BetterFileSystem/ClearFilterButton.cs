#if TOOLS
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace BetterFileSystem;

[Tool]
public partial class ClearFilterButton : IconButton
{
    public Node filters;

    public override void _Ready()
    {
        Pressed += () => Clear(null);
    }

    public void Clear(FilterButton dont)
    {
        foreach (FilterButton filterButton in GetAllFilterButtons())
        {
            if (filterButton != dont && filterButton.autoDisable)
                filterButton.Reset();
        }

        Plugin.instance.filterBar.Text = "";

        Plugin.instance.ManuelUpdate();
    }

    public IEnumerable<FilterButton> GetAllFilterButtons()
    {
        return GetAllChildren(filters).OfType<FilterButton>();
    }

    private List<Node> GetAllChildren(Node parent)
    {
        List<Node> children = new();

        for (int i = 0; i < parent.GetChildCount(); i++)
        {
            Node child = parent.GetChild(i);
            children.Add(child);
            children.AddRange(GetAllChildren(child));
        }

        return children;
    }
}

#endif