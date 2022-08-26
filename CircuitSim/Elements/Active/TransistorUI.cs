using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Active {
    class TransistorUI : BaseUI {
        const int FLAG_FLIP = 1;

        const int BODY_LEN = 16;
        const int HS = 16;

        double mCurCountC;
        double mCurCountE;
        double mCurCountB;

        Point[] mColl;
        Point[] mEmit;
        Point mTbase;

        Point[] mRectPoly;
        Point[] mArrowPoly;

        public TransistorUI(Point pos, bool pnpflag) : base(pos) {
            Elm = new TransistorElm(pnpflag);
            ReferenceName = "Tr";
            setup();
        }

        public TransistorUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new TransistorElm(st);
            try {
                ReferenceName = st.nextToken();
            } catch { }
            setup();
        }

        public override bool CanViewInScope { get { return true; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.TRANSISTOR; } }

        protected override string dump() {
            var ce = (TransistorElm)Elm;
            return ce.NPN
                + " " + (ce.Vb - ce.Vc)
                + " " + (ce.Vb - ce.Ve)
                + " " + ce.Hfe
                + " " + ReferenceName;
        }

        public override Point GetPost(int n) {
            return (n == 0) ? mPost1 : (n == 1) ? mColl[0] : mEmit[0];
        }

        void setup() {
            ((TransistorElm)Elm).Setup();
            mNoDiagonal = true;
        }

        public override void SetPoints() {
            base.SetPoints();
            var ce = (TransistorElm)Elm;

            if ((DumpInfo.Flags & FLAG_FLIP) != 0) {
                mDsign = -mDsign;
            }
            int hs2 = HS * mDsign * ce.NPN;

            /* calc collector, emitter posts */
            mColl = new Point[2];
            mEmit = new Point[2];
            interpPointAB(ref mColl[0], ref mEmit[0], 1, hs2);

            /* calc rectangle edges */
            var rect = new Point[4];
            interpPointAB(ref rect[0], ref rect[1], 1 - (BODY_LEN - 1) / mLen, HS);
            interpPointAB(ref rect[2], ref rect[3], 1 - (BODY_LEN - 3) / mLen, HS);

            /* calc points where collector/emitter leads contact rectangle */
            interpPointAB(ref mColl[1], ref mEmit[1], 1 - (BODY_LEN - 3) / mLen, 6 * mDsign * ce.NPN);

            /* calc point where base lead contacts rectangle */
            interpPoint(ref mTbase, 1 - (BODY_LEN - 1) / mLen);

            /* rectangle */
            mRectPoly = new Point[] { rect[0], rect[2], rect[3], rect[1] };

            /* arrow */
            if (ce.NPN == 1) {
                Utils.CreateArrow(mEmit[1], mEmit[0], out mArrowPoly, 8, 3);
            } else {
                var pt = new Point();
                interpPoint(ref pt, 1 - (BODY_LEN - 2) / mLen, -5 * mDsign * ce.NPN);
                Utils.CreateArrow(mEmit[0], pt, out mArrowPoly, 8, 3);
            }

            setTextPos();
        }

        void setTextPos() {
            var txtW = Context.GetTextSize(ReferenceName).Width;
            var swap = 0 < (DumpInfo.Flags & FLAG_FLIP) ? -1 : 1;
            mNameV = mPost1.Y == mPost2.Y;
            if (mNameV) {
                if (0 < mDsign * swap) {
                    mNamePos = new Point(mPost2.X - 1, mPost2.Y);
                } else {
                    mNamePos = new Point(mPost2.X - 16, mPost2.Y);
                }
            } else if (mPost1.X == mPost2.X) {
                mNamePos = new Point(mPost2.X, mPost2.Y + HS * swap * mDsign * 2 / 3);
            } else {
                interpPoint(ref mNamePos, 0.5, 10 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            setBbox(mPost1, mPost2, 16);

            /* draw collector */
            drawLead(mColl[0], mColl[1]);
            /* draw emitter */
            drawLead(mEmit[0], mEmit[1]);
            /* draw arrow */
            g.FillPolygon(NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor, mArrowPoly);
            /* draw base */
            drawLead(mPost1, mTbase);

            /* draw dots */
            var ce = (TransistorElm)Elm;
            mCurCountB = updateDotCount(-ce.Ib, mCurCountB);
            drawDots(mTbase, mPost1, mCurCountB);
            mCurCountC = updateDotCount(-ce.Ic, mCurCountC);
            drawDots(mColl[1], mColl[0], mCurCountC);
            mCurCountE = updateDotCount(-ce.Ie, mCurCountE);
            drawDots(mEmit[1], mEmit[0], mCurCountE);

            /* draw base rectangle */
            g.FillPolygon(NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor, mRectPoly);

            drawPosts();

            if (ControlPanel.ChkShowName.Checked) {
                if (mNameV) {
                    g.DrawCenteredVText(ReferenceName, mNamePos.X, mNamePos.Y);
                } else {
                    g.DrawCenteredText(ReferenceName, mNamePos.X, mNamePos.Y);
                }
            }
        }

        public override string GetScopeText() {
            var ce = (TransistorElm)Elm;
            return (string.IsNullOrEmpty(ReferenceName) ? "トランジスタ" : ReferenceName)
                + " Vce(" + (1 == ce.NPN ? "npn)" : " pnp)");
        }

        public override void GetInfo(string[] arr) {
            var ce = (TransistorElm)Elm;
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

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo("名前", 0, 0, 0);
                ei.Text = ReferenceName;
                return ei;
            }
            if (n == 1) {
                return new ElementInfo("hfe", ((TransistorElm)Elm).Hfe, 10, 1000).SetDimensionless();
            }
            if (n == 2) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "エミッタ/コレクタ 入れ替え";
                ei.CheckBox.Checked = (DumpInfo.Flags & FLAG_FLIP) != 0;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                ReferenceName = ei.Textf.Text;
                setTextPos();
            }
            if (n == 1) {
                ((TransistorElm)Elm).Hfe = ei.Value;
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
