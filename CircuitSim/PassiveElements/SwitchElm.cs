using System.Drawing;
using System.Windows.Forms;

namespace Circuit.PassiveElements {
    class SwitchElm : CircuitElm {
        const int OPEN_HS = 16;

        Point mP1;
        Point mP2;

        public SwitchElm(Point pos) : base(pos) {
            Momentary = false;
            Position = 0;
            PosCount = 2;
        }

        public SwitchElm(Point pos, bool mm) : base(pos) {
            Position = mm ? 1 : 0;
            Momentary = mm;
            PosCount = 2;
        }

        public SwitchElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            string str = st.nextToken();
            Position = int.Parse(str);
            Momentary = st.nextTokenBool();
            PosCount = 2;
        }

        public bool Momentary { get; protected set; }
        /* position 0 == closed
         * position 1 == open */
        public int Position { get; protected set; }
        public int PosCount { get; protected set; }
        public override bool IsWire { get { return Position == 0; } }
        public override int VoltageSourceCount { get { return (1 == Position) ? 0 : 1; } }
        public override DUMP_ID Shortcut { get { return DUMP_ID.SWITCH; } }
        public override DUMP_ID DumpType { get { return DUMP_ID.SWITCH; } }

        protected override string dump() {
            return Position + " " + Momentary;
        }

        protected override void calculateCurrent() {
            if (Position == 1) {
                mCurrent = 0;
            }
        }

        public void MouseUp() {
            if (Momentary) {
                Toggle();
            }
        }

        public virtual void Toggle() {
            Position++;
            if (Position >= PosCount) {
                Position = 0;
            }
        }

        public virtual Rectangle GetSwitchRect() {
            Utils.InterpPoint(mLead1, mLead2, ref mP1, 0, OPEN_HS);
            var l1 = new Rectangle(mLead1.X, mLead1.Y, 0, 0);
            var l2 = new Rectangle(mLead2.X, mLead2.Y, 0, 0);
            var p = new Rectangle(mP1.X, mP1.Y, 0, 0);
            return Rectangle.Union(l1, Rectangle.Union(l2, p));
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(32);
        }

        public override void Draw(CustomGraphics g) {
            int hs1 = (Position == 1) ? 0 : 2;
            int hs2 = (Position == 1) ? OPEN_HS : 2;
            setBbox(mPoint1, mPoint2, OPEN_HS);
            draw2Leads(g);
            if (Position == 0) {
                doDots(g);
            }
            Utils.InterpPoint(mLead1, mLead2, ref mP1, 0, hs1);
            Utils.InterpPoint(mLead1, mLead2, ref mP2, 1, hs2);
            g.ThickLineColor = NeedsHighlight ? SelectColor : WhiteColor;
            g.DrawThickLine(mP1, mP2);
            drawPosts(g);
        }

        public override void Stamp() {
            if (Position == 0) {
                mCir.StampVoltageSource(Nodes[0], Nodes[1], mVoltSource, 0);
            }
        }

        public override void GetInfo(string[] arr) {
            arr[0] = (Momentary) ? "push switch (SPST)" : "switch (SPST)";
            if (Position == 1) {
                arr[1] = "open";
                arr[2] = "Vd = " + Utils.VoltageDText(VoltageDiff);
            } else {
                arr[1] = "closed";
                arr[2] = "V = " + Utils.VoltageText(Volts[0]);
                arr[3] = "I = " + Utils.CurrentDText(mCurrent);
            }
        }

        public override bool GetConnection(int n1, int n2) { return Position == 0; }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "Momentary Switch";
                ei.CheckBox.Checked = Momentary;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                Momentary = ei.CheckBox.Checked;
            }
        }
    }
}
