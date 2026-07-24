using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BubblesCmd.Core.Models;

namespace BubblesCmd.App.Terminal;

internal sealed class TerminalTabState
{
    private Orientation _layoutOrientation = Orientation.Vertical;

    public Grid PaneGrid { get; } = new();

    public List<TerminalPaneState> Panes { get; } = [];

    public TerminalPaneState ActivePane { get; private set; }

    public ShellProfile Profile => ActivePane.Profile;

    public TerminalView View => ActivePane.View;

    public TabItem TabItem { get; set; } = new();

    public string Title { get; set; }

    public bool IsPinned { get; set; }

    public bool IsPaneZoomed { get; private set; }

    public bool HasRunningSessions => Panes.Any(pane => pane.View.IsRunning);

    public TerminalTabState(TerminalPaneState pane)
    {
        Title = pane.Profile.DisplayName;
        ActivePane = pane;
        Panes.Add(ActivePane);
        SetActivePane(ActivePane);
        RebuildPaneGrid();
    }

    public void AddPane(TerminalPaneState pane, Orientation orientation)
    {
        IsPaneZoomed = false;
        _layoutOrientation = orientation;
        Panes.Add(pane);
        SetActivePane(pane);
        RebuildPaneGrid();
    }

    public bool RemovePane(TerminalPaneState pane)
    {
        if (Panes.Count <= 1)
        {
            return false;
        }

        var index = Panes.IndexOf(pane);
        if (index < 0)
        {
            return false;
        }

        IsPaneZoomed = false;
        Panes.RemoveAt(index);
        SetActivePane(Panes[Math.Clamp(index - 1, 0, Panes.Count - 1)]);
        RebuildPaneGrid();
        return true;
    }

    public bool DetachPane(TerminalPaneState pane)
    {
        return RemovePane(pane);
    }

    public void SwapActivePaneWithPrevious()
    {
        if (Panes.Count <= 1)
        {
            return;
        }

        var currentIndex = Panes.IndexOf(ActivePane);
        if (currentIndex < 0)
        {
            return;
        }

        var targetIndex = currentIndex == 0 ? Panes.Count - 1 : currentIndex - 1;
        (Panes[currentIndex], Panes[targetIndex]) = (Panes[targetIndex], Panes[currentIndex]);
        RebuildPaneGrid();
    }

    public void FocusNextPane()
    {
        if (Panes.Count == 0)
        {
            return;
        }

        var currentIndex = Panes.IndexOf(ActivePane);
        var nextPane = Panes[(currentIndex + 1) % Panes.Count];
        SetActivePane(nextPane);
        RebuildPaneGrid();
        nextPane.View.FocusTerminal();
    }

    public void SetActivePane(TerminalPaneState pane)
    {
        if (!Panes.Contains(pane))
        {
            return;
        }

        ActivePane = pane;
        foreach (var item in Panes)
        {
            item.View.BorderThickness = item == pane ? new Thickness(2) : new Thickness(1);
            item.View.BorderBrush = item == pane
                ? new SolidColorBrush(Color.FromRgb(88, 185, 255))
                : new SolidColorBrush(Color.FromRgb(25, 38, 51));
        }
    }

    public void DisposeSessions()
    {
        foreach (var pane in Panes)
        {
            pane.View.Dispose();
        }
    }

    public void TogglePaneZoom()
    {
        if (Panes.Count <= 1)
        {
            IsPaneZoomed = false;
            RebuildPaneGrid();
            return;
        }

        IsPaneZoomed = !IsPaneZoomed;
        RebuildPaneGrid();
    }

    private void RebuildPaneGrid()
    {
        PaneGrid.Children.Clear();
        PaneGrid.ColumnDefinitions.Clear();
        PaneGrid.RowDefinitions.Clear();

        if (Panes.Count == 1 || IsPaneZoomed)
        {
            ActivePane.View.Margin = new Thickness(0);
            Grid.SetColumn(ActivePane.View, 0);
            Grid.SetRow(ActivePane.View, 0);
            PaneGrid.Children.Add(ActivePane.View);
            return;
        }

        if (_layoutOrientation == Orientation.Vertical)
        {
            for (var index = 0; index < Panes.Count; index++)
            {
                if (index > 0)
                {
                    PaneGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                }

                PaneGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            for (var index = 0; index < Panes.Count; index++)
            {
                Panes[index].View.Margin = new Thickness(index == 0 ? 0 : 6, 0, index == Panes.Count - 1 ? 0 : 6, 0);
                Grid.SetColumn(Panes[index].View, index * 2);
                Grid.SetRow(Panes[index].View, 0);
                PaneGrid.Children.Add(Panes[index].View);
                if (index > 0)
                {
                    var splitter = new GridSplitter
                    {
                        Width = 5,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Background = new SolidColorBrush(Color.FromRgb(88, 185, 255))
                    };
                    Grid.SetColumn(splitter, (index * 2) - 1);
                    Grid.SetRow(splitter, 0);
                    PaneGrid.Children.Add(splitter);
                }
            }

            return;
        }

        for (var index = 0; index < Panes.Count; index++)
        {
            if (index > 0)
            {
                PaneGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            PaneGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        }

        for (var index = 0; index < Panes.Count; index++)
        {
            Panes[index].View.Margin = new Thickness(0, index == 0 ? 0 : 6, 0, index == Panes.Count - 1 ? 0 : 6);
            Grid.SetColumn(Panes[index].View, 0);
            Grid.SetRow(Panes[index].View, index * 2);
            PaneGrid.Children.Add(Panes[index].View);
            if (index > 0)
            {
                var splitter = new GridSplitter
                {
                    Height = 5,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = new SolidColorBrush(Color.FromRgb(88, 185, 255))
                };
                Grid.SetColumn(splitter, 0);
                Grid.SetRow(splitter, (index * 2) - 1);
                PaneGrid.Children.Add(splitter);
            }
        }
    }

    public TerminalTabState(ShellProfile profile, TerminalView view)
    {
        Title = profile.DisplayName;
        ActivePane = new TerminalPaneState(profile, view);
        Panes.Add(ActivePane);
        SetActivePane(ActivePane);
        RebuildPaneGrid();
    }
}
