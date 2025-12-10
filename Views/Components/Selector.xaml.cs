using System;
using System.Windows;
using System.Windows.Controls;

namespace Kinect_Middleware.Views.Components {
    /// <summary>
    /// Interaction logic for Selector.xaml
    /// </summary>
    public partial class Selector : UserControl {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(Selector));

        public static readonly DependencyProperty ItemsSourceProperty =
                    DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(Selector));

        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(Selector));

        public string Text {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public object ItemsSource {
            get { return GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public int SelectedIndex {
            get { return (int)GetValue(SelectedIndexProperty); }
            set { SetValue(SelectedIndexProperty, value); }
        }

        public Selector() {
            InitializeComponent();
            DataContext = this;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var args = new SelectorChangedEventArgs(ComboBox.SelectedIndex);
            OnSelectorChanged(args);
        }

        public event EventHandler<SelectorChangedEventArgs> SelectorChanged;

        protected virtual void OnSelectorChanged(SelectorChangedEventArgs e) {
            SelectorChanged?.Invoke(this, e);
        }
    }

    public class SelectorChangedEventArgs : EventArgs {
        public int SelectedIndex { get; }

        public SelectorChangedEventArgs(int selectedIndex) {
            SelectedIndex = selectedIndex;
        }
    }
}
