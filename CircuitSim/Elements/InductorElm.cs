﻿using System.Windows.Forms;
using System.Drawing;

namespace Circuit.Elements {
    class InductorElm : CircuitElm {
        Inductor ind;
        Point textPos;

        public double Inductance { get; set; }

        public InductorElm(int xx, int yy) : base(xx, yy) {
            ind = new Inductor(Sim, mCir);
            Inductance = 0.001;
            ind.setup(Inductance, mCurrent, mFlags);
        }

        public InductorElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            ind = new Inductor(Sim, mCir);
            Inductance = st.nextTokenDouble();
            mCurrent = st.nextTokenDouble();
            ind.setup(Inductance, mCurrent, mFlags);
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.INDUCTOR; } }

        public override bool NonLinear { get { return ind.nonLinear(); } }

        protected override string dump() {
            return Inductance + " " + mCurrent;
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.INDUCTOR; }

        public void setInductance(double l) {
            Inductance = l;
            ind.setup(Inductance, mCurrent, mFlags);
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(40);
            if (mPoint1.Y == mPoint2.Y) {
                textPos = Utils.InterpPoint(mPoint1, mPoint2, 0.5 + 12 * mDsign / mLen, 12 * mDsign);
            } else {
                textPos = Utils.InterpPoint(mPoint1, mPoint2, 0.5, -5 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            double v1 = Volts[0];
            double v2 = Volts[1];
            int hs = 8;
            setBbox(mPoint1, mPoint2, hs);

            draw2Leads(g);
            drawCoil(g, mLead1, mLead2, v1, v2);

            if (Sim.chkShowValuesCheckItem.Checked) {
                var s = Utils.ShortUnitText(Inductance, "");
                g.DrawRightText(s, textPos.X, textPos.Y);
            }
            doDots(g);
            drawPosts(g);
        }

        public override void Reset() {
            mCurrent = Volts[0] = Volts[1] = mCurCount = 0;
            ind.reset();
        }

        public override void Stamp() { ind.stamp(Nodes[0], Nodes[1]); }

        public override void StartIteration() {
            ind.startIteration(Volts[0] - Volts[1]);
        }

        protected override void calculateCurrent() {
            double voltdiff = Volts[0] - Volts[1];
            mCurrent = ind.calculateCurrent(voltdiff);
        }

        public override void DoStep() {
            double voltdiff = Volts[0] - Volts[1];
            ind.doStep(voltdiff);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "inductor";
            getBasicInfo(arr);
            arr[3] = "L = " + Utils.UnitText(Inductance, "H");
            arr[4] = "P = " + Utils.UnitText(Power, "W");
        }

        public override EditInfo GetEditInfo(int n) {
            if (n == 0) {
                return new EditInfo("Inductance (H)", Inductance, 0, 0);
            }
            if (n == 1) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "Trapezoidal Approximation";
                ei.CheckBox.Checked = ind.IsTrapezoidal;
                return ei;
            }
            return null;
        }

        public override void SetEditValue(int n, EditInfo ei) {
            if (n == 0 && ei.Value > 0) {
                Inductance = ei.Value;
            }
            if (n == 1) {
                if (ei.CheckBox.Checked) {
                    mFlags &= ~Inductor.FLAG_BACK_EULER;
                } else {
                    mFlags |= Inductor.FLAG_BACK_EULER;
                }
            }
            ind.setup(Inductance, mCurrent, mFlags);
        }
    }
}
