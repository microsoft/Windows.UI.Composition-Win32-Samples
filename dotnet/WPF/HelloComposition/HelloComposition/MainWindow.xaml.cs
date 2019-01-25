using System;
using System.Collections.Generic;
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

namespace HelloComposition
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // This line sizes the app window to fit the page content. It's optional.
            Application.Current.MainWindow.SizeToContent = SizeToContent.WidthAndHeight;

            var currentDpi = VisualTreeHelper.GetDpi(this);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var currentDpi = VisualTreeHelper.GetDpi(this);

            Random random = new Random();
            float size = random.Next(50, 150);
            float offsetX = random.Next(0, (int)(ControlHostElement.ActualWidth - size));
            float offsetY = random.Next(0, (int)(ControlHostElement.ActualHeight - 68.5 - size));
            ControlHostElement.AddElement(size, offsetX, offsetY);
        }
    }
}
