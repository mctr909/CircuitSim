using System;
using System.Drawing;
using System.Windows.Forms;

using Circuit.Elements;

namespace Circuit {
    enum SCOPE_MENU {
        MAX_SCALE,
        MANUAL_SCALE,

        SHOW_VOLTAGE,
        SHOW_CURRENT,
        SHOW_SCALE,
        SHOW_PEAK,
        SHOW_NEG_PEAK,
        SHOW_FREQ,
        SHOW_FFT,
        LOG_SPECTRUM,
        SHOW_RMS,
        SHOW_DUTY,

        SHOW_IB,
        SHOW_IC,
        SHOW_IE,
        SHOW_VBE,
        SHOW_VBC,
        SHOW_VCE,
        SHOW_VCE_IC,

        SHOW_V_I,
        PLOT_XY
    }

    class ScopeCheckBox : CheckBox {
        public SCOPE_MENU Menu;
        public ScopeCheckBox(string text, SCOPE_MENU menu) : base() {
            AutoSize = true;
            Text = text;
            Menu = menu;
        }
    }

    class ScopeRadioButton : RadioButton {
        public SCOPE_MENU Menu;
        public ScopeRadioButton(string text, SCOPE_MENU menu) : base() {
            AutoSize = true;
            Text = text;
            Menu = menu;
        }
    }

    class ScopePropertiesDialog : Form {
        CirSim mSim;

        //RichTextArea textBox;

        TextBox textArea;
        CheckBox scaleBox;
        CheckBox manualScaleBox;
        CheckBox maxScaleBox;
        CheckBox voltageBox;
        CheckBox currentBox;
        CheckBox peakBox;
        CheckBox negPeakBox;
        CheckBox freqBox;
        CheckBox spectrumBox;
        CheckBox logSpectrumBox;

        CheckBox rmsBox;
        CheckBox dutyBox;
        CheckBox viBox;
        CheckBox xyBox;
        RadioButton ibBox;
        RadioButton icBox;
        RadioButton ieBox;
        RadioButton vbeBox;
        RadioButton vbcBox;
        RadioButton vceBox;
        CheckBox vceIcBox;

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

            var elm = scope.SingleElm;
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
                /* Manual Scale */
                manualScaleBox = new ScopeCheckBox("Manual Scale", SCOPE_MENU.MANUAL_SCALE);
                manualScaleBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbSpeed, manualScaleBox);
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

            var pnlPlots = new Panel();
            {
                pnlPlots.Text = "Plots";
                pnlPlots.BorderStyle = BorderStyle.FixedSingle;
                pnlPlots.AutoScroll = true;
                gridY = 4;
                if (transistor) {
                    /* Show Ib */
                    ibBox = new ScopeRadioButton("Show Ib", SCOPE_MENU.SHOW_IB);
                    ibBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange((ScopeRadioButton)sender); });
                    addItemToGrid(pnlPlots, ibBox);
                    /* Show Ic */
                    icBox = new ScopeRadioButton("Show Ic", SCOPE_MENU.SHOW_IC);
                    icBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange((ScopeRadioButton)sender); });
                    addItemToGrid(pnlPlots, icBox);
                    /* Show Ie */
                    ieBox = new ScopeRadioButton("Show Ie", SCOPE_MENU.SHOW_IE);
                    ieBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange((ScopeRadioButton)sender); });
                    addItemToGrid(pnlPlots, ieBox);
                    /* Show Vbe */
                    vbeBox = new ScopeRadioButton("Show Vbe", SCOPE_MENU.SHOW_VBE);
                    vbeBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange((ScopeRadioButton)sender); });
                    addItemToGrid(pnlPlots, vbeBox);
                    /* Show Vbc */
                    vbcBox = new ScopeRadioButton("Show Vbc", SCOPE_MENU.SHOW_VBC);
                    vbcBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange((ScopeRadioButton)sender); });
                    addItemToGrid(pnlPlots, vbcBox);
                    /* Show Vce */
                    vceBox = new ScopeRadioButton("Show Vce", SCOPE_MENU.SHOW_VCE);
                    vceBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange((ScopeRadioButton)sender); });
                    addItemToGrid(pnlPlots, vceBox);
                } else {
                    /* Show Voltage */
                    voltageBox = new ScopeCheckBox("Show Voltage", SCOPE_MENU.SHOW_VOLTAGE);
                    voltageBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                    addItemToGrid(pnlPlots, voltageBox);
                    /* Show Current */
                    currentBox = new ScopeCheckBox("Show Current", SCOPE_MENU.SHOW_CURRENT);
                    currentBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                    addItemToGrid(pnlPlots, currentBox);
                }
                /* Show Spectrum */
                spectrumBox = new ScopeCheckBox("Show Spectrum", SCOPE_MENU.SHOW_FFT);
                spectrumBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(pnlPlots, spectrumBox);
                /* Log Spectrum */
                logSpectrumBox = new ScopeCheckBox("Log Spectrum", SCOPE_MENU.LOG_SPECTRUM);
                logSpectrumBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(pnlPlots, logSpectrumBox);
                /* */
                pnlPlots.Left = 4;
                pnlPlots.Top = grbSpeed.Bottom + 8;
                pnlPlots.Width = grbSpeed.Width;
                pnlPlots.Height = Math.Min(130, gridY);
                Controls.Add(pnlPlots);
            }

            var grbXY = new GroupBox();
            {
                grbXY.Text = "X-Y Plots";
                gridY = 12;
                /* Show V vs I */
                viBox = new ScopeCheckBox("Show V vs I", SCOPE_MENU.SHOW_V_I);
                viBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbXY, viBox);
                /* Plot X/Y */
                xyBox = new ScopeCheckBox("Plot X/Y", SCOPE_MENU.PLOT_XY);
                xyBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbXY, xyBox);
                if (transistor) {
                    /* Show Vce vs Ic */
                    vceIcBox = new ScopeCheckBox("Show Vce vs Ic", SCOPE_MENU.SHOW_VCE_IC);
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
                scaleBox = new ScopeCheckBox("Show Scale", SCOPE_MENU.SHOW_SCALE);
                scaleBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbShowInfo, scaleBox);
                /* Show Peak Value */
                peakBox = new ScopeCheckBox("Show Peak Value", SCOPE_MENU.SHOW_PEAK);
                peakBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbShowInfo, peakBox);
                /* Show Negative Peak Value */
                negPeakBox = new ScopeCheckBox("Show Negative Peak Value", SCOPE_MENU.SHOW_NEG_PEAK);
                negPeakBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbShowInfo, negPeakBox);
                /* Show Frequency */
                freqBox = new ScopeCheckBox("Show Frequency", SCOPE_MENU.SHOW_FREQ);
                freqBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbShowInfo, freqBox);
                /* Show RMS Average */
                rmsBox = new ScopeCheckBox("Show RMS Average", SCOPE_MENU.SHOW_RMS);
                rmsBox.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbShowInfo, rmsBox);
                /* Show Duty Cycle */
                dutyBox = new ScopeCheckBox("Show Duty Cycle", SCOPE_MENU.SHOW_DUTY);
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
                saveAsDefaultButton.Click += new EventHandler((sender, e) => { scope.SaveAsDefault(); });
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
        }

        public void Show(int x, int y) {
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Visible = false;
            Show();
            Left = x;
            Top = y;
            Visible = true;
        }

        void setScopeSpeedLabel() {
            scopeSpeedLabel.Text = (Utils.UnitText(scope.CalcGridStepX(), "s") + "/div");
        }

        void addItemToGrid(Control grb, Control ctrl) {
            ctrl.Left = 8;
            ctrl.Top = gridY;
            grb.Controls.Add(ctrl);
            gridY += ctrl.Height + 4;
        }

        void scrollbarChanged() {
            int newsp = (int)Math.Pow(2, 10 - speedBar.Value);
            Console.WriteLine("changed " + scope.Speed + " " + newsp + " " + speedBar.Value);
            if (scope.Speed != newsp) {
                scope.Speed = newsp;
            }
            setScopeSpeedLabel();
        }

        void updateUI() {
            speedBar.Value = (10 - (int)Math.Round(Math.Log(scope.Speed) / Math.Log(2)));
            if (voltageBox != null) {
                voltageBox.Checked = scope.ShowV && !scope.ShowingValue(Scope.VAL.POWER);
                currentBox.Checked = scope.ShowI && !scope.ShowingValue(Scope.VAL.POWER);
            }
            scaleBox.Checked = scope.ShowScale;
            peakBox.Checked = scope.ShowMax;
            negPeakBox.Checked = scope.ShowMin;
            freqBox.Checked = scope.ShowFreq;
            spectrumBox.Checked = scope.ShowFFT;
            rmsBox.Checked = scope.ShowRMS;
            rmsBox.Text = scope.CanShowRMS ? "Show RMS Average" : "Show Average";
            viBox.Checked = scope.Plot2d && !scope.PlotXY;
            xyBox.Checked = scope.PlotXY;
            if (vbeBox != null) {
                if (ibBox.Checked) scope.ShowingValue(Scope.VAL.IB);
                if (icBox.Checked) scope.ShowingValue(Scope.VAL.IC);
                if (ieBox.Checked) scope.ShowingValue(Scope.VAL.IE);
                if (vbeBox.Checked) scope.ShowingValue(Scope.VAL.VBE);
                if (vbcBox.Checked) scope.ShowingValue(Scope.VAL.VBC);
                if (vceBox.Checked) scope.ShowingValue(Scope.VAL.VCE);
                if (vceIcBox.Checked) {
                    vceIcBox.Checked = scope.IsShowingVceAndIc;
                }
            }
            manualScaleLabel.Text = "Scale (Max Value)" + " (" + scope.ScaleUnitsText + ")";
            manualScaleTextBox.Text = ElementInfoDialog.unitString(null, scope.ScaleValue);
            manualScaleBox.Checked = scope.LockScale;
            manualScaleTextBox.Enabled = scope.LockScale;
            logSpectrumBox.Checked = scope.LogSpectrum;
            logSpectrumBox.Enabled = scope.ShowFFT;
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
                double d = ElementInfoDialog.parseUnits(manualScaleTextBox.Text);
                scope.ScaleValue = d;
            } catch { }
        }

        void onValueChange(object sender) {
            var cb = (ScopeCheckBox)sender;
            scope.HandleMenu(cb.Menu, cb.Checked);
            updateUI();
        }
        void onValueChange(ScopeRadioButton sender) {
            scope.HandleMenu(sender.Menu, sender.Checked);
            updateUI();
        }
    }
}
