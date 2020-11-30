using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class OutputElm : CircuitElm {
        const int FLAG_VALUE = 1;
        int scale;

        Font fontRegular = new Font("Meiryo UI", 14, FontStyle.Regular);
        Font fontBold = new Font("Meiryo UI", 14, FontStyle.Bold);

        public OutputElm(int xx, int yy) : base(xx, yy) {
            scale = SCALE_AUTO;
        }

        public OutputElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            scale = SCALE_AUTO;
            try {
                scale = st.nextTokenInt();
            } catch { }
        }

        public override string dump() {
            return base.dump() + " " + scale;
        }

        public override DUMP_ID getDumpType() { return DUMP_ID.OUTPUT; }

        public override int getPostCount() { return 1; }

        public override void setPoints() {
            base.setPoints();
            mLead1 = new Point();
        }

        public override void draw(Graphics g) {
            bool selected = needsHighlight();
            var font = selected ? fontBold : fontRegular;
            PEN_THICK_LINE.Color = selected ? selectColor : whiteColor;

            string txt = (mFlags & FLAG_VALUE) != 0 ? getUnitTextWithScale(Volts[0], "V", scale) : "out";
            if (this == sim.plotXElm) {
                txt = "X";
            }
            if (this == sim.plotYElm) {
                txt = "Y";
            }
            interpPoint(mPoint1, mPoint2, ref mLead1, 1 - ((int)g.MeasureString(txt, font).Width / 2 + 8) / mElmLen);
            setBbox(mPoint1, mLead1, 0);
            drawCenteredText(g, txt, X2, Y2, true);
            PEN_THICK_LINE.Color = getVoltageColor(Volts[0]);
            if (selected) {
                PEN_THICK_LINE.Color = selectColor;
            }
            drawThickLine(g, mPoint1, mLead1);
            drawPosts(g);
        }

        public override double getVoltageDiff() { return Volts[0]; }

        public override void getInfo(string[] arr) {
            arr[0] = "output";
            arr[1] = "V = " + getVoltageText(Volts[0]);
        }

        public override EditInfo getEditInfo(int n) {
            if (n == 0) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.checkbox = new CheckBox();
                ei.checkbox.Text = "Show Voltage";
                ei.checkbox.Checked = (mFlags & FLAG_VALUE) != 0;
                return ei;
            }
            if (n == 1) {
                var ei = new EditInfo("Scale", 0);
                ei.choice = new ComboBox();
                ei.choice.Items.Add("Auto");
                ei.choice.Items.Add("V");
                ei.choice.Items.Add("mV");
                ei.choice.Items.Add(CirSim.muString + "V");
                ei.choice.SelectedIndex = scale;
                return ei;
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (n == 0) {
                mFlags = ei.checkbox.Checked ? (mFlags | FLAG_VALUE) : (mFlags & ~FLAG_VALUE);
            }
            if (n == 1) {
                scale = ei.choice.SelectedIndex;
            }
        }
    }
}
