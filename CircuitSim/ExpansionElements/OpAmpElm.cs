using System;
using System.Drawing;

namespace Circuit.Elements {
    class OpAmpElm : CircuitElm {
        protected const int FLAG_SWAP = 1;
        protected const int FLAG_SMALL = 2;
        protected const int FLAG_LOWGAIN = 4;
        protected const int FLAG_GAIN = 8;

        const int V_N = 0;
        const int V_P = 1;
        const int V_O = 2;

        int opsize;
        int opheight;
        int opwidth;
        int opaddtext;
        double maxOut;
        double minOut;
        double gain;
        double gbw;

        Point[] in1p;
        Point[] in2p;
        Point[] textp;
        Point[] triangle;

        double lastvd;

        public OpAmpElm(int xx, int yy) : base(xx, yy) {
            mNoDiagonal = true;
            maxOut = 15;
            minOut = -15;
            gbw = 1e6;
            mFlags = FLAG_GAIN; /* need to do this before setSize() */
            gain = 100000;
            setSize(1);
        }

        public OpAmpElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            /* GBW has no effect in this version of the simulator,
             * but we retain it to keep the file format the same */
            try {
                maxOut = st.nextTokenDouble();
                minOut = st.nextTokenDouble();
                gbw = st.nextTokenDouble();
                Volts[V_N] = st.nextTokenDouble();
                Volts[V_P] = st.nextTokenDouble();
                gain = st.nextTokenDouble();
            } catch {
                maxOut = 15;
                minOut = -15;
                gbw = 1e6;
            }
            mNoDiagonal = true;
            setSize((f & FLAG_SMALL) != 0 ? 1 : 2);
            setGain();
        }

        public override double VoltageDiff { get { return Volts[V_O] - Volts[V_P]; } }

        public override double Power { get { return Volts[V_O] * mCurrent; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override bool NonLinear { get { return true; } }

        public override int PostCount { get { return 3; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.OPAMP; } }

        protected override string dump() {
            mFlags |= FLAG_GAIN;
            return maxOut
                + " " + minOut
                + " " + gbw
                + " " + Volts[V_N]
                + " " + Volts[V_P]
                + " " + gain;
        }

        void setGain() {
            if ((mFlags & FLAG_GAIN) != 0) {
                return;
            }
            /* gain of 100000 breaks e-amp-dfdx.txt
             * gain was 1000, but it broke amp-schmitt.txt */
            gain = ((mFlags & FLAG_LOWGAIN) != 0) ? 1000 : 100000;
        }

        public override void Draw(CustomGraphics g) {
            setBbox(mPoint1, mPoint2, opheight * 2);

            g.DrawThickLine(getVoltageColor(Volts[V_N]), in1p[0], in1p[1]);
            g.DrawThickLine(getVoltageColor(Volts[V_P]), in2p[0], in2p[1]);
            g.DrawThickLine(getVoltageColor(Volts[V_O]), mLead2, mPoint2);

            g.ThickLineColor = NeedsHighlight ? SelectColor : GrayColor;
            g.DrawThickPolygon(triangle);

            drawCenteredLText(g, "-", textp[0].X, textp[0].Y - 2, true);
            drawCenteredLText(g, "+", textp[1].X, textp[1].Y, true);
            mCurCount = updateDotCount(mCurrent, mCurCount);
            drawDots(g, mPoint2, mLead2, mCurCount);
            drawPosts(g);
        }

        void setSize(int s) {
            opsize = s;
            opheight = 8 * s;
            opwidth = 16 * s;
            mFlags = (mFlags & ~FLAG_SMALL) | ((s == 1) ? FLAG_SMALL : 0);
        }

        public override void SetPoints() {
            base.SetPoints();
            if (mLen > 150 && this == Sim.dragElm) {
                setSize(2);
            }
            int ww = opwidth;
            if (ww > mLen / 2) {
                ww = (int)(mLen / 2);
            }
            calcLeads(ww * 2);
            int hs = opheight * mDsign;
            if ((mFlags & FLAG_SWAP) != 0) {
                hs = -hs;
            }
            in1p = new Point[2];
            in2p = new Point[2];
            textp = new Point[2];
            Utils.InterpPoint(mPoint1, mPoint2, ref in1p[0], ref in2p[0], 0, hs);
            Utils.InterpPoint(mLead1, mLead2, ref in1p[1], ref in2p[1], 0, hs);
            Utils.InterpPoint(mLead1, mLead2, ref textp[0], ref textp[1], 0.2, hs);
            var tris = new Point[2];
            Utils.InterpPoint(mLead1, mLead2, ref tris[0], ref tris[1], 0, hs * 2);
            triangle = new Point[] { tris[0], tris[1], mLead2 };
        }

        public override Point GetPost(int n) {
            return (n == 0) ? in1p[0] : (n == 1) ? in2p[0] : mPoint2;
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "op-amp";
            arr[1] = "V+ = " + Utils.VoltageText(Volts[V_P]);
            arr[2] = "V- = " + Utils.VoltageText(Volts[V_N]);
            /* sometimes the voltage goes slightly outside range,
             * to make convergence easier.  so we hide that here. */
            double vo = Math.Max(Math.Min(Volts[V_O], maxOut), minOut);
            arr[3] = "Vout = " + Utils.VoltageText(vo);
            arr[4] = "Iout = " + Utils.CurrentText(-mCurrent);
            arr[5] = "range = " + Utils.VoltageText(minOut)
                + " to " + Utils.VoltageText(maxOut);
        }

        public override void Stamp() {
            int vn = mCir.NodeList.Count + mVoltSource;
            mCir.StampNonLinear(vn);
            mCir.StampMatrix(Nodes[2], vn, 1);
        }

        public override void DoStep() {
            double vd = Volts[V_P] - Volts[V_N];
            if (Math.Abs(lastvd - vd) > .1) {
                mCir.Converged = false;
            } else if (Volts[V_O] > maxOut + .1 || Volts[V_O] < minOut - .1) {
                mCir.Converged = false;
            }
            double x = 0;
            int vn = mCir.NodeList.Count + mVoltSource;
            double dx = 0;
            if (vd >= maxOut / gain && (lastvd >= 0 || CirSim.random.Next(4) == 1)) {
                dx = 1e-4;
                x = maxOut - dx * maxOut / gain;
            } else if (vd <= minOut / gain && (lastvd <= 0 || CirSim.random.Next(4) == 1)) {
                dx = 1e-4;
                x = minOut - dx * minOut / gain;
            } else {
                dx = gain;
            }
            /*Console.WriteLine("opamp " + vd + " " + Volts[V_O] + " " + dx + " "  + x + " " + lastvd + " " + Cir.Converged);*/

            /* newton-raphson */
            mCir.StampMatrix(vn, Nodes[0], dx);
            mCir.StampMatrix(vn, Nodes[1], -dx);
            mCir.StampMatrix(vn, Nodes[2], 1);
            mCir.StampRightSide(vn, x);

            lastvd = vd;
        }

        /* there is no current path through the op-amp inputs,
         * but there is an indirect path through the output to ground. */
        public override bool GetConnection(int n1, int n2) { return false; }

        public override bool HasGroundConnection(int n1) { return n1 == 2; }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("Max Output (V)", maxOut, 1, 20);
            }
            if (n == 1) {
                return new ElementInfo("Min Output (V)", minOut, -20, 0);
            }
            if (n == 2) {
                return new ElementInfo("Gain", gain, 10, 1000000);
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                maxOut = ei.Value;
            }
            if (n == 1) {
                minOut = ei.Value;
            }
            if (n == 2 && ei.Value > 0) {
                gain = ei.Value;
            }
        }

        public override double GetCurrentIntoNode(int n) {
            if (n == 2) {
                return -mCurrent;
            }
            return 0;
        }
    }
}
