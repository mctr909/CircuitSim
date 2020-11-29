using System;
using System.Windows.Forms;
using System.Drawing;

namespace Circuit.Elements {
    class PotElm : CircuitElm {
        const int FLAG_SHOW_VALUES = 1;
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
        Point ps3, ps4;
        int bodyLen;

        string sliderText;

        public PotElm(int xx, int yy) : base(xx, yy) {
            setup();
            maxResistance = 1000;
            position = .5;
            sliderText = "Resistance";
            flags = FLAG_SHOW_VALUES;
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

        void setup() { }

        public override int getPostCount() { return 3; }

        public override DUMP_ID getDumpType() { return DUMP_ID.POT; }

        public override Point getPost(int n) {
            return (n == 0) ? point1 : (n == 1) ? point2 : post3;
        }

        public override string dump() {
            return base.dump()
                + " " + maxResistance
                + " " + position
                + " " + sliderText;
        }

        void createSlider() {
            sim.addWidgetToVerticalPanel(label = new Label() { Text = sliderText });
            int value = (int)(position * 100);
            sim.addWidgetToVerticalPanel(slider = new TrackBar() {
                Minimum = 0,
                Maximum = 101,
                SmallChange = 1,
                LargeChange = 5,
                TickFrequency = 10,
                Value = value,
                Width = 100
            });
            slider.MouseWheel += new MouseEventHandler((s, e) => { onMouseWheel(s, e); });
        }

        public void execute() {
            sim.analyzeFlag = true;
            setPoints();
        }

        public override void delete() {
            sim.removeWidgetFromVerticalPanel(label);
            sim.removeWidgetFromVerticalPanel(slider);
            base.delete();
        }

        public override void setPoints() {
            base.setPoints();
            int offset = 0;
            int myLen = 0;
            if (Math.Abs(dx) > Math.Abs(dy)) {
                myLen = 2 * sim.gridSize * Math.Sign(dx) * (((Math.Abs(dx)) + 2 * sim.gridSize - 1) / (2 * sim.gridSize));
                point2.X = point1.X + myLen;
                offset = (dx < 0) ? dy : -dy;
                point2.Y = point1.Y;
            } else {
                myLen = 2 * sim.gridSize * Math.Sign(dy) * (((Math.Abs(dy)) + 2 * sim.gridSize - 1) / (2 * sim.gridSize));
                if (dy != 0) {
                    point2.Y = point1.Y + myLen;
                    offset = (dy > 0) ? dx : -dx;
                    point2.X = point1.X;
                }
            }
            if (offset == 0) {
                offset = sim.gridSize;
            }
            dn = distance(point1, point2);
            int bodyLen = 32;
            calcLeads(bodyLen);
            position = slider.Value * .0099 + .005;
            int soff = (int)((position - .5) * bodyLen);
            post3 = interpPoint(point1, point2, .5, offset);
            corner2 = interpPoint(point1, point2, soff / dn + .5, offset);
            arrowPoint = interpPoint(point1, point2, soff / dn + .5, 8 * Math.Sign(offset));
            midpoint = interpPoint(point1, point2, soff / dn + .5);
            arrow1 = new Point();
            arrow2 = new Point();
            double clen = Math.Abs(offset) - 8;
            interpPoint(corner2, arrowPoint, ref arrow1, ref arrow2, (clen - 8) / clen, 8);
            ps3 = new Point();
            ps4 = new Point();
        }

        public override void draw(Graphics g) {
            int segments = 12;
            int i;
            int hs = sim.chkAnsiResistorCheckItem.Checked ? 6 : 5;
            double v1 = volts[0];
            double v2 = volts[1];
            double v3 = volts[2];
            setBbox(point1, point2, hs);
            draw2Leads(g);

            double segf = 1.0 / segments;
            int divide = (int)(segments * position);

            if (sim.chkAnsiResistorCheckItem.Checked) {
                /* draw zigzag */
                int oy = 0;
                int ny;
                for (i = 0; i != segments; i++) {
                    switch (i & 3) {
                    case 0: ny = hs; break;
                    case 2: ny = -hs; break;
                    default: ny = 0; break;
                    }
                    double v = v1 + (v3 - v1) * i / divide;
                    if (i >= divide) {
                        v = v3 + (v2 - v3) * (i - divide) / (segments - divide);
                    }
                    interpPoint(lead1, lead2, ref ps1, i * segf, oy);
                    interpPoint(lead1, lead2, ref ps2, (i + 1) * segf, ny);
                    drawThickLine(g, getVoltageColor(v), ps1, ps2);
                    oy = ny;
                }
            } else {
                /* draw rectangle */
                PEN_THICK_LINE.Color = getVoltageColor(v1);
                interpPoint(lead1, lead2, ref ps1, ref ps2, 0, hs);
                drawThickLine(g, ps1, ps2);
                for (i = 0; i != segments; i++) {
                    double v = v1 + (v3 - v1) * i / divide;
                    if (i >= divide) {
                        v = v3 + (v2 - v3) * (i - divide) / (segments - divide);
                    }
                    interpPoint(lead1, lead2, ref ps1, ref ps2, i * segf, hs);
                    interpPoint(lead1, lead2, ref ps3, ref ps4, (i + 1) * segf, hs);
                    PEN_THICK_LINE.Color = getVoltageColor(v);
                    drawThickLine(g, ps1, ps3);
                    drawThickLine(g, ps2, ps4);
                }
                interpPoint(lead1, lead2, ref ps1, ref ps2, 1, hs);
                drawThickLine(g, ps1, ps2);
            }

            PEN_THICK_LINE.Color = getVoltageColor(v3);
            drawThickLine(g, post3, corner2);
            drawThickLine(g, corner2, arrowPoint);
            drawThickLine(g, arrow1, arrowPoint);
            drawThickLine(g, arrow2, arrowPoint);
            curcount1 = updateDotCount(current1, curcount1);
            curcount2 = updateDotCount(current2, curcount2);
            curcount3 = updateDotCount(current3, curcount3);
            if (sim.dragElm != this) {
                drawDots(g, point1, midpoint, curcount1);
                drawDots(g, point2, midpoint, curcount2);
                drawDots(g, post3, corner2, curcount3);
                drawDots(g, corner2, midpoint, curcount3 + distance(post3, corner2));
            }
            drawPosts(g);

            if (sim.chkShowValuesCheckItem.Checked && resistance1 > 0 && (flags & FLAG_SHOW_VALUES) != 0) {
                /* check for vertical pot with 3rd terminal on left */
                bool reverseY = (post3.X < lead1.X && lead1.X == lead2.X);
                /* check for horizontal pot with 3rd terminal on top */
                bool reverseX = (post3.Y < lead1.Y && lead1.X != lead2.X);
                /* check if we need to swap texts (if leads are reversed, e.g. drawn right to left) */
                bool rev = (lead1.X == lead2.X && lead1.Y < lead2.Y) || (lead1.Y == lead2.Y && lead1.X > lead2.X);

                /* draw units */
                string s1 = getShortUnitText(rev ? resistance2 : resistance1, "");
                string s2 = getShortUnitText(rev ? resistance1 : resistance2, "");
                int ya = FONT_TEXT.Height / 2;
                int w = (int)g.MeasureString(s1, FONT_TEXT).Width;

                /* vertical? */
                if (lead1.X == lead2.X) {
                    g.DrawString(s1, FONT_TEXT, BRUSH_TEXT, !reverseY ? arrowPoint.X + 2 : arrowPoint.X - 2 - w, Math.Max(arrow1.Y, arrow2.Y) + 5 + ya);
                } else {
                    g.DrawString(s1, FONT_TEXT, BRUSH_TEXT, Math.Min(arrow1.X, arrow2.X) - 2 - w, !reverseX ? arrowPoint.Y + 4 + ya : arrowPoint.Y - 4);
                }

                w = (int)g.MeasureString(s2, FONT_TEXT).Width;
                if (lead1.X == lead2.X) {
                    g.DrawString(s2, FONT_TEXT, BRUSH_TEXT, !reverseY ? arrowPoint.X + 2 : arrowPoint.X - 2 - w, Math.Min(arrow1.Y, arrow2.Y) - 3);
                } else {
                    g.DrawString(s2, FONT_TEXT, BRUSH_TEXT, Math.Max(arrow1.X, arrow2.X) + 2, !reverseX ? arrowPoint.Y + 4 + ya : arrowPoint.Y - 4);
                }
            }
        }

        /* draw component values (number of resistor ohms, etc).  hs = offset */
        void drawValues(Graphics g, string s, Point pt, int hs) {
            if (s == null) {
                return;
            }
            int w = (int)g.MeasureString(s, FONT_TEXT).Width;
            int ya = FONT_TEXT.Height / 2;
            int xc = pt.X;
            int yc = pt.Y;
            int dpx = hs;
            int dpy = 0;
            if (lead1.X != lead2.X) {
                dpx = 0;
                dpy = -hs;
            }
            Console.WriteLine("dv " + dpx + " " + w);
            if (dpx == 0) {
                g.DrawString(s, FONT_TEXT, BRUSH_TEXT, xc - w / 2, yc - Math.Abs(dpy) - 2);
            } else {
                int xx = xc + Math.Abs(dpx) + 2;
                g.DrawString(s, FONT_TEXT, BRUSH_TEXT, xx, yc + dpy + ya);
            }
        }

        public override void reset() {
            curcount1 = curcount2 = curcount3 = 0;
            base.reset();
        }

        public override void calculateCurrent() {
            if (resistance1 == 0) {
                return; /* avoid NaN */
            }
            current1 = (volts[0] - volts[2]) / resistance1;
            current2 = (volts[1] - volts[2]) / resistance2;
            current3 = -current1 - current2;
        }

        public override double getCurrentIntoNode(int n) {
            if (n == 0) {
                return -current1;
            }
            if (n == 1) {
                return -current2;
            }
            return -current3;
        }

        public override void stamp() {
            resistance1 = maxResistance * position;
            resistance2 = maxResistance * (1 - position);
            cir.stampResistor(nodes[0], nodes[2], resistance1);
            cir.stampResistor(nodes[2], nodes[1], resistance2);
        }

        public override void getInfo(string[] arr) {
            arr[0] = "potentiometer";
            arr[1] = "Vd = " + getVoltageDText(getVoltageDiff());
            arr[2] = "R1 = " + getUnitText(resistance1, CirSim.ohmString);
            arr[3] = "R2 = " + getUnitText(resistance2, CirSim.ohmString);
            arr[4] = "I1 = " + getCurrentDText(current1);
            arr[5] = "I2 = " + getCurrentDText(current2);
        }

        public override EditInfo getEditInfo(int n) {
            /* ohmString doesn't work here on linux */
            if (n == 0) {
                return new EditInfo("Resistance (ohms)", maxResistance, 0, 0);
            }
            if (n == 1) {
                var ei = new EditInfo("Slider Text", 0, -1, -1);
                ei.text = sliderText;
                return ei;
            }
            if (n == 2) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.checkbox = new CheckBox();
                ei.checkbox.Text = "Show Values";
                ei.checkbox.Checked = (flags & FLAG_SHOW_VALUES) != 0;
                return ei;
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (n == 0) {
                maxResistance = ei.value;
            }
            if (n == 1) {
                sliderText = ei.textf.Text;
                label.Text = sliderText;
                sim.setiFrameHeight();
            }
            if (n == 2) {
                flags = ei.changeFlag(flags, FLAG_SHOW_VALUES);
            }
        }

        public override void setMouseElm(bool v) {
            base.setMouseElm(v);
        }

        public virtual void onMouseWheel(object s, MouseEventArgs e) { }
    }
}
