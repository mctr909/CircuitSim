using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class SwitchElm : CircuitElm {
        const int openhs = 16;

        public bool momentary { get; protected set; }
        /* position 0 == closed
         * position 1 == open */
        public int position { get; protected set; }
        public int posCount { get; protected set; }
        Point ps1;
        Point ps2;

        public SwitchElm(int xx, int yy) : base(xx, yy) {
            momentary = false;
            position = 0;
            posCount = 2;
        }

        public SwitchElm(int xx, int yy, bool mm) : base(xx, yy) {
            position = mm ? 1 : 0;
            momentary = mm;
            posCount = 2;
        }

        public SwitchElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            string str = st.nextToken();
            if (str.CompareTo("true") == 0) {
                position = (this is LogicInputElm) ? 0 : 1;
            } else if (str.CompareTo("false") == 0) {
                position = (this is LogicInputElm) ? 1 : 0;
            } else {
                position = int.Parse(str);
            }
            momentary = st.nextTokenBool();
            posCount = 2;
        }

        public override bool IsWire { get { return position == 0; } }

        public override int VoltageSourceCount { get { return (1 == position) ? 0 : 1; } }

        public override DUMP_ID Shortcut { get { return DUMP_ID.SWITCH; } }

        protected override string dump() {
            return position + " " + momentary;
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.SWITCH; }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(32);
        }

        public override void Draw(CustomGraphics g) {
            int hs1 = (position == 1) ? 0 : 2;
            int hs2 = (position == 1) ? openhs : 2;
            setBbox(mPoint1, mPoint2, openhs);
            draw2Leads(g);
            if (position == 0) {
                doDots(g);
            }
            interpPoint(mLead1, mLead2, ref ps1, 0, hs1);
            interpPoint(mLead1, mLead2, ref ps2, 1, hs2);
            g.ThickLineColor = NeedsHighlight ? SelectColor : WhiteColor;
            g.DrawThickLine(ps1, ps2);
            drawPosts(g);
        }

        public virtual Rectangle getSwitchRect() {
            interpPoint(mLead1, mLead2, ref ps1, 0, openhs);
            var l1 = new Rectangle(mLead1.X, mLead1.Y, 0, 0);
            var l2 = new Rectangle(mLead2.X, mLead2.Y, 0, 0);
            var p = new Rectangle(ps1.X, ps1.Y, 0, 0);
            return Rectangle.Union(l1, Rectangle.Union(l2, p));
        }

        protected override void calculateCurrent() {
            if (position == 1) {
                mCurrent = 0;
            }
        }

        public override void Stamp() {
            if (position == 0) {
                mCir.StampVoltageSource(Nodes[0], Nodes[1], mVoltSource, 0);
            }
        }

        public void mouseUp() {
            if (momentary) {
                toggle();
            }
        }

        public virtual void toggle() {
            position++;
            if (position >= posCount) {
                position = 0;
            }
        }

        public override void GetInfo(string[] arr) {
            arr[0] = (momentary) ? "push switch (SPST)" : "switch (SPST)";
            if (position == 1) {
                arr[1] = "open";
                arr[2] = "Vd = " + getVoltageDText(VoltageDiff);
            } else {
                arr[1] = "closed";
                arr[2] = "V = " + getVoltageText(Volts[0]);
                arr[3] = "I = " + getCurrentDText(mCurrent);
            }
        }

        public override bool GetConnection(int n1, int n2) { return position == 0; }

        public override EditInfo GetEditInfo(int n) {
            if (n == 0) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "Momentary Switch";
                ei.CheckBox.Checked = momentary;
                return ei;
            }
            return null;
        }

        public override void SetEditValue(int n, EditInfo ei) {
            if (n == 0) {
                momentary = ei.CheckBox.Checked;
            }
        }
    }
}
