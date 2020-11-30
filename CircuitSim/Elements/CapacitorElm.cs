﻿using System.Windows.Forms;
using System.Drawing;

namespace Circuit.Elements {
    class CapacitorElm : CircuitElm {
        public double capacitance { get; private set; }
        double compResistance;
        double voltdiff;
        double curSourceValue;

        Point[] plate1;
        Point[] plate2;

        /* used for PolarCapacitorElm */
        Point[] platePoints;

        public static readonly int FLAG_BACK_EULER = 2;

        public CapacitorElm(int xx, int yy) : base(xx, yy) {
            capacitance = 1e-5;
        }

        public CapacitorElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            capacitance = st.nextTokenDouble();
            voltdiff = st.nextTokenDouble();
        }

        public bool isTrapezoidal() { return (mFlags & FLAG_BACK_EULER) == 0; }

        public double getCapacitance() { return capacitance; }

        public void setCapacitance(double c) { capacitance = c; }

        public override void setNodeVoltage(int n, double c) {
            base.setNodeVoltage(n, c);
            voltdiff = Volts[0] - Volts[1];
        }

        public override void reset() {
            base.reset();
            mCurrent = mCurCount = curSourceValue = 0;
            /* put small charge on caps when reset to start oscillators */
            voltdiff = 1e-3;
        }

        public void shorted() {
            base.reset();
            voltdiff = mCurrent = mCurCount = curSourceValue = 0;
        }

        public override DUMP_ID getDumpType() { return DUMP_ID.CAPACITOR; }

        public override string dump() {
            return base.dump() + " " + capacitance + " " + voltdiff;
        }

        public override void setPoints() {
            base.setPoints();
            double f = (mElmLen / 2 - 4) / mElmLen;
            /* calc leads */
            mLead1 = interpPoint(mPoint1, mPoint2, f);
            mLead2 = interpPoint(mPoint1, mPoint2, 1 - f);
            /* calc plates */
            plate1 = newPointArray(2);
            plate2 = newPointArray(2);
            interpPoint(mPoint1, mPoint2, ref plate1[0], ref plate1[1], f, 10);
            interpPoint(mPoint1, mPoint2, ref plate2[0], ref plate2[1], 1 - f, 10);
        }

        public override void draw(Graphics g) {
            int hs = 8;
            setBbox(mPoint1, mPoint2, hs);

            /* draw first lead and plate */
            PenThickLine.Color = getVoltageColor(Volts[0]);
            drawThickLine(g, mPoint1, mLead1);
            drawThickLine(g, plate1[0], plate1[1]);
            /* draw second lead and plate */
            PenThickLine.Color = getVoltageColor(Volts[1]);
            drawThickLine(g, mPoint2, mLead2);
            drawThickLine(g, plate2[0], plate2[1]);

            if (platePoints == null) {
                drawThickLine(g, plate2[0], plate2[1]);
            } else {
                for (int i = 0; i != 7; i++) {
                    drawThickLine(g, platePoints[i], platePoints[i + 1]);
                }
            }

            updateDotCount();
            if (Sim.dragElm != this) {
                drawDots(g, mPoint1, mLead1, mCurCount);
                drawDots(g, mPoint2, mLead2, -mCurCount);
            }
            drawPosts(g);
            if (Sim.chkShowValuesCheckItem.Checked) {
                var s = getShortUnitText(capacitance, "F");
                drawValues(g, s, hs);
            }
        }

        public override void stamp() {
            if (Sim.dcAnalysisFlag) {
                /* when finding DC operating point, replace cap with a 100M resistor */
                Cir.StampResistor(Nodes[0], Nodes[1], 1e8);
                curSourceValue = 0;
                return;
            }

            /* capacitor companion model using trapezoidal approximation
             * (Norton equivalent) consists of a current source in
             * parallel with a resistor.  Trapezoidal is more accurate
             * than backward euler but can cause oscillatory behavior
             * if RC is small relative to the timestep. */
            if (isTrapezoidal()) {
                compResistance = Sim.timeStep / (2 * capacitance);
            } else {
                compResistance = Sim.timeStep / capacitance;
            }
            Cir.StampResistor(Nodes[0], Nodes[1], compResistance);
            Cir.StampRightSide(Nodes[0]);
            Cir.StampRightSide(Nodes[1]);
        }

        public override void startIteration() {
            if (isTrapezoidal()) {
                curSourceValue = -voltdiff / compResistance - mCurrent;
            } else {
                curSourceValue = -voltdiff / compResistance;
            }
        }

        public override void calculateCurrent() {
            double voltdiff = Volts[0] - Volts[1];
            if (Sim.dcAnalysisFlag) {
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

        public override void doStep() {
            if (Sim.dcAnalysisFlag) {
                return;
            }
            Cir.StampCurrentSource(Nodes[0], Nodes[1], curSourceValue);
        }

        public override void getInfo(string[] arr) {
            arr[0] = "capacitor";
            getBasicInfo(arr);
            arr[3] = "C = " + getUnitText(capacitance, "F");
            arr[4] = "P = " + getUnitText(getPower(), "W");
        }

        public override string getScopeText(int v) {
            base.getScopeText(v);
            return "capacitor, " + getUnitText(capacitance, "F");
        }

        public override EditInfo getEditInfo(int n) {
            if (n == 0) {
                return new EditInfo("Capacitance (F)", capacitance, 0, 0);
            }
            if (n == 1) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "Trapezoidal Approximation";
                ei.CheckBox.Checked = isTrapezoidal();
                return ei;
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (n == 0 && ei.Value > 0) {
                capacitance = ei.Value;
            }
            if (n == 1) {
                if (ei.CheckBox.Checked) {
                    mFlags &= ~FLAG_BACK_EULER;
                } else {
                    mFlags |= FLAG_BACK_EULER;
                }
            }
        }

        public override DUMP_ID getShortcut() { return DUMP_ID.CAPACITOR; }
    }
}
