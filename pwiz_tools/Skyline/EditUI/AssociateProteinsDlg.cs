/*
 * Original author: Yuval Boss <yuval .at. uw.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2016 University of Washington - Seattle, WA
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.AuditLog;
using pwiz.Skyline.Model.Proteome;
using pwiz.Skyline.Properties;
using pwiz.Skyline.SettingsUI;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.EditUI
{
    public partial class AssociateProteinsDlg : ModeUIInvariantFormEx,  // This dialog has nothing to do with small molecules, always display as proteomic even in mixed mode
                  IAuditLogModifier<AssociateProteinsSettings>
    {
        private readonly SkylineWindow _parent;
        private bool _isFasta;
        private ProteinAssociation _proteinAssociation;
        private readonly SettingsListComboDriver<BackgroundProteomeSpec> _driverBackgroundProteome;

        // cached per-process without persisting to settings for now
        // (discussion with Nick suggested we will automatically create lightweight background proteomes in a later PR)
        private static string _lastFastaFileName;
        private bool _reuseLastFasta;

        public string FastaFileName
        {
            get { return tbxFastaTargets.Text; }
            set { _lastFastaFileName = tbxFastaTargets.Text = value; }
        }

        public AssociateProteinsDlg(SkylineWindow parent, bool reuseLastFasta = true)
        {
            InitializeComponent();
            _parent = parent;
            _reuseLastFasta = reuseLastFasta;
            btnOk.Enabled = false;

            var peptideSettings = parent.DocumentUI.Settings.PeptideSettings;

            foreach (var proteinParsimony in Enum.GetNames(typeof(ProteinAssociation.ProteinParsimony)))
                comboProteinParsimony.Items.Add(EnumNames.ResourceManager.GetString(@"ProteinParsimony_" + proteinParsimony) ?? throw new InvalidOperationException(proteinParsimony));

            foreach (var sharedPeptides in Enum.GetNames(typeof(ProteinAssociation.SharedPeptides)))
                comboSharedPeptides.Items.Add(EnumNames.ResourceManager.GetString(@"SharedPeptides_" + sharedPeptides) ?? throw new InvalidOperationException(sharedPeptides));

            GroupProteins = peptideSettings.Filter.ParsimonySettings?.GroupProteins ?? false;
            SelectedProteinParsimony = peptideSettings.Filter.ParsimonySettings?.ProteinParsimony ?? ProteinAssociation.ProteinParsimony.KeepAllProteins;
            SelectedSharedPeptides = peptideSettings.Filter.ParsimonySettings?.SharedPeptides ?? ProteinAssociation.SharedPeptides.DuplicatedBetweenProteins;

            _driverBackgroundProteome = new SettingsListComboDriver<BackgroundProteomeSpec>(comboBackgroundProteome, Settings.Default.BackgroundProteomeList);
            _driverBackgroundProteome.LoadList(peptideSettings.BackgroundProteome.Name);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (_parent.Document.PeptideCount == 0)
            {
                MessageDlg.Show(this, Resources.ImportFastaControl_ImportFasta_The_document_does_not_contain_any_peptides_);
                Close();
            }

            if (_reuseLastFasta && !_lastFastaFileName.IsNullOrEmpty())
                tbxFastaTargets.Text = _lastFastaFileName;
        }

        private void Initialize()
        {
            if (_proteinAssociation != null)
                return;

            using (var longWaitDlg = new LongWaitDlg())
            {
                longWaitDlg.PerformWork(this, 1000, broker =>
                {
                    _proteinAssociation = new ProteinAssociation(_parent.Document, broker);
                });

                if (longWaitDlg.IsCanceled)
                    _proteinAssociation = null;
            }
        }

        public IEnumerable<KeyValuePair<ProteinAssociation.IProteinRecord, ProteinAssociation.PeptideAssociationGroup>> AssociatedProteins => _proteinAssociation?.AssociatedProteins;
        public IEnumerable<KeyValuePair<ProteinAssociation.IProteinRecord, ProteinAssociation.PeptideAssociationGroup>> ParsimoniousProteins => _proteinAssociation?.ParsimoniousProteins;
        public ProteinAssociation.IMappingResults Results => _proteinAssociation?.Results;
        public ProteinAssociation.IMappingResults FinalResults => _proteinAssociation?.FinalResults;

        public bool GroupProteins
        {
            get => cbGroupProteins.Checked;
            set => cbGroupProteins.Checked = value;
        }

        public ProteinAssociation.ProteinParsimony SelectedProteinParsimony
        {
            get => (ProteinAssociation.ProteinParsimony) comboProteinParsimony.SelectedIndex;
            set => comboProteinParsimony.SelectedIndex = (int) value;
        }

        public ProteinAssociation.SharedPeptides SelectedSharedPeptides
        {
            get => (ProteinAssociation.SharedPeptides) comboSharedPeptides.SelectedIndex;
            set => comboSharedPeptides.SelectedIndex = (int) value;
        }

        public int MinPeptidesPerProtein
        {
            get => (int) numMinPeptides.Value;
            set => numMinPeptides.Value = value;
        }

        private void UpdateParsimonyResults()
        {
            if (Results == null)
                return;

            var groupProteins = GroupProteins;
            var selectedProteinParsimony = SelectedProteinParsimony;
            var selectedSharedPeptides = SelectedSharedPeptides;
            var minPeptidesPerProtein = MinPeptidesPerProtein;

            using (var longWaitDlg = new LongWaitDlg())
            {
                longWaitDlg.PerformWork(this, 1000,
                    broker => _proteinAssociation.ApplyParsimonyOptions(groupProteins, selectedProteinParsimony, selectedSharedPeptides, minPeptidesPerProtein, broker));
                if (longWaitDlg.IsCanceled)
                    return;
            }

            dgvAssociateResults.RowCount = 2;
            dgvAssociateResults.Invalidate();
        }

        private void cbGroupProteins_CheckedChanged(object sender, EventArgs e)
        {
            UpdateParsimonyResults();
        }

        private void comboParsimony_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateParsimonyResults();
        }

        private void numMinPeptides_ValueChanged(object sender, EventArgs e)
        {
            UpdateParsimonyResults();
        }

        // find matches using the background proteome
        public void UseBackgroundProteome()
        {
            var backgroundProteome = new BackgroundProteome(_driverBackgroundProteome.SelectedItem);
            if (backgroundProteome.Equals(BackgroundProteome.NONE))
                return;

            Initialize();

            _isFasta = false;
            //FastaFileName = _parent.Document.Settings.PeptideSettings.BackgroundProteome.DatabasePath;
            
            using (var longWaitDlg = new LongWaitDlg())
            {
                longWaitDlg.PerformWork(this, 1000, broker => _proteinAssociation.UseBackgroundProteome(backgroundProteome, broker));
                if (longWaitDlg.IsCanceled)
                    return;
            }

            if (Results.PeptidesMapped == 0)
                MessageDlg.Show(this, Resources.AssociateProteinsDlg_UseBackgroundProteome_No_matches_were_found_using_the_background_proteome_);
            UpdateParsimonyResults();
            btnOk.Enabled = true;
        }

        private void btnUseFasta_Click(object sender, EventArgs e)
        {
            ImportFasta();
        }

        private void rbCheckedChanged(object sender, EventArgs e)
        {
            tbxFastaTargets.Enabled = browseFastaTargetsBtn.Enabled = rbFASTA.Checked;
            comboBackgroundProteome.Enabled = rbBackgroundProteome.Checked;
            if (comboBackgroundProteome.Enabled)
                UseBackgroundProteome();
        }

        private void tbxFastaTargets_TextChanged(object sender, EventArgs e)
        {
            if (File.Exists(tbxFastaTargets.Text))
                UseFastaFile(tbxFastaTargets.Text);
        }

        private void comboBackgroundProteome_SelectedIndexChanged(object sender, EventArgs e)
        {
            _driverBackgroundProteome.SelectedIndexChangedEvent(sender, e);
            if (comboBackgroundProteome.Enabled)
                UseBackgroundProteome();
        }

        // prompts user to select a fasta file to use for matching proteins
        public void ImportFasta()
        {
            using (OpenFileDialog dlg = new OpenFileDialog
            {
                Title = Resources.SkylineWindow_ImportFastaFile_Import_FASTA,
                InitialDirectory = Settings.Default.FastaDirectory,
                CheckPathExists = true,
                Filter = TextUtil.FileDialogFiltersAll(TextUtil.FileDialogFilter(Resources.OpenFileDialog_FASTA_files, DataSourceUtil.EXT_FASTA))
            })
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    Settings.Default.FastaDirectory = Path.GetDirectoryName(dlg.FileName);
                    FastaFileName = dlg.FileName;
                }
            }
        }

        // find matches using a FASTA file
        // needed for Testing purposes so we can skip ImportFasta() because of the OpenFileDialog
        public void UseFastaFile(string file)
        {
            Initialize();

            _isFasta = true;
            //FastaFileName = file;

            using (var longWaitDlg = new LongWaitDlg())
            {
                longWaitDlg.PerformWork(this, 1000, broker => _proteinAssociation.UseFastaFile(file, broker));
                if (longWaitDlg.IsCanceled)
                    return;
            }

            if (Results.PeptidesMapped == 0)
                MessageDlg.Show(this, Resources.AssociateProteinsDlg_FindProteinMatchesWithFasta_No_matches_were_found_using_the_imported_fasta_file_);
            UpdateParsimonyResults();
            btnOk.Enabled = true;
        }

        private SrmDocument CreateDocTree(SrmDocument current)
        {
            SrmDocument result = null;
            using (var longWaitDlg = new LongWaitDlg())
            {
                longWaitDlg.PerformWork(this, 1000, monitor =>
                {
                    result = _proteinAssociation.CreateDocTree(current, monitor);
                });
                if (longWaitDlg.IsCanceled)
                    return null;
            }
            return result;
        }

        private void btnApplyChanges_Click(object sender, EventArgs e)
        {
            ApplyChanges();
        }

        public AssociateProteinsSettings FormSettings
        {
            get
            {
                var fileName = FastaFileName;
                return new AssociateProteinsSettings(_proteinAssociation.FinalResults, _isFasta ? fileName : null, _isFasta ? null : fileName);
            }
        }

        public void ApplyChanges()
        {
            /*var newFilterSettings = _parent.Document.Settings.PeptideSettings.ChangeFilter(_parent.Document
                .Settings.PeptideSettings.Filter
                .ChangeGroupProteins(FinalResults.GroupProteins)
                .ChangeProteinParsimony(FinalResults.ProteinParsimony)
                .ChangeSharedPeptides(FinalResults.SharedPeptides));
            if (!Equals(newFilterSettings, _parent.Document.Settings.PeptideSettings))
            {
                _parent.ChangeSettingsMonitored(_parent, Resources.PeptideSettingsUI_OkDialog_Changing_peptide_settings, settings => settings.ChangePeptideSettings(newFilterSettings));
            }*/
            lock (_parent.GetDocumentChangeLock())
            {
                _parent.ModifyDocument(Resources.AssociateProteinsDlg_ApplyChanges_Associated_proteins, CreateDocTree, FormSettings.EntryCreator.Create);
            }

            DialogResult = DialogResult.OK;
        }

        private void dgvAssociateResults_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (FinalResults == null)
                return;

            const int separatorThreshold = 10000;
            var culture = LocalizationHelper.CurrentCulture;
            Func<int, string> resultToString = count => count < separatorThreshold ? count.ToString(culture) : count.ToString(@"N0", culture);
            
            const int proteinRowIndex = 0;
            const int peptideRowIndex = 1;

            if (e.ColumnIndex == headerColumn.Index)
            {
                if (e.RowIndex == proteinRowIndex)
                    e.Value = Resources.AnnotationDef_AnnotationTarget_Proteins;
                else if (e.RowIndex == peptideRowIndex)
                    e.Value = Resources.AlignmentForm_UpdateGraph_Peptides;
            }
            else if (e.ColumnIndex == mappedColumn.Index)
            {
                if (e.RowIndex == proteinRowIndex)
                    e.Value = resultToString(FinalResults.ProteinsMapped);
                else if (e.RowIndex == peptideRowIndex)
                    e.Value = resultToString(FinalResults.PeptidesMapped);
            }
            else if (e.ColumnIndex == unmappedColumn.Index)
            {
                if (e.RowIndex == proteinRowIndex)
                    e.Value = resultToString(FinalResults.ProteinsUnmapped);
                else if (e.RowIndex == peptideRowIndex)
                    e.Value = resultToString(FinalResults.PeptidesUnmapped);
            }
            else if (e.ColumnIndex == targetsColumn.Index)
            {
                if (e.RowIndex == proteinRowIndex)
                    e.Value = resultToString(FinalResults.FinalProteinCount);
                else if (e.RowIndex == peptideRowIndex)
                    e.Value = resultToString(FinalResults.FinalPeptideCount);
            }
        }
    }
}
