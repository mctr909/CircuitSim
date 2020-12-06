using System;
using System.Drawing;

namespace Circuit.Elements {
    class LEDElm : DiodeElm {
        static string lastLEDModelName = "default-led";
        double colorR;
        double colorG;
        double colorB;
        double maxBrightnessCurrent;

        Point ledLead1;
        Point ledLead2;
        Point ledCenter;

        public LEDElm(int xx, int yy) : base(xx, yy) {
            modelName = lastLEDModelName;
            setup();
            maxBrightnessCurrent = .01;
            colorR = 1; colorG = colorB = 0;
        }

        public LEDElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f, st) {
            if ((f & (FLAG_MODEL | FLAG_FWDROP)) == 0) {
                const double fwdrop = 2.1024259;
                model = DiodeModel.getModelWithParameters(fwdrop, 0);
                modelName = model.name;
                Console.WriteLine("model name wparams = " + modelName);
                setup();
            }
            colorR = st.nextTokenDouble();
            colorG = st.nextTokenDouble();
            colorB = st.nextTokenDouble();
            maxBrightnessCurrent = .01;
            try {
                maxBrightnessCurrent = st.nextTokenDouble();
            } catch { }
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.INVALID; } }

        protected override DUMP_ID getDumpType() { return DUMP_ID.LED; }

        public override void SetPoints() {
            base.SetPoints();
            int cr = 12;
            ledLead1 = interpPoint(mPoint1, mPoint2, .5 - cr / mLen);
            ledLead2 = interpPoint(mPoint1, mPoint2, .5 + cr / mLen);
            ledCenter = interpPoint(mPoint1, mPoint2, .5);
        }

        public override void Draw(CustomGraphics g) {
            if (NeedsHighlight || this == Sim.dragElm) {
                base.Draw(g);
                return;
            }
            
            g.DrawThickLine(getVoltageColor(Volts[0]), mPoint1, ledLead1);
            g.DrawThickLine(getVoltageColor(Volts[1]), ledLead2, mPoint2);

            g.ThickLineColor = GrayColor;

            int cr = 12;
            g.DrawThickCircle(ledCenter.X, ledCenter.Y, cr);
            cr -= 4;
            double w = mCurrent / maxBrightnessCurrent;
            if (w > 0) {
                w = 255 * (1 + .2 * Math.Log(w));
            }
            if (w > 255) {
                w = 255;
            }
            if (w < 0) {
                w = 0;
            }

            g.LineColor = Color.FromArgb((int)(colorR * w), (int)(colorG * w), (int)(colorB * w));
            g.FillCircle(ledCenter.X, ledCenter.Y, cr);
            setBbox(mPoint1, mPoint2, cr);
            updateDotCount();
            drawDots(g, mPoint1, ledLead1, mCurCount);
            drawDots(g, mPoint2, ledLead2, -mCurCount);
            drawPosts(g);
        }

        public override void GetInfo(string[] arr) {
            base.GetInfo(arr);
            if (model.oldStyle) {
                arr[0] = "LED";
            } else {
                arr[0] = "LED (" + modelName + ")";
            }
        }

        public override EditInfo GetEditInfo(int n) {
            if (n == 0) {
                return new EditInfo("Red Value (0-1)", colorR, 0, 1).SetDimensionless();
            }
            if (n == 1) {
                return new EditInfo("Green Value (0-1)", colorG, 0, 1).SetDimensionless();
            }
            if (n == 2) {
                return new EditInfo("Blue Value (0-1)", colorB, 0, 1).SetDimensionless();
            }
            if (n == 3) {
                return new EditInfo("Max Brightness Current (A)", maxBrightnessCurrent, 0, .1);
            }
            return base.GetEditInfo(n - 4);
        }

        public override void SetEditValue(int n, EditInfo ei) {
            if (n == 0) {
                colorR = ei.Value;
            }
            if (n == 1) {
                colorG = ei.Value;
            }
            if (n == 2) {
                colorB = ei.Value;
            }
            if (n == 3) {
                maxBrightnessCurrent = ei.Value;
            }
            base.SetEditValue(n - 4, ei);
        }

        void setLastModelName(string n) {
            lastLEDModelName = n;
        }
    }
}
