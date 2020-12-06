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
            setSize(Sim.chkSmallGridCheckItem.Checked ? 1 : 2);
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

        protected override string dump() {
            mFlags |= FLAG_GAIN;
            return maxOut
                + " " + minOut
                + " " + gbw
                + " " + Volts[V_N]
                + " " + Volts[V_P]
                + " " + gain;
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.OPAMP; }

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

            g.ThickLineColor = NeedsHighlight ? SelectColor : LightGrayColor;
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
            opwidth = 13 * s;
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
            in1p = newPointArray(2);
            in2p = newPointArray(2);
            textp = newPointArray(2);
            interpPoint(mPoint1, mPoint2, ref in1p[0], ref in2p[0], 0, hs);
            interpPoint(mLead1, mLead2, ref in1p[1], ref in2p[1], 0, hs);
            interpPoint(mLead1, mLead2, ref textp[0], ref textp[1], 0.2, hs);
            var tris = new Point[2];
            interpPoint(mLead1, mLead2, ref tris[0], ref tris[1], 0, hs * 2);
            triangle = new Point[] { tris[0], tris[1], mLead2 };
        }

        public override Point GetPost(int n) {
            return (n == 0) ? in1p[0] : (n == 1) ? in2p[0] : mPoint2;
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "op-amp";
            arr[1] = "V+ = " + getVoltageText(Volts[V_P]);
            arr[2] = "V- = " + getVoltageText(Volts[V_N]);
            /* sometimes the voltage goes slightly outside range,
             * to make convergence easier.  so we hide that here. */
            double vo = Math.Max(Math.Min(Volts[V_O], maxOut), minOut);
            arr[3] = "Vout = " + getVoltageText(vo);
            arr[4] = "Iout = " + getCurrentText(-mCurrent);
            arr[5] = "range = " + getVoltageText(minOut) + " to " +
            getVoltageText(maxOut);
        }

        public override void Stamp() {
            int vn = Cir.NodeList.Count + mVoltSource;
            Cir.StampNonLinear(vn);
            Cir.StampMatrix(Nodes[2], vn, 1);
        }

        public override void DoStep() {
            double vd = Volts[V_P] - Volts[V_N];
            if (Math.Abs(lastvd - vd) > .1) {
                Cir.Converged = false;
            } else if (Volts[V_O] > maxOut + .1 || Volts[V_O] < minOut - .1) {
                Cir.Converged = false;
            }
            double x = 0;
            int vn = Cir.NodeList.Count + mVoltSource;
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
            Cir.StampMatrix(vn, Nodes[0], dx);
            Cir.StampMatrix(vn, Nodes[1], -dx);
            Cir.StampMatrix(vn, Nodes[2], 1);
            Cir.StampRightSide(vn, x);

            lastvd = vd;
        }

        /* there is no current path through the op-amp inputs,
         * but there is an indirect path through the output to ground. */
        public override bool GetConnection(int n1, int n2) { return false; }

        public override bool HasGroundConnection(int n1) { return n1 == 2; }

        public override EditInfo GetEditInfo(int n) {
            if (n == 0) {
                return new EditInfo("Max Output (V)", maxOut, 1, 20);
            }
            if (n == 1) {
                return new EditInfo("Min Output (V)", minOut, -20, 0);
            }
            if (n == 2) {
                return new EditInfo("Gain", gain, 10, 1000000);
            }
            return null;
        }

        public override void SetEditValue(int n, EditInfo ei) {
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
