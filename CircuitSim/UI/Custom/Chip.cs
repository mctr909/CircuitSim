using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Custom;

namespace Circuit.UI.Custom {
    abstract class Chip : BaseUI {
        const int FLAG_SMALL = 1;
        protected const int FLAG_FLIP_X = 1024;
        protected const int FLAG_FLIP_Y = 2048;

        public const int SIDE_N = 0;
        public const int SIDE_S = 1;
        public const int SIDE_W = 2;
        public const int SIDE_E = 3;

        int csize;
        int cspc;
        int cspc2;

        Point[] rectPoints;
        Point[] clockPoints;
        public int sizeX;
        public int sizeY;

        public class Pin {
            Chip mElm;
            public int pos;
            public int side;
            public string text;

            public Point post;
            public Point stub;
            public Point textloc { get; private set; }

            public int voltSource;
            public Point bubblePos;

            public bool lineOver;
            public bool bubble;
            public bool clock;
            public bool output;
            public bool value;
            public bool state = true;
            public bool selected;
            public double curcount;
            public double current;

            public Pin(Chip elm, int p, int s, string t) {
                mElm = elm;
                pos = p;
                side = s;
                text = t;
            }

            public void setPoint(int px, int py, int dx, int dy, int dax, int day, int sx, int sy) {
                if ((mElm.DumpInfo.Flags & FLAG_FLIP_X) != 0) {
                    dx = -dx;
                    dax = -dax;
                    px += mElm.cspc2 * (mElm.sizeX - 1);
                    sx = -sx;
                }
                if ((mElm.DumpInfo.Flags & FLAG_FLIP_Y) != 0) {
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
                    bubblePos = new Point(xa + dax * 10 * mElm.csize, ya + day * 10 * mElm.csize);
                }
                if (clock) {
                    mElm.clockPoints = new Point[3];
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

        public Chip(Point pos) : base(pos) {
            mNoDiagonal = true;
            setSize(1);
        }

        public Chip(Point p1, Point p2, int f) : base(p1, p2, f) {
            mNoDiagonal = true;
            setSize(1);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.INVALID; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmChip)Elm;
            if (ce.NeedsBits()) {
                optionList.Add(ce.Bits);
            }
            for (int i = 0; i != ce.PostCount; i++) {
                if (ce.Pins[i].state) {
                    optionList.Add(ce.Volts[i].ToString("0.000000"));
                }
            }
        }

        protected void setSize(int s) {
            csize = s;
            cspc = 8 * s;
            cspc2 = cspc * 2;
            DumpInfo.Flags &= ~FLAG_SMALL;
            DumpInfo.Flags |= (s == 1) ? FLAG_SMALL : 0;
        }

        public override void Draw(CustomGraphics g) {
            drawChip(g);
        }

        public void drawChip(CustomGraphics g) {
            var ce = (ElmChip)Elm;
            for (int i = 0; i != ce.PostCount; i++) {
                var p = ce.Pins[i];
                var a = p.post;
                var b = p.stub;
                drawLead(a, b);
                updateDotCount(p.current, ref p.curcount);
                drawDots(b, a, p.curcount);
                if (p.bubble) {
                    g.DrawColor = Color.White;
                    g.DrawCircle(p.bubblePos, 1);
                    g.DrawColor = CustomGraphics.LineColor;
                    g.DrawCircle(p.bubblePos, 3);
                }
                g.DrawColor = p.selected ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
                var bkSize = CustomGraphics.TextSize;
                CustomGraphics.TextSize = 12 * csize;
                while (true) {
                    int txtW = (int)g.GetTextSize(p.text).Width;
                    /* scale font down if it's too big */
                    if (12 * csize < txtW) {
                        CustomGraphics.TextSize -= 0.5f;
                        continue;
                    }
                    g.DrawCenteredText(p.text, p.textloc.X, p.textloc.Y);
                    if (p.lineOver) {
                        int ya = p.textloc.Y;
                        g.DrawLine(p.textloc.X - txtW / 2, ya, p.textloc.X + txtW / 2, ya);
                    }
                    break;
                }
                CustomGraphics.TextSize = bkSize;
            }
            g.DrawColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            g.DrawPolygon(rectPoints);
            if (clockPoints != null) {
                g.DrawPolygon(clockPoints);
            }
            drawPosts();
        }

        public override void Drag(Point pos) {
            pos = CirSimForm.SnapGrid(pos);
            if (pos.X < DumpInfo.P1.X) {
                pos.X = DumpInfo.P1.X;
                pos.Y = DumpInfo.P1.Y;
            } else {
                DumpInfo.SetPosition(
                    DumpInfo.P1.X, pos.Y,
                    CirSimForm.SnapGrid(pos.X), pos.Y
                );
            }
            SetPoints();
        }

        public override void SetPoints() {
            var ce = (ElmChip)Elm;
            clockPoints = null;
            int hs = cspc;
            int x0 = DumpInfo.P1.X + cspc2;
            int y0 = DumpInfo.P1.Y;
            var r = new Point(x0 - cspc, y0 - cspc);
            int xs = sizeX * cspc2;
            int ys = sizeY * cspc2;
            rectPoints = new Point[] {
                new Point(r.X, r.Y),
                new Point(r.X + xs, r.Y),
                new Point(r.X + xs, r.Y + ys),
                new Point(r.X, r.Y + ys)
            };
            DumpInfo.SetBbox(r, rectPoints[2]);
            for (int i = 0; i != ce.PostCount; i++) {
                var p = ce.Pins[i];
                switch (p.side) {
                case SIDE_N:
                    p.setPoint(x0, y0, 1, 0, 0, -1, 0, 0);
                    break;
                case SIDE_S:
                    p.setPoint(x0, y0, 1, 0, 0, 1, 0, ys - cspc2);
                    break;
                case SIDE_W:
                    p.setPoint(x0, y0, 0, 1, -1, 0, 0, 0);
                    break;
                case SIDE_E:
                    p.setPoint(x0, y0, 0, 1, 1, 0, xs - cspc2, 0);
                    break;
                }
            }
        }

        /* see if we can move pin to position xp, yp, and return the new position */
        public bool getPinPos(int xp, int yp, int pin, int[] pos) {
            var ce = (ElmChip)Elm;
            int x0 = DumpInfo.P1.X + cspc2;
            int y0 = DumpInfo.P1.Y;
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

            for (int i = 0; i != ce.PostCount; i++) {
                if (pin == i) {
                    continue;
                }
                if (ce.Pins[i].overlaps(pos[0], pos[1])) {
                    return false;
                }
            }
            return true;
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmChip)Elm;
            arr[0] = getChipName();
            int a = 1;
            for (int i = 0; i != ce.PostCount; i++) {
                var p = ce.Pins[i];
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
                arr[a] += t + " = " + Utils.VoltageText(ce.Volts[i]);
                if (i % 2 == 1) {
                    a++;
                }
            }
        }

        string getChipName() { return "chip"; }

        public override ElementInfo GetElementInfo(int r, int c) {
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("Flip X", (DumpInfo.Flags & FLAG_FLIP_X) != 0);
            }
            if (r == 1) {
                return new ElementInfo("Flip Y", (DumpInfo.Flags & FLAG_FLIP_Y) != 0);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            if (n == 0) {
                if (ei.CheckBox.Checked) {
                    DumpInfo.Flags |= FLAG_FLIP_X;
                } else {
                    DumpInfo.Flags &= ~FLAG_FLIP_X;
                }
                SetPoints();
            }
            if (n == 1) {
                if (ei.CheckBox.Checked) {
                    DumpInfo.Flags |= FLAG_FLIP_Y;
                } else {
                    DumpInfo.Flags &= ~FLAG_FLIP_Y;
                }
                SetPoints();
            }
        }
    }
}
