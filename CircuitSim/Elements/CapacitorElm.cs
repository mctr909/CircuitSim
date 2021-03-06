﻿using System.Windows.Forms;
using System.Drawing;

namespace Circuit.Elements {
    class CapacitorElm : CircuitElm {
        public static readonly int FLAG_BACK_EULER = 2;

        double compResistance;
        double voltdiff;
        double curSourceValue;

        Point[] plate1;
        Point[] plate2;
        Point textPos;

        public double Capacitance { get; set; }

        public bool IsTrapezoidal { get { return (mFlags & FLAG_BACK_EULER) == 0; } }

        public CapacitorElm(int xx, int yy) : base(xx, yy) {
            Capacitance = 1e-5;
        }

        public CapacitorElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            Capacitance = st.nextTokenDouble();
            voltdiff = st.nextTokenDouble();
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.CAPACITOR; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.CAPACITOR; } }

        protected override string dump() {
            return Capacitance + " " + voltdiff;
        }

        public void setCapacitance(double c) { Capacitance = c; }

        public override void SetNodeVoltage(int n, double c) {
            base.SetNodeVoltage(n, c);
            voltdiff = Volts[0] - Volts[1];
        }

        public override void Reset() {
            base.Reset();
            mCurrent = mCurCount = curSourceValue = 0;
            /* put small charge on caps when reset to start oscillators */
            voltdiff = 1e-3;
        }

        public void shorted() {
            base.Reset();
            voltdiff = mCurrent = mCurCount = curSourceValue = 0;
        }

        public override void SetPoints() {
            base.SetPoints();
            double f = (mLen / 2 - 3) / mLen;
            /* calc leads */
            mLead1 = Utils.InterpPoint(mPoint1, mPoint2, f);
            mLead2 = Utils.InterpPoint(mPoint1, mPoint2, 1 - f);
            /* calc plates */
            plate1 = new Point[2];
            plate2 = new Point[2];
            Utils.InterpPoint(mPoint1, mPoint2, ref plate1[0], ref plate1[1], f, 8);
            Utils.InterpPoint(mPoint1, mPoint2, ref plate2[0], ref plate2[1], 1 - f, 8);
            if (mPoint1.Y == mPoint2.Y) {
                textPos = Utils.InterpPoint(mPoint1, mPoint2, 0.5 + 12 * mDsign / mLen, 16 * mDsign);
            } else if (mPoint1.X == mPoint2.X) {
                textPos = Utils.InterpPoint(mPoint1, mPoint2, 0.5, -8 * mDsign);
            } else {
                textPos = Utils.InterpPoint(mPoint1, mPoint2, 0.5, -10 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            int hs = 8;
            setBbox(mPoint1, mPoint2, hs);

            /* draw first lead and plate */
            g.ThickLineColor = getVoltageColor(Volts[0]);
            g.DrawThickLine(mPoint1, mLead1);
            g.DrawThickLine(plate1[0], plate1[1]);
            /* draw second lead and plate */
            g.ThickLineColor = getVoltageColor(Volts[1]);
            g.DrawThickLine(mPoint2, mLead2);
            g.DrawThickLine(plate2[0], plate2[1]);

            updateDotCount();
            if (Sim.DragElm != this) {
                drawDots(g, mPoint1, mLead1, mCurCount);
                drawDots(g, mPoint2, mLead2, -mCurCount);
            }
            drawPosts(g);
            if (ControlPanel.ChkShowValues.Checked) {
                var s = Utils.ShortUnitText(Capacitance, "");
                g.DrawRightText(s, textPos.X, textPos.Y);
            }
        }

        public override void Stamp() {
            if (Sim.DcAnalysisFlag) {
                /* when finding DC operating point, replace cap with a 100M resistor */
                mCir.StampResistor(Nodes[0], Nodes[1], 1e8);
                curSourceValue = 0;
                return;
            }

            /* capacitor companion model using trapezoidal approximation
             * (Norton equivalent) consists of a current source in
             * parallel with a resistor.  Trapezoidal is more accurate
             * than backward euler but can cause oscillatory behavior
             * if RC is small relative to the timestep. */
            if (IsTrapezoidal) {
                compResistance = ControlPanel.TimeStep / (2 * Capacitance);
            } else {
                compResistance = ControlPanel.TimeStep / Capacitance;
            }
            mCir.StampResistor(Nodes[0], Nodes[1], compResistance);
            mCir.StampRightSide(Nodes[0]);
            mCir.StampRightSide(Nodes[1]);
        }

        public override void StartIteration() {
            if (IsTrapezoidal) {
                curSourceValue = -voltdiff / compResistance - mCurrent;
            } else {
                curSourceValue = -voltdiff / compResistance;
            }
        }

        protected override void calculateCurrent() {
            double voltdiff = Volts[0] - Volts[1];
            if (Sim.DcAnalysisFlag) {
                mCurrent = voltdiff / 1e8;
                return;
            }
            /* we check compResistance because this might get called
             * before stamp(), which sets compResistance, causing
             * infinite current */
            if (compResistance > 0) {
                mCurrent = voltdiff / compResistance + curSourceValue;
            }
        }

        public override void DoStep() {
            if (Sim.DcAnalysisFlag) {
                return;
            }
            mCir.StampCurrentSource(Nodes[0], Nodes[1], curSourceValue);
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
                return new ElementInfo("Capacitance (F)", Capacitance, 0, 0);
            }
            if (n == 1) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "Trapezoidal Approximation";
                ei.CheckBox.Checked = IsTrapezoidal;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0 && ei.Value > 0) {
                Capacitance = ei.Value;
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
