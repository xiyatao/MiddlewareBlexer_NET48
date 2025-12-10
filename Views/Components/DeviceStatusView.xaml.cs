using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Kinect_Middleware.Views.Components
{
    /// <summary>
    /// Interaction logic for DeviceStatusView.xaml
    /// </summary>
    public partial class DeviceStatusView : UserControl
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text",
            typeof(string),
            typeof(DeviceStatusView),
            new PropertyMetadata(string.Empty)
        );

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
            "Status",
            typeof(bool),
            typeof(DeviceStatusView),
            new PropertyMetadata(false)
        );

        public bool Status
        {
            get => (bool)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        public DeviceStatusView()
        {
            InitializeComponent();
        }
    }

    /// <summary>
    /// Converts boolean values to color (Green for true, Red for false)
    /// </summary>
    public class BoolToColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? Colors.Green : Colors.Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
