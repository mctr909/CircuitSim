using System;
using System.Drawing;

namespace Circuit.Elements {
    class FMElm : CircuitElm {
        const int FLAG_COS = 2;
        const int circleSize = 32;

        double carrierfreq;
        double signalfreq;
        double maxVoltage;
        double freqTimeZero;
        double deviation;
        double lasttime = 0;
        double funcx = 0;

        public FMElm(int xx, int yy) : base(xx, yy) {
            deviation = 200;
            maxVoltage = 5;
            carrierfreq = 800;
            signalfreq = 40;
            Reset();
        }

        public FMElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            carrierfreq = st.nextTokenDouble();
            signalfreq = st.nextTokenDouble();
            maxVoltage = st.nextTokenDouble();
            deviation = st.nextTokenDouble();
            if ((mFlags & FLAG_COS) != 0) {
                mFlags &= ~FLAG_COS;
            }
            Reset();
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.FM; }

        protected override string dump() {
            return carrierfreq + " " + signalfreq + " " + maxVoltage + " " + deviation;
        }

        public override void Reset() {
            freqTimeZero = 0;
            mCurCount = 0;
        }

        public override int PostCount { get { return 1; } }

        public override void Stamp() {
            mCir.StampVoltageSource(0, Nodes[0], mVoltSource);
        }

        public override void DoStep() {
            mCir.UpdateVoltageSource(0, Nodes[0], mVoltSource, getVoltage());
        }

        double getVoltage() {
            double deltaT = Sim.t - lasttime;
            lasttime = Sim.t;
            double signalamplitude = Math.Sin(2 * Pi * (Sim.t - freqTimeZero) * signalfreq);
            funcx += deltaT * (carrierfreq + (signalamplitude * deviation));
            double w = 2 * Pi * funcx;
            return Math.Sin(w) * maxVoltage;
        }

        public override void Draw(CustomGraphics g) {
            setBbox(mPoint1, mPoint2, circleSize);
            g.DrawThickLine(getVoltageColor(Volts[0]), mPoint1, mLead1);

            g.TextColor = NeedsHighlight ? SelectColor : WhiteColor;
            double v = getVoltage();
            string s = "FM";
            drawCenteredText(g, s, X2, Y2, true);
            drawWaveform(g, mPoint2);
            drawPosts(g);
            mCurCount = updateDotCount(-mCurrent, mCurCount);
            if (Sim.dragElm != this) {
                drawDots(g, mPoint1, mLead1, mCurCount);
            }
        }

        void drawWaveform(CustomGraphics g, Point center) {
            g.ThickLineColor = NeedsHighlight ? SelectColor : GrayColor;
            int xc = center.X;
            int yc = center.Y;
            g.DrawThickCircle(xc, yc, circleSize);
            adjustBbox(xc - circleSize, yc - circleSize, xc + circleSize, yc + circleSize);
        }

        public override void SetPoints() {
            base.SetPoints();
            mLead1 = Utils.InterpPoint(mPoint1, mPoint2, 1 - 0.5 * circleSize / mLen);
        }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override bool HasGroundConnection(int n1) { return true; }

        public override int VoltageSourceCount { get { return 1; } }

        public override double Power { get { return -VoltageDiff * mCurrent; } }

        public override void GetInfo(string[] arr) {
            arr[0] = "FM Source";
            arr[1] = "I = " + Utils.CurrentText(Current);
            arr[2] = "V = " + Utils.VoltageText(VoltageDiff);
            arr[3] = "cf = " + Utils.UnitText(carrierfreq, "Hz");
            arr[4] = "sf = " + Utils.UnitText(signalfreq, "Hz");
            arr[5] = "dev =" + Utils.UnitText(deviation, "Hz");
            arr[6] = "Vmax = " + Utils.VoltageText(maxVoltage);
        }

        public override EditInfo GetEditInfo(int n) {
            if (n == 0) {
                return new EditInfo("Max Voltage", maxVoltage, -20, 20);
            }
            if (n == 1) {
                return new EditInfo("Carrier Frequency (Hz)", carrierfreq, 4, 500);
            }
            if (n == 2) {
                return new EditInfo("Signal Frequency (Hz)", signalfreq, 4, 500);
            }
            if (n == 3) {
                return new EditInfo("Deviation (Hz)", deviation, 4, 500);
            }
            return null;
        }

        public override void SetEditValue(int n, EditInfo ei) {
            if (n == 0) {
                maxVoltage = ei.Value;
            }
            if (n == 1) {
                carrierfreq = ei.Value;
            }
            if (n == 2) {
                signalfreq = ei.Value;
            }
            if (n == 3) {
                deviation = ei.Value;
            }
        }
    }
}
