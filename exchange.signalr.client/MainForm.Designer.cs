namespace exchange.signalr.client
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.currentPriceListView = new System.Windows.Forms.ListView();
            this.productNameColumnHeader = new System.Windows.Forms.ColumnHeader();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.currentPriceColumnHeader = new System.Windows.Forms.ColumnHeader();
            this.SuspendLayout();
            // 
            // currentPriceListView
            // 
            this.currentPriceListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.productNameColumnHeader,
            this.currentPriceColumnHeader});
            this.currentPriceListView.FullRowSelect = true;
            this.currentPriceListView.GridLines = true;
            this.currentPriceListView.HideSelection = false;
            this.currentPriceListView.Location = new System.Drawing.Point(12, 12);
            this.currentPriceListView.Name = "currentPriceListView";
            this.currentPriceListView.Size = new System.Drawing.Size(776, 426);
            this.currentPriceListView.TabIndex = 0;
            this.currentPriceListView.UseCompatibleStateImageBehavior = false;
            this.currentPriceListView.View = System.Windows.Forms.View.Details;
            // 
            // productNameColumnHeader
            // 
            this.productNameColumnHeader.Name = "productNameColumnHeader";
            this.productNameColumnHeader.Text = "Product Name";
            this.productNameColumnHeader.Width = 200;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Name = "columnHeader1";
            // 
            // currentPriceColumnHeader
            // 
            this.currentPriceColumnHeader.Name = "currentPriceColumnHeader";
            this.currentPriceColumnHeader.Text = "CurrentPrice";
            this.currentPriceColumnHeader.Width = 200;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.currentPriceListView);
            this.Name = "MainForm";
            this.Text = "MainForm";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView currentPriceListView;
        private System.Windows.Forms.ColumnHeader productNameColumnHeader;
        private System.Windows.Forms.ColumnHeader currentPriceColumnHeader;
        private System.Windows.Forms.ColumnHeader columnHeader1;
    }
}

