using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class ProbeElm : CircuitElm {
        const int FLAG_SHOWVOLTAGE = 1;

        const int TP_VOL = 0;
        const int TP_RMS = 1;
        const int TP_MAX = 2;
        const int TP_MIN = 3;
        const int TP_P2P = 4;
        const int TP_BIN = 5;
        const int TP_FRQ = 6;
        const int TP_PER = 7;
        const int TP_PWI = 8;
        const int TP_DUT = 9; /* mark to space ratio */

        int meter;
        int units;
        int scale;

        double rmsV = 0, total, count;
        double binaryLevel = 0; /*0 or 1 - double because we only pass doubles back to the web page */
        int zerocount = 0;
        double maxV = 0, lastMaxV;
        double minV = 0, lastMinV;
        double frequency = 0;
        double period = 0;
        double pulseWidth = 0;
        double dutyCycle = 0;
        double selectedValue = 0;

        bool increasingV = true;
        bool decreasingV = true;

        long periodStart; /* time between consecutive max values */
        long periodLength;
        long pulseStart;

        Point center;

        public ProbeElm(int xx, int yy) : base(xx, yy) {
            meter = TP_VOL;

            /* default for new elements */
            mFlags = FLAG_SHOWVOLTAGE;
            scale = SCALE_AUTO;
        }

        public ProbeElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            meter = TP_VOL;
            scale = SCALE_AUTO;
            try {
                meter = st.nextTokenInt(); /* get meter type from saved dump */
                scale = st.nextTokenInt();
            } catch { }
        }

        protected override string dump() {
            return meter + " " + scale;
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.PROBE; }

        public override DUMP_ID getShortcut() { return DUMP_ID.PROBE; }

        string getMeter() {
            switch (meter) {
            case TP_VOL:
                return "V";
            case TP_RMS:
                return "V(rms)";
            case TP_MAX:
                return "Vmax";
            case TP_MIN:
                return "Vmin";
            case TP_P2P:
                return "Peak to peak";
            case TP_BIN:
                return "Binary";
            case TP_FRQ:
                return "Frequency";
            case TP_PER:
                return "Period";
            case TP_PWI:
                return "Pulse width";
            case TP_DUT:
                return "Duty cycle";
            }
            return "";
        }

        public override void setPoints() {
            base.setPoints();
            center = interpPoint(mPoint1, mPoint2, .5);
        }

        public override void draw(Graphics g) {
            int hs = 8;
            setBbox(mPoint1, mPoint2, hs);
            bool selected = needsHighlight();
            double len = (selected || Sim.dragElm == this || mustShowVoltage()) ? 16 : mLen - 32;
            calcLeads((int)len);

            if (selected) {
                PenThickLine.Color = SelectColor;
            } else {
                PenThickLine.Color = getVoltageColor(Volts[0]);
            }
            drawThickLine(g, mPoint1, mLead1);

            if (selected) {
                PenThickLine.Color = SelectColor;
            } else {
                PenThickLine.Color = getVoltageColor(Volts[1]);
            }
            drawThickLine(g, mLead2, mPoint2);

            if (this == Sim.plotXElm) {
                drawCenteredText(g, "X", center.X, center.Y, true);
            }
            if (this == Sim.plotYElm) {
                drawCenteredText(g, "Y", center.X, center.Y, true);
            }

            if (mustShowVoltage()) {
                string s = "";
                switch (meter) {
                case TP_VOL:
                    s = getUnitTextWithScale(getVoltageDiff(), "V", scale);
                    break;
                case TP_RMS:
                    s = getUnitTextWithScale(rmsV, "V(rms)", scale);
                    break;
                case TP_MAX:
                    s = getUnitTextWithScale(lastMaxV, "Vpk", scale);
                    break;
                case TP_MIN:
                    s = getUnitTextWithScale(lastMinV, "Vmin", scale);
                    break;
                case TP_P2P:
                    s = getUnitTextWithScale(lastMaxV - lastMinV, "Vp2p", scale);
                    break;
                case TP_BIN:
                    s = binaryLevel + "";
                    break;
                case TP_FRQ:
                    s = getUnitText(frequency, "Hz");
                    break;
                case TP_PER:
                    s = "percent:" + period + " " + Sim.timeStep + " " + Sim.t + " " + Sim.getIterCount();
                    break;
                case TP_PWI:
                    s = getUnitText(pulseWidth, "S");
                    break;
                case TP_DUT:
                    s = dutyCycle.ToString("0.000");
                    break;
                }
                drawValues(g, s, 4);
            }

            var plusPoint = interpPoint(mPoint1, mPoint2, (mLen / 2 - len / 2 - 4) / mLen, -10 * mDsign);
            if (Y2 > Y1) {
                plusPoint.Y += 4;
            }
            if (Y1 > Y2) {
                plusPoint.Y += 3;
            }

            int w = (int)g.MeasureString("+", FONT_UNITS).Width;
            g.DrawString("+", FONT_UNITS, BrushText, plusPoint.X - w / 2, plusPoint.Y);
            drawPosts(g);
        }

        bool mustShowVoltage() {
            return (mFlags & FLAG_SHOWVOLTAGE) != 0;
        }

        public override void stepFinished() {
            count++; /*how many counts are in a cycle */
            double v = getVoltageDiff();
            total += v * v;

            if (v < 2.5) {
                binaryLevel = 0;
            } else {
                binaryLevel = 1;
            }

            /* V going up, track maximum value with */
            if (v > maxV && increasingV) {
                maxV = v;
                increasingV = true;
                decreasingV = false;
            }

            if (v < maxV && increasingV) { /* change of direction V now going down - at start of waveform */
                lastMaxV = maxV; /* capture last maximum */
                                 /* capture time between */
                var now = DateTime.Now.ToFileTimeUtc();
                periodLength = now - periodStart;
                periodStart = now;
                period = periodLength;
                pulseWidth = now - pulseStart;
                dutyCycle = pulseWidth / periodLength;
                minV = v; /* track minimum value with V */
                increasingV = false;
                decreasingV = true;

                /* rms data */
                total = total / count;
                rmsV = Math.Sqrt(total);
                if (double.IsNaN(rmsV)) {
                    rmsV = 0;
                }
                count = 0;
                total = 0;
            }

            if (v < minV && decreasingV) { /* V going down, track minimum value with V */
                minV = v;
                increasingV = false;
                decreasingV = true;
            }

            if (v > minV && decreasingV) { /* change of direction V now going up */
                lastMinV = minV; /* capture last minimum */
                pulseStart = DateTime.Now.ToFileTimeUtc();
                maxV = v;
                increasingV = true;
                decreasingV = false;

                /* rms data */
                total = total / count;
                rmsV = Math.Sqrt(total);
                if (double.IsNaN(rmsV)) {
                    rmsV = 0;
                }
                count = 0;
                total = 0;
            }

            /* need to zero the rms value if it stays at 0 for a while */
            if (v == 0) {
                zerocount++;
                if (zerocount > 5) {
                    total = 0;
                    rmsV = 0;
                    maxV = 0;
                    minV = 0;
                }
            } else {
                zerocount = 0;
            }
        }

        public override void getInfo(string[] arr) {
            arr[0] = "voltmeter";
            arr[1] = "Vd = " + getVoltageText(getVoltageDiff());
        }

        public override bool getConnection(int n1, int n2) { return false; }

        public override EditInfo getEditInfo(int n) {
            if (n == 0) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "Show Value";
                ei.CheckBox.Checked = mustShowVoltage();
                return ei;
            }
            if (n == 1) {
                var ei = new EditInfo("Value", selectedValue, -1, -1);
                ei.Choice = new ComboBox();
                ei.Choice.Items.Add("Voltage");
                ei.Choice.Items.Add("RMS Voltage");
                ei.Choice.Items.Add("Max Voltage");
                ei.Choice.Items.Add("Min Voltage");
                ei.Choice.Items.Add("P2P Voltage");
                ei.Choice.Items.Add("Binary Value");
                /*ei.choice.Items.Add("Frequency");
                ei.choice.Items.Add("Period");
                ei.choice.Items.Add("Pulse Width");
                ei.choice.Items.Add("Duty Cycle"); */
                ei.Choice.SelectedIndex = meter;
                return ei;
            }
            if (n == 2) {
                var ei = new EditInfo("Scale", 0);
                ei.Choice = new ComboBox();
                ei.Choice.Items.Add("Auto");
                ei.Choice.Items.Add("V");
                ei.Choice.Items.Add("mV");
                ei.Choice.Items.Add(CirSim.muString + "V");
                ei.Choice.SelectedIndex = scale;
                return ei;
            }

            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (n == 0) {
                if (ei.CheckBox.Checked) {
                    mFlags = FLAG_SHOWVOLTAGE;
                } else {
                    mFlags &= ~FLAG_SHOWVOLTAGE;
                }
            }
            if (n == 1) {
                meter = ei.Choice.SelectedIndex;
            }
            if (n == 2) {
                scale = ei.Choice.SelectedIndex;
            }
        }
    }
}
