namespace VisualLayerIntegration
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.CustomerGrid = new System.Windows.Forms.DataGridView();
            this.barGraphHostControl1 = new VisualLayerIntegration.BarGraphHostControl();
            this.label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.CustomerGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 20F);
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(372, 37);
            this.label1.TabIndex = 0;
            this.label1.Text = "Customer Management Portal";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))));
            this.label2.Location = new System.Drawing.Point(17, 54);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(242, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Select a row to show data graph on the right.";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Italic);
            this.label3.Location = new System.Drawing.Point(17, 72);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(337, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Select another to see the graph animate from one data set to the next.";
            // 
            // CustomerGrid
            // 
            this.CustomerGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.CustomerGrid.Location = new System.Drawing.Point(12, 106);
            this.CustomerGrid.Name = "CustomerGrid";
            this.CustomerGrid.Size = new System.Drawing.Size(560, 600);
            this.CustomerGrid.TabIndex = 3;
            this.CustomerGrid.SelectionChanged += new System.EventHandler(this.CustomerGrid_SelectionChanged);
            // 
            // barGraphHostControl1
            // 
            this.barGraphHostControl1.Location = new System.Drawing.Point(596, 106);
            this.barGraphHostControl1.Name = "barGraphHostControl1";
            this.barGraphHostControl1.Size = new System.Drawing.Size(645, 600);
            this.barGraphHostControl1.TabIndex = 4;
            this.barGraphHostControl1.Text = "barGraphHostControl1";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 14.25F);
            this.label4.Location = new System.Drawing.Point(591, 72);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(168, 25);
            this.label4.TabIndex = 5;
            this.label4.Text = "Investment History";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1258, 718);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.barGraphHostControl1);
            this.Controls.Add(this.CustomerGrid);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.CustomerGrid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DataGridView CustomerGrid;
        private BarGraphHostControl barGraphHostControl1;
        private System.Windows.Forms.Label label4;
    }
}

