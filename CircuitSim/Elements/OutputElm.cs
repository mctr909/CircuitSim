using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class OutputElm : CircuitElm {
        const int FLAG_VALUE = 1;
        int scale;

        public OutputElm(int xx, int yy) : base(xx, yy) {
            scale = SCALE_AUTO;
        }

        public OutputElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            scale = SCALE_AUTO;
            try {
                scale = st.nextTokenInt();
            } catch { }
        }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override int PostCount { get { return 1; } }

        protected override string dump() {
            return scale.ToString();
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.OUTPUT; }

        public override void SetPoints() {
            base.SetPoints();
            mLead1 = new Point();
        }

        public override void Draw(Graphics g) {
            bool selected = needsHighlight();
            PenThickLine.Color = selected ? SelectColor : WhiteColor;

            string txt = (mFlags & FLAG_VALUE) != 0 ? getUnitTextWithScale(Volts[0], "V", scale) : "out";
            if (this == Sim.plotXElm) {
                txt = "X";
            }
            if (this == Sim.plotYElm) {
                txt = "Y";
            }
            interpPoint(mPoint1, mPoint2, ref mLead1, 1 - ((int)g.MeasureString(txt, FONT_TEXT).Width / 2 + 8) / mLen);
            setBbox(mPoint1, mLead1, 0);
            drawCenteredLText(g, txt, X2, Y2, true);
            PenThickLine.Color = getVoltageColor(Volts[0]);
            if (selected) {
                PenThickLine.Color = SelectColor;
            }
            drawThickLine(g, mPoint1, mLead1);
            drawPosts(g);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "output";
            arr[1] = "V = " + getVoltageText(Volts[0]);
        }

        public override EditInfo GetEditInfo(int n) {
            if (n == 0) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "Show Voltage";
                ei.CheckBox.Checked = (mFlags & FLAG_VALUE) != 0;
                return ei;
            }
            if (n == 1) {
                var ei = new EditInfo("Scale", 0);
                ei.Choice = new ComboBox();
                ei.Choice.Items.Add("Auto");
                ei.Choice.Items.Add("V");
                ei.Choice.Items.Add("mV");
                ei.Choice.Items.Add(CirSim.muString + "V");
                ei.Choice.SelectedIndex = scale;
                return ei;
            }
            return null;
        }

        public override void SetEditValue(int n, EditInfo ei) {
            if (n == 0) {
                mFlags = ei.CheckBox.Checked ? (mFlags | FLAG_VALUE) : (mFlags & ~FLAG_VALUE);
            }
            if (n == 1) {
                scale = ei.Choice.SelectedIndex;
            }
        }
    }
}
