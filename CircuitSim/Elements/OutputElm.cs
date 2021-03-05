using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class OutputElm : CircuitElm {
        const int FLAG_VALUE = 1;
        E_SCALE scale;

        public OutputElm(int xx, int yy) : base(xx, yy) {
            scale = E_SCALE.AUTO;
        }

        public OutputElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            scale = E_SCALE.AUTO;
            try {
                scale = st.nextTokenEnum<E_SCALE>();
            } catch { }
        }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override int PostCount { get { return 1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.OUTPUT; } }

        protected override string dump() {
            return scale.ToString();
        }

        public override void SetPoints() {
            base.SetPoints();
            mLead1 = new Point();
        }

        public override void Draw(CustomGraphics g) {
            string txt = (mFlags & FLAG_VALUE) != 0 ? Utils.UnitTextWithScale(Volts[0], "V", scale) : "out";
            if (this == Sim.plotXElm) {
                txt = "X";
            }
            if (this == Sim.plotYElm) {
                txt = "Y";
            }

            Utils.InterpPoint(mPoint1, mPoint2, ref mLead1, 1 - ((int)g.GetLTextSize(txt).Width / 2 + 8) / mLen);
            setBbox(mPoint1, mLead1, 0);

            bool selected = NeedsHighlight;
            g.TextColor = selected ? SelectColor : TextColor;
            drawCenteredLText(g, txt, X2, Y2, true);

            if (selected) {
                g.ThickLineColor = SelectColor;
            } else {
                g.ThickLineColor = getVoltageColor(Volts[0]);
            }
            g.DrawThickLine(mPoint1, mLead1);
            drawPosts(g);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "output";
            arr[1] = "V = " + Utils.VoltageText(Volts[0]);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "Show Voltage";
                ei.CheckBox.Checked = (mFlags & FLAG_VALUE) != 0;
                return ei;
            }
            if (n == 1) {
                var ei = new ElementInfo("Scale", 0);
                ei.Choice = new ComboBox();
                ei.Choice.Items.Add("Auto");
                ei.Choice.Items.Add("V");
                ei.Choice.Items.Add("mV");
                ei.Choice.Items.Add(CirSim.muString + "V");
                ei.Choice.SelectedIndex = (int)scale;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                mFlags = ei.CheckBox.Checked ? (mFlags | FLAG_VALUE) : (mFlags & ~FLAG_VALUE);
            }
            if (n == 1) {
                scale = (E_SCALE)ei.Choice.SelectedIndex;
            }
        }
    }
}
