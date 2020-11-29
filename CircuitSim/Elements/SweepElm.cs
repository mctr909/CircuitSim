using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class SweepElm : CircuitElm {
        const int FLAG_LOG = 1;
        const int FLAG_BIDIR = 2;

        const int circleSize = 30;

        double maxV;
        double maxF;
        double minF;
        double sweepTime;
        double frequency;

        double fadd;
        double fmul;
        double freqTime;
        double savedTimeStep;
        double v;
        int dir = 1;

        public SweepElm(int xx, int yy) : base(xx, yy) {
            minF = 20;
            maxF = 4000;
            maxV = 5;
            sweepTime = .1;
            flags = FLAG_BIDIR;
            reset();
        }

        public SweepElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            minF = st.nextTokenDouble();
            maxF = st.nextTokenDouble();
            maxV = st.nextTokenDouble();
            sweepTime = st.nextTokenDouble();
            reset();
        }

        public override DUMP_ID getDumpType() { return DUMP_ID.SWEEP; }

        public override int getPostCount() { return 1; }

        public override string dump() {
            return base.dump()
                + " " + minF
                + " " + maxF
                + " " + maxV
                + " " + sweepTime;
        }

        public override void setPoints() {
            base.setPoints();
            lead1 = interpPoint(point1, point2, 1 - circleSize / dn);
        }

        public override void draw(Graphics g) {
            setBbox(point1, point2, circleSize);

            drawThickLine(g, getVoltageColor(volts[0]), point1, lead1);

            PEN_THICK_LINE.Color = needsHighlight() ? selectColor : Color.Gray;

            int xc = point2.X;
            int yc = point2.Y;
            drawThickCircle(g, xc, yc, circleSize);

            adjustBbox(xc - circleSize, yc - circleSize, xc + circleSize, yc + circleSize);

            int wl = 10;
            int xl = 10;
            long tm = DateTime.Now.ToFileTimeUtc();
            tm %= 2000;
            if (tm > 1000) {
                tm = 2000 - tm;
            }
            double w = 1 + tm * .002;
            if (sim.simIsRunning()) {
                w = 1 + 2 * (frequency - minF) / (maxF - minF);
            }

            int x0 = 0;
            float y0 = 0;
            PEN_LINE.Color = Color.YellowGreen;
            for (int i = -xl; i <= xl; i++) {
                float yy = yc + (float)(.95 * Math.Sin(i * pi * w / xl) * wl);
                if (i == -xl) {
                    x0 = xc + i;
                    y0 = yy;
                } else {
                    drawLine(g, x0, y0, xc + i, yy);
                    x0 = xc + i;
                    y0 = yy;
                }
            }

            if (sim.chkShowValuesCheckItem.Checked) {
                string s = getShortUnitText(frequency, "Hz");
                if (dx == 0 || dy == 0) {
                    drawValues(g, s, circleSize);
                }
            }

            drawPosts(g);
            curcount = updateDotCount(-current, curcount);
            if (sim.dragElm != this) {
                drawDots(g, point1, lead1, curcount);
            }
        }

        public override void stamp() {
            cir.stampVoltageSource(0, nodes[0], voltSource);
        }

        void setParams() {
            if (frequency < minF || frequency > maxF) {
                frequency = minF;
                freqTime = 0;
                dir = 1;
            }
            if ((flags & FLAG_LOG) == 0) {
                fadd = dir * sim.timeStep * (maxF - minF) / sweepTime;
                fmul = 1;
            } else {
                fadd = 0;
                fmul = Math.Pow(maxF / minF, dir * sim.timeStep / sweepTime);
            }
            savedTimeStep = sim.timeStep;
        }

        public override void reset() {
            frequency = minF;
            freqTime = 0;
            dir = 1;
            setParams();
        }

        public override void startIteration() {
            // has timestep been changed?
            if (sim.timeStep != savedTimeStep) {
                setParams();
            }
            v = Math.Sin(freqTime) * maxV;
            freqTime += frequency * 2 * pi * sim.timeStep;
            frequency = frequency * fmul + fadd;
            if (frequency >= maxF && dir == 1) {
                if ((flags & FLAG_BIDIR) != 0) {
                    fadd = -fadd;
                    fmul = 1 / fmul;
                    dir = -1;
                } else {
                    frequency = minF;
                }
            }
            if (frequency <= minF && dir == -1) {
                fadd = -fadd;
                fmul = 1 / fmul;
                dir = 1;
            }
        }

        public override void doStep() {
            cir.updateVoltageSource(0, nodes[0], voltSource, v);
        }

        public override double getVoltageDiff() { return volts[0]; }

        public override int getVoltageSourceCount() { return 1; }

        public override bool hasGroundConnection(int n1) { return true; }

        public override void getInfo(string[] arr) {
            arr[0] = "sweep " + (((flags & FLAG_LOG) == 0) ? "(linear)" : "(log)");
            arr[1] = "I = " + getCurrentDText(getCurrent());
            arr[2] = "V = " + getVoltageText(volts[0]);
            arr[3] = "f = " + getUnitText(frequency, "Hz");
            arr[4] = "range = " + getUnitText(minF, "Hz") + " .. " + getUnitText(maxF, "Hz");
            arr[5] = "time = " + getUnitText(sweepTime, "s");
        }

        public override EditInfo getEditInfo(int n) {
            if (n == 0) {
                return new EditInfo("Min Frequency (Hz)", minF, 0, 0);
            }
            if (n == 1) {
                return new EditInfo("Max Frequency (Hz)", maxF, 0, 0);
            }
            if (n == 2) {
                return new EditInfo("Sweep Time (s)", sweepTime, 0, 0);
            }
            if (n == 3) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.checkbox = new CheckBox() {
                    Text = "Logarithmic",
                    Checked = (flags & FLAG_LOG) != 0
                };
                return ei;
            }
            if (n == 4) {
                return new EditInfo("Max Voltage", maxV, 0, 0);
            }
            if (n == 5) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.checkbox = new CheckBox() {
                    Text = "Bidirectional",
                    Checked = (flags & FLAG_BIDIR) != 0
                };
                return ei;
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            double maxfreq = 1 / (8 * sim.timeStep);
            if (n == 0) {
                minF = ei.value;
                if (minF > maxfreq) {
                    minF = maxfreq;
                }
            }
            if (n == 1) {
                maxF = ei.value;
                if (maxF > maxfreq) {
                    maxF = maxfreq;
                }
            }
            if (n == 2) {
                sweepTime = ei.value;
            }
            if (n == 3) {
                flags &= ~FLAG_LOG;
                if (ei.checkbox.Checked) {
                    flags |= FLAG_LOG;
                }
            }
            if (n == 4)
                maxV = ei.value;
            if (n == 5) {
                flags &= ~FLAG_BIDIR;
                if (ei.checkbox.Checked) {
                    flags |= FLAG_BIDIR;
                }
            }
            setParams();
        }

        public override double getPower() { return -getVoltageDiff() * current; }
    }
}
