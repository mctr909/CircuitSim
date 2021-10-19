using System;
using System.Drawing;

namespace Circuit.Elements.Active {
    class LEDElm : DiodeElm {
        const int CR = 10;
        const int CR_INNER = 7;

        static string mLastLEDModelName = "default";

        double mMaxBrightnessCurrent;
        double mColorR;
        double mColorG;
        double mColorB;

        Point mLedLead1;
        Point mLedLead2;
        Point mLedCenter;

        public LEDElm(Point pos) : base(pos) {
            mModelName = mLastLEDModelName;
            setup();
            mMaxBrightnessCurrent = .01;
            mColorR = 1;
            mColorG = mColorB = 0;
        }

        public LEDElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            if ((f & (FLAG_MODEL | FLAG_FWDROP)) == 0) {
                const double fwdrop = 2.1024259;
                mModel = DiodeModel.getModelWithParameters(fwdrop, 0);
                mModelName = mModel.name;
                Console.WriteLine("model name wparams = " + mModelName);
                setup();
            }
            mColorR = st.nextTokenDouble();
            mColorG = st.nextTokenDouble();
            mColorB = st.nextTokenDouble();
            mMaxBrightnessCurrent = .01;
            try {
                mMaxBrightnessCurrent = st.nextTokenDouble();
            } catch { }
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.INVALID; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.LED; } }

        public override void SetPoints() {
            base.SetPoints();
            interpPoint(ref mLedLead1, 0.5 - CR / mLen);
            interpPoint(ref mLedLead2, 0.5 + CR / mLen);
            interpPoint(ref mLedCenter, 0.5);
        }

        public override void Draw() {
            if (NeedsHighlight || this == CirSim.Sim.DragElm) {
                base.Draw();
                return;
            }

            drawVoltage(0, mPoint1, mLedLead1);
            drawVoltage(1, mLedLead2, mPoint2);

            Context.DrawCircle(CustomGraphics.GrayColor, mLedCenter, CR);

            double w = mCurrent / mMaxBrightnessCurrent;
            if (0 < w) {
                w = 255 * (1 + .2 * Math.Log(w));
            }
            if (255 < w) {
                w = 255;
            }
            if (w < 0) {
                w = 0;
            }

            Context.LineColor = Color.FromArgb((int)(mColorR * w), (int)(mColorG * w), (int)(mColorB * w));
            Context.FillCircle(mLedCenter.X, mLedCenter.Y, CR_INNER);

            setBbox(mPoint1, mPoint2, CR_INNER);
            updateDotCount();
            drawDots(mPoint1, mLedLead1, mCurCount);
            drawDots(mPoint2, mLedLead2, -mCurCount);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            base.GetInfo(arr);
            if (mModel.oldStyle) {
                arr[0] = "LED";
            } else {
                arr[0] = "LED (" + mModelName + ")";
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
