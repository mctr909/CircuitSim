using System;
using System.Windows.Forms;

namespace Circuit.Forms {
	public partial class ScopeProperties : Form {
		static ScopeProperties mInstance = null;
		ScopePlot mPlot;

		public static void Show(ScopePlot plot, int x, int y) {
			if (null != mInstance) {
				mInstance.Close();
			}
			mInstance = new ScopeProperties(plot);
			mInstance.Show(x, y);
		}

		private ScopeProperties(ScopePlot plot) {
			InitializeComponent();
			mPlot = plot;
		}

		private void Show(int x, int y) {
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

			StartPosition = FormStartPosition.Manual;
			Left = Math.Max(0, x - Width / 2);
			Top = Math.Max(0, y - Height / 2);
			Show();
		}

		private void ScopeProperties_Load(object sender, EventArgs e) {
			if (mPlot.SelectedWave < 0 || cmbColor.Items.Count <= mPlot.SelectedWave) {
				return;
			}
			cmbColor.SelectedIndex = (int)mPlot.Waves[mPlot.SelectedWave].Color;
			chkScale.Checked = mPlot.ShowScale;
			chkRms.Checked = mPlot.ShowRMS;
			chkFreq.Checked = mPlot.ShowFreq;

			tbSpeed.Value = tbSpeed.Maximum - (int)Math.Round(Math.Log(mPlot.Speed) / Math.Log(2));
			txtManualScale.Text = ElementInfoDialog.UnitString(null, mPlot.Scale);
			chkManualScale.Checked = mPlot.ManualScale;

			cmbColor.Enabled = mPlot.ShowVoltage;
			chkScale.Enabled = mPlot.ShowVoltage;
			lblColor.Enabled = mPlot.ShowVoltage;
			txtManualScale.Enabled = mPlot.ManualScale;
			chkLogSpectrum.Checked = mPlot.LogSpectrum;
			chkLogSpectrum.Enabled = mPlot.ShowFFT;

			txtLabel.Text = mPlot.Text;

			setScopeSpeedLabel();
		}

		private void tbSpeed_ValueChanged(object sender, EventArgs e) {
			int newsp = (int)Math.Pow(2, tbSpeed.Maximum - tbSpeed.Value);
			if (mPlot.Speed != newsp) {
				mPlot.SetSpeed(newsp);
			}
			setScopeSpeedLabel();
		}

		private void txtManualScale_TextChanged(object sender, EventArgs e) {
			double d;
			if (Utils.ParseUnits(txtManualScale.Text, out d)) {
				mPlot.SetScale(d);
			} else {
				d = mPlot.Scale;
			}
			txtManualScale.Text = Utils.UnitText(d);
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

		private void chkRms_CheckedChanged(object sender, EventArgs e) {
			mPlot.ShowRMS = chkRms.Checked;
		}

		private void chkFreq_CheckedChanged(object sender, EventArgs e) {
			mPlot.ShowFreq = chkFreq.Checked;
		}

		private void rbVoltage_CheckedChanged(object sender, EventArgs e) {
			mPlot.SetShowVoltage(rbVoltage.Checked);
			ScopeProperties_Load(sender, e);
		}

		private void rbSpectrum_CheckedChanged(object sender, EventArgs e) {
			mPlot.ShowFFT = rbSpectrum.Checked;
			chkLogSpectrum.Enabled = rbSpectrum.Checked;
		}

		private void cmbColor_SelectedIndexChanged(object sender, EventArgs e) {
			if (mPlot.SelectedWave < 0) {
				return;
			}
			mPlot.Waves[mPlot.SelectedWave].SetColor(cmbColor.SelectedIndex);
		}

		private void chkLogSpectrum_CheckedChanged(object sender, EventArgs e) {
			mPlot.LogSpectrum = chkLogSpectrum.Checked;
		}

		private void setScopeSpeedLabel() {
			lblScopeSpeed.Text = Utils.UnitText(mPlot.CalcGridTime(), "s") + "/div";
		}
	}
}
