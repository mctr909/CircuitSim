using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Output {
    class VoltMeterElm : CircuitElm {
        const int FLAG_SHOWVOLTAGE = 1;

        Point mCenter;
        Point mPlusPoint;

        public VoltMeterElm(Point pos) : base(pos) {
            CirElm = new VoltMeterElmE();
            /* default for new elements */
            mFlags = FLAG_SHOWVOLTAGE;
        }

        public VoltMeterElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            CirElm = new VoltMeterElmE(st);
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.VOLTMETER; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.VOLTMETER; } }

        protected override string dump() {
            var ce = (VoltMeterElmE)CirElm;
            return ce.Meter + " " + ce.Scale;
        }

        public override bool GetConnection(int n1, int n2) { return false; }

        public override void SetPoints() {
            base.SetPoints();
            interpPoint(ref mCenter, 0.5, 12 * mDsign);
            interpPoint(ref mPlusPoint, 8.0 / mLen, 6 * mDsign);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (VoltMeterElmE)CirElm;
            int hs = 8;
            setBbox(mPoint1, mPoint2, hs);
            bool selected = NeedsHighlight;
            double len = (selected || CirSim.Sim.DragElm == this || mustShowVoltage()) ? 16 : mLen - 32;
            calcLeads((int)len);

            if (selected) {
                g.LineColor = CustomGraphics.SelectColor;
            } else {
                g.LineColor = CustomGraphics.GrayColor;
            }
            drawLead(mPoint1, mLead1);

            if (selected) {
                g.LineColor = CustomGraphics.SelectColor;
            } else {
                g.LineColor = CustomGraphics.GrayColor;
            }
            drawLead(mLead2, mPoint2);

            if (this == CirSim.Sim.PlotXElm) {
                drawCenteredLText("X", mCenter, true);
            }
            if (this == CirSim.Sim.PlotYElm) {
                drawCenteredLText("Y", mCenter, true);
            }

            if (mustShowVoltage()) {
                string s = "";
                switch (ce.Meter) {
                case VoltMeterElmE.TP_VOL:
                    s = Utils.UnitTextWithScale(ce.VoltageDiff, "V", ce.Scale);
                    break;
                case VoltMeterElmE.TP_RMS:
                    s = Utils.UnitTextWithScale(ce.RmsV, "V(rms)", ce.Scale);
                    break;
                case VoltMeterElmE.TP_MAX:
                    s = Utils.UnitTextWithScale(ce.LastMaxV, "Vpk", ce.Scale);
                    break;
                case VoltMeterElmE.TP_MIN:
                    s = Utils.UnitTextWithScale(ce.LastMinV, "Vmin", ce.Scale);
                    break;
                case VoltMeterElmE.TP_P2P:
                    s = Utils.UnitTextWithScale(ce.LastMaxV - ce.LastMinV, "Vp2p", ce.Scale);
                    break;
                case VoltMeterElmE.TP_BIN:
                    s = ce.BinaryLevel + "";
                    break;
                case VoltMeterElmE.TP_FRQ:
                    s = Utils.UnitText(ce.Frequency, "Hz");
                    break;
                case VoltMeterElmE.TP_PER:
                    s = "percent:" + ce.Period + " " + ControlPanel.TimeStep + " " + CirSim.Sim.Time + " " + CirSim.Sim.getIterCount();
                    break;
                case VoltMeterElmE.TP_PWI:
                    s = Utils.UnitText(ce.PulseWidth, "S");
                    break;
                case VoltMeterElmE.TP_DUT:
                    s = ce.DutyCycle.ToString("0.000");
                    break;
                }
                drawCenteredText(s, mCenter, true);
            }
            drawCenteredLText("+", mPlusPoint, true);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (VoltMeterElmE)CirElm;
            arr[0] = "voltmeter";
            arr[1] = "Vd = " + Utils.VoltageText(ce.VoltageDiff);
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (VoltMeterElmE)CirElm;
            if (n == 0) {
                var ei = new ElementInfo("表示", ce.SelectedValue, -1, -1);
                ei.Choice = new ComboBox();
                ei.Choice.Items.Add("瞬時値");
                ei.Choice.Items.Add("実効値");
                ei.Choice.Items.Add("最大値");
                ei.Choice.Items.Add("最小値");
                ei.Choice.Items.Add("P-P");
                ei.Choice.Items.Add("2値");
                ei.Choice.SelectedIndex = ce.Meter;
                return ei;
            }
            if (n == 1) {
                var ei = new ElementInfo("スケール", 0);
                ei.Choice = new ComboBox();
                ei.Choice.Items.Add("自動");
                ei.Choice.Items.Add("V");
                ei.Choice.Items.Add("mV");
                ei.Choice.Items.Add("uV");
                ei.Choice.SelectedIndex = (int)ce.Scale;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (VoltMeterElmE)CirElm;
            if (n == 0) {
                ce.Meter = ei.Choice.SelectedIndex;
            }
            if (n == 1) {
                ce.Scale = (E_SCALE)ei.Choice.SelectedIndex;
            }
        }

        bool mustShowVoltage() {
            return (mFlags & FLAG_SHOWVOLTAGE) != 0;
        }
    }
}
