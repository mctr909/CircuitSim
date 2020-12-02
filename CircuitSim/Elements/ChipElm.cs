﻿using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class ChipElm : CircuitElm {
        const int FLAG_SMALL = 1;
        const int FLAG_FLIP_X = 1024;
        const int FLAG_FLIP_Y = 2048;

        public const int SIDE_N = 0;
        public const int SIDE_S = 1;
        public const int SIDE_W = 2;
        public const int SIDE_E = 3;

        int csize;
        int cspc;
        int cspc2;
        protected int bits;

        Point[] rectPoints;
        Point[] clockPoints;
        public Pin[] pins;
        public int sizeX;
        public int sizeY;
        protected bool lastClock;

        public class Pin {
            ChipElm mElm;
            public int pos;
            public int side;
            public string text;

            public Point post;
            public Point stub;
            public Point textloc { get; private set; }

            public int voltSource;
            public int bubbleX;
            public int bubbleY;

            public bool lineOver;
            public bool bubble;
            public bool clock;
            public bool output;
            public bool value;
            public bool state;
            public bool selected;
            public double curcount;
            public double current;

            public Pin(ChipElm elm, int p, int s, string t) {
                mElm = elm;
                pos = p;
                side = s;
                text = t;
            }

            public void setPoint(int px, int py, int dx, int dy, int dax, int day, int sx, int sy) {
                if ((mElm.mFlags & FLAG_FLIP_X) != 0) {
                    dx = -dx;
                    dax = -dax;
                    px += mElm.cspc2 * (mElm.sizeX - 1);
                    sx = -sx;
                }
                if ((mElm.mFlags & FLAG_FLIP_Y) != 0) {
                    dy = -dy;
                    day = -day;
                    py += mElm.cspc2 * (mElm.sizeY - 1);
                    sy = -sy;
                }
                int xa = px + mElm.cspc2 * dx * pos + sx;
                int ya = py + mElm.cspc2 * dy * pos + sy;
                post = new Point(xa + dax * mElm.cspc2, ya + day * mElm.cspc2);
                stub = new Point(xa + dax * mElm.cspc, ya + day * mElm.cspc);
                textloc = new Point(xa, ya);
                if (bubble) {
                    bubbleX = xa + dax * 10 * mElm.csize;
                    bubbleY = ya + day * 10 * mElm.csize;
                }
                if (clock) {
                    mElm.clockPoints = new Point [3];
                    mElm.clockPoints[0] = new Point(
                        xa + dax * mElm.cspc - dx * mElm.cspc / 2,
                        ya + day * mElm.cspc - dy * mElm.cspc / 2
                    );
                    mElm.clockPoints[1] = new Point(xa, ya);
                    mElm.clockPoints[2] = new Point(
                        xa + dax * mElm.cspc + dx * mElm.cspc / 2,
                        ya + day * mElm.cspc + dy * mElm.cspc / 2
                    );
                }
            }

            /* convert position, side to a grid position (0=top left) so we can detect overlaps */
            int toGrid(int p, int s) {
                if (s == SIDE_N) {
                    return p;
                }
                if (s == SIDE_S) {
                    return p + mElm.sizeX * (mElm.sizeY - 1);
                }
                if (s == SIDE_W) {
                    return p * mElm.sizeX;
                }
                if (s == SIDE_E) {
                    return p * mElm.sizeX + mElm.sizeX - 1;
                }
                return -1;
            }

            public bool overlaps(int p, int s) {
                int g = toGrid(p, s);
                if (g == -1) {
                    return true;
                }
                return toGrid(pos, side) == g;
            }

            public void fixName() {
                if (text.StartsWith("/")) {
                    text = text.Substring(1);
                    lineOver = true;
                }
                if (text.CompareTo("clk") == 0) {
                    text = "";
                    clock = true;
                }
            }
        }

        public ChipElm(int xx, int yy) : base(xx, yy) {
            if (needsBits()) {
                bits = (this is RingCounterElm) ? 10 : 4;
            }
            mNoDiagonal = true;
            setupPins();
            setSize(Sim.chkSmallGridCheckItem.Checked ? 1 : 2);
        }

        public ChipElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            if (needsBits()) {
                bits = st.nextTokenInt();
            }
            mNoDiagonal = true;
            setupPins();
            setSize((f & FLAG_SMALL) != 0 ? 1 : 2);
            int i;
            for (i = 0; i != getPostCount(); i++) {
                if (pins == null) {
                    Volts[i] = st.nextTokenDouble();
                } else if (pins[i].state) {
                    Volts[i] = st.nextTokenDouble();
                    pins[i].value = Volts[i] > 2.5;
                }
            }
        }

        protected override string dump() {
            string s = "";
            if (needsBits()) {
                s = string.Join(" ", s, bits);
            }
            for (int i = 0; i != getPostCount(); i++) {
                if (pins[i].state) {
                    s = string.Join(" ", s, Volts[i]);
                }
            }
            return s;
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.INVALID; }

        bool needsBits() { return false; }

        protected void setSize(int s) {
            csize = s;
            cspc = 8 * s;
            cspc2 = cspc * 2;
            mFlags &= ~FLAG_SMALL;
            mFlags |= (s == 1) ? FLAG_SMALL : 0;
        }

        public virtual void setupPins() { }

        public override void draw(Graphics g) {
            drawChip(g);
        }

        public void drawChip(Graphics g) {
            int i;
            for (i = 0; i != getPostCount(); i++) {
                var p = pins[i];
                getVoltageColor(Volts[i]);
                var a = p.post;
                var b = p.stub;
                drawThickLine(g, a, b);
                p.curcount = updateDotCount(p.current, p.curcount);
                drawDots(g, b, a, p.curcount);
                if (p.bubble) {
                    PenThickLine.Color = Sim.chkPrintableCheckItem.Checked ? Color.White : Color.Black;
                    drawThickCircle(g, p.bubbleX, p.bubbleY, 1);
                    PenThickLine.Color = LightGrayColor;
                    drawThickCircle(g, p.bubbleX, p.bubbleY, 3);
                }
                PenThickLine.Color  = p.selected ? SelectColor : LightGrayColor;
                int fsz = 12 * csize;
                var font = FONT_TEXT;
                while (true) {
                    int sw = (int)g.MeasureString(p.text, font).Width;
                    /* scale font down if it's too big */
                    if (sw > 12 * csize) {
                        fsz--;
                        font = new Font(FONT_TEXT.Name, fsz);
                        continue;
                    }
                    g.DrawString(p.text, font, BrushText, p.textloc.X - sw / 2, p.textloc.Y);
                    if (p.lineOver) {
                        int ya = p.textloc.Y;
                        drawThickLine(g, p.textloc.X - sw / 2, ya, p.textloc.X + sw / 2, ya);
                    }
                    break;
                }
            }
            PenThickLine.Color = needsHighlight() ? SelectColor : LightGrayColor;
            drawThickPolygon(g, rectPoints);
            if (clockPoints != null) {
                drawThickPolygon(g, clockPoints);
            }
            drawPosts(g);
        }

        public override void drag(int xx, int yy) {
            yy = Sim.snapGrid(yy);
            if (xx < X1) {
                xx = X1;
                yy = Y1;
            } else {
                Y1 = Y2 = yy;
                X2 = Sim.snapGrid(xx);
            }
            setPoints();
        }

        public override void setPoints() {
            clockPoints = null;
            if (X2 - X1 > sizeX * cspc2 && this == Sim.dragElm) {
                setSize(2);
            }
            int hs = cspc;
            int x0 = X1 + cspc2; int y0 = Y1;
            int xr = x0 - cspc;
            int yr = y0 - cspc;
            int xs = sizeX * cspc2;
            int ys = sizeY * cspc2;
            rectPoints = new Point[] {
                new Point(xr, yr),
                new Point(xr + xs, yr),
                new Point(xr + xs, yr + ys),
                new Point(xr, yr + ys)
            };
            setBbox(xr, yr, rectPoints[2].X, rectPoints[2].Y);
            int i;
            for (i = 0; i != getPostCount(); i++) {
                var p = pins[i];
                switch (p.side) {
                case SIDE_N:
                    p.setPoint(x0, y0, 1, 0, 0, -1, 0, 0); break;
                case SIDE_S:
                    p.setPoint(x0, y0, 1, 0, 0, 1, 0, ys - cspc2); break;
                case SIDE_W:
                    p.setPoint(x0, y0, 0, 1, -1, 0, 0, 0); break;
                case SIDE_E:
                    p.setPoint(x0, y0, 0, 1, 1, 0, xs - cspc2, 0); break;
                }
            }
        }

        /* see if we can move pin to position xp, yp, and return the new position */
        public bool getPinPos(int xp, int yp, int pin, int[] pos) {
            int x0 = X1 + cspc2; int y0 = Y1;
            int xr = x0 - cspc;
            int yr = y0 - cspc;
            double xd = (xp - xr) / (double)cspc2 - .5;
            double yd = (yp - yr) / (double)cspc2 - .5;
            if (xd < .25 && yd > 0 && yd < sizeY - 1) {
                pos[0] = (int)Math.Max(Math.Round(yd), 0);
                pos[1] = SIDE_W;
            } else if (xd > sizeX - .75) {
                pos[0] = (int)Math.Min(Math.Round(yd), sizeY - 1);
                pos[1] = SIDE_E;
            } else if (yd < .25) {
                pos[0] = (int)Math.Max(Math.Round(xd), 0);
                pos[1] = SIDE_N;
            } else if (yd > sizeY - .75) {
                pos[0] = (int)Math.Min(Math.Round(xd), sizeX - 1);
                pos[1] = SIDE_S;
            } else {
                return false;
            }

            if (pos[0] < 0) {
                return false;
            }
            if ((pos[1] == SIDE_N || pos[1] == SIDE_S) && pos[0] >= sizeX) {
                return false;
            }
            if ((pos[1] == SIDE_W || pos[1] == SIDE_E) && pos[0] >= sizeY) {
                return false;
            }

            for (int i = 0; i != getPostCount(); i++) {
                if (pin == i) {
                    continue;
                }
                if (pins[i].overlaps(pos[0], pos[1])) {
                    return false;
                }
            }
            return true;
        }

        public override Point getPost(int n) {
            return pins[n].post;
        }

        public override void setVoltageSource(int j, int vs) {
            for (int i = 0; i != getPostCount(); i++) {
                var p = pins[i];
                if (p.output && j-- == 0) {
                    p.voltSource = vs;
                    return;
                }
            }
            Console.WriteLine("setVoltageSource failed for " + this);
        }

        public override void stamp() {
            for (int i = 0; i != getPostCount(); i++) {
                var p = pins[i];
                if (p.output) {
                    Cir.StampVoltageSource(0, Nodes[i], p.voltSource);
                }
            }
        }

        protected virtual void execute() { }

        public override void doStep() {
            int i;
            for (i = 0; i != getPostCount(); i++) {
                var p = pins[i];
                if (!p.output) {
                    p.value = Volts[i] > 2.5;
                }
            }
            execute();
            for (i = 0; i != getPostCount(); i++) {
                var p = pins[i];
                if (p.output) {
                    Cir.UpdateVoltageSource(0, Nodes[i], p.voltSource, p.value ? 5 : 0);
                }
            }
        }

        public override void reset() {
            for (int i = 0; i != getPostCount(); i++) {
                pins[i].value = false;
                pins[i].curcount = 0;
                Volts[i] = 0;
            }
            lastClock = false;
        }

        public override void getInfo(string[] arr) {
            arr[0] = getChipName();
            int a = 1;
            for (int i = 0; i != getPostCount(); i++) {
                var p = pins[i];
                if (arr[a] != null) {
                    arr[a] += "; ";
                } else {
                    arr[a] = "";
                }
                string t = p.text;
                if (p.lineOver) {
                    t += '\'';
                }
                if (p.clock) {
                    t = "Clk";
                }
                arr[a] += t + " = " + getVoltageText(Volts[i]);
                if (i % 2 == 1) {
                    a++;
                }
            }
        }

        public override void setCurrent(int x, double c) {
            for (int i = 0; i != getPostCount(); i++) {
                if (pins[i].output && pins[i].voltSource == x) {
                    pins[i].current = c;
                }
            }
        }

        string getChipName() { return "chip"; }

        public override bool getConnection(int n1, int n2) { return false; }

        public override bool hasGroundConnection(int n1) {
            return pins[n1].output;
        }

        public override double getCurrentIntoNode(int n) {
            return pins[n].current;
        }

        public override EditInfo getEditInfo(int n) {
            if (n == 0) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "Flip X";
                ei.CheckBox.Checked = (mFlags & FLAG_FLIP_X) != 0;
                return ei;
            }
            if (n == 1) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "Flip Y";
                ei.CheckBox.Checked = (mFlags & FLAG_FLIP_Y) != 0;
                return ei;
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (n == 0) {
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_FLIP_X;
                } else {
                    mFlags &= ~FLAG_FLIP_X;
                }
                setPoints();
            }
            if (n == 1) {
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_FLIP_Y;
                } else {
                    mFlags &= ~FLAG_FLIP_Y;
                }
                setPoints();
            }
        }
    }
}