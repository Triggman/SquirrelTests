using LoggingConsole.Module.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LoggingConsole.Module.Views
{
    /// <summary>
    /// Interaction logic for MessageQueueView.xaml
    /// </summary>
    public partial class MessageQueueView : UserControl
    {
        public MessageQueueView()
        {
            InitializeComponent();
        }


        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof(MessageQueueViewModel), typeof(MessageQueueView),
            new PropertyMetadata(default(MessageQueueViewModel), OnViewModelChanged));


        public MessageQueueViewModel ViewModel
        {
            get => (MessageQueueViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public static readonly DependencyProperty ShouldAutoScrollProperty = DependencyProperty.Register(
            "ShouldAutoScroll", typeof(bool), typeof(MessageQueueView), new PropertyMetadata(true));



        public bool ShouldAutoScroll
        {
            get => (bool)GetValue(ShouldAutoScrollProperty);
            set => SetValue(ShouldAutoScrollProperty, value);
        }

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewModel = (MessageQueueViewModel)e.NewValue;
            if (viewModel?.DisplayItems == null) return;

            var sender = (MessageQueueView)d;
            viewModel.DisplayItems.CollectionChanged += sender.DisplayingItemsQuantityChanged;
        }

        private void DisplayingItemsQuantityChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add) return;
            if (ShouldAutoScroll)
                PART_Scroller.ScrollToEnd();
        }
    }
}
