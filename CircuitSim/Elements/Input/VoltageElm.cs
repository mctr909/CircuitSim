using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Input {
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
            PWM_POSITIVE,
            PWM_NEGATIVE,
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

        const double defaultPulseDuty = 0.5 / Math.PI;

        protected VoltageElm(Point pos, WAVEFORM wf) : base(pos) {
            waveform = wf;
            maxVoltage = 5;
            frequency = 40;
            dutyCycle = .5;
            Reset();
        }

        public VoltageElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
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
                phaseShift = Math.PI / 2;
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
            if (x < Math.PI) {
                return x * (2 / Math.PI) - 1;
            }
            return 1 - (x - Math.PI) * (2 / Math.PI);
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
                noiseValue = (CirSim.Random.NextDouble() * 2 - 1) * maxVoltage + bias;
            }
        }

        public virtual double getVoltage() {
            if (waveform != WAVEFORM.DC && CirSim.Sim.DcAnalysisFlag) {
                return bias;
            }

            double t = 2 * Math.PI * CirSim.Sim.Time;
            double wt = t * frequency + phaseShift;

            switch (waveform) {
            case WAVEFORM.DC:
                return maxVoltage + bias;
            case WAVEFORM.AC:
                return Math.Sin(wt) * maxVoltage + bias;
            case WAVEFORM.SQUARE:
                return bias + ((wt % (2 * Math.PI) > ((2 * Math.PI) * dutyCycle)) ? -maxVoltage : maxVoltage);
            case WAVEFORM.TRIANGLE:
                return bias + triangleFunc(wt % (2 * Math.PI)) * maxVoltage;
            case WAVEFORM.SAWTOOTH:
                return bias + (wt % (2 * Math.PI)) * (maxVoltage / Math.PI) - maxVoltage;
            case WAVEFORM.PULSE:
                return ((wt % (2 * Math.PI)) < ((2 * Math.PI) * dutyCycle)) ? maxVoltage + bias : bias;
            case WAVEFORM.PWM_BOTH: {
                var maxfreq = 1 / (32 * ControlPanel.TimeStep);
                var cr = 0.5 - 0.5 * triangleFunc(t * maxfreq % (2 * Math.PI));
                var sg = dutyCycle * Math.Sin(wt) + Math.Sin(wt * 3) / 6;
                if (0.0 <= sg) {
                    return bias + (cr < sg ? maxVoltage : 0);
                } else {
                    return bias - (sg < -cr ? maxVoltage : 0);
                }
            }
            case WAVEFORM.PWM_POSITIVE: {
                var maxfreq = 1 / (32 * ControlPanel.TimeStep);
                var cr = 0.5 - 0.5 * triangleFunc(t * maxfreq % (2 * Math.PI));
                var sg = dutyCycle * Math.Sin(wt) + Math.Sin(wt * 3) / 6;
                if (0.0 <= sg) {
                    return bias + (cr < sg ? maxVoltage : 0);
                } else {
                    return bias;
                }
            }
            case WAVEFORM.PWM_NEGATIVE: {
                var maxfreq = 1 / (32 * ControlPanel.TimeStep);
                var cr = 0.5 - 0.5 * triangleFunc(t * maxfreq % (2 * Math.PI));
                var sg = dutyCycle * Math.Sin(wt) + Math.Sin(wt * 3) / 6;
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
                interpPoint(ref textPos, 0.5, -16 * sign);
            } else {
                interpPoint(ref textPos, (mLen / 2 + 0.7 * circleSize) / mLen, 10 * sign);
            }
        }

        public override void Draw(CustomGraphics g) {
            setBbox(P1.X, P1.Y, P2.X, P2.Y);
            draw2Leads(g);

            if (waveform == WAVEFORM.DC) {
                interpLeadAB(ref ps1, ref ps2, 0, 10);
                drawVoltage(g, 0, ps1, ps2);

                int hs = 16;
                setBbox(mPoint1, mPoint2, hs);
                interpLeadAB(ref ps1, ref ps2, 1, hs);
                drawVoltage(g, 1, ps1, ps2);
                string s = Utils.ShortUnitText(maxVoltage, "V");
                g.DrawRightText(s, textPos.X, textPos.Y);
            } else {
                setBbox(mPoint1, mPoint2, circleSize);
                interpLead(ref ps1, 0.5);
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

            if (CirSim.Sim.DragElm != this) {
                if (waveform == WAVEFORM.DC) {
                    drawDots(g, mPoint1, mPoint2, mCurCount);
                } else {
                    drawDots(g, mPoint1, mLead1, mCurCount);
                    drawDots(g, mPoint2, mLead2, -mCurCount);
                }
            }
            drawPosts(g);
        }

        protected void drawWaveform(CustomGraphics g, PointF center) {
            var x = center.X;
            var y = center.Y;

            if (waveform != WAVEFORM.NOISE) {
                g.ThickLineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
                g.DrawThickCircle(center, circleSize);
            }

            adjustBbox(
                x - circleSize, y - circleSize,
                x + circleSize, y + circleSize
            );

            float h = 11;
            float xd = (float)(h * 2 * dutyCycle - h + x);
            xd = Math.Max(x - h + 1, Math.Min(x + h - 1, xd));

            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;

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
                var xl = 10f;
                var x0 = 0f;
                float y0 = 0;
                for (var i = -xl; i <= xl; i++) {
                    var yy = y + (float)(.95 * Math.Sin(i * Math.PI / xl) * h);
                    if (i != -xl) {
                        g.DrawLine(x0, y0, x + i, yy);
                    }
                    x0 = x + i;
                    y0 = yy;
                }
                break;
            }
            }

            if (ControlPanel.ChkShowValues.Checked && waveform != WAVEFORM.NOISE) {
                var s = Utils.ShortUnitText(maxVoltage, "V\r\n");
                s += Utils.ShortUnitText(frequency, "Hz\r\n");
                s += Utils.ShortUnitText(phaseShift * 180 / Math.PI, "°");
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
                arr[i++] = "(R = " + Utils.UnitText(maxVoltage / mCurrent, CirSim.OHM_TEXT) + ")";
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
                ei.Choice.Items.Add(WAVEFORM.PWM_POSITIVE);
                ei.Choice.Items.Add(WAVEFORM.PWM_NEGATIVE);
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
                return new ElementInfo("Phase Offset (degrees)", phaseShift * 180 * Math.PI, -180, 180).SetDimensionless();
            }
            if (n == 5 && (waveform == WAVEFORM.PULSE || waveform == WAVEFORM.SQUARE
                || waveform == WAVEFORM.PWM_BOTH || waveform == WAVEFORM.PWM_POSITIVE || waveform == WAVEFORM.PWM_NEGATIVE)) {
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
                double maxfreq = 1 / (8 * ControlPanel.TimeStep);
                if (maxfreq < frequency) {
                    if (MessageBox.Show("Adjust timestep to allow for higher frequencies?", "", MessageBoxButtons.OKCancel) == DialogResult.OK) {
                        ControlPanel.TimeStep = 1 / (32 * frequency);
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
                phaseShift = ei.Value * Math.PI / 180;
            }
            if (n == 5) {
                dutyCycle = ei.Value * .01;
            }
        }
    }
}
