namespace pwiz.Skyline.EditUI
{
    partial class AssociateProteinsDlg
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AssociateProteinsDlg));
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblDescription = new System.Windows.Forms.Label();
            this.btnOk = new System.Windows.Forms.Button();
            this.cbGroupProteins = new System.Windows.Forms.CheckBox();
            this.comboProteinParsimony = new System.Windows.Forms.ComboBox();
            this.comboSharedPeptides = new System.Windows.Forms.ComboBox();
            this.gbParsimonyOptions = new System.Windows.Forms.GroupBox();
            this.lblMinPeptides = new System.Windows.Forms.Label();
            this.numMinPeptides = new System.Windows.Forms.NumericUpDown();
            this.lblSharedPeptides = new System.Windows.Forms.Label();
            this.lblParsimonyOption = new System.Windows.Forms.Label();
            this.rbFASTA = new System.Windows.Forms.RadioButton();
            this.rbBackgroundProteome = new System.Windows.Forms.RadioButton();
            this.comboBackgroundProteome = new System.Windows.Forms.ComboBox();
            this.tbxFastaTargets = new System.Windows.Forms.TextBox();
            this.browseFastaTargetsBtn = new System.Windows.Forms.Button();
            this.dgvAssociateResults = new pwiz.Common.Controls.CommonDataGridView();
            this.headerColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.mappedColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.unmappedColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.targetsColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblResults = new System.Windows.Forms.Label();
            this.gbParsimonyOptions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMinPeptides)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAssociateResults)).BeginInit();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // lblDescription
            // 
            resources.ApplyResources(this.lblDescription, "lblDescription");
            this.lblDescription.Name = "lblDescription";
            // 
            // btnOk
            // 
            resources.ApplyResources(this.btnOk, "btnOk");
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Name = "btnOk";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnApplyChanges_Click);
            // 
            // cbGroupProteins
            // 
            resources.ApplyResources(this.cbGroupProteins, "cbGroupProteins");
            this.cbGroupProteins.Name = "cbGroupProteins";
            this.cbGroupProteins.UseVisualStyleBackColor = true;
            this.cbGroupProteins.CheckedChanged += new System.EventHandler(this.cbGroupProteins_CheckedChanged);
            // 
            // comboProteinParsimony
            // 
            resources.ApplyResources(this.comboProteinParsimony, "comboProteinParsimony");
            this.comboProteinParsimony.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboProteinParsimony.FormattingEnabled = true;
            this.comboProteinParsimony.Name = "comboProteinParsimony";
            this.comboProteinParsimony.SelectedIndexChanged += new System.EventHandler(this.comboParsimony_SelectedIndexChanged);
            // 
            // comboSharedPeptides
            // 
            resources.ApplyResources(this.comboSharedPeptides, "comboSharedPeptides");
            this.comboSharedPeptides.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboSharedPeptides.FormattingEnabled = true;
            this.comboSharedPeptides.Name = "comboSharedPeptides";
            this.comboSharedPeptides.SelectedIndexChanged += new System.EventHandler(this.comboParsimony_SelectedIndexChanged);
            // 
            // gbParsimonyOptions
            // 
            resources.ApplyResources(this.gbParsimonyOptions, "gbParsimonyOptions");
            this.gbParsimonyOptions.Controls.Add(this.lblMinPeptides);
            this.gbParsimonyOptions.Controls.Add(this.numMinPeptides);
            this.gbParsimonyOptions.Controls.Add(this.lblSharedPeptides);
            this.gbParsimonyOptions.Controls.Add(this.lblParsimonyOption);
            this.gbParsimonyOptions.Controls.Add(this.comboSharedPeptides);
            this.gbParsimonyOptions.Controls.Add(this.cbGroupProteins);
            this.gbParsimonyOptions.Controls.Add(this.comboProteinParsimony);
            this.gbParsimonyOptions.Name = "gbParsimonyOptions";
            this.gbParsimonyOptions.TabStop = false;
            // 
            // lblMinPeptides
            // 
            resources.ApplyResources(this.lblMinPeptides, "lblMinPeptides");
            this.lblMinPeptides.Name = "lblMinPeptides";
            // 
            // numMinPeptides
            // 
            resources.ApplyResources(this.numMinPeptides, "numMinPeptides");
            this.numMinPeptides.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numMinPeptides.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numMinPeptides.Name = "numMinPeptides";
            this.numMinPeptides.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numMinPeptides.ValueChanged += new System.EventHandler(this.numMinPeptides_ValueChanged);
            // 
            // lblSharedPeptides
            // 
            resources.ApplyResources(this.lblSharedPeptides, "lblSharedPeptides");
            this.lblSharedPeptides.Name = "lblSharedPeptides";
            // 
            // lblParsimonyOption
            // 
            resources.ApplyResources(this.lblParsimonyOption, "lblParsimonyOption");
            this.lblParsimonyOption.Name = "lblParsimonyOption";
            // 
            // rbFASTA
            // 
            resources.ApplyResources(this.rbFASTA, "rbFASTA");
            this.rbFASTA.Checked = true;
            this.rbFASTA.Name = "rbFASTA";
            this.rbFASTA.TabStop = true;
            this.rbFASTA.UseVisualStyleBackColor = true;
            this.rbFASTA.CheckedChanged += new System.EventHandler(this.rbCheckedChanged);
            // 
            // rbBackgroundProteome
            // 
            resources.ApplyResources(this.rbBackgroundProteome, "rbBackgroundProteome");
            this.rbBackgroundProteome.Name = "rbBackgroundProteome";
            this.rbBackgroundProteome.UseVisualStyleBackColor = true;
            this.rbBackgroundProteome.CheckedChanged += new System.EventHandler(this.rbCheckedChanged);
            // 
            // comboBackgroundProteome
            // 
            this.comboBackgroundProteome.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.comboBackgroundProteome, "comboBackgroundProteome");
            this.comboBackgroundProteome.FormattingEnabled = true;
            this.comboBackgroundProteome.Name = "comboBackgroundProteome";
            this.comboBackgroundProteome.SelectedIndexChanged += new System.EventHandler(this.comboBackgroundProteome_SelectedIndexChanged);
            // 
            // tbxFastaTargets
            // 
            resources.ApplyResources(this.tbxFastaTargets, "tbxFastaTargets");
            this.tbxFastaTargets.Name = "tbxFastaTargets";
            this.tbxFastaTargets.TextChanged += new System.EventHandler(this.tbxFastaTargets_TextChanged);
            // 
            // browseFastaTargetsBtn
            // 
            resources.ApplyResources(this.browseFastaTargetsBtn, "browseFastaTargetsBtn");
            this.browseFastaTargetsBtn.Name = "browseFastaTargetsBtn";
            this.browseFastaTargetsBtn.UseVisualStyleBackColor = true;
            this.browseFastaTargetsBtn.Click += new System.EventHandler(this.btnUseFasta_Click);
            // 
            // dgvAssociateResults
            // 
            this.dgvAssociateResults.AllowUserToAddRows = false;
            this.dgvAssociateResults.AllowUserToDeleteRows = false;
            this.dgvAssociateResults.AllowUserToResizeColumns = false;
            this.dgvAssociateResults.AllowUserToResizeRows = false;
            resources.ApplyResources(this.dgvAssociateResults, "dgvAssociateResults");
            this.dgvAssociateResults.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvAssociateResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvAssociateResults.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.headerColumn,
            this.mappedColumn,
            this.unmappedColumn,
            this.targetsColumn});
            this.dgvAssociateResults.Name = "dgvAssociateResults";
            this.dgvAssociateResults.ReadOnly = true;
            this.dgvAssociateResults.RowHeadersVisible = false;
            this.dgvAssociateResults.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            this.dgvAssociateResults.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dgvAssociateResults.ShowEditingIcon = false;
            this.dgvAssociateResults.VirtualMode = true;
            this.dgvAssociateResults.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.dgvAssociateResults_CellValueNeeded);
            // 
            // headerColumn
            // 
            resources.ApplyResources(this.headerColumn, "headerColumn");
            this.headerColumn.Name = "headerColumn";
            this.headerColumn.ReadOnly = true;
            // 
            // mappedColumn
            // 
            resources.ApplyResources(this.mappedColumn, "mappedColumn");
            this.mappedColumn.Name = "mappedColumn";
            this.mappedColumn.ReadOnly = true;
            // 
            // unmappedColumn
            // 
            resources.ApplyResources(this.unmappedColumn, "unmappedColumn");
            this.unmappedColumn.Name = "unmappedColumn";
            this.unmappedColumn.ReadOnly = true;
            // 
            // targetsColumn
            // 
            resources.ApplyResources(this.targetsColumn, "targetsColumn");
            this.targetsColumn.Name = "targetsColumn";
            this.targetsColumn.ReadOnly = true;
            // 
            // lblResults
            // 
            resources.ApplyResources(this.lblResults, "lblResults");
            this.lblResults.Name = "lblResults";
            // 
            // AssociateProteinsDlg
            // 
            this.AcceptButton = this.btnOk;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.Controls.Add(this.lblResults);
            this.Controls.Add(this.dgvAssociateResults);
            this.Controls.Add(this.tbxFastaTargets);
            this.Controls.Add(this.browseFastaTargetsBtn);
            this.Controls.Add(this.comboBackgroundProteome);
            this.Controls.Add(this.rbBackgroundProteome);
            this.Controls.Add(this.rbFASTA);
            this.Controls.Add(this.gbParsimonyOptions);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AssociateProteinsDlg";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.gbParsimonyOptions.ResumeLayout(false);
            this.gbParsimonyOptions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMinPeptides)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAssociateResults)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.CheckBox cbGroupProteins;
        private System.Windows.Forms.ComboBox comboProteinParsimony;
        private System.Windows.Forms.ComboBox comboSharedPeptides;
        private System.Windows.Forms.GroupBox gbParsimonyOptions;
        private System.Windows.Forms.Label lblSharedPeptides;
        private System.Windows.Forms.Label lblParsimonyOption;
        private System.Windows.Forms.RadioButton rbFASTA;
        private System.Windows.Forms.RadioButton rbBackgroundProteome;
        private System.Windows.Forms.ComboBox comboBackgroundProteome;
        private System.Windows.Forms.TextBox tbxFastaTargets;
        private System.Windows.Forms.Button browseFastaTargetsBtn;
        private pwiz.Common.Controls.CommonDataGridView dgvAssociateResults;
        private System.Windows.Forms.Label lblResults;
        private System.Windows.Forms.DataGridViewTextBoxColumn headerColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn mappedColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn unmappedColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn targetsColumn;
        private System.Windows.Forms.Label lblMinPeptides;
        private System.Windows.Forms.NumericUpDown numMinPeptides;
    }
}