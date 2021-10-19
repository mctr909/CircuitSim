using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Active {
    class AnalogSwitchElm : CircuitElm {
        const int FLAG_INVERT = 1;
        const int OPEN_HS = 16;
        const int BODY_LEN = 24;

        double mResistance;
        double mR_on;
        double mR_off;
        bool mIsOpen;
        Point mPs;
        Point mPoint3;
        Point mLead3;

        public AnalogSwitchElm(Point pos) : base(pos) {
            mR_on = 20;
            mR_off = 1e10;
        }

        public AnalogSwitchElm(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
            mR_on = 20;
            mR_off = 1e10;
            try {
                mR_on = double.Parse(st.nextToken());
                mR_off = double.Parse(st.nextToken());
            } catch { }
        }

        protected override string dump() {
            return mR_on + " " + mR_off;
        }

        protected override void calculateCurrent() {
            mCurrent = (Volts[0] - Volts[1]) / mResistance;
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.ANALOG_SW; } }

        // we need this to be able to change the matrix for each step
        public override bool NonLinear { get { return true; } }

        public override int PostCount { get { return 3; } }

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

        public override double GetCurrentIntoNode(int n) {
            if (n == 0) {
                return -mCurrent;
            }
            if (n == 2) {
                return 0;
            }
            return mCurrent;
        }

        public override void Stamp() {
            mCir.StampNonLinear(Nodes[0]);
            mCir.StampNonLinear(Nodes[1]);
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
            int hs = mIsOpen ? OPEN_HS : 0;
            setBbox(mPoint1, mPoint2, OPEN_HS);

            draw2Leads();

            interpLead(ref mPs, 1, hs);
            g.LineColor = CustomGraphics.WhiteColor;
            g.DrawLine(mLead1, mPs);

            drawVoltage(2, mPoint3, mLead3);

            if (!mIsOpen) {
                doDots();
            }
            drawPosts();
        }

        public override void DoStep() {
            mIsOpen = Volts[2] < 2.5;
            if ((mFlags & FLAG_INVERT) != 0) {
                mIsOpen = !mIsOpen;
            }
            mResistance = mIsOpen ? mR_off : mR_on;
            mCir.StampResistor(Nodes[0], Nodes[1], mResistance);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "analog switch";
            arr[1] = mIsOpen ? "open" : "closed";
            arr[2] = "Vd = " + Utils.VoltageDText(VoltageDiff);
            arr[3] = "I = " + Utils.CurrentDText(Current);
            arr[4] = "Vc = " + Utils.VoltageText(Volts[2]);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    Text = "ノーマリクローズ",
                    Checked = (mFlags & FLAG_INVERT) != 0
                };
                return ei;
            }
            if (n == 1) {
                return new ElementInfo("オン抵抗(Ω)", mR_on, 0, 0);
            }
            if (n == 2) {
                return new ElementInfo("オフ抵抗(Ω)", mR_off, 0, 0);
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                mFlags = ei.CheckBox.Checked ? (mFlags | FLAG_INVERT) : (mFlags & ~FLAG_INVERT);
            }
            if (n == 1 && 0 < ei.Value) {
                mR_on = ei.Value;
            }
            if (n == 2 && 0 < ei.Value) {
                mR_off = ei.Value;
            }
        }
    }
}
