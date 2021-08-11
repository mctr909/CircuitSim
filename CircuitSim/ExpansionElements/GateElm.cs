﻿using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    abstract class GateElm : CircuitElm {
        const int FLAG_SMALL = 1;
        const int FLAG_SCHMITT = 2;
        protected int inputCount = 2;
        bool lastOutput;
        double highVoltage;
        public static double lastHighVoltage = 5;
        static bool lastSchmitt = false;

        const int gwidth = 7;
        const int gwidth2 = 14;
        const int gheight = 8;

        protected int hs2;

        Point[] inPosts;
        Point[] inGates;

        bool[] inputStates;

        int oscillationCount;

        protected int ww;

        protected PointF[] gatePolyEuro;
        protected PointF[] gatePolyAnsi;

        PointF[] schmittPoly;

        protected int circleSize;
        protected PointF circlePos;
        protected PointF[] linePoints;

        public GateElm(Point pos) : base(pos) {
            mNoDiagonal = true;
            inputCount = 2;

            /* copy defaults from last gate edited */
            highVoltage = lastHighVoltage;
            if (lastSchmitt) {
                mFlags |= FLAG_SCHMITT;
            }

            mFlags |= FLAG_SMALL;
        }

        public GateElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            inputCount = st.nextTokenInt();
            double lastOutputVoltage = st.nextTokenDouble();
            mNoDiagonal = true;
            try {
                highVoltage = st.nextTokenDouble();
            } catch {
                highVoltage = 5;
            }
            lastOutput = lastOutputVoltage > highVoltage * .5;

            mFlags |= FLAG_SMALL;
        }

        public override int VoltageSourceCount { get { return 1; } }

        public override int PostCount { get { return inputCount + 1; } }

        protected virtual bool isInverting() { return false; }

        protected override string dump() {
            return inputCount + " " + Volts[inputCount] + " " + highVoltage;
        }

        public override void SetPoints() {
            base.SetPoints();
            inputStates = new bool[inputCount];
            int hs = gheight;
            int i;
            ww = gwidth2;
            if (ww > mLen / 2) {
                ww = (int)(mLen / 2);
            }
            if (isInverting() && ww + 8 > mLen / 2) {
                ww = (int)(mLen / 2 - 8);
            }
            calcLeads(ww * 2);
            inPosts = new Point[inputCount];
            inGates = new Point[inputCount];
            allocNodes();
            int i0 = -inputCount / 2;
            for (i = 0; i != inputCount; i++, i0++) {
                if (i0 == 0 && (inputCount & 1) == 0) {
                    i0++;
                }
                Utils.InterpPoint(mPoint1, mPoint2, ref inPosts[i], 0, hs * i0);
                Utils.InterpPoint(mLead1, mLead2, ref inGates[i], 0, hs * i0);
                Volts[i] = (lastOutput ^ isInverting()) ? 5 : 0;
            }
            hs2 = gwidth * (inputCount / 2 + 1);
            setBbox(mPoint1, mPoint2, hs2);
            if (hasSchmittInputs()) {
                schmittPoly = Utils.CreateSchmitt(mLead1, mLead2, 1, .47f);
            }
        }

        protected void createEuroGatePolygon() {
            gatePolyEuro = new PointF[4];
            Utils.InterpPoint(mLead1, mLead2, ref gatePolyEuro[0], ref gatePolyEuro[1], 0, hs2);
            Utils.InterpPoint(mLead1, mLead2, ref gatePolyEuro[3], ref gatePolyEuro[2], 1, hs2);
        }

        protected virtual string getGateText() { return null; }

        public static bool useAnsiGates() { return ControlPanel.ChkUseAnsiSymbols.Checked; }

        public override void Draw(CustomGraphics g) {
            int i;
            for (i = 0; i != inputCount; i++) {
                g.DrawThickLine(getVoltageColor(Volts[i]), inPosts[i], inGates[i]);
            }
            g.DrawThickLine(getVoltageColor(Volts[inputCount]), mLead2, mPoint2);
            g.ThickLineColor = NeedsHighlight ? SelectColor : GrayColor;
            if (useAnsiGates()) {
                g.DrawThickPolygon(gatePolyAnsi);
            } else {
                g.DrawThickPolygon(gatePolyEuro);
                var center = new PointF();
                Utils.InterpPoint(mPoint1, mPoint2, ref center, .5);
                drawCenteredLText(g, getGateText(), center.X, center.Y - 6, true);
            }
            if (hasSchmittInputs()) {
                g.LineColor = WhiteColor;
                g.DrawPolygon(schmittPoly);
            }
            if (linePoints != null && useAnsiGates()) {
                for (i = 0; i != linePoints.Length - 1; i++) {
                    g.DrawThickLine(linePoints[i], linePoints[i + 1]);
                }
            }
            if (isInverting()) {
                g.DrawThickCircle(circlePos, circleSize);
            }
            mCurCount = updateDotCount(mCurrent, mCurCount);
            drawDots(g, mLead2, mPoint2, mCurCount);
            drawPosts(g);
        }

        public override Point GetPost(int n) {
            if (n == inputCount) {
                return mPoint2;
            }
            return inPosts[n];
        }

        protected abstract string getGateName();

        public override void GetInfo(string[] arr) {
            arr[0] = getGateName();
            arr[1] = "Vout = " + Utils.VoltageText(Volts[inputCount]);
            arr[2] = "Iout = " + Utils.CurrentText(mCurrent);
        }

        public override void Stamp() {
            mCir.StampVoltageSource(0, Nodes[inputCount], mVoltSource);
        }

        bool hasSchmittInputs() { return (mFlags & FLAG_SCHMITT) != 0; }

        protected bool getInput(int x) {
            if (!hasSchmittInputs()) {
                return Volts[x] > highVoltage * .5;
            }
            bool res = Volts[x] > highVoltage * (inputStates[x] ? .35 : .55);
            inputStates[x] = res;
            return res;
        }

        protected abstract bool calcFunction();

        public override void DoStep() {
            bool f = calcFunction();
            if (isInverting()) {
                f = !f;
            }

            /* detect oscillation (using same strategy as Atanua) */
            if (lastOutput == !f) {
                if (oscillationCount++ > 50) {
                    /* output is oscillating too much, randomly leave output the same */
                    oscillationCount = 0;
                    if (CirSim.Random.Next(10) > 5) {
                        f = lastOutput;
                    }
                }
            } else {
                oscillationCount = 0;
            }
            lastOutput = f;
            double res = f ? highVoltage : 0;
            mCir.UpdateVoltageSource(0, Nodes[inputCount], mVoltSource, res);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("# of Inputs", inputCount, 1, 8).SetDimensionless();
            }
            if (n == 1) {
                return new ElementInfo("High Voltage (V)", highVoltage, 1, 10);
            }
            if (n == 2) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    Text = "Schmitt Inputs",
                    Checked = hasSchmittInputs()
                };
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0 && ei.Value >= 1) {
                inputCount = (int)ei.Value;
                SetPoints();
            }
            if (n == 1) {
                highVoltage = lastHighVoltage = ei.Value;
            }
            if (n == 2) {
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_SCHMITT;
                } else {
                    mFlags &= ~FLAG_SCHMITT;
                }
                lastSchmitt = hasSchmittInputs();
                SetPoints();
            }
        }

        /* there is no current path through the gate inputs,
         * but there is an indirect path through the output to ground. */
        public override bool GetConnection(int n1, int n2) { return false; }

        public override bool HasGroundConnection(int n1) {
            return (n1 == inputCount);
        }

        public override double GetCurrentIntoNode(int n) {
            if (n == inputCount) {
                return mCurrent;
            }
            return 0;
        }
    }
}
