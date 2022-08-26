using System;
using System.Collections.Generic;
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
            DumpInfo.Flags = FLAG_GAIN; /* need to do this before setSize() */
            DumpInfo.Flags |= FLAG_SMALL;
            Elm = new OpAmpElm();
        }

        public OpAmpUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new OpAmpElm(st);
            mNoDiagonal = true;
            DumpInfo.Flags |= FLAG_SMALL;
            setGain();
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.OPAMP; } }

        protected override void dump(List<object> optionList) {
            var ce = (OpAmpElm)Elm;
            DumpInfo.Flags |= FLAG_GAIN;
            optionList.Add(ce.MaxOut);
            optionList.Add(ce.MinOut);
            optionList.Add(ce.Gbw);
            optionList.Add(ce.Volts[OpAmpElm.V_N].ToString("0.000000"));
            optionList.Add(ce.Volts[OpAmpElm.V_P].ToString("0.000000"));
            optionList.Add(ce.Gain);
        }

        public override void Draw(CustomGraphics g) {
            setBbox(mPost1, mPost2, mOpHeight * 2);

            drawLead(mIn1p[0], mIn1p[1]);
            drawLead(mIn2p[0], mIn2p[1]);
            drawLead(mLead2, mPost2);

            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            g.DrawPolygon(mTriangle);

            drawLead(mTextp[0], mTextp[1]);
            drawLead(mTextp[2], mTextp[3]);
            drawLead(mTextp[4], mTextp[5]);

            var ce = (OpAmpElm)Elm;
            ce.CurCount = updateDotCount(ce.Current, ce.CurCount);
            drawDots(mPost2, mLead2, ce.CurCount);
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
            if ((DumpInfo.Flags & FLAG_SWAP) != 0) {
                hs = -hs;
            }
            mIn1p = new Point[2];
            mIn2p = new Point[2];
            interpPointAB(ref mIn1p[0], ref mIn2p[0], 0, hs);
            interpLeadAB(ref mIn1p[1], ref mIn2p[1], 0, hs);

            var signp = new Point[2];
            interpLeadAB(ref signp[0], ref signp[1], 0.2, hs);
            mTextp = new Point[] {
                new Point(signp[0].X - 3, signp[0].Y),
                new Point(signp[0].X + 3, signp[0].Y),
                new Point(signp[1].X - 3, signp[1].Y),
                new Point(signp[1].X + 3, signp[1].Y),
                new Point(signp[1].X, signp[1].Y - 3),
                new Point(signp[1].X, signp[1].Y + 3)
            };

            var tris = new Point[2];
            interpLeadAB(ref tris[0], ref tris[1], 0, hs * 2);
            mTriangle = new Point[] { tris[0], tris[1], mLead2 };
        }

        public override Point GetPost(int n) {
            return (n == 0) ? mIn1p[0] : (n == 1) ? mIn2p[0] : mPost2;
        }

        public override void GetInfo(string[] arr) {
            var ce = (OpAmpElm)Elm;
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
            var ce = (OpAmpElm)Elm;
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
            var ce = (OpAmpElm)Elm;
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
            if ((DumpInfo.Flags & FLAG_GAIN) != 0) {
                return;
            }
            var ce = (OpAmpElm)Elm;
            /* gain of 100000 breaks e-amp-dfdx.txt
             * gain was 1000, but it broke amp-schmitt.txt */
            ce.Gain = ((DumpInfo.Flags & FLAG_LOWGAIN) != 0) ? 1000 : 100000;
        }
    }
}
