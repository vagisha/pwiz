using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Protobuf;
using pwiz.Skyline.FileUI;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Model.Spectra;
using pwiz.Skyline.Properties;

namespace pwiz.Skyline.Controls
{
    public partial class FullScanGrid : Form
    {
        public FullScanGrid()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var openDataSourceDlg = new OpenDataSourceDialog(Settings.Default.RemoteAccountList))
            {
                if (openDataSourceDlg.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                var file = openDataSourceDlg.DataSource;
                var skylineDataSchema = SkylineDataSchema.MemoryDataSchema(
                    new SrmDocument(SrmSettingsList.GetDefault()), SkylineDataSchema.GetLocalizedSchemaLocalizer());
                var viewContext = new SpectraViewContext(skylineDataSchema, file);
                databoundGridControl1.BindingListSource.SetViewContext(viewContext);
            }
        }
    }
}
