#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;

using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Windows.Controls.Primitives;

#endregion

//This namespace holds Add ons in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.AddOns.DtwAddOns
{
    public class DtwUi
    {
        private readonly FrameworkElement _chartControl;
        private readonly SortedSet<IDtwUiChartElement> _chartItems = new SortedSet<IDtwUiChartElement>();
        private readonly List<IDtwUiElement> _uiItems = new List<IDtwUiElement>();

        public void AddElement(IDtwUiElement element)
        {
            _uiItems.Add(element);
        }
        public void AddElement(IDtwUiChartElement element)
        {
            _chartItems.Add(element);
        }

        public void OnStateChangeHistorical()
        {
            var chartWindow = Window.GetWindow(_chartControl.Parent) as Chart;
            if (chartWindow == null) return;

            chartWindow.MainTabControl.SelectionChanged += TabSelectionChangedHandler;
        }

        public void OnStateChangeTerminated()
        {
            var chartWindow = Window.GetWindow(_chartControl.Parent) as Chart;
            if (chartWindow == null) return;

            chartWindow.MainTabControl.SelectionChanged -= TabSelectionChangedHandler;

            foreach (var item in _uiItems)
            {
                item.Dispose();
            }

            foreach (var item in _chartItems)
            {
                item.Dispose();
            }
        }

        public void Refresh()
        {
            if (TabSelected())
                AddChartItems();
            foreach (var item in _uiItems)
                item.Refresh();
            foreach (var item in _chartItems)
                item.Refresh();
        }

        private void TabSelectionChangedHandler(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0) return;

            var tabItem = e.AddedItems[0] as System.Windows.Controls.TabItem;
            if (tabItem == null) return;

            var chartTab = tabItem.Content as ChartTab;
            if (chartTab == null) return;

            if (TabSelected())
                AddChartItems();
            else
                RemoveChartItems();
        }

        private bool TabSelected()
        {
            var chartWindow = Window.GetWindow(_chartControl.Parent) as Chart;
            if (chartWindow == null) return false;

            foreach (TabItem tab in chartWindow.MainTabControl.Items)
            {
                var chartTab = tab.Content as ChartTab;
                if (chartTab == null) continue;
                if (chartTab.ChartControl == null) continue;
                if (chartTab.ChartControl.Equals(_chartControl) && tab.Equals(chartWindow.MainTabControl.SelectedItem))
                    return true;
            }
            return false;
        }

        private void AddChartItems()
        {
            foreach (var item in _chartItems)
                item.Add();
        }

        private void RemoveChartItems()
        {
            foreach (var item in _chartItems)
                item.Remove();
        }

        public DtwUi(FrameworkElement chartControl)
        {
            _chartControl = chartControl;
        }
    }

    /// <summary>
    /// interface to centralize UI elements
    /// </summary>
    public interface IDtwUiElement : IDisposable
    {
        void Refresh();
    }

    /// <summary>
    /// base class for UI elements
    /// to centralize the ability to create/dispose/add/remove
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class DtwUiElement<T> : IDtwUiElement
    {
        protected T _item;
        private readonly Action<T> _onRefresh;

        protected DtwUiElement(T item, Action<T> onRefresh)
        {
            _item = item;
            _onRefresh = onRefresh;
        }

        public T Item
        {
            get { return _item; }
        }

        public abstract void Dispose();

        public virtual void Refresh()
        {
            if (_onRefresh == null) return;
            _onRefresh(_item);
        }
    }

    #region elements

    public class DtwUiButton : DtwUiElement<Button>
    {
        public DtwUiButton(Button item, Action<Button> onRefresh, RoutedEventHandler onClick)
            : base(item, onRefresh)
        {
            _onClick = onClick;

            if (_onClick == null) return;
            _item.Click += _onClick;
        }

        private readonly RoutedEventHandler _onClick;

        public override void Dispose()
        {
            if (_onClick == null) return;
            _item.Click -= _onClick;
        }
    }

    public class DtwUiCheckBox : DtwUiElement<CheckBox>
    {
        public DtwUiCheckBox(CheckBox item, Action<CheckBox> onRefresh, RoutedEventHandler onClick)
            : base(item, onRefresh)
        {
            _onClick = onClick;

            if (_onClick == null) return;
            _item.Click += _onClick;
        }

        private readonly RoutedEventHandler _onClick;

        public override void Dispose()
        {
            if (_onClick == null) return;
            _item.Click -= _onClick;
        }
    }

    public class DtwUiRadioButton : DtwUiElement<RadioButton>
    {
        public DtwUiRadioButton(RadioButton item, Action<RadioButton> onRefresh, RoutedEventHandler onClick)
            : base(item, onRefresh)
        {
            _onClick = onClick;

            if (_onClick == null) return;
            _item.Click += _onClick;
        }

        private readonly RoutedEventHandler _onClick;

        public override void Dispose()
        {
            if (_onClick == null) return;
            _item.Click -= _onClick;
        }
    }

    public class DtwUiTextBlock : DtwUiElement<TextBlock>
    {
        public DtwUiTextBlock(TextBlock item, Action<TextBlock> onRefresh)
            : base(item, onRefresh)
        {

        }

        public override void Dispose()
        {

        }
    }

    public class DtwUiTextBox : DtwUiElement<TextBox>
    {
        public DtwUiTextBox(TextBox item, Action<TextBox> onRefresh, KeyEventHandler onKeyDown, KeyEventHandler onPreviewKeyDown)
            : base(item, onRefresh)
        {
            _onKeyDown = onKeyDown;
            _onPreviewKeyDown = onPreviewKeyDown;

            if (_onKeyDown != null)
                _item.KeyDown += _onKeyDown;
            if (_onPreviewKeyDown != null)
                _item.PreviewKeyDown += _onPreviewKeyDown;
        }

        private readonly KeyEventHandler _onKeyDown;
        private readonly KeyEventHandler _onPreviewKeyDown;

        public override void Dispose()
        {
            if (_onKeyDown != null)
                _item.KeyDown -= _onKeyDown;
            if (_onPreviewKeyDown != null)
                _item.PreviewKeyDown -= _onPreviewKeyDown;
        }
    }

    public class DtwUiTextBoxInteger : DtwUiTextBox
    {
        private static KeyEventHandler MakeOnKeyDown(Action<int> onReturn, Func<int, bool> isEditState, int length, TextBox textBox, bool isPreview)
        {
            var foreground = textBox.Foreground;
            var background = textBox.Background;
            var cursor = textBox.CaretBrush;

            Action<TextBox, bool> change = (item, isEdit) =>
            {
                if (isEdit)
                {
                    item.Foreground = background;
                    item.Background = foreground;
                    item.CaretBrush = background;
                }
                else
                {
                    item.Foreground = foreground;
                    item.Background = background;
                    item.CaretBrush = cursor;
                }
            };

            return (o, e) =>
            {
                /*
                Allows for only numbers totalling 999 (X digit length)
                */

                var item = o as TextBox;
                if (item == null) return;

                if (isPreview)
                {
                    if (e.Key == Key.Back || e.Key == Key.Delete)
                        change(item, true);
                    return;
                }

                if (e.Key == Key.Tab) return;

                e.Handled = true;

                if (e.Key == Key.Return)
                {
                    item.Text = item.Text.Length > 0 ? item.Text : "0";
                    change(item, false);
                    if (onReturn != null)
                        onReturn(int.Parse(item.Text));
                    return;
                }

                if ((item.Text.Length - item.SelectedText.Length) >= length) return;

                string toAdd = null;
                var regex = new Regex(@"^(?:NumPad|D)([\d]{1})$", RegexOptions.Compiled);
                Match m = regex.Match(e.Key.ToString());
                if (m.Success)
                {
                    toAdd = m.Groups[m.Groups.Count - 1].ToString();
                }

                if (toAdd != null)
                {
                    var index = item.SelectionStart;
                    if (item.SelectedText.Length > 0)
                        item.Text = item.Text.Substring(0, index) + toAdd + item.Text.Substring(index + item.SelectedText.Length);
                    else
                        item.Text = item.Text.Substring(0, index) + toAdd + item.Text.Substring(index);

                    item.Text = item.Text.Trim();
                    item.SelectionStart = index + 1;
                }

                if (item.Text.Length == 0 || isEditState(int.Parse(item.Text)))
                {
                    change(item, true);
                }
            };
        }

        public DtwUiTextBoxInteger(TextBox item, Action<TextBox> onRefresh, Action<int> onReturn, Func<int, bool> isEditState, int length)
            : base(item, onRefresh, MakeOnKeyDown(onReturn, isEditState, length, item, false), MakeOnKeyDown(onReturn, isEditState, length, item, true))
        {

        }

        public DtwUiTextBoxInteger(TextBox item, Action<TextBox> onRefresh, Action<int> onReturn, Func<int, bool> isEditState)
            : this(item, onRefresh, onReturn, isEditState, 4)
        {

        }
    }

    public class DtwUiTextBoxDecimal : DtwUiTextBox
    {
        private static KeyEventHandler MakeOnKeyDown(Action<double> onReturn, Func<double, bool> isEditState, int length, TextBox textBox, bool isPreview)
        {
            var foreground = textBox.Foreground;
            var background = textBox.Background;
            var cursor = textBox.CaretBrush;

            Action<TextBox, bool> change = (item, isEdit) =>
            {
                if (isEdit)
                {
                    item.Foreground = background;
                    item.Background = foreground;
                    item.CaretBrush = background;
                }
                else
                {
                    item.Foreground = foreground;
                    item.Background = background;
                    item.CaretBrush = cursor;
                }
            };

            return (o, e) =>
            {
                /*
                Allows for only numbers totalling 999 (X digit length)
                */

                var item = o as TextBox;
                if (item == null) return;

                if (isPreview)
                {
                    if (e.Key == Key.Back || e.Key == Key.Delete)
                        change(item, true);
                    return;
                }

                if (e.Key == Key.Tab) return;

                e.Handled = true;

                if (e.Key == Key.Return)
                {
                    item.Text = item.Text.Length > 0 ? item.Text : "0";
                    change(item, false);
                    if (onReturn != null)
                        onReturn(double.Parse(item.Text));
                    return;
                }

                var len = item.Text.Contains(".") ? (length + 1) : length;
                if ((item.Text.Length - item.SelectedText.Length) >= len) return;

                string toAdd = null;
                var regex = new Regex(@"^(?:NumPad|D)([\d]{1})$", RegexOptions.Compiled);
                Match m = regex.Match(e.Key.ToString());
                if (m.Success)
                {
                    toAdd = m.Groups[m.Groups.Count - 1].ToString();
                }
                else
                {
                    if (Regex.IsMatch(e.Key.ToString(), "(Decimal|OemPeriod)"))
                    {
                        if (!item.Text.Contains("."))
                        {
                            toAdd = ".";
                        }
                    }
                }
                if (toAdd == null) return;

                var index = item.SelectionStart;
                if (item.SelectedText.Length > 0)
                    item.Text = item.Text.Substring(0, index) + toAdd + item.Text.Substring(index + item.SelectedText.Length);
                else
                    item.Text = item.Text.Substring(0, index) + toAdd + item.Text.Substring(index);

                item.Text = item.Text.Trim();
                if (item.Text.StartsWith("."))
                    item.Text = "0" + item.Text;
                item.SelectionStart = index + 1;

                if (item.Text.Length == 0 || isEditState(double.Parse(item.Text)))
                {
                    change(item, true);
                }
            };
        }

        public DtwUiTextBoxDecimal(TextBox item, Action<TextBox> onRefresh, Action<double> onReturn, Func<double, bool> isEditState, int length)
            : base(item, onRefresh, MakeOnKeyDown(onReturn, isEditState, length, item, false), MakeOnKeyDown(onReturn, isEditState, length, item, true))
        {

        }

        public DtwUiTextBoxDecimal(TextBox item, Action<TextBox> onRefresh, Action<double> onReturn, Func<double, bool> isEditState)
            : this(item, onRefresh, onReturn, isEditState, 3)
        {

        }
    }

    public class DtwUiTextBoxTime : DtwUiTextBox
    {
        private static KeyEventHandler MakeOnKeyDown(Action<string> onReturn, Func<string, bool> isEditState, TextBox textBox, bool isPreview, string txt)
        {
            var currentTime = new TimeSpan(0, 0, 0);
            var foreground = textBox.Foreground;
            var background = textBox.Background;
            var cursor = textBox.CaretBrush;

            Action<TextBox, bool> change = (item, isEdit) =>
            {
                if (isEdit)
                {
                    item.Foreground = background;
                    item.Background = foreground;
                    item.CaretBrush = background;
                }
                else
                {
                    item.Foreground = foreground;
                    item.Background = background;
                    item.CaretBrush = cursor;
                }
            };

            return (o, e) =>
            {
                var item = o as TextBox;
                if (item == null) return;

                if (isPreview)
                {
                    if (e.Key == Key.Back || e.Key == Key.Delete)
                        e.Handled = true; //change(item, true);
                    return;
                }

                if (e.Key == Key.Tab) return;

                e.Handled = true;

                if (e.Key == Key.Return)
                {
                    change(item, false);
                    if (onReturn != null)
                        onReturn(item.Text);
                    return;
                }

                string toAdd = null;
                var regex = new Regex(@"^(?:NumPad|D)([\d]{1})$", RegexOptions.Compiled);
                Match m = regex.Match(e.Key.ToString());
                if (m.Success)
                {
                    toAdd = m.Groups[m.Groups.Count - 1].ToString();
                }
                if (toAdd == null) return;

                var parts = item.Text.Split(":".ToCharArray());
                var caretIndex = item.CaretIndex / 3;
                parts[caretIndex] = parts[caretIndex] + toAdd;
                parts[caretIndex] = parts[caretIndex].Substring(parts[caretIndex].Length - 2);

                item.Text = string.Join(":", parts);
                //var index = item.SelectionStart;
                //if (item.SelectedText.Length > 0)
                //    item.Text = item.Text.Substring(0, index) + toAdd + item.Text.Substring(index + item.SelectedText.Length);
                //else
                //    item.Text = item.Text.Substring(0, index) + toAdd + item.Text.Substring(index);

                item.Text = item.Text.Trim();
                item.SelectionStart = caretIndex * 3 + 2;

                if (item.Text.Length == 0 || isEditState(item.Text))
                {
                    change(item, true);
                }
            };
        }

        private TimeSpan _time;

        public DtwUiTextBoxTime(TextBox item, Action<TextBox> onRefresh, Action<string> onReturn, Func<string, bool> isEditState, string time)
            : base(item, onRefresh, MakeOnKeyDown(onReturn, isEditState, item, false, time), MakeOnKeyDown(onReturn, isEditState, item, true, time))
        {

        }

        public DtwUiTextBoxTime(TextBox item, Action<TextBox> onRefresh, Func<string, bool> isEditState, Action<string> onReturn)
            : this(item, onRefresh, onReturn, isEditState, "000000")
        {

        }
    }

    public class DtwUiDatePicker : DtwUiElement<DatePicker>
    {
        public DtwUiDatePicker(DatePicker item, Action<DatePicker> onRefresh, EventHandler<SelectionChangedEventArgs> onSelectionChanged)
            : base(item, onRefresh)
        {
            _onSelectionChanged = onSelectionChanged;

            if (_onSelectionChanged == null) return;
            _item.SelectedDateChanged += _onSelectionChanged;
        }

        private readonly EventHandler<SelectionChangedEventArgs> _onSelectionChanged;

        public override void Dispose()
        {
            if (_onSelectionChanged == null) return;
            _item.SelectedDateChanged -= _onSelectionChanged;
        }
    }

    public class DtwUiComboBox : DtwUiElement<ComboBox>
    {
        public DtwUiComboBox(ComboBox item, Action<ComboBox> onRefresh, SelectionChangedEventHandler onSelectionChanged)
            : base(item, onRefresh)
        {
            _onSelectionChanged = onSelectionChanged;

            if (_onSelectionChanged == null) return;
            _item.SelectionChanged += _onSelectionChanged;
        }

        private readonly SelectionChangedEventHandler _onSelectionChanged;

        public override void Dispose()
        {
            if (_onSelectionChanged == null) return;
            _item.SelectionChanged -= _onSelectionChanged;
        }
    }

    public class DtwUiGroupBox : DtwUiElement<GroupBox>
    {
        public DtwUiGroupBox(GroupBox item, Action<GroupBox> onRefresh)
            : base(item, onRefresh)
        {

        }

        public override void Dispose()
        {

        }
    }

    public class DtwUiProgress : DtwUiElement<ProgressBar>
    {
        public DtwUiProgress(ProgressBar item, Action<ProgressBar> onRefresh)
            : base(item, onRefresh)
        {

        }

        public override void Dispose()
        {

        }
    }

    public class DtwUiStackPanel : DtwUiElement<StackPanel>
    {
        public DtwUiStackPanel(StackPanel item, Action<StackPanel> onRefresh)
            : base(item, onRefresh)
        {

        }

        public override void Dispose()
        {

        }
    }

    public class DtwUiGrid : DtwUiElement<Grid>
    {
        public DtwUiGrid(Grid item, Action<Grid> onRefresh)
            : base(item, onRefresh)
        {

        }

        public override void Dispose()
        {

        }
    }

    public class DtwUiDockPanel : DtwUiElement<DockPanel>
    {
        public DtwUiDockPanel(DockPanel item, Action<DockPanel> onRefresh)
            : base(item, onRefresh)
        {

        }

        public override void Dispose()
        {

        }
    }

    #endregion

    #region chartElements

    public interface IDtwUiChartElement : IDtwUiElement, IComparable
    {
        void Add();
        void Remove();
    }

    /// <summary>
    /// this elements added to the chart itself
    /// so they must be dealt with differently due to tab selection events
    /// </summary>
    public abstract class DtwUiChartElement : IDtwUiChartElement
    {
        private static int _toolBarIndex;
        protected readonly string Id;
        protected readonly Chart ChartWindow;
        private readonly int _sortOrder;
        private readonly Action<DtwUiChartElement> OnRefresh;

        protected DtwUiChartElement(Chart chartWindow, int sortOrder, Action<DtwUiChartElement> onRefresh)
        {
            Id = string.Format("dtw__ftb_{0}", _toolBarIndex++);
            ChartWindow = chartWindow;
            _sortOrder = sortOrder;
            OnRefresh = onRefresh;
        }

        public abstract void Add();
        public abstract void Remove();

        public abstract void Dispose();

        public virtual void Refresh()
        {
            if (OnRefresh == null) return;
            OnRefresh(this);
        }

        public virtual int CompareTo(object obj)
        {
            if (obj == null) return 1;
            var otherObject = obj as DtwUiChartElement;
            if (otherObject == null)
                throw new ArgumentException("Object is not a DtwUIChartElement.");
            return _sortOrder - otherObject._sortOrder;
        }
    }

    public class DtwUiChartToolbarButton : DtwUiChartElement
    {
        private bool _isAdded;

        public DtwUiChartToolbarButton(Chart chartWindow, int sortOrder, Action<DtwUiChartElement> onRefresh, RoutedEventHandler onClick)
            : base(chartWindow, sortOrder, onRefresh)
        {
            _onClick = onClick;

            _item = new Button
            {
                Name = Id,
                VerticalAlignment = VerticalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold,
                Visibility = Visibility.Visible,
                IsEnabled = false
            };

            if (_onClick == null) return;
            _item.Click += _onClick;
        }

        private Button _item;
        public Button Button
        {
            get { return _item; }
        }
        private readonly RoutedEventHandler _onClick;

        public override void Add()
        {
            if (_item == null) return;
            if (_isAdded) return;
            ChartWindow.MainMenu.Add(_item);
            _isAdded = true;
        }

        public override void Remove()
        {
            if (_item == null) return;
            if (!_isAdded) return;
            ChartWindow.MainMenu.Remove(_item);
            _isAdded = false;
        }

        public override void Dispose()
        {
            if (_onClick != null)
                _item.Click -= _onClick;
            Remove();
        }
    }

    public class DtwUiChartPanel : DtwUiChartElement
    {
        private readonly int _width;
        private bool _isAdded;
        private bool _isVisible;
        private readonly string _guid = Guid.NewGuid().ToString("N");

        //public DtwUiChartPanel(Chart chartWindow, int sortOrder, Action<DtwUiChartElement> onRefresh, int width, bool show, SizeChangedEventHandler onSizeChanged)
        public DtwUiChartPanel(Chart chartWindow, int sortOrder, Action<DtwUiChartElement> onRefresh, int width, bool show, DragDeltaEventHandler deltaEvent)
            : base(chartWindow, sortOrder, onRefresh)
        {
            //_onSizeChanged = onSizeChanged;
            _deltaEvent = deltaEvent;
            _width = width;
            _isVisible = show;

            _item = new Grid();
            _item.SetResourceReference(Control.BackgroundProperty, "ChartBackground");

            _splitter = new GridSplitter();
            _splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            _splitter.VerticalAlignment = VerticalAlignment.Stretch;
            //_splitter.Background = Brushes.Transparent;
            _splitter.SetResourceReference(Control.BackgroundProperty, "BackgroundMainWindow");

            if (_isVisible)
                Add();
        }

        private readonly GridSplitter _splitter;

        public GridSplitter Splitter
        {
            get { return _splitter; }
        }

        private Grid _item;

        public Grid Grid
        {
            get { return _item; }
        }

        //private readonly SizeChangedEventHandler _onSizeChanged;
        private readonly DragDeltaEventHandler _deltaEvent;

        private Grid _gridWithChartTrader;
        private Grid _gridWithTabControl;

        private void add()
        {
            if (_item == null) return;
            if (_isAdded) return;

            #region removed 12/21/18
            //var tabControlStartColumn = Grid.GetColumn(ChartWindow.MainTabControl);

            //var chartGrid = ChartWindow.MainTabControl.Parent as Grid;
            //if (chartGrid == null) return;

            //chartGrid.ColumnDefinitions.Insert((tabControlStartColumn + 1), new ColumnDefinition() { Width = new GridLength(_width) });
            //chartGrid.ColumnDefinitions.Insert((tabControlStartColumn + 1), new ColumnDefinition() { Width = new GridLength(5) });

            //for (var i = 0; i < chartGrid.Children.Count; i++)
            //    if (Grid.GetColumn(chartGrid.Children[i]) > tabControlStartColumn)
            //        Grid.SetColumn(chartGrid.Children[i], Grid.GetColumn(chartGrid.Children[i]) + 2);

            //Grid.SetColumn(_splitter, Grid.GetColumn(ChartWindow.MainTabControl) + 1);
            //Grid.SetRow(_splitter, Grid.GetRow(ChartWindow.MainTabControl));

            //chartGrid.Children.Add(_splitter);

            //Grid.SetColumn(_item, Grid.GetColumn(ChartWindow.MainTabControl) + 2);
            //Grid.SetRow(_item, Grid.GetRow(ChartWindow.MainTabControl));

            //chartGrid.Children.Add(_item);
            #endregion

            #region added 12/21/18
            _gridWithChartTrader = VisualTreeHelper.GetParent(ChartWindow.ChartTrader) as Grid;
            if (_gridWithChartTrader == null) return;

            _gridWithTabControl = VisualTreeHelper.GetParent(ChartWindow.MainTabControl) as Grid;
            if (_gridWithTabControl == null) return;

            if (_gridWithChartTrader == _gridWithTabControl) // same grid
            {
                var tmpGrid = new Grid();

                tmpGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                tmpGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                Grid.SetColumn(tmpGrid, Grid.GetColumn(_gridWithTabControl));
                Grid.SetRow(tmpGrid, Grid.GetRow(_gridWithTabControl));

                _gridWithChartTrader.Children.Remove(ChartWindow.MainTabControl);
                tmpGrid.Children.Add(ChartWindow.MainTabControl);

                _gridWithTabControl = tmpGrid;
                _gridWithChartTrader.Children.Add(_gridWithTabControl);
            }

            var gridColumnAdd = _gridWithTabControl.ColumnDefinitions.Count;
            _gridWithTabControl.ColumnDefinitions.Add(new ColumnDefinition { Name = "_" + _guid + "split", Width = new GridLength(5) });

            Grid.SetColumn(_splitter, gridColumnAdd);
            Grid.SetRow(_splitter, 0);
            _gridWithTabControl.Children.Add(_splitter);

            gridColumnAdd = _gridWithTabControl.ColumnDefinitions.Count;
            var colDef = new ColumnDefinition { Name = "_" + _guid + "panel", Width = GridLength.Auto };
            _gridWithTabControl.ColumnDefinitions.Add(colDef);

            Grid.SetColumn(_item, gridColumnAdd);
            Grid.SetRow(_item, 0);
            _gridWithTabControl.Children.Add(_item);
            #endregion

            if (_deltaEvent != null)
                _splitter.DragDelta += _deltaEvent;

            _isAdded = true;
        }

        public override void Add()
        {
            if (_isVisible) add();
        }

        public override void Remove()
        {
            if (_item == null) return;
            if (!_isAdded) return;

            //if (_onSizeChanged != null)
            //    _item.SizeChanged -= _onSizeChanged;
            if (_deltaEvent != null)
                _splitter.DragDelta -= _deltaEvent;

            #region removed 12/21/18
            //var chartGrid = ChartWindow.MainTabControl.Parent as Grid;
            //if (chartGrid == null) return;

            //var tabControlStartColumn = Grid.GetColumn(ChartWindow.MainTabControl);

            //chartGrid.ColumnDefinitions.RemoveAt(tabControlStartColumn + 1);
            //chartGrid.ColumnDefinitions.RemoveAt(tabControlStartColumn + 1);

            //chartGrid.Children.Remove(_splitter);
            //chartGrid.Children.Remove(_item);

            //for (var i = 0; i < chartGrid.Children.Count; i++)
            //    if (Grid.GetColumn(chartGrid.Children[i]) > 0 && Grid.GetColumn(chartGrid.Children[i]) > Grid.GetColumn(_item))
            //        Grid.SetColumn(chartGrid.Children[i], Grid.GetColumn(chartGrid.Children[i]) - 2);
            #endregion

            #region added 12/21/18
            if (_gridWithChartTrader == null) return;
            if (_gridWithTabControl == null) return;

            var colDefs = new List<ColumnDefinition>();
            foreach (var colDef in _gridWithTabControl.ColumnDefinitions)
            {
                if (colDef.Name.StartsWith("_" + _guid))
                {
                    colDefs.Add(colDef);
                }
            }
            foreach (var colDef in colDefs)
            {
                _gridWithTabControl.ColumnDefinitions.Remove(colDef);
            }

            _gridWithTabControl.Children.Remove(_splitter);
            _gridWithTabControl.Children.Remove(_item);

            if (_gridWithTabControl.Children.Count == 1)
            {
                _gridWithTabControl.Children.Remove(ChartWindow.MainTabControl);
                Grid.SetColumn(ChartWindow.MainTabControl, Grid.GetColumn(_gridWithTabControl));
                Grid.SetRow(ChartWindow.MainTabControl, Grid.GetRow(_gridWithTabControl));
                _gridWithChartTrader.Children.Add(ChartWindow.MainTabControl);
            }
            #endregion

            _isAdded = false;
        }

        public void MakeVisible(bool show)
        {
            _isVisible = show;
            if (_isVisible) Add();
            else Remove();
        }

        public override void Dispose()
        {
            Remove();
        }
    }

    #endregion
}
