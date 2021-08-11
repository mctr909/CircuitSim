using System;
using System.Drawing;

namespace Circuit.Elements {
    class SchmittElm : InvertingSchmittElm {
        public SchmittElm(Point pos) : base(pos) { }

        public SchmittElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f, st) { }

        public override DUMP_ID DumpType { get { return DUMP_ID.SCHMITT; } }

        public override void DoStep() {
            double v0 = Volts[1];
            double _out;
            if (state) {//Output is high
                if (Volts[0] > upperTrigger)//Input voltage high enough to set output high
                {
                    state = false;
                    _out = logicOnLevel;
                } else {
                    _out = logicOffLevel;
                }
            } else {//Output is low
                if (Volts[0] < lowerTrigger)//Input voltage low enough to set output low
                {
                    state = true;
                    _out = logicOffLevel;
                } else {
                    _out = logicOnLevel;
                }
            }
            double maxStep = slewRate * ControlPanel.TimeStep * 1e9;
            _out = Math.Max(Math.Min(v0 + maxStep, _out), v0 - maxStep);
            mCir.UpdateVoltageSource(0, Nodes[1], mVoltSource, _out);
        }

        public override void Draw(CustomGraphics g) {
            drawPosts(g);
            draw2Leads(g);
            g.LineColor = NeedsHighlight ? SelectColor : GrayColor;
            g.ThickLineColor = g.LineColor;
            g.DrawThickPolygon(gatePoly);
            g.DrawPolygon(symbolPoly);
            mCurCount = updateDotCount(mCurrent, mCurCount);
            drawDots(g, mLead2, mPoint2, mCurCount);
        }

        public override void SetPoints() {
            base.SetPoints();
            int hs = 16;
            int ww = 16;
            if (ww > mLen / 2) {
                ww = (int)(mLen / 2);
            }
            mLead1 = Utils.InterpPoint(mPoint1, mPoint2, .5 - ww / mLen);
            mLead2 = Utils.InterpPoint(mPoint1, mPoint2, .5 + (ww - 4) / mLen);
            gatePoly = new Point[3];
            Utils.InterpPoint(mLead1, mLead2, ref gatePoly[0], ref gatePoly[1], 0, hs);
            gatePoly[2] = Utils.InterpPoint(mPoint1, mPoint2, .5 + (ww - 5) / mLen);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "Schmitt Trigger~"; // ~ is for localization
        }

        public override double GetCurrentIntoNode(int n) {
            if (n == 1) {
                return mCurrent;
            }
            return 0;
        }
    }
}
