using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Active {
    class AnalogSwitchElm : CircuitElm {
        const int FLAG_INVERT = 1;
        const int OPEN_HS = 16;
        const int BODY_LEN = 24;

        Point mPs;
        Point mPoint3;
        Point mLead3;

        public AnalogSwitchElm(Point pos) : base(pos) {
            CirElm = new AnalogSwitchElmE();
        }

        public AnalogSwitchElm(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
            CirElm = new AnalogSwitchElmE(st);
        }

        protected override string dump() {
            var ce = (AnalogSwitchElmE)CirElm;
            return ce.Ron + " " + ce.Roff;
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.ANALOG_SW; } }

        public override Point GetPost(int n) {
            return (0 == n) ? mPoint1 : (1 == n) ? mPoint2 : mPoint3;
        }

        // we have to just assume current will flow either way, even though that
        // might cause singular matrix errors
        public override bool GetConnection(int n1, int n2) {
            if (n1 == 2 || n2 == 2) {
                return false;
            }
            return true;
        }

        public override void Drag(Point pos) {
            pos = CirSim.Sim.SnapGrid(pos);
            if (Math.Abs(P1.X - pos.X) < Math.Abs(P1.Y - pos.Y)) {
                pos.X = P1.X;
            } else {
                pos.Y = P1.Y;
            }
            int q1 = Math.Abs(P1.X - pos.X) + Math.Abs(P1.Y - pos.Y);
            int q2 = (q1 / 2) % CirSim.GRID_SIZE;
            if (q2 != 0) {
                return;
            }
            P2.X = pos.X;
            P2.Y = pos.Y;
            SetPoints();
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(BODY_LEN);
            mPs = new Point();
            interpPoint(ref mPoint3, 0.5, -OPEN_HS);
            interpPoint(ref mLead3, 0.5, -OPEN_HS / 2);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (AnalogSwitchElmE)CirElm;
            int hs = ce.IsOpen ? OPEN_HS : 0;
            setBbox(mPoint1, mPoint2, OPEN_HS);

            draw2Leads();

            interpLead(ref mPs, 1, hs);
            g.LineColor = CustomGraphics.WhiteColor;
            g.DrawLine(mLead1, mPs);

            drawLead(mPoint3, mLead3);

            if (!ce.IsOpen) {
                doDots();
            }
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (AnalogSwitchElmE)CirElm;
            arr[0] = "analog switch";
            arr[1] = ce.IsOpen ? "open" : "closed";
            arr[2] = "Vd = " + Utils.VoltageAbsText(ce.CirVoltageDiff);
            arr[3] = "I = " + Utils.CurrentAbsText(ce.CirCurrent);
            arr[4] = "Vc = " + Utils.VoltageText(ce.CirVolts[2]);
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (AnalogSwitchElmE)CirElm;
            if (n == 0) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    Text = "ノーマリクローズ",
                    Checked = (mFlags & FLAG_INVERT) != 0
                };
                return ei;
            }
            if (n == 1) {
                return new ElementInfo("オン抵抗(Ω)", ce.Ron, 0, 0);
            }
            if (n == 2) {
                return new ElementInfo("オフ抵抗(Ω)", ce.Roff, 0, 0);
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (AnalogSwitchElmE)CirElm;
            if (n == 0) {
                mFlags = ei.CheckBox.Checked ? (mFlags | FLAG_INVERT) : (mFlags & ~FLAG_INVERT);
                ce.Invert = 0 != (mFlags & FLAG_INVERT);
            }
            if (n == 1 && 0 < ei.Value) {
                ce.Ron = ei.Value;
            }
            if (n == 2 && 0 < ei.Value) {
                ce.Roff = ei.Value;
            }
        }
    }
}
