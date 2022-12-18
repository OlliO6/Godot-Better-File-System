#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace BetterFileSystem;

[Tool]
public partial class Plugin : EditorPlugin
{
    public const string IconThemeType = "EditorIcons";

    public static Plugin instance;

    private bool hidingEmptyDirectories, isScanning;

    private PackedScene firstRowExtrasScene = GD.Load<PackedScene>("res://addons/BetterFileSystem/FirstRowExtras.tscn");
    private PackedScene secondRowExtrasScene = GD.Load<PackedScene>("res://addons/BetterFileSystem/SecondRowExtras.tscn");
    private PackedScene sideBarScene = GD.Load<PackedScene>("res://addons/BetterFileSystem/SideBar.tscn");

    private FileSystemDock fileSystemDock;
    public EditorFileSystem fileSystem;

    public LineEdit filterBar;
    public ClearFilterButton clearButton;

    public List<FileFilter> fileFilters = new();
    public List<DirectoryFilter> directoryFilters = new();

    private Tree fileTree;
    private Node row;
    private Node firstRowExtras;
    private Node secondRowExtras;
    private Node sideBar;
    private HBoxContainer hBox;
    private Button buttonToHide;
    private Button rescanButton;

    private Dictionary<string, TreeItem> favorites = new();

    public Theme EditorTheme => GetEditorInterface().GetBaseControl().Theme;

    public override void _EnterTree()
    {
        instance = this;

        fileSystemDock = GetEditorInterface().GetFileSystemDock();
        fileSystem = GetEditorInterface().GetResourceFilesystem();
        row = fileSystemDock.GetChild(3);
        fileTree = row.GetChild<Tree>(0);

        hBox = new();
        row.RemoveChild(fileTree);
        row.AddChild(hBox);
        row.MoveChild(hBox, 0);

        sideBar = sideBarScene.Instantiate();
        hBox.AddChild(sideBar);
        hBox.AddChild(fileTree);
        fileTree.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;

        Node vBox = fileSystemDock.GetChild(0);
        Node firstRow = vBox.GetChild(0);
        Node secondRow = vBox.GetChild(1);

        firstRowExtras = firstRowExtrasScene.Instantiate();
        firstRow.AddChild(firstRowExtras);
        firstRow.MoveChild(firstRowExtras, 0);

        secondRowExtras = secondRowExtrasScene.Instantiate();
        secondRow.AddChild(secondRowExtras);
        secondRow.MoveChild(secondRowExtras, 0);

        filterBar = secondRow.GetChild<LineEdit>(1);

        rescanButton = firstRow.GetChild<Button>(4); // Hidden feature
        rescanButton.Flat = true;
        rescanButton.Show();

        buttonToHide = firstRow.GetChild<Button>(5);
        buttonToHide.Hide();

        clearButton = secondRowExtras.GetChild<ClearFilterButton>(0);
        clearButton.filters = sideBar;

        rescanButton.Pressed += () => isScanning = true;
        filterBar.TextChanged += (_) => CallDeferred(MethodName.Update);

        secondRowExtras.GetNode<Button>("HideEmpty").Toggled += ToggleHideEmpty;
        secondRowExtras.GetNode<Button>("Collapse").Pressed += CollapseAll;
        secondRowExtras.GetNode<Button>("Expand").Pressed += ExpandAll;

        ManuelUpdate();
    }

    public override void _ExitTree()
    {
        instance = null;

        firstRowExtras.QueueFree();
        secondRowExtras.QueueFree();
        sideBar.QueueFree();

        buttonToHide.Show();
        rescanButton.Hide();

        hBox.RemoveChild(fileTree);
        row.AddChild(fileTree);
        row.MoveChild(fileTree, 0);
        hBox.QueueFree();
    }

    public override void _Process(double delta)
    {
        if (instance is null)
        {
            instance = this;
            // On Build
            GD.Print("Builded");
        }

        if (isScanning && !GetEditorInterface().GetResourceFilesystem().IsScanning())
        {
            isScanning = false;
            Update();
        }
    }

    public void ManuelUpdate()
    {
        filterBar.EmitSignal(LineEdit.SignalName.TextChanged, filterBar.Text);
    }

    public void Update()
    {
        TreeItem favDir = fileTree.GetRoot().GetFirstChild();
        TreeItem resDir = favDir.GetNext();
        TreeItem currentItem = resDir.GetFirstChild();

        Stack<TreeItem> folders = new();

        FilterFiles(currentItem, "res:/", ref folders);

        SetFavotites();

        FilterDirectories(folders);

        void FilterFiles(TreeItem item, string path, ref Stack<TreeItem> folders)
        {
            if (item is null)
                return;

            string parentText = item.GetParent().GetText(0);

            while (item is not null)
            {
                string pathToItem = $"{path}/{item.GetText(0)}";

                string itemType = fileSystem.GetFileType(pathToItem);

                TreeItem next = item.GetNext();

                bool isFolder = itemType is "";

                if (isFolder)
                    folders.Push(item);

                FilterFiles(item.GetFirstChild(), pathToItem, ref folders);

                if (!isFolder && !FileFiltered(pathToItem, item.GetText(0), itemType))
                    item.Free();

                item = next;
            }
        }

        void SetFavotites()
        {
            favorites.Clear();
            TreeItem fav = favDir.GetFirstChild();

            while (fav is not null)
            {
                favorites.Add(fav.GetTooltipText(0), fav);

                fav = fav.GetNext();
            }
        }
    }

    private bool FileFiltered(string path, string name, string itemType)
    {
        bool noIncluders = true;
        bool included = false;

        foreach (FileFilter filter in fileFilters)
        {
            if (noIncluders && filter.includeType is IncludeType.Include) noIncluders = false;

            bool isMathing = false;

            switch (filter.filterType)
            {
                case FileFilterType.DerivedType:
                    isMathing = IsClassDerivedFromType(itemType, filter.filterString);
                    break;

                case FileFilterType.MatchType:
                    isMathing = itemType == filter.filterString;
                    break;

                case FileFilterType.PathContains:
                    isMathing = path.Contains(filter.filterString);
                    break;

                case FileFilterType.PathMatch:
                    isMathing = path == filter.filterString;
                    break;

                case FileFilterType.NameContains:
                    isMathing = name.Contains(filter.filterString);
                    break;

                case FileFilterType.NameMatch:
                    isMathing = name == filter.filterString;
                    break;
            }


            if (!isMathing) continue;

            switch (filter.includeType)
            {
                case IncludeType.Include:
                    included = true;
                    continue;

                case IncludeType.Exclude:
                    return false;
            }
        }

        return noIncluders || included;
    }

    private bool IsClassDerivedFromType(string @class, string type)
    {
        if (@class is "")
            return false;
        if (@class == type)
            return true;

        string parentClass = ClassDB.GetParentClass(@class);

        return IsClassDerivedFromType(parentClass, type);
    }

    private void FilterDirectories(Stack<TreeItem> folders)
    {
        IEnumerable<DirectoryFilter> includers =
        directoryFilters.Where((DirectoryFilter filter) =>
        {
            return filter.includeType is IncludeType.Include;
        });
        IEnumerable<DirectoryFilter> excluders =
        directoryFilters.Where((DirectoryFilter filter) =>
        {
            return filter.includeType is IncludeType.Exclude;
        });

        bool noInluders = includers.Count() == 0;


        foreach (TreeItem folder in folders)
        {
            string path = GetPath(folder);

            foreach (DirectoryFilter filter in excluders)
            {
                if (IsMatching(folder, path, filter))
                {
                    RemoveFolder(folder, path);
                    continue;
                }
            }

            if (noInluders) continue;

            foreach (DirectoryFilter filter in includers)
            {
                if (IsMatching(folder, path, filter)) continue;
            }

            RemoveFolder(folder, path);
        }

        if (hidingEmptyDirectories) RemoveEmptyFolders(folders);


        void RemoveFolder(TreeItem folder, string path)
        {
            if (favorites.ContainsKey(path)) favorites[path].Free();
            folder.CallDeferred("free");
        }

        static bool IsMatching(TreeItem folder, string path, DirectoryFilter filter)
        {
            switch (filter.filterType)
            {
                case DirectoryFilterType.PathMatch:
                    return path == filter.filterString;

                case DirectoryFilterType.PathContains:
                    return path.Contains(filter.filterString);

                case DirectoryFilterType.NameMatch:
                    return folder.GetText(0) == filter.filterString;

                case DirectoryFilterType.NameContains:
                    return folder.GetText(0).Contains(filter.filterString);
            }

            return false;
        }

        static string GetPath(TreeItem folder)
        {
            string path = "";
            while (folder is not null)
            {
                string name = folder.GetText(0);
                folder = folder.GetParent();

                if (name is "res://")
                {
                    path = $"res://{path}";
                    break;
                }
                path = $"{name}/{path}";
            }
            return path;
        }

        static void RemoveEmptyFolders(Stack<TreeItem> folders)
        {
            while (folders.Count > 0)
            {
                TreeItem folder = folders.Pop();

                if (folder is null) continue;

                if (folder.GetFirstChild() is null)
                    folder.Free();
            }
        }
    }

    #region Interactions 

    public void ToggleHideEmpty(bool toggled)
    {
        hidingEmptyDirectories = toggled;
        ManuelUpdate();
    }

    public void ExpandAll()
    {
        ManuelUpdate();
        TreeItem favorites = fileTree.GetRoot().GetFirstChild();
        TreeItem resDir = favorites.GetNext();

        favorites.Collapsed = false;

        SetCollapseRecursive(resDir, false);
    }

    public void CollapseAll()
    {
        ManuelUpdate();
        TreeItem favorites = fileTree.GetRoot().GetFirstChild();
        TreeItem resDir = favorites.GetNext();

        favorites.Collapsed = true;

        SetCollapseRecursive(resDir.GetFirstChild(), true);
    }

    private void SetCollapseRecursive(TreeItem item, bool collapse)
    {
        if (item is null)
            return;

        item.Collapsed = collapse;

        SetCollapseRecursive(item.GetNext(), collapse);
        SetCollapseRecursive(item.GetFirstChild(), collapse);
    }

    public void SetSearchFilter(string filter)
    {
        filterBar.Text = filter;
        // ManuelUpdate();
    }

    public void GoToSelected()
    {
        TreeItem selectedItem = fileTree.GetSelected();

        if (selectedItem == null)
        {
            var filterButtons = clearButton.GetAllFilterButtons();

            foreach (var filterButton in filterButtons)
                filterButton.Reset();

            selectedItem = fileTree.GetSelected();

            if (selectedItem == null)
            {
                foreach (var filterButton in filterButtons)
                    filterButton.ButtonPressed = false;

                selectedItem = fileTree.GetSelected();
            }
        }

        fileTree.ScrollToItem(selectedItem, true);
    }

    #endregion
}

#endif