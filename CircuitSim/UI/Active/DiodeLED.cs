using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Active;

namespace Circuit.UI.Active {
    class DiodeLED : Diode {
        const int CR = 10;
        const int CR_INNER = 7;

        static string mLastLEDModelName = "default-led";

        double mMaxBrightnessCurrent;
        double mColorR;
        double mColorG;
        double mColorB;

        PointF mLedLead1;
        PointF mLedLead2;
        PointF mLedCenter;

        public DiodeLED(Point pos) : base(pos, "D") {
            var ce = (ElmDiode)Elm;
            ce.mModelName = mLastLEDModelName;
            setup();
            mMaxBrightnessCurrent = .01;
            mColorR = 1;
            mColorG = mColorB = 0;
        }

        public DiodeLED(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            var ce = (ElmDiode)Elm;
            if ((f & (FLAG_MODEL | FLAG_FWDROP)) == 0) {
                const double fwdrop = 2.1024259;
                ce.mModel = DiodeModel.GetModelWithParameters(fwdrop, 0);
                ce.mModelName = ce.mModel.Name;
                Console.WriteLine("model name wparams = " + ce.mModelName);
                setup();
            }
            mColorR = st.nextTokenDouble(1.0);
            mColorG = st.nextTokenDouble();
            mColorB = st.nextTokenDouble();
            mMaxBrightnessCurrent = st.nextTokenDouble(1e-3);
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.INVALID; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.LED; } }

        protected override void dump(List<object> optionList) {
            base.dump(optionList);
            optionList.Add(mColorR);
            optionList.Add(mColorG);
            optionList.Add(mColorB);
            optionList.Add(mMaxBrightnessCurrent);
        }

        public override void SetPoints() {
            base.SetPoints();
            Post.SetBbox(CR_INNER);
            interpPost(ref mLedLead1, 0.5 - CR / Post.Len);
            interpPost(ref mLedLead2, 0.5 + CR / Post.Len);
            interpPost(ref mLedCenter, 0.5);
        }

        public override void Draw(CustomGraphics g) {
            if (NeedsHighlight || this == CirSimForm.DragElm) {
                base.Draw(g);
                return;
            }

            drawLine(Elm.Post[0], mLedLead1);
            drawLine(mLedLead2, Elm.Post[1]);
            drawCircle(mLedCenter, CR);

            var ce = (ElmDiode)Elm;
            var lum = ce.Current / mMaxBrightnessCurrent;
            if (0 < lum) {
                lum = 255 * (1 + .2 * Math.Log(lum));
            }
            if (255 < lum) {
                lum = 255;
            }
            if (lum < 0) {
                lum = 0;
            }
            g.FillColor = Color.FromArgb((int)(mColorR * lum), (int)(mColorG * lum), (int)(mColorB * lum));
            g.FillCircle(mLedCenter.X, mLedCenter.Y, CR_INNER);

            updateDotCount();
            drawCurrent(Elm.Post[0], mLedLead1, mCurCount);
            drawCurrent(Elm.Post[1], mLedLead2, -mCurCount);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            base.GetInfo(arr);
            var ce = (ElmDiode)Elm;
            if (ce.mModel.OldStyle) {
                arr[0] = "LED";
            } else {
                arr[0] = "LED (" + ce.mModelName + ")";
            }
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("赤(0～1)", mColorR);
            }
            if (r == 1) {
                return new ElementInfo("緑(0～1)", mColorG);
            }
            if (r == 2) {
                return new ElementInfo("青(0～1)", mColorB);
            }
            if (r == 3) {
                return new ElementInfo("最大輝度電流(A)", mMaxBrightnessCurrent);
            }
            return base.GetElementInfo(r - 4, c);
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            if (n == 0) {
                mColorR = ei.Value;
            }
            if (n == 1) {
                mColorG = ei.Value;
            }
            if (n == 2) {
                mColorB = ei.Value;
            }
            if (n == 3) {
                mMaxBrightnessCurrent = ei.Value;
            }
            base.SetElementValue(n - 4, c, ei);
        }

        void setLastModelName(string n) {
            mLastLEDModelName = n;
        }
    }
}
