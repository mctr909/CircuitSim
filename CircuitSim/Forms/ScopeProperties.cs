using System;
using System.Windows.Forms;

using Circuit.UI.Output;

namespace Circuit.Forms {
    public partial class ScopeProperties : Form {
        Scope.Property mScope;

        public ScopeProperties(Scope.Property scope) {
            InitializeComponent();
            mScope = scope;
            Visible = false;
        }

        public void Show(int x, int y) {
            Show();
            rbVoltage.Checked = mScope.ShowVoltage;
            rbSpectrum.Checked = mScope.ShowFFT;
            Left = x;
            Top = y - Height;
            Visible = true;
        }

        private void ScopeProperties_Load(object sender, EventArgs e) {
            chkScale.Checked = mScope.ShowScale;
            chkPeak.Checked = mScope.ShowMax;
            chkNegPeak.Checked = mScope.ShowMin;
            
            chkScale.Enabled = mScope.ShowVoltage;
            chkPeak.Enabled = mScope.ShowVoltage;
            chkNegPeak.Enabled = mScope.ShowVoltage;
            cmbColor.Enabled = mScope.ShowVoltage;
            lblColor.Enabled = mScope.ShowVoltage;

            chkRms.Checked = mScope.ShowRMS;
            chkFreq.Checked = mScope.ShowFreq;

            tbSpeed.Value = tbSpeed.Maximum - (int)Math.Round(Math.Log(mScope.Speed) / Math.Log(2));
            txtManualScale.Text = ElementInfoDialog.UnitString(null, mScope.ScaleValue);
            chkManualScale.Checked = mScope.ManualScale;
            txtManualScale.Enabled = mScope.ManualScale;
            chkLogSpectrum.Checked = mScope.LogSpectrum;
            chkLogSpectrum.Enabled = mScope.ShowFFT;
            txtLabel.Text = mScope.Text;

            cmbColor.Items.Clear();
            foreach (var c in Enum.GetValues(typeof(Scope.Plot.E_COLOR))) {
                if ((Scope.Plot.E_COLOR)c == Scope.Plot.E_COLOR.INVALID) {
                    continue;
                }
                cmbColor.Items.Add(c);
            }
            var plotIdx = mScope.SelectedPlot;
            if (cmbColor.Items.Count <= plotIdx) {
                return;
            }
            cmbColor.SelectedIndex = (int)mScope.Plots[plotIdx].ColorIndex;
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

        private void rbVoltage_CheckedChanged(object sender, EventArgs e) {
            mScope.ShowVoltage = rbVoltage.Checked;
            ScopeProperties_Load(sender, e);
        }

        private void rbSpectrum_CheckedChanged(object sender, EventArgs e) {
            mScope.ShowFFT = rbSpectrum.Checked;
            chkLogSpectrum.Enabled = rbSpectrum.Checked;
        }

        private void cmbColor_SelectedIndexChanged(object sender, EventArgs e) {
            var plotIdx = mScope.SelectedPlot;
            mScope.Plots[plotIdx].SetColor(cmbColor.SelectedIndex);
        }

        private void chkLogSpectrum_CheckedChanged(object sender, EventArgs e) {
            mScope.LogSpectrum = chkLogSpectrum.Checked;
        }

        private void setScopeSpeedLabel() {
            lblScopeSpeed.Text = Utils.UnitText(mScope.CalcGridStepX(), "s") + "/div";
        }
    }
}
