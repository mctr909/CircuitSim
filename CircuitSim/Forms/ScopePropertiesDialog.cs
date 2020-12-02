using System;
using System.Drawing;
using System.Windows.Forms;

using Circuit.Elements;

namespace Circuit {
    class ScopePropertiesDialog : Form {
        CirSim mSim;

        //RichTextArea textBox;

        TextBox textArea;
        CheckBox scaleBox;
        CheckBox maxScaleBox;
        CheckBox voltageBox;
        CheckBox currentBox;
        CheckBox peakBox;
        CheckBox negPeakBox;
        CheckBox freqBox;
        CheckBox spectrumBox;
        CheckBox manualScaleBox;

        CheckBox rmsBox;
        CheckBox dutyBox;
        CheckBox viBox;
        CheckBox xyBox;
        CheckBox resistanceBox;
        CheckBox ibBox;
        CheckBox icBox;
        CheckBox ieBox;
        CheckBox vbeBox;
        CheckBox vbcBox;
        CheckBox vceBox;
        CheckBox vceIcBox;
        CheckBox logSpectrumBox;

        TextBox labelTextBox;
        TextBox manualScaleTextBox;

        TrackBar speedBar;
        Scope scope;

        int gridY;
        Label scopeSpeedLabel;
        Label manualScaleLabel;

        public ScopePropertiesDialog(CirSim asim, Scope s) : base() {
            mSim = asim;
            scope = s;

            var elm = scope.getSingleElm();
            bool transistor = elm != null && (elm is TransistorElm);

            SuspendLayout();
            Text = "Scope Properties";

            var grbSpeed = new GroupBox();
            {
                grbSpeed.Text = "Scroll Speed";
                gridY = 12;
                /* speedBar */
                speedBar = new TrackBar() {
                    Minimum = 0,
                    Maximum = 10,
                    SmallChange = 1,
                    LargeChange = 1,
                    TickFrequency = 1,
                    Width = 200,
                    Height = 20,
                    TickStyle = TickStyle.TopLeft
                };
                speedBar.ValueChanged += new EventHandler((sender, e) => { scrollbarChanged(); });
                addItemToGrid(grbSpeed, speedBar);
                /* scopeSpeedLabel */
                addItemToGrid(grbSpeed, scopeSpeedLabel = new Label() {
                    AutoSize = true,
                    TextAlign = ContentAlignment.TopLeft
                });
                /* manualScaleLabel */
                addItemToGrid(grbSpeed, manualScaleLabel = new Label() {
                    Text = "Scale",
                    AutoSize = true,
                    TextAlign = ContentAlignment.BottomLeft
                });
                /* manualScaleTextBox */
                addItemToGrid(grbSpeed, manualScaleTextBox = new TextBox() {
                    Width = 100, Height = 24
                });
                /* */
                grbSpeed.Left = 4;
                grbSpeed.Top = 4;
                grbSpeed.Width = speedBar.Right + 4;
                grbSpeed.Height = gridY;
                Controls.Add(grbSpeed);
            }

            var grbPlots = new GroupBox();
            {
                grbPlots.Text = "Plots";
                gridY = 12;
                if (transistor) {
                    /* Show Ib */
                    ibBox = new ScopeCheckBox("Show Ib", SCOPE_MENU.showib);
                    ibBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                    addItemToGrid(grbPlots, ibBox);
                    /* Show Ic */
                    icBox = new ScopeCheckBox("Show Ic", SCOPE_MENU.showic);
                    icBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                    addItemToGrid(grbPlots, icBox);
                    /* Show Ie */
                    ieBox = new ScopeCheckBox("Show Ie", SCOPE_MENU.showie);
                    ieBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                    addItemToGrid(grbPlots, ieBox);
                    /* Show Vbe */
                    vbeBox = new ScopeCheckBox("Show Vbe", SCOPE_MENU.showvbe);
                    vbeBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                    addItemToGrid(grbPlots, vbeBox);
                    /* Show Vbc */
                    vbcBox = new ScopeCheckBox("Show Vbc", SCOPE_MENU.showvbc);
                    vbcBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                    addItemToGrid(grbPlots, vbcBox);
                    /* Show Vce */
                    vceBox = new ScopeCheckBox("Show Vce", SCOPE_MENU.showvce);
                    vceBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                    addItemToGrid(grbPlots, vceBox);
                } else {
                    /* Show Voltage */
                    voltageBox = new ScopeCheckBox("Show Voltage", SCOPE_MENU.showvoltage);
                    voltageBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                    addItemToGrid(grbPlots, voltageBox);
                    /* Show Current */
                    currentBox = new ScopeCheckBox("Show Current", SCOPE_MENU.showcurrent);
                    currentBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                    addItemToGrid(grbPlots, currentBox);
                }
                /* Show Resistance */
                resistanceBox = new ScopeCheckBox("Show Resistance", SCOPE_MENU.showresistance);
                resistanceBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbPlots, resistanceBox);
                /* Show Spectrum */
                spectrumBox = new ScopeCheckBox("Show Spectrum", SCOPE_MENU.showfft);
                spectrumBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbPlots, spectrumBox);
                /* Log Spectrum */
                logSpectrumBox = new ScopeCheckBox("Log Spectrum", SCOPE_MENU.logspectrum);
                logSpectrumBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbPlots, logSpectrumBox);
                /* Manual Scale */
                manualScaleBox = new ScopeCheckBox("Manual Scale", SCOPE_MENU.manualscale);
                manualScaleBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbPlots, manualScaleBox);
                /* */
                grbPlots.Left = 4;
                grbPlots.Top = grbSpeed.Bottom + 8;
                grbPlots.Height = gridY;
                Controls.Add(grbPlots);
            }

            var grbXY = new GroupBox();
            {
                grbXY.Text = "X-Y Plots";
                gridY = 12;
                /* Show V vs I */
                viBox = new ScopeCheckBox("Show V vs I", SCOPE_MENU.showvvsi);
                viBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbXY, viBox);
                /* Plot X/Y */
                xyBox = new ScopeCheckBox("Plot X/Y", SCOPE_MENU.plotxy);
                xyBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbXY, xyBox);
                if (transistor) {
                    /* Show Vce vs Ic */
                    vceIcBox = new ScopeCheckBox("Show Vce vs Ic", SCOPE_MENU.showvcevsic);
                    vceIcBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                    addItemToGrid(grbXY, vceIcBox);
                }
                /* */
                grbXY.Left = grbSpeed.Right + 16;
                grbXY.Top = grbSpeed.Top;
                grbXY.Height = gridY;
                Controls.Add(grbXY);
            }

            var grbShowInfo = new GroupBox();
            {
                grbShowInfo.Text = "Show Info";
                gridY = 12;
                /* Show Scale */
                scaleBox = new ScopeCheckBox("Show Scale", SCOPE_MENU.showscale);
                scaleBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbShowInfo, scaleBox);
                /* Show Peak Value */
                peakBox = new ScopeCheckBox("Show Peak Value", SCOPE_MENU.showpeak);
                peakBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbShowInfo, peakBox);
                /* Show Negative Peak Value */
                negPeakBox = new ScopeCheckBox("Show Negative Peak Value", SCOPE_MENU.shownegpeak);
                negPeakBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbShowInfo, negPeakBox);
                /* Show Frequency */
                freqBox = new ScopeCheckBox("Show Frequency", SCOPE_MENU.showfreq);
                freqBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbShowInfo, freqBox);
                /* Show RMS Average */
                rmsBox = new ScopeCheckBox("Show RMS Average", SCOPE_MENU.showrms);
                rmsBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbShowInfo, rmsBox);
                /* Show Duty Cycle */
                dutyBox = new ScopeCheckBox("Show Duty Cycle", SCOPE_MENU.showduty);
                dutyBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbShowInfo, dutyBox);
                /* Custom Label */
                addItemToGrid(grbShowInfo, new Label() {
                    Text = "Custom Label",
                    AutoSize = true,
                    TextAlign = ContentAlignment.BottomLeft
                });
                labelTextBox = new TextBox();
                addItemToGrid(grbShowInfo, labelTextBox);
                string labelText = scope.Text;
                if (labelText != null) {
                    labelTextBox.Text = labelText;
                }
                /* */
                grbShowInfo.Left = grbXY.Left;
                grbShowInfo.Top = grbXY.Bottom + 8;
                grbShowInfo.Height = gridY;
                Controls.Add(grbShowInfo);
            }

            var pnl = new Panel();
            {
                /* OK */
                var okButton = new Button() { Text = "OK" };
                okButton.Click += new EventHandler((sender, e) => { closeDialog(); });
                okButton.Left = 0;
                okButton.Width = 50;
                okButton.Top = 4;
                pnl.Controls.Add(okButton);
                /* Apply */
                var applyButton = new Button() { Text = "Apply" };
                applyButton.Click += new EventHandler((sender, e) => { apply(); });
                applyButton.Left = okButton.Right + 4;
                applyButton.Width = 50;
                applyButton.Top = 4;
                pnl.Controls.Add(applyButton);
                /* Save as Default */
                var saveAsDefaultButton = new Button() { Text = "Save as Default" };
                saveAsDefaultButton.Click += new EventHandler((sender, e) => { scope.saveAsDefault(); });
                saveAsDefaultButton.Left = applyButton.Right + 4;
                saveAsDefaultButton.Width = 100;
                saveAsDefaultButton.Top = 4;
                pnl.Controls.Add(saveAsDefaultButton);
                /* */
                pnl.Left = grbShowInfo.Left;
                pnl.Top = grbShowInfo.Bottom;
                pnl.Width = saveAsDefaultButton.Right + 4;
                pnl.Height = saveAsDefaultButton.Bottom + 8;
                Controls.Add(pnl);
            }

            updateUI();
            ResumeLayout(false);

            Width = pnl.Right + 18;
            Height = pnl.Bottom + 36;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            StartPosition = FormStartPosition.CenterParent;
            Show();
        }

        void setScopeSpeedLabel() {
            scopeSpeedLabel.Text = (CircuitElm.getUnitText(scope.calcGridStepX(), "s") + "/div");
        }

        void addItemToGrid(GroupBox grb, Control ctrl) {
            ctrl.Left = 8;
            ctrl.Top = gridY;
            grb.Controls.Add(ctrl);
            gridY += ctrl.Height + 4;
        }

        void scrollbarChanged() {
            int newsp = (int)Math.Pow(2, 10 - speedBar.Value);
            Console.WriteLine("changed " + scope.Speed + " " + newsp + " " + speedBar.Value);
            if (scope.Speed != newsp) {
                scope.setSpeed(newsp);
            }
            setScopeSpeedLabel();
        }

        void updateUI() {
            speedBar.Value = (10 - (int)Math.Round(Math.Log(scope.Speed) / Math.Log(2)));
            if (voltageBox != null) {
                voltageBox.Checked = scope.ShowV && !scope.showingValue(Scope.VAL_POWER);
                currentBox.Checked = scope.ShowI && !scope.showingValue(Scope.VAL_POWER);
            }
            scaleBox.Checked = scope.ShowScale;
            peakBox.Checked = scope.ShowMax;
            negPeakBox.Checked = scope.ShowMin;
            freqBox.Checked = scope.ShowFreq;
            spectrumBox.Checked = scope.ShowFFT;
            rmsBox.Checked = scope.ShowRMS;
            rmsBox.Text = scope.canShowRMS() ? "Show RMS Average" : "Show Average";
            viBox.Checked = scope.Plot2d && !scope.PlotXY;
            xyBox.Checked = scope.PlotXY;
            resistanceBox.Checked = scope.showingValue(Scope.VAL_R);
            resistanceBox.Enabled = scope.canShowResistance();
            if (vbeBox != null) {
                ibBox.Checked = scope.showingValue(Scope.VAL_IB);
                icBox.Checked = scope.showingValue(Scope.VAL_IC);
                ieBox.Checked = scope.showingValue(Scope.VAL_IE);
                vbeBox.Checked = scope.showingValue(Scope.VAL_VBE);
                vbcBox.Checked = scope.showingValue(Scope.VAL_VBC);
                vceBox.Checked = scope.showingValue(Scope.VAL_VCE);
                vceIcBox.Checked = scope.isShowingVceAndIc();
            }
            manualScaleLabel.Text = "Scale (Max Value)" + " (" + scope.getScaleUnitsText() + ")";
            manualScaleTextBox.Text = EditDialog.unitString(null, scope.getScaleValue());
            manualScaleBox.Checked = scope.LockScale;
            manualScaleTextBox.Enabled = scope.LockScale;
            logSpectrumBox.Checked = scope.LogSpectrum;
            setScopeSpeedLabel();

            /* if you add more here, make sure it still works with transistor scopes */
        }

        void closeDialog() {
            apply();
            Close();
        }

        void apply() {
            string label = labelTextBox.Text;
            if (label.Length == 0) {
                label = null;
            }
            scope.Text = label;
            try {
                double d = EditDialog.parseUnits(manualScaleTextBox.Text);
                scope.setManualScaleValue(d);
            } catch { }
        }

        void onValueChange(object sender) {
            var cb = (ScopeCheckBox)sender;
            scope.handleMenu(cb.Menu, cb.Checked);
            updateUI();
        }
    }
}
