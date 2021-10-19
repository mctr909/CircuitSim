﻿using System.Windows.Forms;
using System.Drawing;

namespace Circuit.Elements.Passive {
    class CapacitorElm : CircuitElm {
        public static readonly int FLAG_BACK_EULER = 2;

        const int BODY_LEN = 6;
        const int HS = 8;

        double mCompResistance;
        double mVoltDiff;
        double mCurSourceValue;

        Point[] mPlate1;
        Point[] mPlate2;
        Point mTextPos;

        public CapacitorElm(Point pos) : base(pos) {
            Capacitance = 1e-5;
        }

        public CapacitorElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Capacitance = st.nextTokenDouble();
            mVoltDiff = st.nextTokenDouble();
        }

        public double Capacitance { get; set; }

        public bool IsTrapezoidal { get { return (mFlags & FLAG_BACK_EULER) == 0; } }

        public override DUMP_ID Shortcut { get { return DUMP_ID.CAPACITOR; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.CAPACITOR; } }

        protected override string dump() {
            return Capacitance + " " + mVoltDiff;
        }

        protected override void calculateCurrent() {
            double voltdiff = Volts[0] - Volts[1];
            if (CirSim.Sim.DcAnalysisFlag) {
                mCurrent = voltdiff / 1e8;
                return;
            }
            /* we check compResistance because this might get called
             * before stamp(), which sets compResistance, causing
             * infinite current */
            if (0 < mCompResistance) {
                mCurrent = voltdiff / mCompResistance + mCurSourceValue;
            }
        }

        public void Shorted() {
            base.Reset();
            mVoltDiff = mCurrent = mCurCount = mCurSourceValue = 0;
        }

        public override void SetNodeVoltage(int n, double c) {
            base.SetNodeVoltage(n, c);
            mVoltDiff = Volts[0] - Volts[1];
        }

        public override void Reset() {
            base.Reset();
            mCurrent = mCurCount = mCurSourceValue = 0;
            /* put small charge on caps when reset to start oscillators */
            mVoltDiff = 1e-3;
        }

        public override void SetPoints() {
            base.SetPoints();
            double f = (mLen - BODY_LEN) * 0.5 / mLen;
            /* calc leads */
            setLead1(f);
            setLead2(1 - f);
            /* calc plates */
            mPlate1 = new Point[2];
            mPlate2 = new Point[2];
            interpPointAB(ref mPlate1[0], ref mPlate1[1], f, 8);
            interpPointAB(ref mPlate2[0], ref mPlate2[1], 1 - f, 8);
            setTextPos();
        }

        void setTextPos() {
            if (mPoint1.Y == mPoint2.Y) {
                var wh = Context.GetTextSize(Utils.ShortUnitText(Capacitance, "")).Width * 0.5;
                interpPoint(ref mTextPos, 0.5 + wh / mLen * mDsign, 15 * mDsign);
            } else if (mPoint1.X == mPoint2.X) {
                interpPoint(ref mTextPos, 0.5, -8 * mDsign);
            } else {
                interpPoint(ref mTextPos, 0.5, -10 * mDsign);
            }
        }

        public override void Draw() {
            setBbox(mPoint1, mPoint2, HS);

            /* draw first lead and plate */
            drawVoltage(0, mPoint1, mLead1);
            Context.DrawThickLine(mPlate1[0], mPlate1[1]);
            /* draw second lead and plate */
            drawVoltage(1, mPoint2, mLead2);
            Context.DrawThickLine(mPlate2[0], mPlate2[1]);

            updateDotCount();
            if (CirSim.Sim.DragElm != this) {
                drawDots(mPoint1, mLead1, mCurCount);
                drawDots(mPoint2, mLead2, -mCurCount);
            }
            drawPosts();
            if (ControlPanel.ChkShowValues.Checked) {
                var s = Utils.ShortUnitText(Capacitance, "");
                Context.DrawRightText(s, mTextPos.X, mTextPos.Y);
            }
        }

        public override void Stamp() {
            if (CirSim.Sim.DcAnalysisFlag) {
                /* when finding DC operating point, replace cap with a 100M resistor */
                mCir.StampResistor(Nodes[0], Nodes[1], 1e8);
                mCurSourceValue = 0;
                return;
            }

            /* capacitor companion model using trapezoidal approximation
             * (Norton equivalent) consists of a current source in
             * parallel with a resistor.  Trapezoidal is more accurate
             * than backward euler but can cause oscillatory behavior
             * if RC is small relative to the timestep. */
            if (IsTrapezoidal) {
                mCompResistance = ControlPanel.TimeStep / (2 * Capacitance);
            } else {
                mCompResistance = ControlPanel.TimeStep / Capacitance;
            }
            mCir.StampResistor(Nodes[0], Nodes[1], mCompResistance);
            mCir.StampRightSide(Nodes[0]);
            mCir.StampRightSide(Nodes[1]);
        }

        public override void StartIteration() {
            if (IsTrapezoidal) {
                mCurSourceValue = -mVoltDiff / mCompResistance - mCurrent;
            } else {
                mCurSourceValue = -mVoltDiff / mCompResistance;
            }
        }

        public override void DoStep() {
            if (CirSim.Sim.DcAnalysisFlag) {
                return;
            }
            mCir.StampCurrentSource(Nodes[0], Nodes[1], mCurSourceValue);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "capacitor";
            getBasicInfo(arr);
            arr[3] = "C = " + Utils.UnitText(Capacitance, "F");
            arr[4] = "P = " + Utils.UnitText(Power, "W");
        }

        public override string GetScopeText(Scope.VAL v) {
            base.GetScopeText(v);
            return "capacitor, " + Utils.UnitText(Capacitance, "F");
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("静電容量(F)", Capacitance, 0, 0);
            }
            if (n == 1) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "台形近似";
                ei.CheckBox.Checked = IsTrapezoidal;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0 && ei.Value > 0) {
                Capacitance = ei.Value;
                setTextPos();
            }
            if (n == 1) {
                if (ei.CheckBox.Checked) {
                    mFlags &= ~FLAG_BACK_EULER;
                } else {
                    mFlags |= FLAG_BACK_EULER;
                }
            }
        }
    }
}
