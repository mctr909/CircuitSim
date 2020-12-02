using System;
using System.Drawing;

namespace Circuit.Elements {
    class OpAmpElm : CircuitElm {
        protected const int FLAG_SWAP = 1;
        protected const int FLAG_SMALL = 2;
        protected const int FLAG_LOWGAIN = 4;
        protected const int FLAG_GAIN = 8;

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
        Font plusFont;

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
                Volts[0] = st.nextTokenDouble();
                Volts[1] = st.nextTokenDouble();
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

        protected override string dump() {
            mFlags |= FLAG_GAIN;
            return maxOut
                + " " + minOut
                + " " + gbw
                + " " + Volts[0]
                + " " + Volts[1]
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

        public override bool nonLinear() { return true; }

        public override void draw(Graphics g) {
            setBbox(mPoint1, mPoint2, opheight * 2);

            drawThickLine(g, getVoltageColor(Volts[0]), in1p[0], in1p[1]);
            drawThickLine(g, getVoltageColor(Volts[1]), in2p[0], in2p[1]);
            drawThickLine(g, getVoltageColor(Volts[2]), mLead2, mPoint2);

            PenThickLine.Color = needsHighlight() ? SelectColor : LightGrayColor;
            drawThickPolygon(g, triangle);

            drawCenteredText(g, plusFont, "-", textp[0].X, textp[0].Y - 2, true);
            drawCenteredText(g, plusFont, "+", textp[1].X, textp[1].Y, true);
            mCurCount = updateDotCount(mCurrent, mCurCount);
            drawDots(g, mPoint2, mLead2, mCurCount);
            drawPosts(g);
        }

        public override double getPower() { return Volts[2] * mCurrent; }

        void setSize(int s) {
            opsize = s;
            opheight = 8 * s;
            opwidth = 13 * s;
            mFlags = (mFlags & ~FLAG_SMALL) | ((s == 1) ? FLAG_SMALL : 0);
        }

        public override void setPoints() {
            base.setPoints();
            if (mElmLen > 150 && this == Sim.dragElm) {
                setSize(2);
            }
            int ww = opwidth;
            if (ww > mElmLen / 2) {
                ww = (int)(mElmLen / 2);
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
            triangle = createPolygon(tris[0], tris[1], mLead2).ToArray();
            plusFont = new Font("Meiryo UI", opsize == 2 ? 14 : 10);
        }

        public override int getPostCount() { return 3; }

        public override Point getPost(int n) {
            return (n == 0) ? in1p[0] : (n == 1) ? in2p[0] : mPoint2;
        }

        public override int getVoltageSourceCount() { return 1; }

        public override void getInfo(string[] arr) {
            arr[0] = "op-amp";
            arr[1] = "V+ = " + getVoltageText(Volts[1]);
            arr[2] = "V- = " + getVoltageText(Volts[0]);
            /* sometimes the voltage goes slightly outside range,
             * to make convergence easier.  so we hide that here. */
            double vo = Math.Max(Math.Min(Volts[2], maxOut), minOut);
            arr[3] = "Vout = " + getVoltageText(vo);
            arr[4] = "Iout = " + getCurrentText(-mCurrent);
            arr[5] = "range = " + getVoltageText(minOut) + " to " +
            getVoltageText(maxOut);
        }

        public override void stamp() {
            int vn = Cir.NodeList.Count + mVoltSource;
            Cir.StampNonLinear(vn);
            Cir.StampMatrix(Nodes[2], vn, 1);
        }

        public override void doStep() {
            double vd = Volts[1] - Volts[0];
            if (Math.Abs(lastvd - vd) > .1) {
                Cir.Converged = false;
            } else if (Volts[2] > maxOut + .1 || Volts[2] < minOut - .1) {
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
            /*Console.WriteLine("opamp " + vd + " " + Volts[2] + " " + dx + " "  + x + " " + lastvd + " " + Cir.Converged);*/

            /* newton-raphson */
            Cir.StampMatrix(vn, Nodes[0], dx);
            Cir.StampMatrix(vn, Nodes[1], -dx);
            Cir.StampMatrix(vn, Nodes[2], 1);
            Cir.StampRightSide(vn, x);

            lastvd = vd;
        }

        /* there is no current path through the op-amp inputs,
         * but there is an indirect path through the output to ground. */
        public override bool getConnection(int n1, int n2) { return false; }

        public override bool hasGroundConnection(int n1) { return n1 == 2; }

        public override double getVoltageDiff() { return Volts[2] - Volts[1]; }

        public override EditInfo getEditInfo(int n) {
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

        public override void setEditValue(int n, EditInfo ei) {
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

        public override DUMP_ID getShortcut() { return DUMP_ID.OPAMP; }

        public override double getCurrentIntoNode(int n) {
            if (n == 2) {
                return -mCurrent;
            }
            return 0;
        }
    }
}
