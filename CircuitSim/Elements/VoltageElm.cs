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

        const double defaultPulseDuty = 1 / (2 * Math.PI);

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
                phaseShift = pi / 2;
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
            if (x < pi) {
                return x * (2 / pi) - 1;
            }
            return 1 - (x - pi) * (2 / pi);
        }

        public override void stamp() {
            if (waveform == WF_DC) {
                cir.StampVoltageSource(Nodes[0], Nodes[1], mVoltSource, getVoltage());
            } else {
                cir.StampVoltageSource(Nodes[0], Nodes[1], mVoltSource);
            }
        }

        public override void doStep() {
            if (waveform != WF_DC) {
                cir.UpdateVoltageSource(Nodes[0], Nodes[1], mVoltSource, getVoltage());
            }
        }

        public override void stepFinished() {
            if (waveform == WF_NOISE) {
                noiseValue = (CirSim.random.NextDouble() * 2 - 1) * maxVoltage + bias;
            }
        }

        public virtual double getVoltage() {
            if (waveform != WF_DC && sim.dcAnalysisFlag) {
                return bias;
            }

            double w = 2 * pi * (sim.t) * frequency + phaseShift;
            switch (waveform) {
            case WF_DC:
                return maxVoltage + bias;
            case WF_AC:
                return Math.Sin(w) * maxVoltage + bias;
            case WF_SQUARE:
                return bias + ((w % (2 * pi) > (2 * pi * dutyCycle)) ? -maxVoltage : maxVoltage);
            case WF_TRIANGLE:
                return bias + triangleFunc(w % (2 * pi)) * maxVoltage;
            case WF_SAWTOOTH:
                return bias + (w % (2 * pi)) * (maxVoltage / pi) - maxVoltage;
            case WF_PULSE:
                return ((w % (2 * pi)) < (2 * pi * dutyCycle)) ? maxVoltage + bias : bias;
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

            if (sim.dragElm != this) {
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
                PEN_THICK_LINE.Color = needsHighlight() ? selectColor : Color.Gray;
                drawThickCircle(g, x, y, circleSize);
            }

            adjustBbox(x - circleSize, y - circleSize, x + circleSize, y + circleSize);

            float h = 9;
            float xd = (float)(h * 2 * dutyCycle - h + x);
            xd = Math.Max(x - h + 1, Math.Min(x + h - 1, xd));

            PEN_THICK_LINE.Color = needsHighlight() ? selectColor : Color.YellowGreen;

            switch (waveform) {
            case WF_DC: {
                break;
            }
            case WF_SQUARE:
                if (maxVoltage < 0) {
                    drawThickLine(g, x - h, y + h, x - h, y    );
                    drawThickLine(g, x - h, y + h, xd   , y + h);
                    drawThickLine(g, xd   , y + h, xd   , y - h);
                    drawThickLine(g, x + h, y - h, xd   , y - h);
                    drawThickLine(g, x + h, y    , x + h, y - h);
                } else {
                    drawThickLine(g, x - h, y - h, x - h, y    );
                    drawThickLine(g, x - h, y - h, xd   , y - h);
                    drawThickLine(g, xd   , y - h, xd   , y + h);
                    drawThickLine(g, x + h, y + h, xd   , y + h);
                    drawThickLine(g, x + h, y    , x + h, y + h);
                }
                break;
            case WF_PULSE:
                if (maxVoltage < 0) {
                    drawThickLine(g, x + h, y    , x + h, y    );
                    drawThickLine(g, x + h, y    , xd   , y    );
                    drawThickLine(g, xd   , y + h, xd   , y    );
                    drawThickLine(g, x - h, y + h, xd   , y + h);
                    drawThickLine(g, x - h, y + h, x - h, y    );
                } else {
                    drawThickLine(g, x - h, y - h, x - h, y    );
                    drawThickLine(g, x - h, y - h, xd   , y - h);
                    drawThickLine(g, xd   , y - h, xd   , y    );
                    drawThickLine(g, x + h, y    , xd   , y    );
                    drawThickLine(g, x + h, y    , x + h, y    );
                }
                break;
            case WF_SAWTOOTH:
                drawThickLine(g, x, y - h, x - h, y    );
                drawThickLine(g, x, y - h, x    , y + h);
                drawThickLine(g, x, y + h, x + h, y    );
                break;
            case WF_TRIANGLE: {
                int xl = 5;
                drawThickLine(g, x - xl * 2, y    , x - xl    , y - h);
                drawThickLine(g, x - xl    , y - h, x         , y    );
                drawThickLine(g, x         , y    , x + xl    , y + h);
                drawThickLine(g, x + xl    , y + h, x + xl * 2, y    );
                break;
            }
            case WF_NOISE: {
                drawCenteredText(g, "Noise", x, y, true);
                break;
            }
            case WF_AC: {
                int xl = 10;
                int x0 = 0;
                int y0 = 0;
                for (int i = -xl; i <= xl; i++) {
                    int yy = y + (int)(.95 * Math.Sin(i * pi / xl) * h);
                    if (i != -xl) {
                        g.DrawLine(PEN_THICK_LINE, x0, y0, x + i, yy);
                    }
                    x0 = x + i;
                    y0 = yy;
                }
                break;
            }
            }

            if (sim.chkShowValuesCheckItem.Checked && waveform != WF_NOISE) {
                var s = getShortUnitText(maxVoltage, "V ");
                s += getShortUnitText(bias, "V\r\n");
                s += getShortUnitText(frequency, "Hz\r\n");
                s += getShortUnitText(phaseShift * 180 / Math.PI, "°");
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
            if (waveform == WF_DC && mCurrent != 0 && cir.ShowResistanceInVoltageSources) {
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
                ei.choice = new ComboBox();
                ei.choice.Items.Add("D/C");
                ei.choice.Items.Add("A/C");
                ei.choice.Items.Add("Square Wave");
                ei.choice.Items.Add("Triangle");
                ei.choice.Items.Add("Sawtooth");
                ei.choice.Items.Add("Pulse");
                ei.choice.Items.Add("Noise");
                ei.choice.SelectedIndex = waveform;
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
                return new EditInfo("Phase Offset (degrees)", phaseShift * 180 / pi, -180, 180).setDimensionless();
            }
            if (n == 5 && (waveform == WF_PULSE || waveform == WF_SQUARE)) {
                return new EditInfo("Duty Cycle", dutyCycle * 100, 0, 100).setDimensionless();
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (n == 0) {
                maxVoltage = ei.value;
            }
            if (n == 2) {
                bias = ei.value;
            }
            if (n == 3) {
                /* adjust time zero to maintain continuity ind the waveform
                 * even though the frequency has changed. */
                double oldfreq = frequency;
                frequency = ei.value;
                double maxfreq = 1 / (8 * sim.timeStep);
                if (frequency > maxfreq) {
                    if (MessageBox.Show("Adjust timestep to allow for higher frequencies?", "", MessageBoxButtons.OKCancel) == DialogResult.OK) {
                        sim.timeStep = 1 / (32 * frequency);
                    } else {
                        frequency = maxfreq;
                    }
                }
                double adj = frequency - oldfreq;
            }
            if (n == 1) {
                int ow = waveform;
                waveform = ei.choice.SelectedIndex;
                if (waveform == WF_DC && ow != WF_DC) {
                    ei.newDialog = true;
                    bias = 0;
                } else if (waveform != ow) {
                    ei.newDialog = true;
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
                phaseShift = ei.value * pi / 180;
            }
            if (n == 5) {
                dutyCycle = ei.value * .01;
            }
        }
    }
}
