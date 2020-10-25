using Afterbunny.UI.WPF;
using Afterbunny.UI.WPF.Core;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RemoteVisionConsole.Module.Views
{
    /// <summary>
    /// Interaction logic for DataSheet.xaml
    /// </summary>
    public partial class DataSheet : UserControl
    {
        private readonly SynchronizationContext _uiContext;


        public DataSheet()
        {
            InitializeComponent();
            _uiContext = SynchronizationContext.Current;
        }


        public IEnumerable<Dictionary<string, object>> ItemsSource
        {
            get { return (IEnumerable<Dictionary<string, object>>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemsSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable<Dictionary<string, object>>), typeof(DataSheet), new PropertyMetadata(default(IEnumerable<Dictionary<string, object>>), OnItemsSourceChanged));

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Handle new value
            var newCollection = (IEnumerable<Dictionary<string, object>>)e.NewValue;
            var view = (DataSheet)d;

            if (newCollection != null)
            {

                if (newCollection.Any()) view.DisplayAllRows(newCollection, true);

                if (newCollection is ObservableCollection<Dictionary<string, object>> observableCollection)
                {
                    observableCollection.CollectionChanged += view.ChangeRows;
                }
            }

            // Handle old value
            if (e.OldValue != null)
            {
                var oldCollection = (IEnumerable<Dictionary<string, object>>)e.OldValue;
                if (oldCollection is ObservableCollection<Dictionary<string, object>> observableCollection)
                {
                    observableCollection.CollectionChanged -= view.ChangeRows;
                }
            }
        }

        private void ChangeRows(object sender, NotifyCollectionChangedEventArgs e)
        {

            var observableCollection = (ObservableCollection<Dictionary<string, object>>)sender;
            _uiContext.Post(new SendOrPostCallback(o =>
            {

                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    if (observableCollection.Count == 1) GenHeaderRow(observableCollection[0]);
                    AddRows(e.NewItems.Cast<Dictionary<string, object>>());
                }
                else
                {
                    DisplayAllRows(observableCollection, false);
                }
            }), null);
        }

        private void AddRows(IEnumerable<Dictionary<string, object>> newRows)
        {
            foreach (var dict in newRows)
            {
                var rowGrid = GenGridWithIndexedShareSizeGroups(dict.Count);
                FillRow(dict.Values, rowGrid);
                var border = new Border { Child = rowGrid, BorderThickness = new Thickness(0, 0, 0, 0.5), BorderBrush = Brushes.Black };

                stackPanel.Children.Add(border);

            }
        }

        private void DisplayAllRows(IEnumerable<Dictionary<string, object>> collection, bool forceGenerateHeaderRow)
        {
            if (forceGenerateHeaderRow) GenHeaderRow(collection.First());
            AddRows(collection);
        }

        private void GenHeaderRow(Dictionary<string, object> rowData)
        {
            stackPanel.Children.Clear();
            Grid headerGrid = GenGridWithIndexedShareSizeGroups(rowData.Count);
            headerGrid.ShowGridLines = true;
            FillRow(rowData.Keys, headerGrid);
            var border = new Border { Child = headerGrid, BorderThickness = new Thickness(0, 0, 0, 1), Background = Brushes.AliceBlue };
            border.SetResourceReference(Border.BorderBrushProperty, new ThemeResourceExtension { ResourceKey = ThemeResourceKey.ControlBorder });
            border.SetResourceReference(Control.BackgroundProperty, new ThemeResourceExtension { ResourceKey = ThemeResourceKey.ControlBackground });

            stackPanel.Children.Add(border);
        }

        private static void FillRow(IEnumerable<object> rowData, Grid rowGrid)
        {
            // Add headers
            var colIndex = 0;
            foreach (var item in rowData)
            {
                var textBlock = new TextBlock() { Text = item.ToString(), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(5d, 0d, 5d, 0d) };
                rowGrid.Children.Add(textBlock);
                Grid.SetColumn(textBlock, colIndex);
                colIndex++;
            }
        }

        private static Grid GenGridWithIndexedShareSizeGroups(int columnCount)
        {
            var grid = new Grid();
            // Add column definitions
            for (int col = 0; col < columnCount; col++)
            {
                var columnDef = new ColumnDefinition { Width = GridLength.Auto, SharedSizeGroup = $"C{col}" };
                grid.ColumnDefinitions.Add(columnDef);
            }

            return grid;
        }
    }
}
