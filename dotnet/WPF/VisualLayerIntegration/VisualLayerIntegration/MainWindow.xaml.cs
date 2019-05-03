//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Windows;

namespace VisualLayerIntegration
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Random random = new Random();
        private static readonly string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private static readonly string[] customerFirstNames = new [] { "Angel", "Josephine",
            "Wade", "Christie", "Whitney", "Ismael", "Alexandra", "Rhonda", "Dawn", "Roman", "Emanuel",
            "Evan", "Aaron", "Bill", "Margaret", "Mandy", "Carlton", "Cornelius", "Cora", "Alejandro",
            "Annette", "Bertha", "John", "Billy", "Randall" };
        private static readonly string[] customerLastNames = new [] { "Murphy", "Swanson", "Sandoval",
            "Moore", "Adkins", "Tucker", "Cook", "Fernandez", "Schwartz", "Sharp", "Bryant", "Gross", "Spencer",
            "Powers", "Hunter", "Moreno", "Baldwin", "Stewart", "Rice", "Watkins", "Hawkins", "Dean", "Howard",
            "Bailey", "Gill" };

        public MainWindow()
        {
            InitializeComponent();

            CustomerGrid.ItemsSource = customerFirstNames.Select(_ => GenerateRandomCustomer()).ToArray();
        }

        // Send customer info to the control on row select.
        private void CustomerGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            barGraphHost.DataContext = (Customer)CustomerGrid.SelectedItem;
        }

        private static Customer GenerateRandomCustomer()
        {
            return new Customer
            {
                ID = GenerateRandomID(),
                FirstName = customerFirstNames[random.Next(0, customerFirstNames.Length - 1)],
                LastName = customerLastNames[random.Next(0, customerLastNames.Length - 1)],
                CustomerSince = GenerateRandomDay(),
                NewsletterSubscriber = random.NextDouble() >= 0.5,
                Data = GenerateRandomData()
            };
        }

        // Generate random customer ID
        private static string GenerateRandomID()
        {
            // Create string of random chars for ID. ID is not necessarily unique.
            return new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // Generate random customer data.
        private static double[] GenerateRandomData()
        {
            var numDataPoints = 6;
            var data = new double[numDataPoints];

            for (int j = 0; j < numDataPoints; j++)
            {
                data[j] = random.Next(50, 300);
            }
            return data;
        }

        // Generate random date to use for the customer info.
        private static DateTime GenerateRandomDay()
        {
            var start = new DateTime(1995, 1, 1);
            var range = (DateTime.Today - start).Days;
            return start.AddDays(random.Next(range));
        }
    }
}
