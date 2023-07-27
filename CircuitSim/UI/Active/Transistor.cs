using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Active;

namespace Circuit.UI.Active {
    class Transistor : BaseUI {
        const int FLAG_FLIP = 1;

        const int BODY_LEN = 12;
        const int HS = 12;
        const int BASE_THICK = 2;

        double mCurCountC;
        double mCurCountE;
        double mCurCountB;

        Point mTbase;

        Point[] mRectPoly;
        Point[] mArrowPoly;
        Point[] mPosC = new Point[3];
        Point[] mPosE = new Point[3];

        public Transistor(Point pos, bool pnpflag) : base(pos) {
            Elm = new ElmTransistor(pnpflag);
            DumpInfo.ReferenceName = "Tr";
            setup();
        }

        public Transistor(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            int npn;
            var vbe = 0.0;
            var vbc = 0.0;
            var hfe = 100.0;
            st.nextTokenInt(out npn, 1);
            try {
                vbe = st.nextTokenDouble();
                vbc = st.nextTokenDouble();
                hfe = st.nextTokenDouble();
            } catch { }
            Elm = new ElmTransistor(npn, hfe, vbe, vbc);
            setup();
        }

        public override bool CanViewInScope { get { return true; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.TRANSISTOR; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmTransistor)Elm;
            optionList.Add(ce.NPN);
            optionList.Add((ce.Vb - ce.Vc).ToString("0.000000"));
            optionList.Add((ce.Vb - ce.Ve).ToString("0.000000"));
            optionList.Add(ce.Hfe);
        }

        void setup() {
            ((ElmTransistor)Elm).Setup();
            mNoDiagonal = true;
        }

        public override void SetPoints() {
            base.SetPoints();
            var ce = (ElmTransistor)Elm;

            if ((DumpInfo.Flags & FLAG_FLIP) != 0) {
                mDsign = -mDsign;
            }

            /* calc collector, emitter posts */
            var hsm = (HS / 8 + 1) * 8;
            var hs1 = HS * mDsign * ce.NPN;
            var hs2 = hsm * mDsign * ce.NPN;
            interpPointAB(ref mPosC[1], ref mPosE[1], 1, hs1);
            interpPointAB(ref mPosC[2], ref mPosE[2], 1, hs2);

            /* calc rectangle edges */
            var rect = new Point[4];
            interpPointAB(ref rect[0], ref rect[1], 1 - BODY_LEN / mLen, HS);
            interpPointAB(ref rect[2], ref rect[3], 1 - (BODY_LEN - BASE_THICK) / mLen, HS);

            /* calc points where collector/emitter leads contact rectangle */
            interpPointAB(ref mPosC[0], ref mPosE[0], 1 - (BODY_LEN - BASE_THICK) / mLen, 6 * mDsign * ce.NPN);

            /* calc point where base lead contacts rectangle */
            if (mDsign < 0) {
                interpPoint(ref mTbase, 1 - (BODY_LEN - BASE_THICK) / mLen);
            } else {
                interpPoint(ref mTbase, 1 - BODY_LEN / mLen);
            }

            /* rectangle */
            mRectPoly = new Point[] { rect[0], rect[2], rect[3], rect[1] };

            /* arrow */
            if (ce.NPN == 1) {
                Utils.CreateArrow(mPosE[0], mPosE[1], out mArrowPoly, 8, 3);
            } else {
                var pt = new Point();
                interpPoint(ref pt, 1 - BODY_LEN / mLen, -5 * mDsign * ce.NPN);
                Utils.CreateArrow(mPosE[1], pt, out mArrowPoly, 8, 3);
            }
            setTextPos();

            ce.Post[1] = mPosC[2];
            ce.Post[2] = mPosE[2];
        }

        void setTextPos() {
            var txtW = Context.GetTextSize(DumpInfo.ReferenceName).Width;
            var swap = 0 < (DumpInfo.Flags & FLAG_FLIP) ? -1 : 1;
            if (mVertical) {
                mNamePos = new Point(Elm.Post[1].X, Elm.Post[1].Y + HS * swap * mDsign * 2 / 3);
            } else if (mHorizontal) {
                if (0 < mDsign * swap) {
                    mNamePos = new Point(Elm.Post[1].X - 1, Elm.Post[1].Y);
                } else {
                    mNamePos = new Point(Elm.Post[1].X - 16, Elm.Post[1].Y);
                }
            } else {
                interpPoint(ref mNamePos, 0.5, 10 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            setBbox(16);
            var ce = (ElmTransistor)Elm;

            /* draw collector */
            drawLead(mPosC[2], mPosC[1]);
            drawLead(mPosC[1], mPosC[0]);
            /* draw emitter */
            drawLead(mPosE[2], mPosE[1]);
            drawLead(mPosE[1], mPosE[0]);
            /* draw arrow */
            g.FillPolygon(NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor, mArrowPoly);
            /* draw base */
            drawLead(Elm.Post[0], mTbase);

            /* draw dots */
            updateDotCount(-ce.Ib, ref mCurCountB);
            drawDots(mTbase, Elm.Post[0], mCurCountB);
            updateDotCount(-ce.Ic, ref mCurCountC);
            drawDots(mPosC[0], mPosC[2], mCurCountC);
            updateDotCount(-ce.Ie, ref mCurCountE);
            drawDots(mPosE[0], mPosE[2], mCurCountE);

            /* draw base rectangle */
            g.FillPolygon(NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor, mRectPoly);

            drawPosts();

            if (ControlPanel.ChkShowName.Checked) {
                if (mVertical) {
                    g.DrawCenteredText(DumpInfo.ReferenceName, mNamePos.X, mNamePos.Y);
                } else {
                    g.DrawCenteredVText(DumpInfo.ReferenceName, mNamePos.X, mNamePos.Y);
                }
            }
        }

        public override string GetScopeText() {
            var ce = (ElmTransistor)Elm;
            return (string.IsNullOrEmpty(DumpInfo.ReferenceName) ? "トランジスタ" : DumpInfo.ReferenceName)
                + " Vce(" + (1 == ce.NPN ? "npn)" : " pnp)");
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmTransistor)Elm;
            arr[0] = "transistor (" + ((ce.NPN == -1) ? "PNP)" : "NPN)") + " hfe=" + ce.Hfe.ToString("0.000");
            double vbc = ce.Vb - ce.Vc;
            double vbe = ce.Vb - ce.Ve;
            double vce = ce.Vc - ce.Ve;
            if (vbc * ce.NPN > .2) {
                arr[1] = vbe * ce.NPN > .2 ? "saturation" : "reverse active";
            } else {
                arr[1] = vbe * ce.NPN > .2 ? "fwd active" : "cutoff";
            }
            arr[1] = arr[1];
            arr[2] = "Ic = " + Utils.CurrentText(ce.Ic);
            arr[3] = "Ib = " + Utils.CurrentText(ce.Ib);
            arr[4] = "Vbe = " + Utils.VoltageText(vbe);
            arr[5] = "Vbc = " + Utils.VoltageText(vbc);
            arr[6] = "Vce = " + Utils.VoltageText(vce);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("名前", DumpInfo.ReferenceName);
            }
            if (r == 1) {
                return new ElementInfo("hfe", ((ElmTransistor)Elm).Hfe);
            }
            if (r == 2) {
                return new ElementInfo("エミッタ/コレクタ 入れ替え", (DumpInfo.Flags & FLAG_FLIP) != 0);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            if (n == 0) {
                DumpInfo.ReferenceName = ei.Textf.Text;
                setTextPos();
            }
            if (n == 1) {
                ((ElmTransistor)Elm).Hfe = ei.Value;
                setup();
            }
            if (n == 2) {
                if (ei.CheckBox.Checked) {
                    DumpInfo.Flags |= FLAG_FLIP;
                } else {
                    DumpInfo.Flags &= ~FLAG_FLIP;
                }
                SetPoints();
            }
        }
    }
}
