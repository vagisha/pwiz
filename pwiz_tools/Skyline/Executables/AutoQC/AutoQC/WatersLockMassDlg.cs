using System;
using System.Globalization;
using System.Windows.Forms;
using SharedBatch;

namespace AutoQC
{
    public partial class WatersLockmassDlg : Form
    {
        public WatersLockmassDlg(LockMassParameters lockmassParameters)
        {
            InitializeComponent();
            if (lockmassParameters != null)
            {
                LockmassPositive = lockmassParameters.LockmassPositive;
                LockmassNegative = lockmassParameters.LockmassNegative;
                LockmassTolerance = lockmassParameters.LockmassTolerance;
            }
        }

        /// <summary>
        /// Set to valid lockmass parameters, if form is okayed
        /// </summary>
        public LockMassParameters LockMassParameters { get; private set; }

        private void btnOk_Click(object sender, EventArgs e)
        {
            OkDialog();
        }

        public void OkDialog()
        {
            double lockmassPositive;
            if (string.IsNullOrEmpty(textLockmassPositive.Text))
                lockmassPositive = 0;
            else if (!ValidateDecimalTextBox(LockMassParameters.POSITIVE, textLockmassPositive, 0, null, out lockmassPositive))
                return;
            double lockmassNegative;
            if (string.IsNullOrEmpty(textLockmassNegative.Text))
                lockmassNegative = 0;
            else if (!ValidateDecimalTextBox(LockMassParameters.NEGATIVE, textLockmassNegative, 0, null, out lockmassNegative))
                return;
            double lockmassTolerance;
            if (string.IsNullOrEmpty(textLockmassTolerance.Text))
                lockmassTolerance = 0;
            else if (!ValidateDecimalTextBox(LockMassParameters.TOLERANCE, textLockmassTolerance, LockMassParameters.LOCKMASS_TOLERANCE_MIN, LockMassParameters.LOCKMASS_TOLERANCE_MAX, out lockmassTolerance))
                return;
            
            LockMassParameters = new LockMassParameters(lockmassPositive, lockmassNegative, lockmassTolerance);
            DialogResult = DialogResult.OK;
        }

        public bool ValidateDecimalTextBox(string paramName, TextBox control,
            double? min, double? max, out double val)
        {
            if (!ValidateDecimalTextBox(paramName, control.Text, out val))
                return false;
            
            bool valid = false;
            if (min.HasValue && val < min.Value)
                AlertDlg.ShowError(this, "", $"{paramName} must be greater than or equal to {min}");
            else if (max.HasValue && val > max.Value)
                AlertDlg.ShowError(this, "", $"{paramName} must be less than or equal to {max}");
            else
                valid = true;
            return valid;
        }

        public bool ValidateDecimalTextBox(string paramName, string paramValue, out double val)
        {
            bool valid = false;
            val = default(double);
            try
            {
                val = double.Parse(paramValue, CultureInfo.CurrentCulture);
                valid = true;
            }
            catch (FormatException)
            {
                AlertDlg.ShowError(this, Program.AppName, $"{paramName} must contain a decimal value");
            }
            return valid;
        }

        public double? LockmassPositive
        {
            get
            {
                if (string.IsNullOrEmpty(textLockmassPositive.Text))
                    return null;
                return double.Parse(textLockmassPositive.Text);
            }
            set
            {
                textLockmassPositive.Text = value.HasValue ? value.Value.ToString(CultureInfo.CurrentCulture) : string.Empty;
                if (value.HasValue && string.IsNullOrEmpty(textLockmassTolerance.Text))
                    LockmassTolerance = LockMassParameters.LOCKMASS_TOLERANCE_DEFAULT;
            }
        }

        public double? LockmassNegative
        {
            get
            {
                if (string.IsNullOrEmpty(textLockmassNegative.Text))
                    return null;
                return double.Parse(textLockmassNegative.Text);
            }
            set
            {
                textLockmassNegative.Text = value.HasValue ? value.Value.ToString(CultureInfo.CurrentCulture) : string.Empty;
                if (value.HasValue && string.IsNullOrEmpty(textLockmassTolerance.Text))
                    LockmassTolerance = LockMassParameters.LOCKMASS_TOLERANCE_DEFAULT;
            }
        }

        public double? LockmassTolerance
        {
            get
            {
                if (string.IsNullOrEmpty(textLockmassTolerance.Text))
                    return null;
                return double.Parse(textLockmassTolerance.Text);
            }
            set { textLockmassTolerance.Text = value.HasValue ? value.Value.ToString(CultureInfo.CurrentCulture) : string.Empty; }
        }
    }
}
