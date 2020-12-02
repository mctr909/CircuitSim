﻿using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class SweepElm : CircuitElm {
        const int FLAG_LOG = 1;
        const int FLAG_BIDIR = 2;

        const int circleSize = 36;

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
            mFlags = FLAG_BIDIR;
            reset();
        }

        public SweepElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            minF = st.nextTokenDouble();
            maxF = st.nextTokenDouble();
            maxV = st.nextTokenDouble();
            sweepTime = st.nextTokenDouble();
            reset();
        }

        protected override string dump() {
            return minF
                + " " + maxF
                + " " + maxV
                + " " + sweepTime;
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.SWEEP; }

        public override int getPostCount() { return 1; }

        public override void setPoints() {
            base.setPoints();
            mLead1 = interpPoint(mPoint1, mPoint2, 1 - 0.5 * circleSize / mLen);
        }

        public override void draw(Graphics g) {
            setBbox(mPoint1, mPoint2, circleSize);

            drawThickLine(g, getVoltageColor(Volts[0]), mPoint1, mLead1);

            PenThickLine.Color = needsHighlight() ? SelectColor : LightGrayColor;

            int xc = mPoint2.X;
            int yc = mPoint2.Y;
            drawThickCircle(g, xc, yc, circleSize);

            adjustBbox(xc - circleSize, yc - circleSize, xc + circleSize, yc + circleSize);

            int wl = 11;
            int xl = 10;
            long tm = DateTime.Now.ToFileTimeUtc();
            tm %= 2000;
            if (tm > 1000) {
                tm = 2000 - tm;
            }
            double w = 1 + tm * .002;
            if (Sim.simIsRunning()) {
                w = 1 + 3 * (frequency - minF) / (maxF - minF);
            }

            int x0 = 0;
            float y0 = 0;
            PenLine.Color = LightGrayColor;
            for (int i = -xl; i <= xl; i++) {
                float yy = yc + (float)(.95 * Math.Sin(i * PI * w / xl) * wl);
                if (i == -xl) {
                    x0 = xc + i;
                    y0 = yy;
                } else {
                    drawLine(g, x0, y0, xc + i, yy);
                    x0 = xc + i;
                    y0 = yy;
                }
            }

            if (Sim.chkShowValuesCheckItem.Checked) {
                string s = getShortUnitText(frequency, "Hz");
                if (mDx == 0 || mDy == 0) {
                    drawValues(g, s, circleSize);
                }
            }

            drawPosts(g);
            mCurCount = updateDotCount(-mCurrent, mCurCount);
            if (Sim.dragElm != this) {
                drawDots(g, mPoint1, mLead1, mCurCount);
            }
        }

        public override void stamp() {
            Cir.StampVoltageSource(0, Nodes[0], mVoltSource);
        }

        void setParams() {
            if (frequency < minF || frequency > maxF) {
                frequency = minF;
                freqTime = 0;
                dir = 1;
            }
            if ((mFlags & FLAG_LOG) == 0) {
                fadd = dir * Sim.timeStep * (maxF - minF) / sweepTime;
                fmul = 1;
            } else {
                fadd = 0;
                fmul = Math.Pow(maxF / minF, dir * Sim.timeStep / sweepTime);
            }
            savedTimeStep = Sim.timeStep;
        }

        public override void reset() {
            frequency = minF;
            freqTime = 0;
            dir = 1;
            setParams();
        }

        public override void startIteration() {
            /* has timestep been changed? */
            if (Sim.timeStep != savedTimeStep) {
                setParams();
            }
            v = Math.Sin(freqTime) * maxV;
            freqTime += frequency * PI2 * Sim.timeStep;
            frequency = frequency * fmul + fadd;
            if (frequency >= maxF && dir == 1) {
                if ((mFlags & FLAG_BIDIR) != 0) {
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
            Cir.UpdateVoltageSource(0, Nodes[0], mVoltSource, v);
        }

        public override double getVoltageDiff() { return Volts[0]; }

        public override int getVoltageSourceCount() { return 1; }

        public override bool hasGroundConnection(int n1) { return true; }

        public override void getInfo(string[] arr) {
            arr[0] = "sweep " + (((mFlags & FLAG_LOG) == 0) ? "(linear)" : "(log)");
            arr[1] = "I = " + getCurrentDText(getCurrent());
            arr[2] = "V = " + getVoltageText(Volts[0]);
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
                ei.CheckBox = new CheckBox() {
                    Text = "Logarithmic",
                    Checked = (mFlags & FLAG_LOG) != 0
                };
                return ei;
            }
            if (n == 4) {
                return new EditInfo("Max Voltage", maxV, 0, 0);
            }
            if (n == 5) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    Text = "Bidirectional",
                    Checked = (mFlags & FLAG_BIDIR) != 0
                };
                return ei;
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            double maxfreq = 1 / (8 * Sim.timeStep);
            if (n == 0) {
                minF = ei.Value;
                if (minF > maxfreq) {
                    minF = maxfreq;
                }
            }
            if (n == 1) {
                maxF = ei.Value;
                if (maxF > maxfreq) {
                    maxF = maxfreq;
                }
            }
            if (n == 2) {
                sweepTime = ei.Value;
            }
            if (n == 3) {
                mFlags &= ~FLAG_LOG;
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_LOG;
                }
            }
            if (n == 4)
                maxV = ei.Value;
            if (n == 5) {
                mFlags &= ~FLAG_BIDIR;
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_BIDIR;
                }
            }
            setParams();
        }

        public override double getPower() { return -getVoltageDiff() * mCurrent; }
    }
}
