using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Input {
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

        Point textPos;

        public SweepElm(Point pos) : base(pos) {
            minF = 20;
            maxF = 4000;
            maxV = 5;
            sweepTime = .1;
            mFlags = FLAG_BIDIR;
            Reset();
        }

        public SweepElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            minF = st.nextTokenDouble();
            maxF = st.nextTokenDouble();
            maxV = st.nextTokenDouble();
            sweepTime = st.nextTokenDouble();
            Reset();
        }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override double Power { get { return -VoltageDiff * mCurrent; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override int PostCount { get { return 1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.SWEEP; } }

        protected override string dump() {
            return minF
                + " " + maxF
                + " " + maxV
                + " " + sweepTime;
        }

        public override void SetPoints() {
            base.SetPoints();
            setLead1(1 - 0.5 * circleSize / mLen);
            interpPoint(ref textPos, 1.0 + 0.66 * circleSize / Utils.Distance(mPoint1, mPoint2), 24 * mDsign);
        }

        public override void Draw(CustomGraphics g) {
            setBbox(mPoint1, mPoint2, circleSize);

            drawVoltage(g, 0, mPoint1, mLead1);

            g.ThickLineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;

            int xc = mPoint2.X;
            int yc = mPoint2.Y;
            g.DrawThickCircle(mPoint2, circleSize);

            adjustBbox(
                xc - circleSize, yc - circleSize,
                xc + circleSize, yc + circleSize
            );

            int wl = 11;
            int xl = 10;
            long tm = DateTime.Now.ToFileTimeUtc();
            tm %= 2000;
            if (tm > 1000) {
                tm = 2000 - tm;
            }
            double w = 1 + tm * .002;
            if (CirSim.Sim.IsRunning) {
                w = 1 + 3 * (frequency - minF) / (maxF - minF);
            }

            int x0 = 0;
            int y0 = 0;
            g.LineColor = CustomGraphics.GrayColor;
            for (int i = -xl; i <= xl; i++) {
                var yy = yc + (int)(.95 * Math.Sin(i * Math.PI * w / xl) * wl);
                if (i == -xl) {
                    x0 = xc + i;
                    y0 = yy;
                } else {
                    g.DrawLine(x0, y0, xc + i, yy);
                    x0 = xc + i;
                    y0 = yy;
                }
            }

            if (ControlPanel.ChkShowValues.Checked) {
                string s = Utils.ShortUnitText(frequency, "Hz");
                drawValues(g, s, 20, -15);
            }

            drawPosts(g);
            mCurCount = updateDotCount(-mCurrent, mCurCount);
            if (CirSim.Sim.DragElm != this) {
                drawDots(g, mPoint1, mLead1, mCurCount);
            }
        }

        public override void Stamp() {
            mCir.StampVoltageSource(0, Nodes[0], mVoltSource);
        }

        void setParams() {
            if (frequency < minF || frequency > maxF) {
                frequency = minF;
                freqTime = 0;
                dir = 1;
            }
            if ((mFlags & FLAG_LOG) == 0) {
                fadd = dir * ControlPanel.TimeStep * (maxF - minF) / sweepTime;
                fmul = 1;
            } else {
                fadd = 0;
                fmul = Math.Pow(maxF / minF, dir * ControlPanel.TimeStep / sweepTime);
            }
            savedTimeStep = ControlPanel.TimeStep;
        }

        public override void Reset() {
            frequency = minF;
            freqTime = 0;
            dir = 1;
            setParams();
        }

        public override void StartIteration() {
            /* has timestep been changed? */
            if (ControlPanel.TimeStep != savedTimeStep) {
                setParams();
            }
            v = Math.Sin(freqTime) * maxV;
            freqTime += frequency * 2 * Math.PI * ControlPanel.TimeStep;
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

        public override void DoStep() {
            mCir.UpdateVoltageSource(0, Nodes[0], mVoltSource, v);
        }

        public override bool HasGroundConnection(int n1) { return true; }

        public override void GetInfo(string[] arr) {
            arr[0] = "sweep " + (((mFlags & FLAG_LOG) == 0) ? "(linear)" : "(log)");
            arr[1] = "I = " + Utils.CurrentDText(mCurrent);
            arr[2] = "V = " + Utils.VoltageText(Volts[0]);
            arr[3] = "f = " + Utils.UnitText(frequency, "Hz");
            arr[4] = "range = " + Utils.UnitText(minF, "Hz") + " .. " + Utils.UnitText(maxF, "Hz");
            arr[5] = "time = " + Utils.UnitText(sweepTime, "s");
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("Min Frequency (Hz)", minF, 0, 0);
            }
            if (n == 1) {
                return new ElementInfo("Max Frequency (Hz)", maxF, 0, 0);
            }
            if (n == 2) {
                return new ElementInfo("Sweep Time (s)", sweepTime, 0, 0);
            }
            if (n == 3) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    Text = "Logarithmic",
                    Checked = (mFlags & FLAG_LOG) != 0
                };
                return ei;
            }
            if (n == 4) {
                return new ElementInfo("Max Voltage", maxV, 0, 0);
            }
            if (n == 5) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    Text = "Bidirectional",
                    Checked = (mFlags & FLAG_BIDIR) != 0
                };
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            double maxfreq = 1 / (8 * ControlPanel.TimeStep);
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
    }
}
