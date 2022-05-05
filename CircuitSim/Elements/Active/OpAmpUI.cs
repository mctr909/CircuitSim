using System;
using System.Drawing;

namespace Circuit.Elements.Active {
    class OpAmpUI : BaseUI {
        protected const int FLAG_SWAP = 1;
        protected const int FLAG_SMALL = 2;
        protected const int FLAG_LOWGAIN = 4;
        protected const int FLAG_GAIN = 8;

        const int mOpHeight = 8;
        const int mOpWidth = 16;

        Point[] mIn1p;
        Point[] mIn2p;
        Point[] mTextp;
        Point[] mTriangle;

        public OpAmpUI(Point pos) : base(pos) {
            mNoDiagonal = true;
            mFlags = FLAG_GAIN; /* need to do this before setSize() */
            mFlags |= FLAG_SMALL;
            CirElm = new OpAmpElm();
        }

        public OpAmpUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            CirElm = new OpAmpElm(st);
            mNoDiagonal = true;
            mFlags |= FLAG_SMALL;
            setGain();
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.OPAMP; } }

        protected override string dump() {
            var ce = (OpAmpElm)CirElm;
            mFlags |= FLAG_GAIN;
            return ce.MaxOut
                + " " + ce.MinOut
                + " " + ce.Gbw
                + " " + ce.Volts[OpAmpElm.V_N]
                + " " + ce.Volts[OpAmpElm.V_P]
                + " " + ce.Gain;
        }

        public override void Draw(CustomGraphics g) {
            setBbox(mPoint1, mPoint2, mOpHeight * 2);

            drawLead(mIn1p[0], mIn1p[1]);
            drawLead(mIn2p[0], mIn2p[1]);
            drawLead(mLead2, mPoint2);

            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            g.DrawPolygon(mTriangle);

            drawCenteredLText("-", mTextp[0], true);
            drawCenteredLText("+", mTextp[1], true);

            var ce = (OpAmpElm)CirElm;
            ce.CurCount = updateDotCount(ce.Current, ce.CurCount);
            drawDots(mPoint2, mLead2, ce.CurCount);
            drawPosts();
        }

        public override void SetPoints() {
            base.SetPoints();
            int ww = mOpWidth;
            if (ww > mLen / 2) {
                ww = (int)(mLen / 2);
            }
            calcLeads(ww * 2);
            int hs = mOpHeight * mDsign;
            if ((mFlags & FLAG_SWAP) != 0) {
                hs = -hs;
            }
            mIn1p = new Point[2];
            mIn2p = new Point[2];
            mTextp = new Point[2];
            interpPointAB(ref mIn1p[0], ref mIn2p[0], 0, hs);
            interpLeadAB(ref mIn1p[1], ref mIn2p[1], 0, hs);
            interpLeadAB(ref mTextp[0], ref mTextp[1], 0.2, hs);
            var tris = new Point[2];
            interpLeadAB(ref tris[0], ref tris[1], 0, hs * 2);
            mTriangle = new Point[] { tris[0], tris[1], mLead2 };
        }

        public override Point GetPost(int n) {
            return (n == 0) ? mIn1p[0] : (n == 1) ? mIn2p[0] : mPoint2;
        }

        /* there is no current path through the op-amp inputs,
         * but there is an indirect path through the output to ground. */
        public override bool GetConnection(int n1, int n2) { return false; }

        public override void GetInfo(string[] arr) {
            var ce = (OpAmpElm)CirElm;
            arr[0] = "op-amp";
            arr[1] = "V+ = " + Utils.VoltageText(ce.Volts[OpAmpElm.V_P]);
            arr[2] = "V- = " + Utils.VoltageText(ce.Volts[OpAmpElm.V_N]);
            /* sometimes the voltage goes slightly outside range,
             * to make convergence easier.  so we hide that here. */
            double vo = Math.Max(Math.Min(ce.Volts[OpAmpElm.V_O], ce.MaxOut), ce.MinOut);
            arr[3] = "Vout = " + Utils.VoltageText(vo);
            arr[4] = "Iout = " + Utils.CurrentText(-ce.Current);
            arr[5] = "range = " + Utils.VoltageText(ce.MinOut)
                + " to " + Utils.VoltageText(ce.MaxOut);
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (OpAmpElm)CirElm;
            if (n == 0) {
                return new ElementInfo("+電源(V)", ce.MaxOut, 1, 20);
            }
            if (n == 1) {
                return new ElementInfo("-電源(V)", ce.MinOut, -20, 0);
            }
            if (n == 2) {
                return new ElementInfo("ゲイン(db)", 20 * Math.Log10(ce.Gain), 10, 1000000);
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (OpAmpElm)CirElm;
            if (n == 0) {
                ce.MaxOut = ei.Value;
            }
            if (n == 1) {
                ce.MinOut = ei.Value;
            }
            if (n == 2 && ei.Value > 0) {
                ce.Gain = Math.Pow(10.0, ei.Value / 20.0);
            }
        }

        void setGain() {
            if ((mFlags & FLAG_GAIN) != 0) {
                return;
            }
            var ce = (OpAmpElm)CirElm;
            /* gain of 100000 breaks e-amp-dfdx.txt
             * gain was 1000, but it broke amp-schmitt.txt */
            ce.Gain = ((mFlags & FLAG_LOWGAIN) != 0) ? 1000 : 100000;
        }
    }
}
