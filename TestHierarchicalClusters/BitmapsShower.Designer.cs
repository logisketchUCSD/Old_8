namespace TestHierarchicalClusters
{
    partial class BitmapsShower
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
            this.pictureBoxTest = new System.Windows.Forms.PictureBox();
            this.pictureBoxTemplate = new System.Windows.Forms.PictureBox();
            this.pictureBoxOverlay = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxTest)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxTemplate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxOverlay)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBoxTest
            // 
            this.pictureBoxTest.Location = new System.Drawing.Point(12, 12);
            this.pictureBoxTest.Name = "pictureBoxTest";
            this.pictureBoxTest.Size = new System.Drawing.Size(96, 96);
            this.pictureBoxTest.TabIndex = 0;
            this.pictureBoxTest.TabStop = false;
            // 
            // pictureBoxTemplate
            // 
            this.pictureBoxTemplate.Location = new System.Drawing.Point(114, 12);
            this.pictureBoxTemplate.Name = "pictureBoxTemplate";
            this.pictureBoxTemplate.Size = new System.Drawing.Size(96, 96);
            this.pictureBoxTemplate.TabIndex = 1;
            this.pictureBoxTemplate.TabStop = false;
            // 
            // pictureBoxOverlay
            // 
            this.pictureBoxOverlay.Location = new System.Drawing.Point(216, 12);
            this.pictureBoxOverlay.Name = "pictureBoxOverlay";
            this.pictureBoxOverlay.Size = new System.Drawing.Size(96, 96);
            this.pictureBoxOverlay.TabIndex = 2;
            this.pictureBoxOverlay.TabStop = false;
            // 
            // BitmapsShower
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(326, 122);
            this.Controls.Add(this.pictureBoxOverlay);
            this.Controls.Add(this.pictureBoxTemplate);
            this.Controls.Add(this.pictureBoxTest);
            this.Name = "BitmapsShower";
            this.Text = "BitmapsShower";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxTest)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxTemplate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxOverlay)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBoxTest;
        private System.Windows.Forms.PictureBox pictureBoxTemplate;
        private System.Windows.Forms.PictureBox pictureBoxOverlay;
    }
}