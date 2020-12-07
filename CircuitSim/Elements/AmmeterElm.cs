using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class AmmeterElm : CircuitElm {
        const int AM_VOL = 0;
        const int AM_RMS = 1;
        const int FLAG_SHOWCURRENT = 1;

        int meter;
        E_SCALE scale;

        int zerocount = 0;
        double rmsI = 0;
        double total;
        double count;
        double maxI = 0;
        double lastMaxI;
        double minI = 0;
        double lastMinI;
        double selectedValue = 0;

        double[] currents;
        bool increasingI = true;
        bool decreasingI = true;

        Point mid;
        Point[] arrowPoly;
        Point textPos;

        public AmmeterElm(int xx, int yy) : base(xx, yy) {
            mFlags = FLAG_SHOWCURRENT;
            scale = E_SCALE.AUTO;
        }

        public AmmeterElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            meter = st.nextTokenInt();
            try {
                scale = (E_SCALE)st.nextTokenInt();
            } catch {
                scale = E_SCALE.AUTO;
            }
        }

        public override bool IsWire { get { return true; } }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override double Power { get { return 0; } }

        public override int VoltageSourceCount { get { return 1; } }

        protected override string dump() {
            return meter + " " + scale;
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.AMMETER; }

        string getMeter() {
            switch (meter) {
            case AM_VOL:
                return "I";
            case AM_RMS:
                return "Irms";
            }
            return "";
        }

        public override void SetPoints() {
            base.SetPoints();
            mid = Utils.InterpPoint(mPoint1, mPoint2, 0.5 + 8 / mLen);
            arrowPoly = Utils.CreateArrow(mPoint1, mid, 14, 7);
            int sign;
            if (mPoint1.Y == mPoint2.Y) {
                sign = mDsign;
            } else {
                sign = -mDsign;
            }
            textPos = Utils.InterpPoint(mPoint1, mPoint2, 0.5 + 8 * sign / mLen, 12 * sign);
        }

        public override void StepFinished() {
            count++; /*how many counts are in a cycle */
            total += mCurrent * mCurrent; /* sum of squares */
            if (mCurrent > maxI && increasingI) {
                maxI = mCurrent;
                increasingI = true;
                decreasingI = false;
            }

            if (mCurrent < maxI && increasingI) { /* change of direction I now going down - at start of waveform */
                lastMaxI = maxI; /* capture last maximum */
                                 /* capture time between */
                minI = mCurrent; /* track minimum value */
                increasingI = false;
                decreasingI = true;

                /* rms data */
                total = total / count;
                rmsI = Math.Sqrt(total);
                if (double.IsNaN(rmsI)) {
                    rmsI = 0;
                }
                count = 0;
                total = 0;

            }

            if (mCurrent < minI && decreasingI) { /* I going down, track minimum value */
                minI = mCurrent;
                increasingI = false;
                decreasingI = true;
            }

            if (mCurrent > minI && decreasingI) { /* change of direction I now going up */
                lastMinI = minI; /* capture last minimum */

                maxI = mCurrent;
                increasingI = true;
                decreasingI = false;

                /* rms data */
                total = total / count;
                rmsI = Math.Sqrt(total);
                if (double.IsNaN(rmsI)) {
                    rmsI = 0;
                }
                count = 0;
                total = 0;
            }

            /* need to zero the rms value if it stays at 0 for a while */
            if (mCurrent == 0) {
                zerocount++;
                if (zerocount > 5) {
                    total = 0;
                    rmsI = 0;
                    maxI = 0;
                    minI = 0;
                }
            } else {
                zerocount = 0;
            }

            switch (meter) {
            case AM_VOL:
                selectedValue = mCurrent;
                break;
            case AM_RMS:
                selectedValue = rmsI;
                break;
            }
        }

        public override void Draw(CustomGraphics g) {
            base.Draw(g); /* BC required for highlighting */
            var c = getVoltageColor(Volts[0]);
            g.ThickLineColor = c;
            g.DrawThickLine(mPoint1, mPoint2);
            g.FillPolygon(c, arrowPoly);
            doDots(g);
            setBbox(mPoint1, mPoint2, 3);
            string s = "A";
            switch (meter) {
            case AM_VOL:
                s = Utils.UnitTextWithScale(mCurrent, "A", scale);
                break;
            case AM_RMS:
                s = Utils.UnitTextWithScale(rmsI, "A(rms)", scale);
                break;
            }
            g.DrawRightText(s, textPos.X, textPos.Y);
            drawPosts(g);
        }

        public override void Stamp() {
            mCir.StampVoltageSource(Nodes[0], Nodes[1], mVoltSource, 0);
        }

        bool mustShowCurrent() {
            return (mFlags & FLAG_SHOWCURRENT) != 0;
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "Ammeter";
            switch (meter) {
            case AM_VOL:
                arr[1] = "I = " + Utils.UnitText(mCurrent, "A");
                break;
            case AM_RMS:
                arr[1] = "Irms = " + Utils.UnitText(rmsI, "A");
                break;
            }
        }

        public override EditInfo GetEditInfo(int n) {
            if (n == 0) {
                var ei = new EditInfo("Value", selectedValue, -1, -1);
                ei.Choice = new ComboBox();
                ei.Choice.Items.Add("Current");
                ei.Choice.Items.Add("RMS Current");
                ei.Choice.SelectedIndex = meter;
                return ei;
            }
            if (n == 1) {
                var ei = new EditInfo("Scale", 0);
                ei.Choice = new ComboBox();
                ei.Choice.Items.Add("Auto");
                ei.Choice.Items.Add("A");
                ei.Choice.Items.Add("mA");
                ei.Choice.Items.Add(CirSim.muString + "A");
                ei.Choice.SelectedIndex = (int)scale;
                return ei;
            }
            return null;
        }

        public override void SetEditValue(int n, EditInfo ei) {
            if (n == 0) {
                meter = ei.Choice.SelectedIndex;
            }
            if (n == 1) {
                scale = (E_SCALE)ei.Choice.SelectedIndex;
            }
        }
    }
}
