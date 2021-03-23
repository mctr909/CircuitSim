using System;
using System.Windows.Forms;

namespace Circuit {
    static class ControlPanel {
        public static Panel VerticalPanel { get; private set; }
        public static Button BtnReset { get; private set; }
        public static Button BtnRunStop { get; private set; }
        public static TrackBar TrbSpeed { get; private set; }
        public static TrackBar TrbCurrent { get; private set; }
        public static CheckBox ChkShowVolts { get; private set; }
        public static CheckBox ChkShowDots { get; private set; }
        public static CheckBox ChkShowValues { get; private set; }
        public static CheckBox ChkUseAnsiSymbols { get; private set; }
        public static CheckBox ChkPrintable { get; private set; }
        public static CheckBox ChkCrossHair { get; private set; }

        public static double TimeStep {
            get { return mTimeStep; }
            set {
                mTimeStep = value;
                if (null != mTxtTimeStep) {
                    mTxtTimeStep.Text = Utils.ShortUnitText(mTimeStep, "");
                }
            }
        }
        public static double VoltageRange {
            get { return mVoltageRange; }
            set { 
                mVoltageRange = value;
                if (null != mTxtVoltageRange) {
                    mTxtVoltageRange.Text = Utils.ShortUnitText(mVoltageRange, "");
                }
            }
        }

        static TextBox mTxtTimeStep;
        static TextBox mTxtVoltageRange;
        static double mTimeStep;
        static double mVoltageRange;

        static Panel mSliderPanel;

        public static void Init(CirSim sim) {
            int ofsY = 0;
            VerticalPanel = new Panel();

            /* Reset */
            BtnReset = new Button() { AutoSize = true, Text = "Reset" };
            BtnReset.Click += new EventHandler((s, e) => { sim.ResetButton_onClick(); });
            BtnReset.Left = 4;
            BtnReset.Top = ofsY;
            VerticalPanel.Controls.Add(BtnReset);
            ofsY += BtnReset.Height + 4;

            /* Run */
            BtnRunStop = new Button() { AutoSize = true, Text = "RUN" };
            BtnRunStop.Click += new EventHandler((s, e) => { sim.SetSimRunning(!sim.IsRunning); });
            BtnRunStop.Left = 4;
            BtnRunStop.Top = ofsY;
            VerticalPanel.Controls.Add(BtnRunStop);
            ofsY += BtnRunStop.Height + 4;

            /* Simulation Speed */
            var lbl = new Label() { Left = 4, Top = ofsY, AutoSize = true, Text = "Simulation Speed" };
            VerticalPanel.Controls.Add(lbl);
            ofsY += lbl.Height;
            TrbSpeed = new TrackBar() {
                Left = 4,
                Top = ofsY,
                Minimum = 0,
                Maximum = 1000,
                SmallChange = 1,
                LargeChange = 10,
                TickFrequency = 100,
                TickStyle = TickStyle.TopLeft,
                Value = 10,
                Width = 200
            };
            VerticalPanel.Controls.Add(TrbSpeed);
            ofsY += TrbSpeed.Height + 4;

            /* Current Speed */
            lbl = new Label() { Left = 4, Top = ofsY, AutoSize = true, Text = "Current Speed" };
            VerticalPanel.Controls.Add(lbl);
            ofsY += lbl.Height;
            TrbCurrent = new TrackBar() {
                Left = 4,
                Top = ofsY,
                Minimum = 1,
                Maximum = 100,
                SmallChange = 1,
                LargeChange = 10,
                TickFrequency = 10,
                TickStyle = TickStyle.TopLeft,
                Value = 50,
                Width = 200
            };
            VerticalPanel.Controls.Add(TrbCurrent);
            ofsY += TrbCurrent.Height + 4;

            /* Show Voltage */
            ChkShowVolts = new CheckBox() { Left = 4, Top = ofsY, AutoSize = true, Text = "電圧を表示" };
            VerticalPanel.Controls.Add(ChkShowVolts);
            ofsY += ChkShowVolts.Height + 4;

            /* Show Current */
            ChkShowDots = new CheckBox() { Left = 4, Top = ofsY, AutoSize = true, Text = "電流を表示" };
            VerticalPanel.Controls.Add(ChkShowDots);
            ofsY += ChkShowDots.Height + 4;

            /* Show Values */
            ChkShowValues = new CheckBox() { Left = 4, Top = ofsY, AutoSize = true, Text = "値を表示" };
            VerticalPanel.Controls.Add(ChkShowValues);
            ofsY += ChkShowValues.Height + 4;

            /* ANSI */
            ChkUseAnsiSymbols = new CheckBox() { Left = 4, Top = ofsY, AutoSize = true, Text = "ANSI" };
            ChkUseAnsiSymbols.CheckedChanged += new EventHandler((s, e) => {
                setOptionInStorage("ansiResistors", ChkUseAnsiSymbols.Checked);
            });
            VerticalPanel.Controls.Add(ChkUseAnsiSymbols);
            ofsY += ChkUseAnsiSymbols.Height + 4;

            /* White Background */
            ChkPrintable = new CheckBox() { Left = 4, Top = ofsY, AutoSize = true, Text = "白黒表示" };
            ChkPrintable.CheckedChanged += new EventHandler((s, e) => {
                for (int i = 0; i < sim.mScopeCount; i++) {
                    sim.mScopes[i].SetRect(sim.mScopes[i].BoundingBox);
                }
                setOptionInStorage("whiteBackground", ChkPrintable.Checked);
            });
            VerticalPanel.Controls.Add(ChkPrintable);
            ofsY += ChkPrintable.Height + 4;

            /* Show Cursor Cross Hairs */
            ChkCrossHair = new CheckBox() { Left = 4, Top = ofsY, AutoSize = true, Text = "ポインターを表示" };
            ChkCrossHair.CheckedChanged += new EventHandler((s, e) => {
                setOptionInStorage("crossHair", ChkCrossHair.Checked);
            });
            VerticalPanel.Controls.Add(ChkCrossHair);
            ofsY += ChkCrossHair.Height + 4;

            /* TimeStep */
            ofsY += 8;
            var lblTimeStep = new Label() { Left = 4, Top = ofsY, AutoSize = true, Text = "単位時間(sec)" };
            VerticalPanel.Controls.Add(lblTimeStep);
            ofsY += lblTimeStep.Height + 4;
            mTxtTimeStep = new TextBox() { Left = 4, Top = ofsY, Width = 80 };
            mTxtTimeStep.TextChanged += new EventHandler((s, e) => {
                var tmp = 0.0;
                if(Utils.TextToNum(mTxtTimeStep.Text, out tmp)) {
                    mTimeStep = tmp;
                } else {
                    mTxtTimeStep.Text = Utils.ShortUnitText(mTimeStep, "");
                }
            });
            VerticalPanel.Controls.Add(mTxtTimeStep);
            ofsY += mTxtTimeStep.Height + 4;

            /* VoltageRange */
            ofsY += 4;
            var lblVoltageRange = new Label() { Left = 4, Top = ofsY, AutoSize = true, Text = "青から緑への電圧幅(V)" };
            VerticalPanel.Controls.Add(lblVoltageRange);
            ofsY += lblVoltageRange.Height + 4;
            mTxtVoltageRange = new TextBox() { Left = 4, Top = ofsY, Width = 80 };
            mTxtVoltageRange.TextChanged += new EventHandler((s, e) => {
                var tmp = 0.0;
                if (Utils.TextToNum(mTxtVoltageRange.Text, out tmp)) {
                    mVoltageRange = tmp;
                } else {
                    mTxtVoltageRange.Text = Utils.ShortUnitText(mVoltageRange, "");
                }
            });
            VerticalPanel.Controls.Add(mTxtVoltageRange);
            ofsY += mTxtVoltageRange.Height + 4;

            /* SliderPanel */
            mSliderPanel = new Panel() {
                Left = 4,
                Top = ofsY,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };
            VerticalPanel.Controls.Add(mSliderPanel);

            /* */
            VerticalPanel.Width = TrbSpeed.Width + 12;
            VerticalPanel.Height = ofsY;
        }

        public static void Reset() {
            TimeStep = 10e-6;
            VoltageRange = 5;
            ChkShowDots.Checked = false;
            ChkShowVolts.Checked = true;
            ChkShowValues.Checked = true;
            ChkUseAnsiSymbols.Checked = true;
            TrbSpeed.Value = 57;
            TrbCurrent.Value = 50;
        }

        public static void SetSliderPanelHeight() {
            if (mSliderPanel == null) {
                return;
            }
            int height = 0;
            for (int i = 0; i < mSliderPanel.Controls.Count; i++) {
                if (height < mSliderPanel.Controls[i].Bottom) {
                    height = mSliderPanel.Controls[i].Bottom;
                }
            }
            mSliderPanel.Visible = 0 < mSliderPanel.Controls.Count;
            mSliderPanel.Height = Math.Min(220, height + 4);
            VerticalPanel.Height = mSliderPanel.Bottom + 4;
        }

        public static void AddSlider(Control ctrl) {
            if (mSliderPanel == null) {
                return;
            }
            var ofsY = 4;
            for (int i = 0; i < mSliderPanel.Controls.Count; i++) {
                if (ofsY < mSliderPanel.Controls[i].Bottom) {
                    ofsY = mSliderPanel.Controls[i].Bottom;
                }
            }
            ctrl.Top = ofsY;
            mSliderPanel.Controls.Add(ctrl);
            SetSliderPanelHeight();
        }

        public static void RemoveSlider(Control ctrl) {
            if (mSliderPanel == null) {
                return;
            }
            int ofsY = 4;
            mSliderPanel.SuspendLayout();
            mSliderPanel.Controls.Remove(ctrl);
            for (int i = 0; i < mSliderPanel.Controls.Count; i++) {
                mSliderPanel.Controls[i].Top = ofsY;
                ofsY += mSliderPanel.Controls[i].Height + 4;
            }
            mSliderPanel.ResumeLayout(false);
            SetSliderPanelHeight();
        }

        static void setOptionInStorage(string key, bool val) {
            var stor = Storage.getLocalStorageIfSupported();
            if (stor == null) {
                return;
            }
            stor.setItem(key, val ? "true" : "false");
        }
    }
}
