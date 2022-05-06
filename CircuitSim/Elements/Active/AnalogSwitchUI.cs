using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Active {
    class AnalogSwitchUI : BaseUI {
        const int FLAG_INVERT = 1;
        const int OPEN_HS = 16;
        const int BODY_LEN = 24;

        Point mPs;
        Point mPost3;
        Point mLead3;

        public AnalogSwitchUI(Point pos) : base(pos) {
            Elm = new AnalogSwitchElm();
        }

        public AnalogSwitchUI(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
            Elm = new AnalogSwitchElm(st);
        }

        protected override string dump() {
            var ce = (AnalogSwitchElm)Elm;
            return ce.Ron + " " + ce.Roff;
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.ANALOG_SW; } }

        public override Point GetPost(int n) {
            return (0 == n) ? mPost1 : (1 == n) ? mPost2 : mPost3;
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
            interpPoint(ref mPost3, 0.5, -OPEN_HS);
            interpPoint(ref mLead3, 0.5, -OPEN_HS / 2);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (AnalogSwitchElm)Elm;
            int hs = ce.IsOpen ? OPEN_HS : 0;
            setBbox(mPost1, mPost2, OPEN_HS);

            draw2Leads();

            interpLead(ref mPs, 1, hs);
            g.LineColor = CustomGraphics.WhiteColor;
            g.DrawLine(mLead1, mPs);

            drawLead(mPost3, mLead3);

            if (!ce.IsOpen) {
                doDots();
            }
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (AnalogSwitchElm)Elm;
            arr[0] = "analog switch";
            arr[1] = ce.IsOpen ? "open" : "closed";
            arr[2] = "Vd = " + Utils.VoltageAbsText(ce.VoltageDiff);
            arr[3] = "I = " + Utils.CurrentAbsText(ce.Current);
            arr[4] = "Vc = " + Utils.VoltageText(ce.Volts[2]);
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (AnalogSwitchElm)Elm;
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
            var ce = (AnalogSwitchElm)Elm;
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
