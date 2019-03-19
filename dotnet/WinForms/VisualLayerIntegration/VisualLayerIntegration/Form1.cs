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
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace VisualLayerIntegration
{
    public partial class Form1 : Form
    {
        private Random random = new Random();
        private string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private string[] customerFirstNames = new string[] { "Angel", "Josephine", "Wade", "Christie", "Whitney", "Ismael", "Alexandra", "Rhonda", "Dawn", "Roman", "Emanuel", "Evan", "Aaron", "Bill", "Margaret", "Mandy", "Carlton", "Cornelius", "Cora", "Alejandro", "Annette", "Bertha", "John", "Billy", "Randall" };
        private string[] customerLastNames = new string[] { "Murphy", "Swanson", "Sandoval", "Moore", "Adkins", "Tucker", "Cook", "Fernandez", "Schwartz", "Sharp", "Bryant", "Gross", "Spencer", "Powers", "Hunter", "Moreno", "Baldwin", "Stewart", "Rice", "Watkins", "Hawkins", "Dean", "Howard", "Bailey", "Gill" };

        public Form1()
        {
            InitializeComponent();
        }

        // Generate customers and pass data to grid.
        private void Form1_Load(object sender, EventArgs e)
        {
            List<Customer> customers = new List<Customer>();
            for (int i = 0; i < customerFirstNames.Length; i++)
            {
                string id = new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
                customers.Add(new Customer(id, customerFirstNames[i], customerLastNames[random.Next(0, customerLastNames.Length - 1)], GenerateRandomDay(), random.NextDouble() >= 0.5, GenerateRandomData()));
            }

            CustomerGrid.DataSource = customers;
        }

        // Send customer info to the control on row select.
        private void CustomerGrid_SelectionChanged(object sender, EventArgs e)
        {
            barGraphHostControl1.UpdateGraph((Customer)CustomerGrid.CurrentRow.DataBoundItem);
        }

        // Notify the graph control of DPI changes.
        private void Form1_DpiChanged(object sender, DpiChangedEventArgs e)
        {
            barGraphHostControl1.UpdateSize(e.DeviceDpiNew);
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
