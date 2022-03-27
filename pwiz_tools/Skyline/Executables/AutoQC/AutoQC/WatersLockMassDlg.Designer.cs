namespace AutoQC
{
    partial class WatersLockmassDlg
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
            this.textLockmassTolerance = new System.Windows.Forms.TextBox();
            this.labelTolerance = new System.Windows.Forms.Label();
            this.labelLockMassInstructions = new System.Windows.Forms.Label();
            this.textLockmassNegative = new System.Windows.Forms.TextBox();
            this.labelNegative = new System.Windows.Forms.Label();
            this.textLockmassPositive = new System.Windows.Forms.TextBox();
            this.labelPositive = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // textLockmassTolerance
            // 
            this.textLockmassTolerance.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.textLockmassTolerance.Location = new System.Drawing.Point(31, 156);
            this.textLockmassTolerance.Margin = new System.Windows.Forms.Padding(2);
            this.textLockmassTolerance.Name = "textLockmassTolerance";
            this.textLockmassTolerance.Size = new System.Drawing.Size(74, 20);
            this.textLockmassTolerance.TabIndex = 27;
            // 
            // labelTolerance
            // 
            this.labelTolerance.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelTolerance.AutoSize = true;
            this.labelTolerance.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.labelTolerance.Location = new System.Drawing.Point(28, 140);
            this.labelTolerance.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelTolerance.Name = "labelTolerance";
            this.labelTolerance.Size = new System.Drawing.Size(55, 13);
            this.labelTolerance.TabIndex = 26;
            this.labelTolerance.Text = "&Tolerance";
            // 
            // labelLockMassInstructions
            // 
            this.labelLockMassInstructions.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.labelLockMassInstructions.Location = new System.Drawing.Point(9, 13);
            this.labelLockMassInstructions.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelLockMassInstructions.Name = "labelLockMassInstructions";
            this.labelLockMassInstructions.Size = new System.Drawing.Size(283, 40);
            this.labelLockMassInstructions.TabIndex = 21;
            this.labelLockMassInstructions.Text = "Leave these fields empty if no lockspray was used, or if lockmass correction has " +
    "already been applied.";
            // 
            // textLockmassNegative
            // 
            this.textLockmassNegative.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.textLockmassNegative.Location = new System.Drawing.Point(31, 116);
            this.textLockmassNegative.Margin = new System.Windows.Forms.Padding(2);
            this.textLockmassNegative.Name = "textLockmassNegative";
            this.textLockmassNegative.Size = new System.Drawing.Size(76, 20);
            this.textLockmassNegative.TabIndex = 25;
            // 
            // labelNegative
            // 
            this.labelNegative.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelNegative.AutoSize = true;
            this.labelNegative.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.labelNegative.Location = new System.Drawing.Point(28, 100);
            this.labelNegative.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelNegative.Name = "labelNegative";
            this.labelNegative.Size = new System.Drawing.Size(27, 13);
            this.labelNegative.TabIndex = 24;
            this.labelNegative.Text = "E&SI-";
            // 
            // textLockmassPositive
            // 
            this.textLockmassPositive.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.textLockmassPositive.Location = new System.Drawing.Point(31, 75);
            this.textLockmassPositive.Margin = new System.Windows.Forms.Padding(2);
            this.textLockmassPositive.Name = "textLockmassPositive";
            this.textLockmassPositive.Size = new System.Drawing.Size(76, 20);
            this.textLockmassPositive.TabIndex = 23;
            // 
            // labelPositive
            // 
            this.labelPositive.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelPositive.AutoSize = true;
            this.labelPositive.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.labelPositive.Location = new System.Drawing.Point(28, 59);
            this.labelPositive.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelPositive.Name = "labelPositive";
            this.labelPositive.Size = new System.Drawing.Size(30, 13);
            this.labelPositive.TabIndex = 22;
            this.labelPositive.Text = "&ESI+";
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btnCancel.Location = new System.Drawing.Point(187, 153);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(2);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(56, 22);
            this.btnCancel.TabIndex = 29;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btnOk.Location = new System.Drawing.Point(187, 124);
            this.btnOk.Margin = new System.Windows.Forms.Padding(2);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(56, 21);
            this.btnOk.TabIndex = 28;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // WatersLockmassDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(286, 201);
            this.Controls.Add(this.textLockmassTolerance);
            this.Controls.Add(this.labelTolerance);
            this.Controls.Add(this.labelLockMassInstructions);
            this.Controls.Add(this.textLockmassNegative);
            this.Controls.Add(this.labelNegative);
            this.Controls.Add(this.textLockmassPositive);
            this.Controls.Add(this.labelPositive);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "WatersLockmassDlg";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Waters Lockmass Correction";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textLockmassTolerance;
        private System.Windows.Forms.Label labelTolerance;
        private System.Windows.Forms.Label labelLockMassInstructions;
        private System.Windows.Forms.TextBox textLockmassNegative;
        private System.Windows.Forms.Label labelNegative;
        private System.Windows.Forms.TextBox textLockmassPositive;
        private System.Windows.Forms.Label labelPositive;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
    }
}