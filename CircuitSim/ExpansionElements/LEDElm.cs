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

        protected override DUMP_ID getDumpType() { return DUMP_ID.LED; }

        public override void setPoints() {
            base.setPoints();
            int cr = 12;
            ledLead1 = interpPoint(mPoint1, mPoint2, .5 - cr / mElmLen);
            ledLead2 = interpPoint(mPoint1, mPoint2, .5 + cr / mElmLen);
            ledCenter = interpPoint(mPoint1, mPoint2, .5);
        }

        public override void draw(Graphics g) {
            if (needsHighlight() || this == Sim.dragElm) {
                base.draw(g);
                return;
            }
            
            drawThickLine(g, getVoltageColor(Volts[0]), mPoint1, ledLead1);
            drawThickLine(g, getVoltageColor(Volts[1]), ledLead2, mPoint2);

            PenThickLine.Color = LightGrayColor;

            int cr = 12;
            drawThickCircle(g, ledCenter.X, ledCenter.Y, cr);
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

            PenThickLine.Color = Color.FromArgb((int)(colorR * w), (int)(colorG * w), (int)(colorB * w));
            g.FillPie(PenThickLine.Brush, ledCenter.X - cr, ledCenter.Y - cr, cr * 2, cr * 2, 0, 360);
            setBbox(mPoint1, mPoint2, cr);
            updateDotCount();
            drawDots(g, mPoint1, ledLead1, mCurCount);
            drawDots(g, mPoint2, ledLead2, -mCurCount);
            drawPosts(g);
        }

        public override void getInfo(string[] arr) {
            base.getInfo(arr);
            if (model.oldStyle) {
                arr[0] = "LED";
            } else {
                arr[0] = "LED (" + modelName + ")";
            }
        }

        public override EditInfo getEditInfo(int n) {
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
            return base.getEditInfo(n - 4);
        }

        public override void setEditValue(int n, EditInfo ei) {
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
            base.setEditValue(n - 4, ei);
        }

        public override DUMP_ID getShortcut() { return DUMP_ID.INVALID; }

        void setLastModelName(string n) {
            lastLEDModelName = n;
        }
    }
}
