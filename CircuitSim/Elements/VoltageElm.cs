using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class VoltageElm : CircuitElm {
        const int FLAG_COS = 2;
        const int FLAG_PULSE_DUTY = 4;

        public enum WAVEFORM {
            DC,
            AC,
            SQUARE,
            TRIANGLE,
            SAWTOOTH,
            PULSE,
            PWM_BOTH,
            PWM_HIGH,
            PWM_LOW,
            NOISE
        }

        protected WAVEFORM waveform { get; private set; }

        protected const int circleSize = 36;

        protected double frequency;
        protected double maxVoltage;
        protected double bias;
        double phaseShift;
        double dutyCycle;
        double noiseValue;

        Point ps1;
        Point ps2;
        Point textPos;

        const double defaultPulseDuty = 1 / Pi2;

        protected VoltageElm(int xx, int yy, WAVEFORM wf) : base(xx, yy) {
            waveform = wf;
            maxVoltage = 5;
            frequency = 40;
            dutyCycle = .5;
            Reset();
        }

        public VoltageElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            maxVoltage = 5;
            frequency = 40;
            waveform = WAVEFORM.DC;
            dutyCycle = .5;

            try {
                waveform = st.nextTokenEnum<WAVEFORM>();
                frequency = st.nextTokenDouble();
                maxVoltage = st.nextTokenDouble();
                bias = st.nextTokenDouble();
                phaseShift = st.nextTokenDouble();
                dutyCycle = st.nextTokenDouble();
            } catch { }

            if ((mFlags & FLAG_COS) != 0) {
                mFlags &= ~FLAG_COS;
                phaseShift = Pi / 2;
            }

            /* old circuit files have the wrong duty cycle for pulse waveforms (wasn't configurable in the past) */
            if ((mFlags & FLAG_PULSE_DUTY) == 0 && waveform == WAVEFORM.PULSE) {
                dutyCycle = defaultPulseDuty;
            }

            Reset();
        }

        public override double VoltageDiff { get { return Volts[1] - Volts[0]; } }

        public override double Power { get { return -VoltageDiff * mCurrent; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.VOLTAGE; } }

        protected override string dump() {
            /* set flag so we know if duty cycle is correct for pulse waveforms */
            if (waveform == WAVEFORM.PULSE) {
                mFlags |= FLAG_PULSE_DUTY;
            } else {
                mFlags &= ~FLAG_PULSE_DUTY;
            }

            return waveform
                + " " + frequency
                + " " + maxVoltage
                + " " + bias
                + " " + phaseShift
                + " " + dutyCycle;
            /* VarRailElm adds text at the end */
        }

        public override void Reset() {
            mCurCount = 0;
        }

        double triangleFunc(double x) {
            if (x < Pi) {
                return x * (2 / Pi) - 1;
            }
            return 1 - (x - Pi) * (2 / Pi);
        }

        public override void Stamp() {
            if (waveform == WAVEFORM.DC) {
                mCir.StampVoltageSource(Nodes[0], Nodes[1], mVoltSource, getVoltage());
            } else {
                mCir.StampVoltageSource(Nodes[0], Nodes[1], mVoltSource);
            }
        }

        public override void DoStep() {
            if (waveform != WAVEFORM.DC) {
                mCir.UpdateVoltageSource(Nodes[0], Nodes[1], mVoltSource, getVoltage());
            }
        }

        public override void StepFinished() {
            if (waveform == WAVEFORM.NOISE) {
                noiseValue = (CirSim.random.NextDouble() * 2 - 1) * maxVoltage + bias;
            }
        }

        public virtual double getVoltage() {
            if (waveform != WAVEFORM.DC && Sim.dcAnalysisFlag) {
                return bias;
            }

            double t = Pi2 * Sim.t;
            double wt = t * frequency + phaseShift;

            switch (waveform) {
            case WAVEFORM.DC:
                return maxVoltage + bias;
            case WAVEFORM.AC:
                return Math.Sin(wt) * maxVoltage + bias;
            case WAVEFORM.SQUARE:
                return bias + ((wt % Pi2 > (Pi2 * dutyCycle)) ? -maxVoltage : maxVoltage);
            case WAVEFORM.TRIANGLE:
                return bias + triangleFunc(wt % Pi2) * maxVoltage;
            case WAVEFORM.SAWTOOTH:
                return bias + (wt % Pi2) * (maxVoltage / Pi) - maxVoltage;
            case WAVEFORM.PULSE:
                return ((wt % Pi2) < (Pi2 * dutyCycle)) ? maxVoltage + bias : bias;
            case WAVEFORM.PWM_BOTH: {
                var maxfreq = 1 / (32 * Sim.timeStep);
                var cr = 0.5 - 0.5 * triangleFunc(t * maxfreq % Pi2);
                var sg = dutyCycle * Math.Sin(wt);
                if (0.0 <= sg) {
                    return bias + (cr < sg ? maxVoltage : 0);
                } else {
                    return bias - (sg < -cr ? maxVoltage : 0);
                }
            }
            case WAVEFORM.PWM_HIGH: {
                var maxfreq = 1 / (32 * Sim.timeStep);
                var cr = 0.5 - 0.5 * triangleFunc(t * maxfreq % Pi2);
                var sg = dutyCycle * Math.Sin(wt);
                if (0.0 <= sg) {
                    return bias + (cr < sg ? maxVoltage : 0);
                } else {
                    return bias;
                }
            }
            case WAVEFORM.PWM_LOW: {
                var maxfreq = 1 / (32 * Sim.timeStep);
                var cr = 0.5 - 0.5 * triangleFunc(t * maxfreq % Pi2);
                var sg = dutyCycle * Math.Sin(wt);
                if (0.0 <= sg) {
                    return bias;
                } else {
                    return bias + (sg < -cr ? maxVoltage : 0);
                }
            }
            case WAVEFORM.NOISE:
                return noiseValue;
            default: return 0;
            }
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads((waveform == WAVEFORM.DC) ? 8 : circleSize);

            int sign;
            if (mPoint1.Y == mPoint2.Y) {
                sign = -mDsign;
            } else {
                sign = mDsign;
            }
            if(waveform == WAVEFORM.DC) {
                textPos = Utils.InterpPoint(mPoint1, mPoint2, 0.5, -16 * sign);
            } else {
                textPos = Utils.InterpPoint(mPoint1, mPoint2, (mLen / 2 + 0.7 * circleSize) / mLen, 10 * sign);
            }
        }

        public override void Draw(CustomGraphics g) {
            setBbox(X1, Y1, X2, Y2);
            draw2Leads(g);

            if (waveform == WAVEFORM.DC) {
                Utils.InterpPoint(mLead1, mLead2, ref ps1, ref ps2, 0, 10);
                g.DrawThickLine(getVoltageColor(Volts[0]), ps1, ps2);

                int hs = 16;
                setBbox(mPoint1, mPoint2, hs);
                Utils.InterpPoint(mLead1, mLead2, ref ps1, ref ps2, 1, hs);
                g.DrawThickLine(getVoltageColor(Volts[1]), ps1, ps2);
                string s = Utils.ShortUnitText(maxVoltage, "V");
                g.DrawRightText(s, textPos.X, textPos.Y);
            } else {
                setBbox(mPoint1, mPoint2, circleSize);
                Utils.InterpPoint(mLead1, mLead2, ref ps1, .5);
                drawWaveform(g, ps1);
                string inds;
                if (bias > 0 || (bias == 0 && waveform == WAVEFORM.PULSE)) {
                    inds = "+";
                } else {
                    inds = "*";
                }
                drawCenteredLText(g, inds, textPos.X, textPos.Y, true);
            }

            updateDotCount();

            if (Sim.dragElm != this) {
                if (waveform == WAVEFORM.DC) {
                    drawDots(g, mPoint1, mPoint2, mCurCount);
                } else {
                    drawDots(g, mPoint1, mLead1, mCurCount);
                    drawDots(g, mPoint2, mLead2, -mCurCount);
                }
            }
            drawPosts(g);
        }

        protected void drawWaveform(CustomGraphics g, Point center) {
            int x = center.X;
            int y = center.Y;

            if (waveform != WAVEFORM.NOISE) {
                g.ThickLineColor = NeedsHighlight ? SelectColor : GrayColor;
                g.DrawThickCircle(center, circleSize);
            }

            adjustBbox(
                x - circleSize, y - circleSize,
                x + circleSize, y + circleSize
            );

            float h = 11;
            float xd = (float)(h * 2 * dutyCycle - h + x);
            xd = Math.Max(x - h + 1, Math.Min(x + h - 1, xd));

            g.LineColor = NeedsHighlight ? SelectColor : GrayColor;

            switch (waveform) {
            case WAVEFORM.DC: {
                break;
            }
            case WAVEFORM.SQUARE:
                if (maxVoltage < 0) {
                    g.DrawLine(x - h, y + h, x - h, y    );
                    g.DrawLine(x - h, y + h, xd   , y + h);
                    g.DrawLine(xd   , y + h, xd   , y - h);
                    g.DrawLine(x + h, y - h, xd   , y - h);
                    g.DrawLine(x + h, y    , x + h, y - h);
                } else {
                    g.DrawLine(x - h, y - h, x - h, y    );
                    g.DrawLine(x - h, y - h, xd   , y - h);
                    g.DrawLine(xd   , y - h, xd   , y + h);
                    g.DrawLine(x + h, y + h, xd   , y + h);
                    g.DrawLine(x + h, y    , x + h, y + h);
                }
                break;
            case WAVEFORM.PULSE:
                if (maxVoltage < 0) {
                    g.DrawLine(x + h, y    , x + h, y    );
                    g.DrawLine(x + h, y    , xd   , y    );
                    g.DrawLine(xd   , y + h, xd   , y    );
                    g.DrawLine(x - h, y + h, xd   , y + h);
                    g.DrawLine(x - h, y + h, x - h, y    );
                } else {
                    g.DrawLine(x - h, y - h, x - h, y    );
                    g.DrawLine(x - h, y - h, xd   , y - h);
                    g.DrawLine(xd   , y - h, xd   , y    );
                    g.DrawLine(x + h, y    , xd   , y    );
                    g.DrawLine(x + h, y    , x + h, y    );
                }
                break;
            case WAVEFORM.SAWTOOTH:
                g.DrawLine(x, y - h, x - h, y    );
                g.DrawLine(x, y - h, x    , y + h);
                g.DrawLine(x, y + h, x + h, y    );
                break;
            case WAVEFORM.TRIANGLE: {
                int xl = 5;
                g.DrawLine(x - xl * 2, y    , x - xl    , y - h);
                g.DrawLine(x - xl    , y - h, x         , y    );
                g.DrawLine(x         , y    , x + xl    , y + h);
                g.DrawLine(x + xl    , y + h, x + xl * 2, y    );
                break;
            }
            case WAVEFORM.NOISE: {
                drawCenteredText(g, "Noise", x, y, true);
                break;
            }
            case WAVEFORM.AC: {
                int xl = 10;
                int x0 = 0;
                float y0 = 0;
                for (int i = -xl; i <= xl; i++) {
                    var yy = y + (float)(.95 * Math.Sin(i * Pi / xl) * h);
                    if (i != -xl) {
                        g.DrawLine(x0, y0, x + i, yy);
                    }
                    x0 = x + i;
                    y0 = yy;
                }
                break;
            }
            }

            if (Sim.chkShowValues.Checked && waveform != WAVEFORM.NOISE) {
                var s = Utils.ShortUnitText(maxVoltage, "V\r\n");
                s += Utils.ShortUnitText(frequency, "Hz\r\n");
                s += Utils.ShortUnitText(phaseShift * ToDeg, "°");
                drawValues(g, s, 0, 5);
            }
        }

        public override void GetInfo(string[] arr) {
            switch (waveform) {
            case WAVEFORM.DC:
            case WAVEFORM.AC:
            case WAVEFORM.SQUARE:
            case WAVEFORM.PULSE:
            case WAVEFORM.SAWTOOTH:
            case WAVEFORM.TRIANGLE:
            case WAVEFORM.NOISE:
            case WAVEFORM.PWM_BOTH:
                arr[0] = waveform.ToString(); break;
            }

            arr[1] = "I = " + Utils.CurrentText(mCurrent);
            arr[2] = ((this is RailElm) ? "V = " : "Vd = ") + Utils.VoltageText(VoltageDiff);
            int i = 3;
            if (waveform != WAVEFORM.DC && waveform != WAVEFORM.NOISE) {
                arr[i++] = "f = " + Utils.UnitText(frequency, "Hz");
                arr[i++] = "Vmax = " + Utils.VoltageText(maxVoltage);
                if (waveform == WAVEFORM.AC && bias == 0) {
                    arr[i++] = "V(rms) = " + Utils.VoltageText(maxVoltage / 1.41421356);
                }
                if (bias != 0) {
                    arr[i++] = "Voff = " + Utils.VoltageText(bias);
                } else if (frequency > 500) {
                    arr[i++] = "wavelength = " + Utils.UnitText(2.9979e8 / frequency, "m");
                }
            }
            if (waveform == WAVEFORM.DC && mCurrent != 0 && mCir.ShowResistanceInVoltageSources) {
                arr[i++] = "(R = " + Utils.UnitText(maxVoltage / mCurrent, CirSim.ohmString) + ")";
            }
            arr[i++] = "P = " + Utils.UnitText(Power, "W");
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo(waveform == WAVEFORM.DC ? "Voltage" : "Max Voltage", maxVoltage, -20, 20);
            }
            if (n == 1) {
                var ei = new ElementInfo("Waveform", (int)waveform, -1, -1);
                ei.Choice = new ComboBox();
                ei.Choice.Items.Add(WAVEFORM.DC);
                ei.Choice.Items.Add(WAVEFORM.AC);
                ei.Choice.Items.Add(WAVEFORM.SQUARE);
                ei.Choice.Items.Add(WAVEFORM.TRIANGLE);
                ei.Choice.Items.Add(WAVEFORM.SAWTOOTH);
                ei.Choice.Items.Add(WAVEFORM.PULSE);
                ei.Choice.Items.Add(WAVEFORM.PWM_BOTH);
                ei.Choice.Items.Add(WAVEFORM.PWM_HIGH);
                ei.Choice.Items.Add(WAVEFORM.PWM_LOW);
                ei.Choice.Items.Add(WAVEFORM.NOISE);
                ei.Choice.SelectedIndex = (int)waveform;
                return ei;
            }
            if (n == 2) {
                return new ElementInfo("DC Offset (V)", bias, -20, 20);
            }
            if (waveform == WAVEFORM.DC || waveform == WAVEFORM.NOISE) {
                return null;
            }
            if (n == 3) {
                return new ElementInfo("Frequency (Hz)", frequency, 4, 500);
            }
            if (n == 4) {
                return new ElementInfo("Phase Offset (degrees)", phaseShift * ToDeg, -180, 180).SetDimensionless();
            }
            if (n == 5 && (waveform == WAVEFORM.PULSE || waveform == WAVEFORM.SQUARE
                || waveform == WAVEFORM.PWM_BOTH || waveform == WAVEFORM.PWM_HIGH || waveform == WAVEFORM.PWM_LOW)) {
                return new ElementInfo("Duty Cycle", dutyCycle * 100, 0, 100).SetDimensionless();
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                maxVoltage = ei.Value;
            }
            if (n == 2) {
                bias = ei.Value;
            }
            if (n == 3) {
                /* adjust time zero to maintain continuity ind the waveform
                 * even though the frequency has changed. */
                double oldfreq = frequency;
                frequency = ei.Value;
                double maxfreq = 1 / (8 * Sim.timeStep);
                if (maxfreq < frequency) {
                    if (MessageBox.Show("Adjust timestep to allow for higher frequencies?", "", MessageBoxButtons.OKCancel) == DialogResult.OK) {
                        Sim.timeStep = 1 / (32 * frequency);
                    } else {
                        frequency = maxfreq;
                    }
                }
                double adj = frequency - oldfreq;
            }
            if (n == 1) {
                var ow = waveform;
                waveform = (WAVEFORM)ei.Choice.SelectedIndex;
                if (waveform == WAVEFORM.DC && ow != WAVEFORM.DC) {
                    ei.NewDialog = true;
                    bias = 0;
                } else if (waveform != ow) {
                    ei.NewDialog = true;
                }

                /* change duty cycle if we're changing to or from pulse */
                if (waveform == WAVEFORM.PULSE && ow != WAVEFORM.PULSE) {
                    dutyCycle = defaultPulseDuty;
                } else if (ow == WAVEFORM.PULSE && waveform != WAVEFORM.PULSE) {
                    dutyCycle = .5;
                }

                SetPoints();
            }
            if (n == 4) {
                phaseShift = ei.Value * ToRad;
            }
            if (n == 5) {
                dutyCycle = ei.Value * .01;
            }
        }
    }
}
