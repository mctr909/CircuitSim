using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Active;

namespace Circuit.UI.Active {
    class Transistor : BaseUI {
        const int FLAG_FLIP = 1;

        const int BODY_LEN = 13;
        const int HS = 12;
        const int BASE_THICK = 2;

        double mCurCountC;
        double mCurCountE;
        double mCurCountB;

        PointF mTbase;

        PointF[] mRectPoly;
        PointF[] mArrowPoly;
        PointF[] mPosC = new PointF[3];
        PointF[] mPosE = new PointF[3];

        public Transistor(Point pos, bool pnpflag) : base(pos) {
            Elm = new ElmTransistor(pnpflag);
            ReferenceName = "Tr";
            setup();
        }

        public Transistor(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            var npn = st.nextTokenInt(1);
            var vbe = st.nextTokenDouble();
            var vbc = st.nextTokenDouble();
            var hfe = st.nextTokenDouble(100);
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
            Post.SetBbox(16);

            var ce = (ElmTransistor)Elm;

            if ((mFlags & FLAG_FLIP) != 0) {
                Post.Dsign = -Post.Dsign;
            }

            /* calc collector, emitter posts */
            var hsm = (HS / 8 + 1) * 8;
            var hs1 = (HS - 2) * Post.Dsign * ce.NPN;
            var hs2 = hsm * Post.Dsign * ce.NPN;
            interpPostAB(ref mPosC[1], ref mPosE[1], 1, hs1);
            interpPostAB(ref mPosC[2], ref mPosE[2], 1, hs2);

            /* calc rectangle edges */
            var rect = new PointF[4];
            interpPostAB(ref rect[0], ref rect[1], 1 - BODY_LEN / Post.Len, HS);
            interpPostAB(ref rect[2], ref rect[3], 1 - (BODY_LEN - BASE_THICK) / Post.Len, HS);

            /* calc points where collector/emitter leads contact rectangle */
            interpPostAB(ref mPosC[0], ref mPosE[0],
                1 - (BODY_LEN - BASE_THICK * 0.5) / Post.Len,
                5 * Post.Dsign * ce.NPN
            );

            /* calc point where base lead contacts rectangle */
            if (Post.Dsign < 0) {
                interpPost(ref mTbase, 1 - (BODY_LEN - BASE_THICK) / Post.Len);
            } else {
                interpPost(ref mTbase, 1 - BODY_LEN / Post.Len);
            }

            /* rectangle */
            mRectPoly = new PointF[] { rect[0], rect[2], rect[3], rect[1] };

            /* arrow */
            if (ce.NPN == 1) {
                Utils.CreateArrow(mPosE[0], mPosE[1], out mArrowPoly, 8, 3);
            } else {
                var b = new PointF();
                interpPost(ref b, 1 - (BODY_LEN - 1) / Post.Len, -5 * Post.Dsign * ce.NPN);
                Utils.CreateArrow(mPosE[1], b, out mArrowPoly, 8, 3);
            }
            setTextPos();

            ce.Post[1].X = (int)mPosC[2].X;
            ce.Post[1].Y = (int)mPosC[2].Y;
            ce.Post[2].X = (int)mPosE[2].X;
            ce.Post[2].Y = (int)mPosE[2].Y;
        }

        void setTextPos() {
            var swap = 0 < (mFlags & FLAG_FLIP) ? -1 : 1;
            if (Post.Horizontal) {
                if (0 < Post.Dsign * swap) {
                    mNamePos = new Point(Elm.Post[1].X - 1, Elm.Post[1].Y);
                } else {
                    mNamePos = new Point(Elm.Post[1].X - 16, Elm.Post[1].Y);
                }
            } else if (Post.Vertical) {
                mNamePos = new Point(Elm.Post[1].X, Elm.Post[1].Y + HS * swap * Post.Dsign * 2 / 3);
            } else {
                interpPost(ref mNamePos, 0.5, 10 * Post.Dsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmTransistor)Elm;

            /* draw collector */
            drawLine(mPosC[2], mPosC[1]);
            drawLine(mPosC[1], mPosC[0]);
            /* draw emitter */
            drawLine(mPosE[2], mPosE[1]);
            drawLine(mPosE[1], mPosE[0]);
            /* draw arrow */
            fillPolygon(mArrowPoly);
            /* draw base */
            drawLine(Elm.Post[0], mTbase);
            /* draw base rectangle */
            fillPolygon(mRectPoly);

            drawPosts();

            /* draw dots */
            updateDotCount(-ce.Ib, ref mCurCountB);
            updateDotCount(-ce.Ic, ref mCurCountC);
            updateDotCount(-ce.Ie, ref mCurCountE);
            drawCurrent(mTbase, Elm.Post[0], mCurCountB);
            if (0 <= ce.NPN * ce.Ic) {
                drawCurrent(mPosE[1], mTbase, mCurCountB);
            } else {
                drawCurrent(mPosC[1], mTbase, mCurCountB);
            }
            drawCurrent(mPosE[1], mPosC[1], mCurCountC);

            if (ControlPanel.ChkShowName.Checked) {
                if (Post.Vertical) {
                    g.DrawCenteredText(ReferenceName, mNamePos);
                } else {
                    g.DrawCenteredVText(ReferenceName, mNamePos);
                }
            }
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmTransistor)Elm;
            arr[0] = ((ce.NPN == -1) ? "PNP" : "NPN") + "トランジスタ(" + "hfe：" + Utils.UnitText(ce.Hfe) + ")";
            var vbc = ce.Vb - ce.Vc;
            var vbe = ce.Vb - ce.Ve;
            var vce = ce.Vc - ce.Ve;
            if (vbc * ce.NPN > 0.2) {
                arr[1] = "動作領域：" + (vbe * ce.NPN > 0.2 ? "飽和" : "逆流");
            } else {
                arr[1] = "動作領域：" + (vbe * ce.NPN > 0.2 ? "活性" : "遮断");
            }
            arr[2] = "Vce：" + Utils.VoltageText(vce);
            arr[3] = "Vbe：" + Utils.VoltageText(vbe);
            arr[4] = "Vbc：" + Utils.VoltageText(vbc);
            arr[5] = "Ic：" + Utils.CurrentText(ce.Ic);
            arr[6] = "Ib：" + Utils.CurrentText(ce.Ib);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("名前", ReferenceName);
            }
            if (r == 1) {
                return new ElementInfo("hfe", ((ElmTransistor)Elm).Hfe);
            }
            if (r == 2) {
                return new ElementInfo("エミッタ/コレクタ 入れ替え", (mFlags & FLAG_FLIP) != 0);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            if (n == 0) {
                ReferenceName = ei.Text;
                setTextPos();
            }
            if (n == 1) {
                ((ElmTransistor)Elm).Hfe = ei.Value;
                setup();
            }
            if (n == 2) {
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_FLIP;
                } else {
                    mFlags &= ~FLAG_FLIP;
                }
                SetPoints();
            }
        }
    }
}
