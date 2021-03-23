using System;
using System.Windows.Forms;
using System.Drawing;

namespace Circuit.Elements {
    class PotElm : CircuitElm {
        const int FLAG_SHOW_VALUES = 1;

        const int V_L = 0;
        const int V_R = 1;
        const int V_S = 2;

        double position;
        double maxResistance;
        double resistance1;
        double resistance2;
        double current1;
        double current2;
        double current3;
        double curcount1;
        double curcount2;
        double curcount3;
        TrackBar slider;
        Label label;

        Point post3;
        Point corner2;
        Point arrowPoint;
        Point midpoint;
        Point arrow1;
        Point arrow2;
        Point ps1;
        Point ps2;
        Point ps3;
        Point ps4;
        int bodyLen;

        string sliderText;

        public PotElm(int xx, int yy) : base(xx, yy) {
            setup();
            maxResistance = 1000;
            position = .5;
            sliderText = "Resistance";
            mFlags = FLAG_SHOW_VALUES;
            createSlider();
        }

        public PotElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            maxResistance = st.nextTokenDouble();
            position = st.nextTokenDouble();
            sliderText = st.nextToken();
            while (st.hasMoreTokens()) {
                sliderText += ' ' + st.nextToken();
            }
            createSlider();
        }

        public override int PostCount { get { return 3; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.POT; } }

        protected override string dump() {
            return maxResistance + " " + position + " " + sliderText;
        }

        void setup() { }

        public override Point GetPost(int n) {
            return (n == 0) ? mPoint1 : (n == 1) ? mPoint2 : post3;
        }

        void createSlider() {
            ControlPanel.AddSlider(label = new Label() {
                TextAlign = ContentAlignment.BottomLeft,
                Text = sliderText
            });
            int value = (int)(position * 100);
            ControlPanel.AddSlider(slider = new TrackBar() {
                Minimum = 0,
                Maximum = 100,
                SmallChange = 1,
                LargeChange = 5,
                TickFrequency = 10,
                Value = value,
                Width = 175
            });
            slider.ValueChanged += new EventHandler((s, e) => { execute(); });
        }

        void execute() {
            SetPoints();
            Sim.NeedAnalyze();
        }

        public override void Delete() {
            ControlPanel.RemoveSlider(label);
            ControlPanel.RemoveSlider(slider);
            base.Delete();
        }

        public override void SetPoints() {
            base.SetPoints();
            int offset = 0;
            int myLen = 0;
            if (Math.Abs(mDx) > Math.Abs(mDy)) {
                myLen = 2 * CirSim.GRID_SIZE * Math.Sign(mDx) * (
                    (Math.Abs(mDx) + 2 * CirSim.GRID_SIZE - 1) / (2 * CirSim.GRID_SIZE));
                mPoint2.X = mPoint1.X + myLen;
                offset = (mDx < 0) ? mDy : -mDy;
                mPoint2.Y = mPoint1.Y;
            } else {
                myLen = 2 * CirSim.GRID_SIZE * Math.Sign(mDy) * (
                    (Math.Abs(mDy) + 2 * CirSim.GRID_SIZE - 1) / (2 * CirSim.GRID_SIZE));
                if (mDy != 0) {
                    mPoint2.Y = mPoint1.Y + myLen;
                    offset = (mDy > 0) ? mDx : -mDx;
                    mPoint2.X = mPoint1.X;
                }
            }
            if (offset < CirSim.GRID_SIZE) {
                offset = CirSim.GRID_SIZE;
            }
            mLen = Utils.Distance(mPoint1, mPoint2);
            int bodyLen = 38;
            calcLeads(bodyLen);
            position = slider.Value * .0099 + .005;
            int soff = (int)((position - .5) * bodyLen);
            post3 = Utils.InterpPoint(mPoint1, mPoint2, .5, offset);
            corner2 = Utils.InterpPoint(mPoint1, mPoint2, soff / mLen + .5, offset);
            arrowPoint = Utils.InterpPoint(mPoint1, mPoint2, soff / mLen + .5, 8 * Math.Sign(offset));
            midpoint = Utils.InterpPoint(mPoint1, mPoint2, soff / mLen + .5);
            double clen = Math.Abs(offset) - 8;
            Utils.InterpPoint(corner2, arrowPoint, ref arrow1, ref arrow2, (clen - 8) / clen, 4);
        }

        public override void Draw(CustomGraphics g) {
            const int segments = 16;
            const int hs = 5;
            int i;
            double vl = Volts[V_L];
            double vr = Volts[V_R];
            double vs = Volts[V_S];
            setBbox(mPoint1, mPoint2, hs);
            draw2Leads(g);

            double segf = 1.0 / segments;
            int divide = (int)(segments * position);

            if (ControlPanel.ChkUseAnsiSymbols.Checked) {
                /* draw zigzag */
                int oy = 0;
                int ny;
                for (i = 0; i != segments; i++) {
                    switch (i & 3) {
                    case 0: ny = hs; break;
                    case 2: ny = -hs; break;
                    default: ny = 0; break;
                    }
                    double v = vl + (vs - vl) * i / divide;
                    if (i >= divide) {
                        v = vs + (vr - vs) * (i - divide) / (segments - divide);
                    }
                    Utils.InterpPoint(mLead1, mLead2, ref ps1, i * segf, oy);
                    Utils.InterpPoint(mLead1, mLead2, ref ps2, (i + 1) * segf, ny);
                    g.DrawThickLine(getVoltageColor(v), ps1, ps2);
                    oy = ny;
                }
            } else {
                /* draw rectangle */
                Utils.InterpPoint(mLead1, mLead2, ref ps1, ref ps2, 0, hs);
                g.ThickLineColor = getVoltageColor(vl);
                g.DrawThickLine(ps1, ps2);
                for (i = 0; i != segments; i++) {
                    double v = vl + (vs - vl) * i / divide;
                    if (i >= divide) {
                        v = vs + (vr - vs) * (i - divide) / (segments - divide);
                    }
                    Utils.InterpPoint(mLead1, mLead2, ref ps1, ref ps2, i * segf, hs);
                    Utils.InterpPoint(mLead1, mLead2, ref ps3, ref ps4, (i + 1) * segf, hs);
                    g.ThickLineColor = getVoltageColor(v);
                    g.DrawThickLine(ps1, ps3);
                    g.DrawThickLine(ps2, ps4);
                }
                Utils.InterpPoint(mLead1, mLead2, ref ps1, ref ps2, 1, hs);
                g.DrawThickLine(ps1, ps2);
            }

            g.ThickLineColor = getVoltageColor(vs);
            g.DrawThickLine(post3, corner2);
            g.DrawThickLine(corner2, arrowPoint);
            g.DrawThickLine(arrow1, arrowPoint);
            g.DrawThickLine(arrow2, arrowPoint);
            curcount1 = updateDotCount(current1, curcount1);
            curcount2 = updateDotCount(current2, curcount2);
            curcount3 = updateDotCount(current3, curcount3);
            if (Sim.DragElm != this) {
                drawDots(g, mPoint1, midpoint, curcount1);
                drawDots(g, mPoint2, midpoint, curcount2);
                drawDots(g, post3, corner2, curcount3);
                drawDots(g, corner2, midpoint, curcount3 + Utils.Distance(post3, corner2));
            }
            drawPosts(g);

            if (ControlPanel.ChkShowValues.Checked && resistance1 > 0 && (mFlags & FLAG_SHOW_VALUES) != 0) {
                /* check for vertical pot with 3rd terminal on left */
                bool reverseY = (post3.X < mLead1.X && mLead1.X == mLead2.X);
                /* check for horizontal pot with 3rd terminal on top */
                bool reverseX = (post3.Y < mLead1.Y && mLead1.X != mLead2.X);
                /* check if we need to swap texts (if leads are reversed, e.g. drawn right to left) */
                bool rev = (mLead1.X == mLead2.X && mLead1.Y < mLead2.Y) || (mLead1.Y == mLead2.Y && mLead1.X > mLead2.X);

                /* draw units */
                string s1 = Utils.ShortUnitText(rev ? resistance2 : resistance1, "");
                string s2 = Utils.ShortUnitText(rev ? resistance1 : resistance2, "");
                int txtHeightH = CustomGraphics.FontText.Height / 2;
                int txtWidth1 = (int)g.GetTextSize(s1).Width;
                int txtWidth2 = (int)g.GetTextSize(s2).Width;

                /* vertical? */
                if (mLead1.X == mLead2.X) {
                    g.DrawLeftTopText(s1, !reverseY ? arrowPoint.X : arrowPoint.X - txtWidth1, Math.Min(arrow1.Y, arrow2.Y) + 4 * txtHeightH);
                    g.DrawLeftTopText(s2, !reverseY ? arrowPoint.X : arrowPoint.X - txtWidth2, Math.Max(arrow1.Y, arrow2.Y) - txtHeightH);
                } else {
                    g.DrawLeftTopText(s1, Math.Min(arrow1.X, arrow2.X) - txtWidth1, !reverseX ? (arrowPoint.Y + txtHeightH + 10) : arrowPoint.Y);
                    g.DrawLeftTopText(s2, Math.Max(arrow1.X, arrow2.X), !reverseX ? (arrowPoint.Y + txtHeightH + 10) : arrowPoint.Y);
                }
            }
        }

        /* draw component values (number of resistor ohms, etc).  hs = offset */
        void drawValues(CustomGraphics g, string s, Point pt, int hs) {
            if (s == null) {
                return;
            }
            int w = (int)g.GetLTextSize(s).Width;
            int ya = CustomGraphics.FontText.Height / 2;
            int xc = pt.X;
            int yc = pt.Y;
            int dpx = hs;
            int dpy = 0;
            if (mLead1.X != mLead2.X) {
                dpx = 0;
                dpy = -hs;
            }
            Console.WriteLine("dv " + dpx + " " + w);
            if (dpx == 0) {
                g.DrawLeftText(s, xc - w / 2, yc - Math.Abs(dpy) - 2);
            } else {
                int xx = xc + Math.Abs(dpx) + 2;
                g.DrawLeftText(s, xx, yc + dpy + ya);
            }
        }

        public override void Reset() {
            curcount1 = curcount2 = curcount3 = 0;
            base.Reset();
        }

        protected override void calculateCurrent() {
            if (resistance1 == 0) {
                return; /* avoid NaN */
            }
            current1 = (Volts[V_L] - Volts[V_S]) / resistance1;
            current2 = (Volts[V_R] - Volts[V_S]) / resistance2;
            current3 = -current1 - current2;
        }

        public override double GetCurrentIntoNode(int n) {
            if (n == 0) {
                return -current1;
            }
            if (n == 1) {
                return -current2;
            }
            return -current3;
        }

        public override void Stamp() {
            resistance1 = maxResistance * position;
            resistance2 = maxResistance * (1 - position);
            mCir.StampResistor(Nodes[0], Nodes[2], resistance1);
            mCir.StampResistor(Nodes[2], Nodes[1], resistance2);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "potentiometer";
            arr[1] = "Vd = " + Utils.VoltageDText(VoltageDiff);
            arr[2] = "R1 = " + Utils.UnitText(resistance1, CirSim.OHM_TEXT);
            arr[3] = "R2 = " + Utils.UnitText(resistance2, CirSim.OHM_TEXT);
            arr[4] = "I1 = " + Utils.CurrentDText(current1);
            arr[5] = "I2 = " + Utils.CurrentDText(current2);
        }

        public override ElementInfo GetElementInfo(int n) {
            /* ohmString doesn't work here on linux */
            if (n == 0) {
                return new ElementInfo("Resistance (ohms)", maxResistance, 0, 0);
            }
            if (n == 1) {
                var ei = new ElementInfo("Slider Text", 0, -1, -1);
                ei.Text = sliderText;
                return ei;
            }
            if (n == 2) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "Show Values";
                ei.CheckBox.Checked = (mFlags & FLAG_SHOW_VALUES) != 0;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                maxResistance = ei.Value;
            }
            if (n == 1) {
                sliderText = ei.Textf.Text;
                label.Text = sliderText;
                ControlPanel.SetSliderPanelHeight();
            }
            if (n == 2) {
                mFlags = ei.ChangeFlag(mFlags, FLAG_SHOW_VALUES);
            }
        }

        public override void SetMouseElm(bool v) {
            base.SetMouseElm(v);
        }
    }
}
