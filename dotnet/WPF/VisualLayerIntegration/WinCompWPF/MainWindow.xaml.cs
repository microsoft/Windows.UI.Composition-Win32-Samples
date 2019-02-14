using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using BarGraphUtility;
using System.Windows.Media;

namespace WinCompWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Application app;
        Window myWindow;

        private Random random = new Random();
        private string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private string[] customerFirstNames = new string[] { "Angel", "Josephine", "Wade", "Christie", "Whitney", "Ismael", "Alexandra", "Rhonda", "Dawn", "Roman", "Emanuel", "Evan", "Aaron", "Bill", "Margaret", "Mandy", "Carlton", "Cornelius", "Cora", "Alejandro", "Annette", "Bertha", "John", "Billy", "Randall" };
        private string[] customerLastNames = new string[] { "Murphy", "Swanson", "Sandoval", "Moore", "Adkins", "Tucker", "Cook", "Fernandez", "Schwartz", "Sharp", "Bryant", "Gross", "Spencer", "Powers", "Hunter", "Moreno", "Baldwin", "Stewart", "Rice", "Watkins", "Hawkins", "Dean", "Howard", "Bailey", "Gill" };

        private HwndHostControl hostControl;
        private BarGraph currentGraph;
        private Windows.UI.Composition.ContainerVisual graphContainer;

        private double currentDpiX = 96.0;
        private double currentDpiY = 96.0;

        public MainWindow()
        {
            InitializeComponent();
        }

        /*
         * Generate customers, pass data to grid, and create host control
         */
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            app = System.Windows.Application.Current;
            myWindow = app.MainWindow;
            
            List<Customer> customers = new List<Customer>();
            for (int i = 0; i < customerFirstNames.Length; i++)
            {
                string id = new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
                customers.Add(new Customer(id, customerFirstNames[i], customerLastNames[random.Next(0, customerLastNames.Length - 1)], GenerateRandomDay(), random.NextDouble() >= 0.5, GenerateRandomData()));
            }

            CustomerGrid.ItemsSource = customers;

            var currentDpi = VisualTreeHelper.GetDpi(CustomerGrid);
            currentDpiX = currentDpi.PixelsPerInchX;
            currentDpiY = currentDpi.PixelsPerInchY;

            ControlHostElement.Width = ControlHostElement.Height = DetailsPane.ActualHeight;
            hostControl = new HwndHostControl(ControlHostElement.Width, ControlHostElement.Height, currentDpiX, currentDpiY);

            hostControl.DpiChanged += HostControl_DpiChanged;

            hostControl.MouseMoved += HostControl_MouseMoved;
            hostControl.MouseLClick += HostControl_MouseLClick;

            ControlHostElement.Child = hostControl;
            graphContainer = hostControl.Compositor.CreateContainerVisual();
            hostControl.Child = graphContainer;
        }

        private void HostControl_MouseLClick(object sender, HwndMouseEventArgs e)
        {
            //show bar info in response to click event.
            if (currentGraph != null)
            {
                //Convert mouse position to DIP (is raised in physical pixels).
                var posDip = GetPointInDIP(e.point);

                Point adjustedTopLeft = GetControlPointInDIP(ControlHostElement);

                //Get point relative to control.
                Point relativePoint = new Point(posDip.X - adjustedTopLeft.X, posDip.Y - adjustedTopLeft.Y);

                //TODO get clicked element
                //    currentGraph.UpdateLight(relativePoint);
            }
        }

        private void HostControl_MouseMoved(object sender, HwndMouseEventArgs e)
        {
            //Adjust light position.
            if (currentGraph != null)
            {
                //Convert mouse position to DIP (is raised in physical pixels).
                var posDip = GetPointInDIP(e.point);

                Point adjustedTopLeft = GetControlPointInDIP(ControlHostElement);

                //Get point relative to control.
                Point relativePoint = new Point(posDip.X - adjustedTopLeft.X, posDip.Y - adjustedTopLeft.Y);

                //Update light position.
                currentGraph.UpdateLight(relativePoint);
            }
        }

        private Point GetPointInDIP(Point point)
        {
            var posDipX = point.X / (currentDpiX / 96.0);
            var posDipY = point.Y / (currentDpiY / 96.0);
            return new Point(posDipX, posDipY);
        }

        private Point GetControlPointInDIP(UIElement control)
        {
            //Get bounds of hwnd host control.
            Point controlTopLeft = control.PointToScreen(new Point(0, 0));  //Top left of control relative to screen.
            //Convert screen coord to DIP.
            var adjustedX = controlTopLeft.X / (currentDpiX / 96.0);
            var adjustedY = controlTopLeft.Y / (currentDpiY / 96.0);
            return new Point(adjustedX, adjustedY);
        }

        private void HostControl_DpiChanged(object sender, DpiChangedEventArgs e)
        {
            currentDpiX = e.NewDpi.PixelsPerInchX;
            currentDpiY = e.NewDpi.PixelsPerInchY;

            if (this.ActualWidth > 0 && currentGraph != null)
            {
                currentGraph.UpdateDPI(currentDpiX, currentDpiY, ControlHostElement.Width, ControlHostElement.Height);
            }

        }

        // Handle Composition tree creation and updates.
        public void UpdateGraph(Customer customer)
        {
            var graphTitle = customer.FirstName + " Investment History";
            var xAxisTitle = "Investment #";
            var yAxisTitle = "# Shares of Stock";

            // If graph already exists update values. Otherwise create new graph.
            if (graphContainer.Children.Count > 0 && currentGraph != null)
            {
                currentGraph.UpdateGraphData(graphTitle, xAxisTitle, yAxisTitle, customer.Data);
            }
            else
            {
                BarGraph graph = new BarGraph(hostControl.Compositor, hostControl.hwndHost, graphTitle, xAxisTitle, yAxisTitle,
                    (float)ControlHostElement.Width, (float)ControlHostElement.Height, currentDpiX, currentDpiY, customer.Data, 
                    true, BarGraph.GraphBarStyle.PerBarLinearGradient,
                    new List<Windows.UI.Color> { Windows.UI.Color.FromArgb(255,246, 65, 108), Windows.UI.Color.FromArgb(255, 255, 246, 183)});

                currentGraph = graph;
                graphContainer.Children.InsertAtTop(graph.GraphRoot);
            }
        }

        // Send customer info to the control on row select.
        private void CustomerGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateGraph((Customer)CustomerGrid.SelectedItem);
        }

        // Generate random customer data.
        private float[] GenerateRandomData()
        {
            var numDataPoints = 6;
            float[] data = new float[numDataPoints];

            for (int j = 0; j < numDataPoints; j++)
            {
                data[j] = random.Next(50, 300);
            }

            return data;
        }

        // Generate random date to use for the customer info.
        private DateTime GenerateRandomDay()
        {
            DateTime start = new DateTime(1995, 1, 1);
            int range = (DateTime.Today - start).Days;
            return start.AddDays(random.Next(range));
        }

    }
}
