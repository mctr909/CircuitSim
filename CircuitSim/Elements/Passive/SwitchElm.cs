using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Passive {
    class SwitchElm : CircuitElm {
        const int OPEN_HS = 16;
        const int BODY_LEN = 28;

        Point mP1;
        Point mP2;

        public SwitchElm(Point pos, int dummy) : base(pos) { }

        public SwitchElm(Point pos) : base(pos) {
            CirElm = new SwitchElmE();
        }

        public SwitchElm(Point pos, bool mm) : base(pos) {
            CirElm = new SwitchElmE(mm);
        }

        public SwitchElm(Point p1, Point p2, int f) : base(p1, p2, f) { }

        public SwitchElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            CirElm = new SwitchElmE(st);
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.SWITCH; } }
        public override DUMP_ID DumpType { get { return DUMP_ID.SWITCH; } }

        protected override string dump() {
            var ce = (SwitchElmE)CirElm;
            return ce.Position + " " + ce.Momentary;
        }

        public void MouseUp() {
            var ce = (SwitchElmE)CirElm;
            if (ce.Momentary) {
                Toggle();
            }
        }

        public virtual void Toggle() {
            var ce = (SwitchElmE)CirElm;
            ce.Position++;
            if (ce.PosCount <= ce.Position) {
                ce.Position = 0;
            }
        }

        public virtual Rectangle GetSwitchRect() {
            interpLead(ref mP1, 0, OPEN_HS);
            var l1 = new Rectangle(mLead1.X, mLead1.Y, 0, 0);
            var l2 = new Rectangle(mLead2.X, mLead2.Y, 0, 0);
            var p = new Rectangle(mP1.X, mP1.Y, 0, 0);
            return Rectangle.Union(l1, Rectangle.Union(l2, p));
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(BODY_LEN);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (SwitchElmE)CirElm;
            int hs1 = (ce.Position == 1) ? 0 : 2;
            int hs2 = (ce.Position == 1) ? OPEN_HS : 2;
            setBbox(mPoint1, mPoint2, OPEN_HS);
            draw2Leads();
            if (ce.Position == 0) {
                doDots();
            }
            interpLead(ref mP1, 0, hs1);
            interpLead(ref mP2, 1, hs2);
            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.WhiteColor;
            g.DrawLine(mP1, mP2);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (SwitchElmE)CirElm;
            arr[0] = (ce.Momentary) ? "push switch (SPST)" : "switch (SPST)";
            if (ce.Position == 1) {
                arr[1] = "open";
                arr[2] = "Vd = " + Utils.VoltageAbsText(ce.CirVoltageDiff);
            } else {
                arr[1] = "closed";
                arr[2] = "V = " + Utils.VoltageText(ce.CirVolts[0]);
                arr[3] = "I = " + Utils.CurrentAbsText(ce.mCirCurrent);
            }
        }

        public override bool CirGetConnection(int n1, int n2) { return 0 == ((SwitchElmE)CirElm).Position; }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (SwitchElmE)CirElm;
            if (n == 0) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "モーメンタリ";
                ei.CheckBox.Checked = ce.Momentary;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (SwitchElmE)CirElm;
            if (n == 0) {
                ce.Momentary = ei.CheckBox.Checked;
            }
        }
    }
}
