using System;
using System.Drawing;

namespace Circuit.Elements {
    partial class CircuitElm : Editable {
        const int ColorScaleCount = 64;
        static readonly Pen PenHandle = new Pen(Color.Cyan, 3.0f);

        public static readonly Color SelectColor = Color.Cyan;
        public static Color TextColor { get; set; }
        public static Color WhiteColor { get; set; }
        public static Color GrayColor { get; set; }

        public static double VoltageRange { get; set; } = 5;

        static Color[] mColorScale;

        public static void setColorScale() {
            mColorScale = new Color[ColorScaleCount];
            for (int i = 0; i != ColorScaleCount; i++) {
                double v = i * 1.0 / ColorScaleCount - 0.5;
                if (v < 0) {
                    int n1 = (int)(128 * -v) + 127;
                    int n2 = (int)(127 * (1 + v));
                    mColorScale[i] = Color.FromArgb(n2, n2, n1);
                } else {
                    int n1 = (int)(128 * v) + 127;
                    int n2 = (int)(127 * (1 - v));
                    mColorScale[i] = Color.FromArgb(n2, n1, n2);
                }
            }
        }

        /// <summary>
        /// draw current dots from point a to b
        /// </summary>
        /// <param name="g"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="pos"></param>
        protected static void drawDots(CustomGraphics g, Point a, Point b, double pos) {
            if ((!Sim.simIsRunning()) || pos == 0 || !Sim.chkDotsCheckItem.Checked) {
                return;
            }
            int dx = b.X - a.X;
            int dy = b.Y - a.Y;
            double dn = Math.Sqrt(dx * dx + dy * dy);
            int ds = 16;
            pos %= ds;
            if (pos < 0) {
                pos += ds;
            }
            double di = 0;
            if (Sim.chkPrintableCheckItem.Checked) {
                g.LineColor = GrayColor;
            } else {
                g.LineColor = Color.Yellow;
            }
            for (di = pos; di < dn; di += ds) {
                var x0 = (float)(a.X + di * dx / dn);
                var y0 = (float)(a.Y + di * dy / dn);
                g.FillCircle(x0, y0, 2);
            }
        }

        public void DrawHandles(CustomGraphics g) {
            if (mLastHandleGrabbed == -1) {
                g.FillRectangle(PenHandle.Color, X1 - 3, Y1 - 3, 7, 7);
            } else if (mLastHandleGrabbed == 0) {
                g.FillRectangle(PenHandle.Color, X1 - 4, Y1 - 4, 9, 9);
            }
            if (mNumHandles == 2) {
                if (mLastHandleGrabbed == -1) {
                    g.FillRectangle(PenHandle.Color, X2 - 3, Y2 - 3, 7, 7);
                } else if (mLastHandleGrabbed == 1) {
                    g.FillRectangle(PenHandle.Color, X2 - 4, Y2 - 4, 9, 9);
                }
            }
        }

        protected Color getVoltageColor(double volts) {
            if (NeedsHighlight) {
                return SelectColor;
            }
            if (!Sim.chkVoltsCheckItem.Checked || Sim.chkPrintableCheckItem.Checked) {
                return GrayColor;
            }
            int c = (int)((volts + VoltageRange) * (ColorScaleCount - 1) / (VoltageRange * 2));
            if (c < 0) {
                c = 0;
            }
            if (c >= ColorScaleCount) {
                c = ColorScaleCount - 1;
            }
            return mColorScale[c];
        }

        protected void drawPosts(CustomGraphics g) {
            /* we normally do this in updateCircuit() now because the logic is more complicated.
             * we only handle the case where we have to draw all the posts.  That happens when
             * this element is selected or is being created */
            if (Sim.dragElm == null && !NeedsHighlight) {
                return;
            }
            if (Sim.mouseMode == CirSim.MOUSE_MODE.DRAG_ROW || Sim.mouseMode == CirSim.MOUSE_MODE.DRAG_COLUMN) {
                return;
            }
            for (int i = 0; i != PostCount; i++) {
                var p = GetPost(i);
                g.DrawPost(p);
            }
        }

        protected void draw2Leads(CustomGraphics g) {
            /* draw first lead */
            g.DrawThickLine(getVoltageColor(Volts[0]), mPoint1, mLead1);
            /* draw second lead */
            g.DrawThickLine(getVoltageColor(Volts[1]), mLead2, mPoint2);
        }

        protected void drawCenteredText(CustomGraphics g, string s, int x, int y, bool cx) {
            var fs = g.GetTextSize(s);
            int w = (int)fs.Width;
            int h2 = (int)fs.Height / 2;
            if (cx) {
                adjustBbox(x - w / 2, y - h2, x + w / 2, y + h2);
            } else {
                adjustBbox(x, y - h2, x + w, y + h2);
            }
            g.DrawCenteredText(s, x, y);
        }

        protected void drawCenteredLText(CustomGraphics g, string s, int x, int y, bool cx) {
            var fs = g.GetLTextSize(s);
            int w = (int)fs.Width;
            int h2 = (int)fs.Height / 2;
            if (cx) {
                adjustBbox(x - w / 2, y - h2, x + w / 2, y + h2);
            } else {
                adjustBbox(x, y - h2, x + w, y + h2);
            }
            g.DrawCenteredLText(s, x, y);
        }

        /// <summary>
        /// draw component values (number of resistor ohms, etc).
        /// </summary>
        /// <param name="g"></param>
        /// <param name="s"></param>
        protected void drawValues(CustomGraphics g, string s, int offsetX = 0, int offsetY = 0) {
            if (s == null) {
                return;
            }
            var textSize = g.GetTextSize(s);
            int xc, yc;
            if ((this is RailElm) || (this is SweepElm)) {
                xc = X2;
                yc = Y2;
            } else {
                xc = (X2 + X1) / 2;
                yc = (Y2 + Y1) / 2;
            }
            g.DrawRightText(s, xc + offsetX, yc - textSize.Height + offsetY);
        }

        protected void drawCoil(CustomGraphics g, Point p1, Point p2, double v1, double v2) {
            var coilLen = (float)Utils.Distance(p1, p2);
            if (0 == coilLen) {
                return;
            }
            /* draw more loops for a longer coil */
            int loopCt = (int)Math.Ceiling(coilLen / 12);
            float w = coilLen / loopCt;
            float h = w * 1.2f;
            float wh = w * 0.5f;
            float hh = h * 0.5f;
            float th = (float)(Utils.Angle(p1, p2) * ToDeg);
            var pos = new Point();
            for (int loop = 0; loop != loopCt; loop++) {
                Utils.InterpPoint(p1, p2, ref pos, (loop + 0.5) / loopCt, 0);
                double v = v1 + (v2 - v1) * loop / loopCt;
                g.ThickLineColor = getVoltageColor(v);
                g.DrawThickArc(pos.X, pos.Y, w, th, -180);
            }
        }

        protected void drawCoil(CustomGraphics g, Point p1, Point p2, double v1, double v2, float dir) {
            var coilLen = (float)Utils.Distance(p1, p2);
            if (0 == coilLen) {
                return;
            }
            /* draw more loops for a longer coil */
            int loopCt = (int)Math.Ceiling(coilLen / 12);
            float w = coilLen / loopCt;
            float wh = w * 0.5f;
            if (Utils.Angle(p1, p2) < 0) {
                dir = -dir;
            }
            var pos = new Point();
            for (int loop = 0; loop != loopCt; loop++) {
                Utils.InterpPoint(p1, p2, ref pos, (loop + 0.5) / loopCt, 0);
                double v = v1 + (v2 - v1) * loop / loopCt;
                g.ThickLineColor = getVoltageColor(v);
                g.DrawThickArc(pos.X, pos.Y, w, dir, -180);
            }
        }

        protected Point[] getSchmittPolygon(float gsize, float ctr) {
            var pts = new Point[6];
            float hs = 3 * gsize;
            float h1 = 3 * gsize;
            float h2 = h1 * 2;
            double len = Utils.Distance(mLead1, mLead2);
            pts[0] = Utils.InterpPoint(mLead1, mLead2, ctr - h2 / len, hs);
            pts[1] = Utils.InterpPoint(mLead1, mLead2, ctr + h1 / len, hs);
            pts[2] = Utils.InterpPoint(mLead1, mLead2, ctr + h1 / len, -hs);
            pts[3] = Utils.InterpPoint(mLead1, mLead2, ctr + h2 / len, -hs);
            pts[4] = Utils.InterpPoint(mLead1, mLead2, ctr - h1 / len, -hs);
            pts[5] = Utils.InterpPoint(mLead1, mLead2, ctr - h1 / len, hs);
            return pts;
        }
    }
}
