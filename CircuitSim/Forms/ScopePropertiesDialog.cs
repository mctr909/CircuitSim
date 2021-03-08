﻿using System;
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
        SHOW_VCE
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
        CheckBox chkScale;
        CheckBox chkManualScale;
        CheckBox chkVoltage;
        CheckBox chkCurrent;
        CheckBox chkPeak;
        CheckBox chkNegPeak;
        CheckBox chkFreq;
        CheckBox chkSpectrum;
        CheckBox chkLogSpectrum;
        CheckBox chkRms;
        CheckBox chkDuty;

        RadioButton rbIb;
        RadioButton rbIc;
        RadioButton rbIe;
        RadioButton rbVbe;
        RadioButton rbVbc;
        RadioButton rbVce;

        TextBox txtLabel;
        TextBox txtManualScale;

        Label lblScopeSpeed;
        Label lblManualScale;

        TrackBar tbSpeed;

        Button btnOk;
        Button btnApply;
        Button btnSaveAsDefault;

        Scope scope;
        int gridY;

        public ScopePropertiesDialog(Scope s) : base() {
            scope = s;

            var elm = scope.SingleElm;
            bool transistor = elm != null && (elm is TransistorElm);

            SuspendLayout();
            Text = "Scope Properties";

            var grbSpeed = new GroupBox();
            {
                grbSpeed.Text = "Scroll Speed";
                gridY = 12;
                /* tbSpeed */
                tbSpeed = new TrackBar() {
                    Minimum = 0,
                    Maximum = 10,
                    SmallChange = 1,
                    LargeChange = 1,
                    TickFrequency = 1,
                    Width = 200,
                    Height = 20,
                    TickStyle = TickStyle.TopLeft
                };
                tbSpeed.ValueChanged += new EventHandler((sender, e) => { scrollbarChanged(); });
                addItemToGrid(grbSpeed, tbSpeed);
                /* lblScopeSpeed */
                addItemToGrid(grbSpeed, lblScopeSpeed = new Label() {
                    AutoSize = true,
                    TextAlign = ContentAlignment.TopLeft
                });
                /* chkManualScale */
                chkManualScale = new ScopeCheckBox("Manual Scale", SCOPE_MENU.MANUAL_SCALE);
                chkManualScale.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbSpeed, chkManualScale);
                /* lblManualScale */
                addItemToGrid(grbSpeed, lblManualScale = new Label() {
                    Text = "Scale",
                    AutoSize = true,
                    TextAlign = ContentAlignment.BottomLeft
                });
                /* txtManualScale */
                addItemToGrid(grbSpeed, txtManualScale = new TextBox() {
                    Width = 100, Height = 24
                });
                /* */
                grbSpeed.Left = 4;
                grbSpeed.Top = 4;
                grbSpeed.Width = tbSpeed.Right + 4;
                grbSpeed.Height = gridY;
                Controls.Add(grbSpeed);
            }

            var grbShowInfo = new GroupBox();
            {
                grbShowInfo.Text = "Show Info";
                gridY = 12;
                /* chkScale */
                chkScale = new ScopeCheckBox("Show Scale", SCOPE_MENU.SHOW_SCALE);
                chkScale.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbShowInfo, chkScale);
                /* chkPeak */
                chkPeak = new ScopeCheckBox("Show Peak Value", SCOPE_MENU.SHOW_PEAK);
                chkPeak.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbShowInfo, chkPeak);
                /* chkNegPeak */
                chkNegPeak = new ScopeCheckBox("Show Negative Peak Value", SCOPE_MENU.SHOW_NEG_PEAK);
                chkNegPeak.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbShowInfo, chkNegPeak);
                /* chkFreq */
                chkFreq = new ScopeCheckBox("Show Frequency", SCOPE_MENU.SHOW_FREQ);
                chkFreq.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbShowInfo, chkFreq);
                /* chkRms */
                chkRms = new ScopeCheckBox("Show RMS Average", SCOPE_MENU.SHOW_RMS);
                chkRms.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbShowInfo, chkRms);
                /* chkDuty */
                chkDuty = new ScopeCheckBox("Show Duty Cycle", SCOPE_MENU.SHOW_DUTY);
                chkDuty.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(grbShowInfo, chkDuty);
                /* txtLabel */
                addItemToGrid(grbShowInfo, new Label() {
                    Text = "Custom Label",
                    AutoSize = true,
                    TextAlign = ContentAlignment.BottomLeft
                });
                txtLabel = new TextBox();
                addItemToGrid(grbShowInfo, txtLabel);
                string labelText = scope.Text;
                if (labelText != null) {
                    txtLabel.Text = labelText;
                }
                /* */
                grbShowInfo.Left = grbSpeed.Left;
                grbShowInfo.Top = grbSpeed.Bottom + 8;
                grbShowInfo.Width = grbSpeed.Width;
                grbShowInfo.Height = gridY;
                Controls.Add(grbShowInfo);
            }

            var pnlPlots = new Panel();
            {
                pnlPlots.Text = "Plots";
                pnlPlots.BorderStyle = BorderStyle.FixedSingle;
                pnlPlots.AutoScroll = true;
                gridY = 4;
                if (transistor) {
                    /* rbIb */
                    rbIb = new ScopeRadioButton("Show Ib", SCOPE_MENU.SHOW_IB);
                    rbIb.CheckedChanged += new EventHandler((sender, e) => { onValueChange((ScopeRadioButton)sender); });
                    addItemToGrid(pnlPlots, rbIb);
                    /* rbIc */
                    rbIc = new ScopeRadioButton("Show Ic", SCOPE_MENU.SHOW_IC);
                    rbIc.CheckedChanged += new EventHandler((sender, e) => { onValueChange((ScopeRadioButton)sender); });
                    addItemToGrid(pnlPlots, rbIc);
                    /* rbIe */
                    rbIe = new ScopeRadioButton("Show Ie", SCOPE_MENU.SHOW_IE);
                    rbIe.CheckedChanged += new EventHandler((sender, e) => { onValueChange((ScopeRadioButton)sender); });
                    addItemToGrid(pnlPlots, rbIe);
                    /* rbVbe */
                    rbVbe = new ScopeRadioButton("Show Vbe", SCOPE_MENU.SHOW_VBE);
                    rbVbe.CheckedChanged += new EventHandler((sender, e) => { onValueChange((ScopeRadioButton)sender); });
                    addItemToGrid(pnlPlots, rbVbe);
                    /* rbVbc */
                    rbVbc = new ScopeRadioButton("Show Vbc", SCOPE_MENU.SHOW_VBC);
                    rbVbc.CheckedChanged += new EventHandler((sender, e) => { onValueChange((ScopeRadioButton)sender); });
                    addItemToGrid(pnlPlots, rbVbc);
                    /* rbVce */
                    rbVce = new ScopeRadioButton("Show Vce", SCOPE_MENU.SHOW_VCE);
                    rbVce.CheckedChanged += new EventHandler((sender, e) => { onValueChange((ScopeRadioButton)sender); });
                    addItemToGrid(pnlPlots, rbVce);
                } else {
                    /* chkVoltage */
                    chkVoltage = new ScopeCheckBox("Show Voltage", SCOPE_MENU.SHOW_VOLTAGE);
                    chkVoltage.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                    addItemToGrid(pnlPlots, chkVoltage);
                    /* chkCurrent */
                    chkCurrent = new ScopeCheckBox("Show Current", SCOPE_MENU.SHOW_CURRENT);
                    chkCurrent.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                    addItemToGrid(pnlPlots, chkCurrent);
                }
                /* chkSpectrum */
                chkSpectrum = new ScopeCheckBox("Show Spectrum", SCOPE_MENU.SHOW_FFT);
                chkSpectrum.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(pnlPlots, chkSpectrum);
                /* chkLogSpectrum */
                chkLogSpectrum = new ScopeCheckBox("Log Spectrum", SCOPE_MENU.LOG_SPECTRUM);
                chkLogSpectrum.CheckedChanged += new EventHandler((sender, e) => { onValueChange(sender); });
                addItemToGrid(pnlPlots, chkLogSpectrum);
                /* */
                pnlPlots.Left = grbShowInfo.Left;
                pnlPlots.Top = grbShowInfo.Bottom + 8;
                pnlPlots.Width = grbShowInfo.Width;
                pnlPlots.Height = Math.Min(100, gridY);
                Controls.Add(pnlPlots);
            }

            var pnl = new Panel();
            {
                /* OK */
                btnOk = new Button() { Text = "OK" };
                btnOk.Click += new EventHandler((sender, e) => { closeDialog(); });
                btnOk.Left = 0;
                btnOk.Width = 50;
                btnOk.Top = 4;
                pnl.Controls.Add(btnOk);
                /* Apply */
                btnApply = new Button() { Text = "Apply" };
                btnApply.Click += new EventHandler((sender, e) => { apply(); });
                btnApply.Left = btnOk.Right + 4;
                btnApply.Width = 50;
                btnApply.Top = 4;
                pnl.Controls.Add(btnApply);
                /* Save as Default */
                btnSaveAsDefault = new Button() { Text = "Save as Default" };
                btnSaveAsDefault.Click += new EventHandler((sender, e) => { scope.SaveAsDefault(); });
                btnSaveAsDefault.Left = btnApply.Right + 4;
                btnSaveAsDefault.Width = 100;
                btnSaveAsDefault.Top = 4;
                pnl.Controls.Add(btnSaveAsDefault);
                /* */
                pnl.Left = pnlPlots.Left;
                pnl.Top = pnlPlots.Bottom;
                pnl.Width = btnSaveAsDefault.Right + 4;
                pnl.Height = btnSaveAsDefault.Bottom + 8;
                Controls.Add(pnl);
            }

            updateUI();
            ResumeLayout(false);

            Width = pnl.Right + 18;
            Height = pnl.Bottom + 36;
        }

        public void Show(Form parent) {
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Visible = false;
            Show();
            Left = parent.Location.X + parent.Width / 2 - Width / 2;
            Top = parent.Location.X + parent.Height / 2 - Height / 2;
            Visible = true;
        }

        void setScopeSpeedLabel() {
            lblScopeSpeed.Text = (Utils.UnitText(scope.CalcGridStepX(), "s") + "/div");
        }

        void addItemToGrid(Control grb, Control ctrl) {
            ctrl.Left = 8;
            ctrl.Top = gridY;
            grb.Controls.Add(ctrl);
            gridY += ctrl.Height + 4;
        }

        void scrollbarChanged() {
            int newsp = (int)Math.Pow(2, 10 - tbSpeed.Value);
            Console.WriteLine("changed " + scope.Speed + " " + newsp + " " + tbSpeed.Value);
            if (scope.Speed != newsp) {
                scope.Speed = newsp;
            }
            setScopeSpeedLabel();
        }

        void updateUI() {
            tbSpeed.Value = (10 - (int)Math.Round(Math.Log(scope.Speed) / Math.Log(2)));
            if (chkVoltage != null) {
                chkVoltage.Checked = scope.ShowV;
                chkCurrent.Checked = scope.ShowI;
            }
            chkScale.Checked = scope.ShowScale;
            chkPeak.Checked = scope.ShowMax;
            chkNegPeak.Checked = scope.ShowMin;
            chkFreq.Checked = scope.ShowFreq;
            chkSpectrum.Checked = scope.ShowFFT;
            chkRms.Checked = scope.ShowRMS;
            chkRms.Text = scope.CanShowRMS ? "Show RMS Average" : "Show Average";
            if (rbVbe != null) {
                if (rbIb.Checked) scope.ShowingValue(Scope.VAL.IB);
                if (rbIc.Checked) scope.ShowingValue(Scope.VAL.IC);
                if (rbIe.Checked) scope.ShowingValue(Scope.VAL.IE);
                if (rbVbe.Checked) scope.ShowingValue(Scope.VAL.VBE);
                if (rbVbc.Checked) scope.ShowingValue(Scope.VAL.VBC);
                if (rbVce.Checked) scope.ShowingValue(Scope.VAL.VCE);
            }
            lblManualScale.Text = "Scale (Max Value)" + " (" + scope.ScaleUnitsText + ")";
            txtManualScale.Text = ElementInfoDialog.unitString(null, scope.ScaleValue);
            chkManualScale.Checked = scope.LockScale;
            txtManualScale.Enabled = scope.LockScale;
            chkLogSpectrum.Checked = scope.LogSpectrum;
            chkLogSpectrum.Enabled = scope.ShowFFT;
            setScopeSpeedLabel();

            /* if you add more here, make sure it still works with transistor scopes */
        }

        void closeDialog() {
            apply();
            Close();
        }

        void apply() {
            string label = txtLabel.Text;
            if (label.Length == 0) {
                label = null;
            }
            scope.Text = label;
            try {
                double d = ElementInfoDialog.parseUnits(txtManualScale.Text);
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
