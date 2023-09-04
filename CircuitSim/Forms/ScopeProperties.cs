using System;
using System.Windows.Forms;
using Circuit.Common;

namespace Circuit.Forms {
    public partial class ScopeProperties : Form {
        ScopePlot mPlot;

        public ScopeProperties(ScopePlot scope) {
            InitializeComponent();
            mPlot = scope;
            Visible = false;
        }

        public void Show(int x, int y) {
            Show();
            rbVoltage.Checked = mPlot.ShowVoltage;
            rbSpectrum.Checked = mPlot.ShowFFT;
            cmbColor.Items.Clear();
            foreach (var c in Enum.GetValues(typeof(ScopePlot.E_COLOR))) {
                if ((ScopePlot.E_COLOR)c == ScopePlot.E_COLOR.INVALID) {
                    continue;
                }
                cmbColor.Items.Add(c);
            }
            if (0 <= mPlot.SelectedWave) {
                cmbColor.SelectedIndex = (int)mPlot.Waves[mPlot.SelectedWave].Color;
            }
            setScopeSpeedLabel();
            ScopeProperties_Load(null, null);
            Left = Math.Max(0, x - Width / 2);
            Top = Math.Max(0, y - Height / 2);
            Visible = true;
        }

        private void ScopeProperties_Load(object sender, EventArgs e) {
            if (mPlot.SelectedWave < 0 || cmbColor.Items.Count <= mPlot.SelectedWave) {
                return;
            }
            chkScale.Checked = mPlot.ShowScale;
            chkPeak.Checked = mPlot.ShowMax;
            chkNegPeak.Checked = mPlot.ShowMin;
            
            chkScale.Enabled = mPlot.ShowVoltage;
            chkPeak.Enabled = mPlot.ShowVoltage;
            chkNegPeak.Enabled = mPlot.ShowVoltage;
            cmbColor.Enabled = mPlot.ShowVoltage;
            lblColor.Enabled = mPlot.ShowVoltage;

            chkRms.Checked = mPlot.ShowRMS;
            chkFreq.Checked = mPlot.ShowFreq;

            tbSpeed.Value = tbSpeed.Maximum - (int)Math.Round(Math.Log(mPlot.Speed) / Math.Log(2));
            txtManualScale.Text = ElementInfoDialog.UnitString(null, mPlot.ScaleValue);
            chkManualScale.Checked = mPlot.ManualScale;
            txtManualScale.Enabled = mPlot.ManualScale;
            chkLogSpectrum.Checked = mPlot.LogSpectrum;
            chkLogSpectrum.Enabled = mPlot.ShowFFT;
            txtLabel.Text = mPlot.Text;

            cmbColor.SelectedIndex = (int)mPlot.Waves[mPlot.SelectedWave].Color;
            setScopeSpeedLabel();
        }

        private void tbSpeed_ValueChanged(object sender, EventArgs e) {
            int newsp = (int)Math.Pow(2, tbSpeed.Maximum - tbSpeed.Value);
            if (mPlot.Speed != newsp) {
                mPlot.Speed = newsp;
            }
            setScopeSpeedLabel();
        }

        private void txtManualScale_TextChanged(object sender, EventArgs e) {
            var d = ElementInfoDialog.ParseUnits(txtManualScale.Text);
            mPlot.ScaleValue = d;
        }

        private void txtLabel_TextChanged(object sender, EventArgs e) {
            string label = txtLabel.Text;
            if (label.Length == 0) {
                label = null;
            }
            mPlot.Text = label;
        }

        private void chkManualScale_CheckedChanged(object sender, EventArgs e) {
            mPlot.ManualScale = chkManualScale.Checked;
            txtManualScale.Enabled = chkManualScale.Checked;
            if (chkManualScale.Checked) {
                txtManualScale_TextChanged(sender, e);
            }
        }

        private void chkScale_CheckedChanged(object sender, EventArgs e) {
            mPlot.ShowScale = chkScale.Checked;
        }

        private void chkPeak_CheckedChanged(object sender, EventArgs e) {
            mPlot.ShowMax = chkPeak.Checked;
        }

        private void chkNegPeak_CheckedChanged(object sender, EventArgs e) {
            mPlot.ShowMin = chkNegPeak.Checked;
        }

        private void chkRms_CheckedChanged(object sender, EventArgs e) {
            mPlot.ShowRMS = chkRms.Checked;
        }

        private void chkFreq_CheckedChanged(object sender, EventArgs e) {
            mPlot.ShowFreq = chkFreq.Checked;
        }

        private void rbVoltage_CheckedChanged(object sender, EventArgs e) {
            mPlot.ShowVoltage = rbVoltage.Checked;
            ScopeProperties_Load(sender, e);
        }

        private void rbSpectrum_CheckedChanged(object sender, EventArgs e) {
            mPlot.ShowFFT = rbSpectrum.Checked;
            chkLogSpectrum.Enabled = rbSpectrum.Checked;
        }

        private void cmbColor_SelectedIndexChanged(object sender, EventArgs e) {
            mPlot.Waves[mPlot.SelectedWave].SetColor(cmbColor.SelectedIndex);
        }

        private void chkLogSpectrum_CheckedChanged(object sender, EventArgs e) {
            mPlot.LogSpectrum = chkLogSpectrum.Checked;
        }

        private void setScopeSpeedLabel() {
            lblScopeSpeed.Text = Utils.UnitText(mPlot.CalcGridStepX(), "s") + "/div";
        }
    }
}
