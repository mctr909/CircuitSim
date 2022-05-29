using System;
using System.Drawing;

namespace Circuit.Elements.Active {
    class DiodeUILED : DiodeUI {
        const int CR = 10;
        const int CR_INNER = 7;

        static string mLastLEDModelName = "default-led";

        double mMaxBrightnessCurrent;
        double mColorR;
        double mColorG;
        double mColorB;

        Point mLedLead1;
        Point mLedLead2;
        Point mLedCenter;

        public DiodeUILED(Point pos) : base(pos, "D") {
            var ce = (DiodeElm)Elm;
            ce.mModelName = mLastLEDModelName;
            setup();
            mMaxBrightnessCurrent = .01;
            mColorR = 1;
            mColorG = mColorB = 0;
        }

        public DiodeUILED(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            var ce = (DiodeElm)Elm;
            if ((f & (FLAG_MODEL | FLAG_FWDROP)) == 0) {
                const double fwdrop = 2.1024259;
                ce.mModel = DiodeModel.GetModelWithParameters(fwdrop, 0);
                ce.mModelName = ce.mModel.Name;
                Console.WriteLine("model name wparams = " + ce.mModelName);
                setup();
            }
            mColorR = 1.0;
            mColorG = 0.0;
            mColorB = 0.0;
            mMaxBrightnessCurrent = 0.01;
            try {
                mColorR = st.nextTokenDouble();
                mColorG = st.nextTokenDouble();
                mColorB = st.nextTokenDouble();
                mMaxBrightnessCurrent = st.nextTokenDouble();
            } catch { }
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.INVALID; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.LED; } }

        protected override string dump() {
            return base.dump()
                + " " + mColorR
                + " " + mColorG
                + " " + mColorB
                + " " + mMaxBrightnessCurrent;
        }

        public override void SetPoints() {
            base.SetPoints();
            interpPoint(ref mLedLead1, 0.5 - CR / mLen);
            interpPoint(ref mLedLead2, 0.5 + CR / mLen);
            interpPoint(ref mLedCenter, 0.5);
        }

        public override void Draw(CustomGraphics g) {
            if (NeedsHighlight || this == CirSimForm.Sim.DragElm) {
                base.Draw(g);
                return;
            }

            drawLead(mPost1, mLedLead1);
            drawLead(mLedLead2, mPost2);

            g.LineColor = CustomGraphics.GrayColor;
            g.DrawCircle(mLedCenter, CR);

            var ce = (DiodeElm)Elm;

            double w = ce.Current / mMaxBrightnessCurrent;
            if (0 < w) {
                w = 255 * (1 + .2 * Math.Log(w));
            }
            if (255 < w) {
                w = 255;
            }
            if (w < 0) {
                w = 0;
            }

            g.LineColor = Color.FromArgb((int)(mColorR * w), (int)(mColorG * w), (int)(mColorB * w));
            g.FillCircle(mLedCenter.X, mLedCenter.Y, CR_INNER);

            setBbox(mPost1, mPost2, CR_INNER);
            updateDotCount();
            drawDots(mPost1, mLedLead1, ce.CurCount);
            drawDots(mPost2, mLedLead2, -ce.CurCount);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            base.GetInfo(arr);
            var ce = (DiodeElm)Elm;
            if (ce.mModel.OldStyle) {
                arr[0] = "LED";
            } else {
                arr[0] = "LED (" + ce.mModelName + ")";
            }
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("赤(0～1)", mColorR, 0, 1).SetDimensionless();
            }
            if (n == 1) {
                return new ElementInfo("緑(0～1)", mColorG, 0, 1).SetDimensionless();
            }
            if (n == 2) {
                return new ElementInfo("青(0～1)", mColorB, 0, 1).SetDimensionless();
            }
            if (n == 3) {
                return new ElementInfo("最大輝度電流(A)", mMaxBrightnessCurrent, 0, .1);
            }
            return base.GetElementInfo(n - 4);
        }

        public override void SetElementValue(int n, ElementInfo ei) {
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
            base.SetElementValue(n - 4, ei);
        }

        void setLastModelName(string n) {
            mLastLEDModelName = n;
        }
    }
}
