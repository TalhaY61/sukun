using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Sukun.Core;

namespace Sukun.Windows;

public class SearchComboBox : System.Windows.Controls.ComboBox
{
    public static readonly DependencyProperty SearchEnabledProperty = DependencyProperty.Register(nameof(SearchEnabled), typeof(bool), typeof(SearchComboBox),
        new PropertyMetadata(false, (d, e) => ((SearchComboBox)d).IsEditable = (bool)e.NewValue));
    public bool SearchEnabled { get => (bool)GetValue(SearchEnabledProperty); set => SetValue(SearchEnabledProperty, value); }
    public static readonly DependencyProperty PopupWidthProperty = DependencyProperty.Register(nameof(PopupWidth), typeof(double), typeof(SearchComboBox), new PropertyMetadata(180d));
    public double PopupWidth { get => (double)GetValue(PopupWidthProperty); private set => SetValue(PopupWidthProperty, value); }
    public bool IsFiltering { get; private set; }
    private System.Windows.Controls.TextBox? editor;
    private ICollectionView? view;
    private bool internalChange;
    private string query = "";
    private object? chosen;
    private int searchIndex = -1;
    private Rect ownerBounds;

    public SearchComboBox()
    {
        IsTextSearchEnabled = false;
        VirtualizingPanel.SetScrollUnit(this, ScrollUnit.Pixel);
        SetCurrentValue(ScrollViewer.CanContentScrollProperty, true);
    }
    public override void OnApplyTemplate()
    {
        if (editor is not null) editor.TextChanged -= SearchChanged;
        base.OnApplyTemplate();
        editor = GetTemplateChild("PART_EditableTextBox") as System.Windows.Controls.TextBox;
        if (editor is not null) editor.TextChanged += SearchChanged;
        if (GetTemplateChild("PART_Popup") is Popup popup)
        {
            popup.PlacementTarget = this;
            popup.Placement = PlacementMode.Custom;
            popup.CustomPopupPlacementCallback = PlacePopup;
        }
    }
    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        internalChange = true;
        try
        {
            base.OnSelectionChanged(e);
            if (!IsFiltering && SelectedItem is not null) chosen = SelectedItem;
        }
        finally { internalChange = false; }
    }
    protected override void OnDropDownOpened(EventArgs e)
    {
        chosen = SelectedItem;
        query = "";
        ApplyFilter();
        MeasureRoom();
        base.OnDropDownOpened(e);
        if (SearchEnabled && editor is not null) { editor.Focus(); editor.SelectAll(); }
    }
    protected override void OnDropDownClosed(EventArgs e)
    {
        query = "";
        ApplyFilter();
        internalChange = true;
        IsFiltering = true;
        try
        {
            if (SelectedItem is null && chosen is not null && Items.Contains(chosen)) SetCurrentValue(SelectedItemProperty, chosen);
            SetCurrentValue(TextProperty, SelectedItem?.ToString() ?? "");
        }
        finally { internalChange = false; IsFiltering = false; }
        base.OnDropDownClosed(e);
    }
    protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
    {
        if (view is not null) view.Filter = null;
        base.OnItemsSourceChanged(oldValue, newValue);
        view = newValue is null ? null : CollectionViewSource.GetDefaultView(newValue);
    }
    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (SearchEnabled && e.OriginalSource is DependencyObject source && Ancestor<System.Windows.Controls.TextBox>(source) is not null)
            SetCurrentValue(IsDropDownOpenProperty, true);
        base.OnPreviewMouseLeftButtonDown(e);
    }
    protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        if (SearchEnabled && IsDropDownOpen && !string.IsNullOrEmpty(query) && e.Key is Key.Down or Key.Up or Key.Enter)
        {
            // WPF can retain the pre-filter highlighted container. Commit from the CURRENT
            // filtered Items view, never from that stale native highlight.
            if (Items.Count > 0)
            {
                if (e.Key == Key.Enter)
                {
                    var item = searchIndex >= 0 && searchIndex < Items.Count ? Items[searchIndex] : Items[0];
                    SetCurrentValue(SelectedItemProperty, item);
                }
                else
                {
                    searchIndex = Math.Clamp(searchIndex < 0 ? 0 : searchIndex + (e.Key == Key.Down ? 1 : -1), 0, Items.Count - 1);
                    SetCurrentValue(SelectedItemProperty, Items[searchIndex]);
                }
            }
            if (e.Key == Key.Enter) SetCurrentValue(IsDropDownOpenProperty, false);
            e.Handled = true;
            return;
        }
        if (SearchEnabled && !IsDropDownOpen && (e.Key is Key.Back or Key.Space || ((int)e.Key >= (int)Key.A && (int)e.Key <= (int)Key.Z)))
            SetCurrentValue(IsDropDownOpenProperty, true);
        base.OnPreviewKeyDown(e);
    }
    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        if (!IsDropDownOpen)
        {
            e.Handled = true; // Hovering a closed selector must NEVER change an operational setting.
            var scroll = Ancestor<ScrollViewer>(VisualTreeHelper.GetParent(this));
            scroll?.ScrollToVerticalOffset(scroll.VerticalOffset - e.Delta / 3d);
            return;
        }
        base.OnPreviewMouseWheel(e);
    }
    private void SearchChanged(object sender, TextChangedEventArgs e)
    {
        if (!SearchEnabled || internalChange || IsFiltering || !IsDropDownOpen || editor is null) return;
        query = editor.Text;
        searchIndex = -1;
        ApplyFilter();
    }
    private void ApplyFilter()
    {
        if (view is null || !view.CanFilter) return;
        IsFiltering = true;
        try
        {
            var typed = query;
            view.Filter = string.IsNullOrWhiteSpace(typed) ? null : item => SearchText.Matches(item is MediaChoice media ? media.Label + " " + media.Detail : item?.ToString() ?? "", typed);
            if (SearchEnabled && IsDropDownOpen && editor is not null && !string.IsNullOrEmpty(typed))
            {
                internalChange = true;
                editor.Text = typed;
                editor.CaretIndex = typed.Length;
                internalChange = false;
            }
        }
        finally { IsFiltering = false; }
    }
    private void MeasureRoom()
    {
        if (Window.GetWindow(this)?.Content is not FrameworkElement root) return;
        var topLeft = root.TranslatePoint(new System.Windows.Point(0, 0), this);
        ownerBounds = new Rect(topLeft, new System.Windows.Size(root.ActualWidth, root.ActualHeight));
        PopupWidth = Math.Min(ActualWidth, ownerBounds.Width);
        var below = Math.Max(0, ownerBounds.Bottom - ActualHeight - 5);
        var above = Math.Max(0, -ownerBounds.Top - 5);
        MaxDropDownHeight = Math.Max(32, Math.Min(224, Math.Max(below, above)));
    }
    private CustomPopupPlacement[] PlacePopup(System.Windows.Size popup, System.Windows.Size target, System.Windows.Point offset)
    {
        // Popup callbacks supply device pixels, while TranslatePoint/ActualHeight are DIPs.
        // Mixing them made 150% scaling overlap the input and misjudge available room.
        var dpi = VisualTreeHelper.GetDpi(this);
        var position = PopupGeometry.Place(popup.Width, popup.Height, target.Height, ownerBounds.Left, ownerBounds.Top,
            ownerBounds.Right, ownerBounds.Bottom, dpi.DpiScaleX, dpi.DpiScaleY);
        return [new CustomPopupPlacement(new System.Windows.Point(position.X, position.Y), PopupPrimaryAxis.None)];
    }

    private static T? Ancestor<T>(DependencyObject? node) where T : DependencyObject
    {
        while (node is not null)
        {
            if (node is T found) return found;
            node = node is Visual ? VisualTreeHelper.GetParent(node) : LogicalTreeHelper.GetParent(node);
        }
        return null;
    }
}

public static class CardState
{
    public static readonly DependencyProperty IsNextProperty = DependencyProperty.RegisterAttached("IsNext", typeof(bool), typeof(CardState), new FrameworkPropertyMetadata(false));
    public static bool GetIsNext(DependencyObject obj) => (bool)obj.GetValue(IsNextProperty);
    public static void SetIsNext(DependencyObject obj, bool value) => obj.SetValue(IsNextProperty, value);
}

