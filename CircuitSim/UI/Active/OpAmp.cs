using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Active;

namespace Circuit.UI.Active {
    class OpAmp : BaseUI {
        protected const int FLAG_SWAP = 1;
        protected const int FLAG_SMALL = 2;
        protected const int FLAG_LOWGAIN = 4;
        protected const int FLAG_GAIN = 8;

        const int HEIGHT = 8;
        const int WIDTH = 16;

        PointF[] mTextp;
        PointF[] mTriangle;
        Point mPosOut = new Point();
        PointF[] mPosIn1 = new PointF[2];
        PointF[] mPosIn2 = new PointF[2];

        public OpAmp(Point pos) : base(pos) {
            Post.NoDiagonal = true;
            mFlags = FLAG_GAIN; /* need to do this before setSize() */
            mFlags |= FLAG_SMALL;
            Elm = new ElmOpAmp();
        }

        public OpAmp(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            var elm = new ElmOpAmp();
            Elm = elm;
            elm.MaxOut = st.nextTokenDouble();
            elm.MinOut = st.nextTokenDouble();
            elm.Gain = st.nextTokenDouble();
            elm.Volts[ElmOpAmp.V_N] = st.nextTokenDouble();
            elm.Volts[ElmOpAmp.V_P] = st.nextTokenDouble();
            Post.NoDiagonal = true;
            mFlags |= FLAG_SMALL;
            setGain();
        }

        public override DUMP_ID DumpId { get { return DUMP_ID.OPAMP; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmOpAmp)Elm;
            mFlags |= FLAG_GAIN;
            optionList.Add(ce.MaxOut);
            optionList.Add(ce.MinOut);
            optionList.Add(ce.Gain);
            optionList.Add(ce.Volts[ElmOpAmp.V_N].ToString("g3"));
            optionList.Add(ce.Volts[ElmOpAmp.V_P].ToString("g3"));
        }

        public override void SetPoints() {
            base.SetPoints();
            int ww = WIDTH;
            if (ww > Post.Len / 2) {
                ww = (int)(Post.Len / 2);
            }
            setLeads(ww * 2);
            int hs = HEIGHT * Post.Dsign;
            if ((mFlags & FLAG_SWAP) != 0) {
                hs = -hs;
            }
            interpPostAB(ref mPosIn1[0], ref mPosIn2[0], 0, hs);
            interpLeadAB(ref mPosIn1[1], ref mPosIn2[1], 0, hs);
            mPosOut = Post.B;

            var signp = new PointF[2];
            interpLeadAB(ref signp[0], ref signp[1], 0.2, hs);

            mTextp = new PointF[] {
                new PointF(signp[0].X - 3, signp[0].Y),
                new PointF(signp[0].X + 3, signp[0].Y),
                new PointF(signp[1].X - 3, signp[1].Y),
                new PointF(signp[1].X + 3, signp[1].Y),
                new PointF(signp[1].X, signp[1].Y - 3),
                new PointF(signp[1].X, signp[1].Y + 3)
            };

            var tris = new PointF[2];
            interpLeadAB(ref tris[0], ref tris[1], 0, hs * 2);
            mTriangle = new PointF[] { tris[0], tris[1], mLead2 };

            Elm.SetNodePos(mPosIn1[0], mPosIn2[0], mPosOut);
        }

        public override void Draw(CustomGraphics g) {
            drawLine(mPosIn1[0], mPosIn1[1]);
            drawLine(mPosIn2[0], mPosIn2[1]);
            drawLine(mLead2, mPosOut);

            drawPolygon(mTriangle);

            drawLine(mTextp[0], mTextp[1]);
            drawLine(mTextp[2], mTextp[3]);
            drawLine(mTextp[4], mTextp[5]);

            updateDotCount(Elm.Current, ref mCurCount);
            drawCurrent(mLead2, mPosOut, -mCurCount);
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmOpAmp)Elm;
            arr[0] = "オペアンプ";
            arr[1] = "+電源：" + Utils.VoltageText(ce.MaxOut);
            arr[2] = "-電源：" + Utils.VoltageText(ce.MinOut);
            arr[3] = "Vin(+)：" + Utils.VoltageText(ce.Volts[ElmOpAmp.V_P]);
            arr[4] = "Vin(-)：" + Utils.VoltageText(ce.Volts[ElmOpAmp.V_N]);
            var vo = Math.Max(Math.Min(ce.Volts[ElmOpAmp.V_O], ce.MaxOut), ce.MinOut);
            arr[5] = "Vout：" + Utils.VoltageText(vo);
            arr[6] = "Iout：" + Utils.CurrentText(-ce.Current);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmOpAmp)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("+電源", ce.MaxOut);
            }
            if (r == 1) {
                return new ElementInfo("-電源", ce.MinOut);
            }
            if (r == 2) {
                return new ElementInfo("ゲイン(db)", 20 * Math.Log10(ce.Gain));
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmOpAmp)Elm;
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
            var ce = (ElmOpAmp)Elm;
            /* gain of 100000 breaks e-amp-dfdx.txt
             * gain was 1000, but it broke amp-schmitt.txt */
            ce.Gain = ((mFlags & FLAG_LOWGAIN) != 0) ? 1000 : 100000;
        }
    }
}
