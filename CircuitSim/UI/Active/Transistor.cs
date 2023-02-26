using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Active;

namespace Circuit.UI.Active {
    class Transistor : BaseUI {
        const int FLAG_FLIP = 1;

        const int BODY_LEN = 16;
        const int HS = 16;
        const int BASE_THICK = 2;

        double mCurCountC;
        double mCurCountE;
        double mCurCountB;

        Point mTbase;

        Point[] mRectPoly;
        Point[] mArrowPoly;

        public Transistor(Point pos, bool pnpflag) : base(pos) {
            Elm = new ElmTransistor(pnpflag);
            DumpInfo.ReferenceName = "Tr";
            setup();
        }

        public Transistor(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            var npn = 1;
            var vbe = 0.0;
            var vbc = 0.0;
            var hfe = 100.0;
            try {
                npn = st.nextTokenInt();
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
            int hs2 = HS * mDsign * ce.NPN;

            /* calc collector, emitter posts */
            ce.Coll = new Point[2];
            ce.Emit = new Point[2];
            interpPointAB(ref ce.Coll[0], ref ce.Emit[0], 1, hs2);

            /* calc rectangle edges */
            var rect = new Point[4];
            interpPointAB(ref rect[0], ref rect[1], 1 - BODY_LEN / mLen, HS);
            interpPointAB(ref rect[2], ref rect[3], 1 - (BODY_LEN - BASE_THICK) / mLen, HS);

            /* calc points where collector/emitter leads contact rectangle */
            interpPointAB(ref ce.Coll[1], ref ce.Emit[1], 1 - (BODY_LEN - BASE_THICK) / mLen, 6 * mDsign * ce.NPN);

            /* calc point where base lead contacts rectangle */
            if (mDsign < 0) {
                interpPoint(ref mTbase, 1 - (BODY_LEN - BASE_THICK + 1) / mLen);
            } else {
                interpPoint(ref mTbase, 1 - BODY_LEN / mLen);
            }

            /* rectangle */
            mRectPoly = new Point[] { rect[0], rect[2], rect[3], rect[1] };

            /* arrow */
            if (ce.NPN == 1) {
                var e0 = ce.Emit[0];
                var e1 = ce.Emit[1];
                Utils.CreateArrow(e1.X, e1.Y, e0.X, e0.Y, out mArrowPoly, 8, 3);
            } else {
                var e0 = ce.Emit[0];
                var pt = new Point();
                interpPoint(ref pt, 1 - BODY_LEN / mLen, -5 * mDsign * ce.NPN);
                Utils.CreateArrow(e0.X, e0.Y, pt.X, pt.Y, out mArrowPoly, 8, 3);
            }

            setTextPos();
        }

        void setTextPos() {
            var txtW = Context.GetTextSize(DumpInfo.ReferenceName).Width;
            var swap = 0 < (DumpInfo.Flags & FLAG_FLIP) ? -1 : 1;
            if (mVertical) {
                mNamePos = new Point(Elm.Post2X, Elm.Post2Y + HS * swap * mDsign * 2 / 3);
            } else if (mHorizontal) {
                if (0 < mDsign * swap) {
                    mNamePos = new Point(Elm.Post2X - 1, Elm.Post2Y);
                } else {
                    mNamePos = new Point(Elm.Post2X - 16, Elm.Post2Y);
                }
            } else {
                interpPoint(ref mNamePos, 0.5, 10 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            setBbox(16);
            var ce = (ElmTransistor)Elm;

            /* draw collector */
            drawLead(ce.Coll[0], ce.Coll[1]);
            /* draw emitter */
            drawLead(ce.Emit[0], ce.Emit[1]);
            /* draw arrow */
            g.FillPolygon(NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor, mArrowPoly);
            /* draw base */
            drawLead(Elm.Post1X, Elm.Post1Y, mTbase);

            /* draw dots */
            mCurCountB = updateDotCount(-ce.Ib, mCurCountB);
            drawDots(mTbase, Elm.Post1X, Elm.Post1Y, mCurCountB);
            mCurCountC = updateDotCount(-ce.Ic, mCurCountC);
            drawDots(ce.Coll[1], ce.Coll[0], mCurCountC);
            mCurCountE = updateDotCount(-ce.Ie, mCurCountE);
            drawDots(ce.Emit[1], ce.Emit[0], mCurCountE);

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
            arr[7] = "P = " + Utils.UnitText(ce.Power, "W");
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
