﻿using System;
using System.Drawing;

namespace Circuit.Elements.Active {
    class LEDElm : DiodeElm {
        static string lastLEDModelName = "default-led";
        double colorR;
        double colorG;
        double colorB;
        double maxBrightnessCurrent;

        Point ledLead1;
        Point ledLead2;
        Point ledCenter;

        public LEDElm(Point pos) : base(pos) {
            modelName = lastLEDModelName;
            setup();
            maxBrightnessCurrent = .01;
            colorR = 1;
            colorG = colorB = 0;
        }

        public LEDElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
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

        public override DUMP_ID DumpType { get { return DUMP_ID.LED; } }

        public override void SetPoints() {
            base.SetPoints();
            int cr = 12;
            interpPoint(ref ledLead1, 0.5 - cr / mLen);
            interpPoint(ref ledLead2, 0.5 + cr / mLen);
            interpPoint(ref ledCenter, 0.5);
        }

        public override void Draw(CustomGraphics g) {
            if (NeedsHighlight || this == CirSim.Sim.DragElm) {
                base.Draw(g);
                return;
            }

            drawVoltage(g, 0, mPoint1, ledLead1);
            drawVoltage(g, 1, ledLead2, mPoint2);

            g.ThickLineColor = CustomGraphics.GrayColor;

            int cr = 12;
            g.DrawThickCircle(ledCenter, cr);
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

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("Red Value (0-1)", colorR, 0, 1).SetDimensionless();
            }
            if (n == 1) {
                return new ElementInfo("Green Value (0-1)", colorG, 0, 1).SetDimensionless();
            }
            if (n == 2) {
                return new ElementInfo("Blue Value (0-1)", colorB, 0, 1).SetDimensionless();
            }
            if (n == 3) {
                return new ElementInfo("Max Brightness Current (A)", maxBrightnessCurrent, 0, .1);
            }
            return base.GetElementInfo(n - 4);
        }

        public override void SetElementValue(int n, ElementInfo ei) {
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
            base.SetElementValue(n - 4, ei);
        }

        void setLastModelName(string n) {
            lastLEDModelName = n;
        }
    }
}