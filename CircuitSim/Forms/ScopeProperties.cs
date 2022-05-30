using System;
using System.Windows.Forms;

namespace Circuit.Forms {
    public partial class ScopeProperties : Form {
        Scope mScope;

        public ScopeProperties(Scope scope) {
            InitializeComponent();
            mScope = scope;
        }

        public void Show(Form parent) {
            Visible = false;
            Show();
            Left = parent.Left;
            Top = parent.Bottom - Height;
            Visible = true;
        }

        private void ScopeProperties_Load(object sender, EventArgs e) {
            tbSpeed.Value = tbSpeed.Maximum - (int)Math.Round(Math.Log(mScope.Speed) / Math.Log(2));
            if (chkVoltage != null) {
                chkVoltage.Checked = mScope.ShowVoltage;
            }
            chkScale.Checked = mScope.ShowScale;
            chkPeak.Checked = mScope.ShowMax;
            chkNegPeak.Checked = mScope.ShowMin;
            chkFreq.Checked = mScope.ShowFreq;
            chkSpectrum.Checked = mScope.ShowFFT;
            chkRms.Checked = mScope.ShowRMS;
            txtManualScale.Text = ElementInfoDialog.UnitString(null, mScope.ScaleValue);
            chkManualScale.Checked = mScope.ManualScale;
            txtManualScale.Enabled = mScope.ManualScale;
            chkLogSpectrum.Checked = mScope.LogSpectrum;
            chkLogSpectrum.Enabled = mScope.ShowFFT;
            setScopeSpeedLabel();
        }

        private void tbSpeed_ValueChanged(object sender, EventArgs e) {
            int newsp = (int)Math.Pow(2, tbSpeed.Maximum - tbSpeed.Value);
            if (mScope.Speed != newsp) {
                mScope.Speed = newsp;
            }
            setScopeSpeedLabel();
        }

        private void txtManualScale_TextChanged(object sender, EventArgs e) {
            try {
                double d = ElementInfoDialog.ParseUnits(txtManualScale.Text);
                mScope.ScaleValue = d;
            } catch { }
        }

        private void txtLabel_TextChanged(object sender, EventArgs e) {
            string label = txtLabel.Text;
            if (label.Length == 0) {
                label = null;
            }
            mScope.Text = label;
        }

        private void chkManualScale_CheckedChanged(object sender, EventArgs e) {
            mScope.ManualScale = chkManualScale.Checked;
            txtManualScale.Enabled = chkManualScale.Checked;
            if (chkManualScale.Checked) {
                txtManualScale_TextChanged(sender, e);
            }
        }

        private void chkScale_CheckedChanged(object sender, EventArgs e) {
            mScope.ShowScale = chkScale.Checked;
        }

        private void chkPeak_CheckedChanged(object sender, EventArgs e) {
            mScope.ShowMax = chkPeak.Checked;
        }

        private void chkNegPeak_CheckedChanged(object sender, EventArgs e) {
            mScope.ShowMin = chkNegPeak.Checked;
        }

        private void chkRms_CheckedChanged(object sender, EventArgs e) {
            mScope.ShowRMS = chkRms.Checked;
        }

        private void chkFreq_CheckedChanged(object sender, EventArgs e) {
            mScope.ShowFreq = chkFreq.Checked;
        }

        private void chkVoltage_CheckedChanged(object sender, EventArgs e) {
            mScope.ShowVoltage = chkVoltage.Checked;
        }

        private void chkSpectrum_CheckedChanged(object sender, EventArgs e) {
            mScope.ShowFFT = chkSpectrum.Checked;
            chkLogSpectrum.Enabled = chkSpectrum.Checked;
        }

        private void chkLogSpectrum_CheckedChanged(object sender, EventArgs e) {
            mScope.LogSpectrum = chkLogSpectrum.Checked;
        }

        private void setScopeSpeedLabel() {
            lblScopeSpeed.Text = Utils.UnitText(mScope.CalcGridStepX(), "s") + "/div";
        }
    }
}
