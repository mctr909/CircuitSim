using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class VoltageElm : CircuitElm {
        const int FLAG_COS = 2;
        const int FLAG_PULSE_DUTY = 4;

        protected int waveform;

        protected const int WF_DC = 0;
        protected const int WF_AC = 1;
        protected const int WF_SQUARE = 2;
        protected const int WF_TRIANGLE = 3;
        protected const int WF_SAWTOOTH = 4;
        protected const int WF_PULSE = 5;
        protected const int WF_NOISE = 6;
        protected const int WF_VAR = 7;

        protected const int circleSize = 32;

        protected double frequency;
        protected double maxVoltage;
        protected double bias;
        double phaseShift;
        double dutyCycle;
        double noiseValue;

        const double defaultPulseDuty = 1 / PI2;

        protected VoltageElm(int xx, int yy, int wf) : base(xx, yy) {
            waveform = wf;
            maxVoltage = 5;
            frequency = 40;
            dutyCycle = .5;
            reset();
        }

        public VoltageElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            maxVoltage = 5;
            frequency = 40;
            waveform = WF_DC;
            dutyCycle = .5;

            try {
                waveform = st.nextTokenInt();
                frequency = st.nextTokenDouble();
                maxVoltage = st.nextTokenDouble();
                bias = st.nextTokenDouble();
                phaseShift = st.nextTokenDouble();
                dutyCycle = st.nextTokenDouble();
            } catch { }

            if ((mFlags & FLAG_COS) != 0) {
                mFlags &= ~FLAG_COS;
                phaseShift = PI / 2;
            }

            /* old circuit files have the wrong duty cycle for pulse waveforms (wasn't configurable in the past) */
            if ((mFlags & FLAG_PULSE_DUTY) == 0 && waveform == WF_PULSE) {
                dutyCycle = defaultPulseDuty;
            }

            reset();
        }

        public override DUMP_ID getDumpType() { return DUMP_ID.VOLTAGE; }

        public override string dump() {
            /* set flag so we know if duty cycle is correct for pulse waveforms */
            if (waveform == WF_PULSE) {
                mFlags |= FLAG_PULSE_DUTY;
            } else {
                mFlags &= ~FLAG_PULSE_DUTY;
            }

            return base.dump()
                + " " + waveform
                + " " + frequency
                + " " + maxVoltage
                + " " + bias
                + " " + phaseShift
                + " " + dutyCycle;
            /* VarRailElm adds text at the end */
        }

        public override void reset() {
            mCurCount = 0;
        }

        double triangleFunc(double x) {
            if (x < PI) {
                return x * (2 / PI) - 1;
            }
            return 1 - (x - PI) * (2 / PI);
        }

        public override void stamp() {
            if (waveform == WF_DC) {
                Cir.StampVoltageSource(Nodes[0], Nodes[1], mVoltSource, getVoltage());
            } else {
                Cir.StampVoltageSource(Nodes[0], Nodes[1], mVoltSource);
            }
        }

        public override void doStep() {
            if (waveform != WF_DC) {
                Cir.UpdateVoltageSource(Nodes[0], Nodes[1], mVoltSource, getVoltage());
            }
        }

        public override void stepFinished() {
            if (waveform == WF_NOISE) {
                noiseValue = (CirSim.random.NextDouble() * 2 - 1) * maxVoltage + bias;
            }
        }

        public virtual double getVoltage() {
            if (waveform != WF_DC && Sim.dcAnalysisFlag) {
                return bias;
            }

            double w = PI2 * (Sim.t) * frequency + phaseShift;
            switch (waveform) {
            case WF_DC:
                return maxVoltage + bias;
            case WF_AC:
                return Math.Sin(w) * maxVoltage + bias;
            case WF_SQUARE:
                return bias + ((w % PI2 > (PI2 * dutyCycle)) ? -maxVoltage : maxVoltage);
            case WF_TRIANGLE:
                return bias + triangleFunc(w % PI2) * maxVoltage;
            case WF_SAWTOOTH:
                return bias + (w % PI2) * (maxVoltage / PI) - maxVoltage;
            case WF_PULSE:
                return ((w % PI2) < (PI2 * dutyCycle)) ? maxVoltage + bias : bias;
            case WF_NOISE:
                return noiseValue;
            default: return 0;
            }
        }

        public override void setPoints() {
            base.setPoints();
            calcLeads((waveform == WF_DC || waveform == WF_VAR) ? 8 : circleSize);
        }

        public override void draw(Graphics g) {
            setBbox(X1, Y1, X2, Y2);
            draw2Leads(g);
            if (waveform == WF_DC) {
                interpPoint(mLead1, mLead2, ref ps1, ref ps2, 0, 10);
                drawThickLine(g, getVoltageColor(Volts[0]), ps1, ps2);

                int hs = 16;
                setBbox(mPoint1, mPoint2, hs);
                interpPoint(mLead1, mLead2, ref ps1, ref ps2, 1, hs);
                drawThickLine(g, getVoltageColor(Volts[1]), ps1, ps2);
            } else {
                setBbox(mPoint1, mPoint2, circleSize);
                interpPoint(mLead1, mLead2, ref ps1, .5);
                drawWaveform(g, ps1);
                string inds;
                if (bias > 0 || (bias == 0 && waveform == WF_PULSE)) {
                    inds = "+";
                } else {
                    inds = "*";
                }

                var plusPoint = interpPoint(mPoint1, mPoint2, (mElmLen / 2 + circleSize + 4) / mElmLen, 10 * mDsign);
                plusPoint.Y += 4;
                var w = (int)g.MeasureString(inds, FONT_TERM_NAME).Width;
                g.DrawString(inds, FONT_TERM_NAME, BRUSH_TERM_NAME, plusPoint.X - w / 2, plusPoint.Y);
            }

            updateDotCount();

            if (Sim.dragElm != this) {
                if (waveform == WF_DC) {
                    drawDots(g, mPoint1, mPoint2, mCurCount);
                } else {
                    drawDots(g, mPoint1, mLead1, mCurCount);
                    drawDots(g, mPoint2, mLead2, -mCurCount);
                }
            }
            drawPosts(g);
        }

        protected void drawWaveform(Graphics g, Point center) {
            int x = center.X;
            int y = center.Y;

            if (waveform != WF_NOISE) {
                PenThickLine.Color = needsHighlight() ? SelectColor : Color.Gray;
                drawThickCircle(g, x, y, circleSize);
            }

            adjustBbox(x - circleSize, y - circleSize, x + circleSize, y + circleSize);

            float h = 10;
            float xd = (float)(h * 2 * dutyCycle - h + x);
            xd = Math.Max(x - h + 1, Math.Min(x + h - 1, xd));

            PenLine.Color = needsHighlight() ? SelectColor : Color.YellowGreen;

            switch (waveform) {
            case WF_DC: {
                break;
            }
            case WF_SQUARE:
                if (maxVoltage < 0) {
                    drawLine(g, x - h, y + h, x - h, y    );
                    drawLine(g, x - h, y + h, xd   , y + h);
                    drawLine(g, xd   , y + h, xd   , y - h);
                    drawLine(g, x + h, y - h, xd   , y - h);
                    drawLine(g, x + h, y    , x + h, y - h);
                } else {
                    drawLine(g, x - h, y - h, x - h, y    );
                    drawLine(g, x - h, y - h, xd   , y - h);
                    drawLine(g, xd   , y - h, xd   , y + h);
                    drawLine(g, x + h, y + h, xd   , y + h);
                    drawLine(g, x + h, y    , x + h, y + h);
                }
                break;
            case WF_PULSE:
                if (maxVoltage < 0) {
                    drawLine(g, x + h, y    , x + h, y    );
                    drawLine(g, x + h, y    , xd   , y    );
                    drawLine(g, xd   , y + h, xd   , y    );
                    drawLine(g, x - h, y + h, xd   , y + h);
                    drawLine(g, x - h, y + h, x - h, y    );
                } else {
                    drawLine(g, x - h, y - h, x - h, y    );
                    drawLine(g, x - h, y - h, xd   , y - h);
                    drawLine(g, xd   , y - h, xd   , y    );
                    drawLine(g, x + h, y    , xd   , y    );
                    drawLine(g, x + h, y    , x + h, y    );
                }
                break;
            case WF_SAWTOOTH:
                drawLine(g, x, y - h, x - h, y    );
                drawLine(g, x, y - h, x    , y + h);
                drawLine(g, x, y + h, x + h, y    );
                break;
            case WF_TRIANGLE: {
                int xl = 5;
                drawLine(g, x - xl * 2, y    , x - xl    , y - h);
                drawLine(g, x - xl    , y - h, x         , y    );
                drawLine(g, x         , y    , x + xl    , y + h);
                drawLine(g, x + xl    , y + h, x + xl * 2, y    );
                break;
            }
            case WF_NOISE: {
                drawCenteredText(g, "Noise", x, y, true);
                break;
            }
            case WF_AC: {
                int xl = 10;
                int x0 = 0;
                float y0 = 0;
                for (int i = -xl; i <= xl; i++) {
                    var yy = y + (float)(.95 * Math.Sin(i * PI / xl) * h);
                    if (i != -xl) {
                        drawLine(g, x0, y0, x + i, yy);
                    }
                    x0 = x + i;
                    y0 = yy;
                }
                break;
            }
            }

            if (Sim.chkShowValuesCheckItem.Checked && waveform != WF_NOISE) {
                var s = getShortUnitText(maxVoltage, "V ");
                s += getShortUnitText(bias, "V\r\n");
                s += getShortUnitText(frequency, "Hz\r\n");
                s += getShortUnitText(phaseShift * TO_DEG, "°");
                drawValues(g, s, 0);
            }
        }

        public override int getVoltageSourceCount() {
            return 1;
        }

        public override double getPower() { return -getVoltageDiff() * mCurrent; }

        public override double getVoltageDiff() { return Volts[1] - Volts[0]; }

        public override void getInfo(string[] arr) {
            switch (waveform) {
            case WF_DC:
            case WF_VAR:
                arr[0] = "voltage source"; break;
            case WF_AC:
                arr[0] = "A/C source"; break;
            case WF_SQUARE:
                arr[0] = "square wave gen"; break;
            case WF_PULSE:
                arr[0] = "pulse gen"; break;
            case WF_SAWTOOTH:
                arr[0] = "sawtooth gen"; break;
            case WF_TRIANGLE:
                arr[0] = "triangle gen"; break;
            case WF_NOISE:
                arr[0] = "noise gen"; break;
            }

            arr[1] = "I = " + getCurrentText(getCurrent());
            arr[2] = (typeof(RailElm) == GetType() ? "V = " : "Vd = ") + getVoltageText(getVoltageDiff());
            int i = 3;
            if (waveform != WF_DC && waveform != WF_VAR && waveform != WF_NOISE) {
                arr[i++] = "f = " + getUnitText(frequency, "Hz");
                arr[i++] = "Vmax = " + getVoltageText(maxVoltage);
                if (waveform == WF_AC && bias == 0) {
                    arr[i++] = "V(rms) = " + getVoltageText(maxVoltage / 1.41421356);
                }
                if (bias != 0) {
                    arr[i++] = "Voff = " + getVoltageText(bias);
                } else if (frequency > 500) {
                    arr[i++] = "wavelength = " + getUnitText(2.9979e8 / frequency, "m");
                }
            }
            if (waveform == WF_DC && mCurrent != 0 && Cir.ShowResistanceInVoltageSources) {
                arr[i++] = "(R = " + getUnitText(maxVoltage / mCurrent, CirSim.ohmString) + ")";
            }
            arr[i++] = "P = " + getUnitText(getPower(), "W");
        }

        public override EditInfo getEditInfo(int n) {
            if (n == 0) {
                return new EditInfo(waveform == WF_DC ? "Voltage" : "Max Voltage", maxVoltage, -20, 20);
            }
            if (n == 1) {
                var ei = new EditInfo("Waveform", waveform, -1, -1);
                ei.Choice = new ComboBox();
                ei.Choice.Items.Add("D/C");
                ei.Choice.Items.Add("A/C");
                ei.Choice.Items.Add("Square Wave");
                ei.Choice.Items.Add("Triangle");
                ei.Choice.Items.Add("Sawtooth");
                ei.Choice.Items.Add("Pulse");
                ei.Choice.Items.Add("Noise");
                ei.Choice.SelectedIndex = waveform;
                return ei;
            }
            if (n == 2) {
                return new EditInfo("DC Offset (V)", bias, -20, 20);
            }
            if (waveform == WF_DC || waveform == WF_NOISE) {
                return null;
            }
            if (n == 3) {
                return new EditInfo("Frequency (Hz)", frequency, 4, 500);
            }
            if (n == 4) {
                return new EditInfo("Phase Offset (degrees)", phaseShift * TO_DEG, -180, 180).SetDimensionless();
            }
            if (n == 5 && (waveform == WF_PULSE || waveform == WF_SQUARE)) {
                return new EditInfo("Duty Cycle", dutyCycle * 100, 0, 100).SetDimensionless();
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
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
                if (frequency > maxfreq) {
                    if (MessageBox.Show("Adjust timestep to allow for higher frequencies?", "", MessageBoxButtons.OKCancel) == DialogResult.OK) {
                        Sim.timeStep = 1 / (32 * frequency);
                    } else {
                        frequency = maxfreq;
                    }
                }
                double adj = frequency - oldfreq;
            }
            if (n == 1) {
                int ow = waveform;
                waveform = ei.Choice.SelectedIndex;
                if (waveform == WF_DC && ow != WF_DC) {
                    ei.NewDialog = true;
                    bias = 0;
                } else if (waveform != ow) {
                    ei.NewDialog = true;
                }

                /* change duty cycle if we're changing to or from pulse */
                if (waveform == WF_PULSE && ow != WF_PULSE) {
                    dutyCycle = defaultPulseDuty;
                } else if (ow == WF_PULSE && waveform != WF_PULSE) {
                    dutyCycle = .5;
                }

                setPoints();
            }
            if (n == 4) {
                phaseShift = ei.Value * TO_RAD;
            }
            if (n == 5) {
                dutyCycle = ei.Value * .01;
            }
        }
    }
}
