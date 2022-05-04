using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Input {
    class VoltageElm : CircuitElm {
        const int FLAG_COS = 2;
        const int FLAG_PULSE_DUTY = 4;

        const double DEFAULT_PULSE_DUTY = 0.5 / Math.PI;

        protected const int BODY_LEN = 28;
        const int BODY_LEN_DC = 8;

        protected double mFrequency;
        protected double mMaxVoltage;
        protected double mBias;
        double mPhaseShift;
        double mDutyCycle;
        double mNoiseValue;

        Point mPs1;
        Point mPs2;
        Point mTextPos;

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

        protected VoltageElm(Point pos, WAVEFORM wf) : base(pos) {
            waveform = wf;
            mMaxVoltage = 5;
            mFrequency = 40;
            mDutyCycle = .5;
            CirReset();
        }

        public VoltageElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mMaxVoltage = 5;
            mFrequency = 40;
            waveform = WAVEFORM.DC;
            mDutyCycle = .5;

            try {
                waveform = st.nextTokenEnum<WAVEFORM>();
                mFrequency = st.nextTokenDouble();
                mMaxVoltage = st.nextTokenDouble();
                mBias = st.nextTokenDouble();
                mPhaseShift = st.nextTokenDouble();
                mDutyCycle = st.nextTokenDouble();
            } catch { }

            if ((mFlags & FLAG_COS) != 0) {
                mFlags &= ~FLAG_COS;
                mPhaseShift = Math.PI / 2;
            }

            /* old circuit files have the wrong duty cycle for pulse waveforms (wasn't configurable in the past) */
            if ((mFlags & FLAG_PULSE_DUTY) == 0 && waveform == WAVEFORM.PULSE) {
                mDutyCycle = DEFAULT_PULSE_DUTY;
            }

            CirReset();
        }

        public override double CirVoltageDiff { get { return CirVolts[1] - CirVolts[0]; } }

        public override double CirPower { get { return -CirVoltageDiff * mCirCurrent; } }

        public override int CirVoltageSourceCount { get { return 1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.VOLTAGE; } }

        protected override string dump() {
            /* set flag so we know if duty cycle is correct for pulse waveforms */
            if (waveform == WAVEFORM.PULSE) {
                mFlags |= FLAG_PULSE_DUTY;
            } else {
                mFlags &= ~FLAG_PULSE_DUTY;
            }

            return waveform
                + " " + mFrequency
                + " " + mMaxVoltage
                + " " + mBias
                + " " + mPhaseShift
                + " " + mDutyCycle;
            /* VarRailElm adds text at the end */
        }

        public override void CirReset() {
            mCirCurCount = 0;
        }

        double triangleFunc(double x) {
            if (x < Math.PI) {
                return x * (2 / Math.PI) - 1;
            }
            return 1 - (x - Math.PI) * (2 / Math.PI);
        }

        public override void CirStamp() {
            if (waveform == WAVEFORM.DC) {
                mCir.StampVoltageSource(CirNodes[0], CirNodes[1], mCirVoltSource, getVoltage());
            } else {
                mCir.StampVoltageSource(CirNodes[0], CirNodes[1], mCirVoltSource);
            }
        }

        public override void CirDoStep() {
            if (waveform != WAVEFORM.DC) {
                mCir.UpdateVoltageSource(CirNodes[0], CirNodes[1], mCirVoltSource, getVoltage());
            }
        }

        public override void CirStepFinished() {
            if (waveform == WAVEFORM.NOISE) {
                mNoiseValue = (CirSim.Random.NextDouble() * 2 - 1) * mMaxVoltage + mBias;
            }
        }

        public virtual double getVoltage() {
            if (waveform != WAVEFORM.DC && CirSim.Sim.DcAnalysisFlag) {
                return mBias;
            }

            double t = 2 * Math.PI * CirSim.Sim.Time;
            double wt = t * mFrequency + mPhaseShift;

            switch (waveform) {
            case WAVEFORM.DC:
                return mMaxVoltage + mBias;
            case WAVEFORM.AC:
                return Math.Sin(wt) * mMaxVoltage + mBias;
            case WAVEFORM.SQUARE:
                return mBias + ((wt % (2 * Math.PI) > ((2 * Math.PI) * mDutyCycle)) ? -mMaxVoltage : mMaxVoltage);
            case WAVEFORM.TRIANGLE:
                return mBias + triangleFunc(wt % (2 * Math.PI)) * mMaxVoltage;
            case WAVEFORM.SAWTOOTH:
                return mBias + (wt % (2 * Math.PI)) * (mMaxVoltage / Math.PI) - mMaxVoltage;
            case WAVEFORM.PULSE:
                return ((wt % (2 * Math.PI)) < ((2 * Math.PI) * mDutyCycle)) ? mMaxVoltage + mBias : mBias;
            case WAVEFORM.PWM_BOTH: {
                var maxfreq = 1 / (32 * ControlPanel.TimeStep);
                var cr = 0.5 - 0.5 * triangleFunc(t * maxfreq % (2 * Math.PI));
                var sg = mDutyCycle * Math.Sin(wt);
                if (0.0 <= sg) {
                    return mBias + (cr < sg ? mMaxVoltage : 0);
                } else {
                    return mBias - (sg < -cr ? mMaxVoltage : 0);
                }
            }
            case WAVEFORM.PWM_POSITIVE: {
                var maxfreq = 1 / (32 * ControlPanel.TimeStep);
                var cr = 0.5 - 0.5 * triangleFunc(t * maxfreq % (2 * Math.PI));
                var sg = mDutyCycle * Math.Sin(wt);
                if (0.0 <= sg) {
                    return mBias + (cr < sg ? mMaxVoltage : 0);
                } else {
                    return mBias;
                }
            }
            case WAVEFORM.PWM_NEGATIVE: {
                var maxfreq = 1 / (32 * ControlPanel.TimeStep);
                var cr = 0.5 - 0.5 * triangleFunc(t * maxfreq % (2 * Math.PI));
                var sg = mDutyCycle * Math.Sin(wt);
                if (0.0 <= sg) {
                    return mBias;
                } else {
                    return mBias + (sg < -cr ? mMaxVoltage : 0);
                }
            }
            case WAVEFORM.NOISE:
                return mNoiseValue;
            default: return 0;
            }
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads((waveform == WAVEFORM.DC) ? BODY_LEN_DC : BODY_LEN);

            int sign;
            if (mPoint1.Y == mPoint2.Y) {
                sign = -mDsign;
            } else {
                sign = mDsign;
            }
            if (waveform == WAVEFORM.DC) {
                interpPoint(ref mTextPos, 0.5, -2 * BODY_LEN_DC * sign);
            } else {
                interpPoint(ref mTextPos, (mLen / 2 + 0.7 * BODY_LEN) / mLen, 10 * sign);
            }
        }

        public override void Draw(CustomGraphics g) {
            setBbox(P1, P2);
            draw2Leads();

            if (waveform == WAVEFORM.DC) {
                int hs = 12;
                setBbox(mPoint1, mPoint2, hs);

                interpLeadAB(ref mPs1, ref mPs2, 0, hs * 0.5);
                drawLead(mPs1, mPs2);

                interpLeadAB(ref mPs1, ref mPs2, 1, hs);
                drawLead(mPs1, mPs2);

                string s = Utils.UnitText(mMaxVoltage, "V");
                g.DrawRightText(s, mTextPos.X, mTextPos.Y);
            } else {
                setBbox(mPoint1, mPoint2, BODY_LEN);
                interpLead(ref mPs1, 0.5);
                drawWaveform(g, mPs1);
                string inds;
                if (0 < mBias || (0 == mBias && WAVEFORM.PULSE == waveform)) {
                    inds = "+";
                } else {
                    inds = "*";
                }
                drawCenteredLText(inds, mTextPos, true);
            }

            cirUpdateDotCount();

            if (CirSim.Sim.DragElm != this) {
                if (waveform == WAVEFORM.DC) {
                    drawDots(mPoint1, mPoint2, mCirCurCount);
                } else {
                    drawDots(mPoint1, mLead1, mCirCurCount);
                    drawDots(mPoint2, mLead2, -mCirCurCount);
                }
            }
            drawPosts();
        }

        protected void drawWaveform(CustomGraphics g, Point center) {
            var x = center.X;
            var y = center.Y;

            if (waveform != WAVEFORM.NOISE) {
                g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
                g.DrawCircle(center, BODY_LEN / 2);
            }

            adjustBbox(
                x - BODY_LEN, y - BODY_LEN,
                x + BODY_LEN, y + BODY_LEN
            );

            var h = 7;
            var xd = (int)(h * 2 * mDutyCycle - h + x);
            xd = Math.Max(x - h + 1, Math.Min(x + h - 1, xd));

            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;

            switch (waveform) {
            case WAVEFORM.DC: {
                break;
            }
            case WAVEFORM.SQUARE:
                if (mMaxVoltage < 0) {
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
                if (mMaxVoltage < 0) {
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
                drawCenteredText("Noise", center, true);
                break;
            }
            case WAVEFORM.AC: {
                var xl = 10;
                var x0 = 0;
                var y0 = 0;
                for (var i = -xl; i <= xl; i++) {
                    var yy = y + (int)(.95 * Math.Sin(i * Math.PI / xl) * h);
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
                var s = Utils.UnitText(mMaxVoltage, "V\r\n");
                s += Utils.UnitText(mFrequency, "Hz\r\n");
                s += Utils.UnitText(mPhaseShift * 180 / Math.PI, "°");
                drawValues(s, 0, 5);
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

            arr[1] = "I = " + Utils.CurrentText(mCirCurrent);
            arr[2] = ((this is RailElm) ? "V = " : "Vd = ") + Utils.VoltageText(CirVoltageDiff);
            int i = 3;
            if (waveform != WAVEFORM.DC && waveform != WAVEFORM.NOISE) {
                arr[i++] = "f = " + Utils.UnitText(mFrequency, "Hz");
                arr[i++] = "Vmax = " + Utils.VoltageText(mMaxVoltage);
                if (waveform == WAVEFORM.AC && mBias == 0) {
                    arr[i++] = "V(rms) = " + Utils.VoltageText(mMaxVoltage / 1.41421356);
                }
                if (mBias != 0) {
                    arr[i++] = "Voff = " + Utils.VoltageText(mBias);
                } else if (mFrequency > 500) {
                    arr[i++] = "wavelength = " + Utils.UnitText(2.9979e8 / mFrequency, "m");
                }
            }
            if (waveform == WAVEFORM.DC && mCirCurrent != 0 && mCir.ShowResistanceInVoltageSources) {
                arr[i++] = "(R = " + Utils.UnitText(mMaxVoltage / mCirCurrent, CirSim.OHM_TEXT) + ")";
            }
            arr[i++] = "P = " + Utils.UnitText(CirPower, "W");
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo(waveform == WAVEFORM.DC ? "電圧(V)" : "振幅(V)", mMaxVoltage, -20, 20);
            }
            if (n == 1) {
                var ei = new ElementInfo("波形", (int)waveform, -1, -1);
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
                return new ElementInfo("オフセット電圧(V)", mBias, -20, 20);
            }
            if (waveform == WAVEFORM.DC || waveform == WAVEFORM.NOISE) {
                return null;
            }
            if (n == 3) {
                return new ElementInfo("周波数(Hz)", mFrequency, 4, 500);
            }
            if (n == 4) {
                return new ElementInfo("位相(degrees)", double.Parse((mPhaseShift * 180 / Math.PI).ToString("0.00")), -180, 180).SetDimensionless();
            }
            if (n == 5 && (waveform == WAVEFORM.PULSE || waveform == WAVEFORM.SQUARE
                || waveform == WAVEFORM.PWM_BOTH || waveform == WAVEFORM.PWM_POSITIVE || waveform == WAVEFORM.PWM_NEGATIVE)) {
                return new ElementInfo("デューティ比", mDutyCycle * 100, 0, 100).SetDimensionless();
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                mMaxVoltage = ei.Value;
            }
            if (n == 2) {
                mBias = ei.Value;
            }
            if (n == 3) {
                /* adjust time zero to maintain continuity ind the waveform
                 * even though the frequency has changed. */
                double oldfreq = mFrequency;
                mFrequency = ei.Value;
                double maxfreq = 1 / (8 * ControlPanel.TimeStep);
                if (maxfreq < mFrequency) {
                    if (MessageBox.Show("Adjust timestep to allow for higher frequencies?", "", MessageBoxButtons.OKCancel) == DialogResult.OK) {
                        ControlPanel.TimeStep = 1 / (32 * mFrequency);
                    } else {
                        mFrequency = maxfreq;
                    }
                }
                double adj = mFrequency - oldfreq;
            }
            if (n == 1) {
                var ow = waveform;
                waveform = (WAVEFORM)ei.Choice.SelectedIndex;
                if (waveform == WAVEFORM.DC && ow != WAVEFORM.DC) {
                    ei.NewDialog = true;
                    mBias = 0;
                } else if (waveform != ow) {
                    ei.NewDialog = true;
                }

                /* change duty cycle if we're changing to or from pulse */
                if (waveform == WAVEFORM.PULSE && ow != WAVEFORM.PULSE) {
                    mDutyCycle = DEFAULT_PULSE_DUTY;
                } else if (ow == WAVEFORM.PULSE && waveform != WAVEFORM.PULSE) {
                    mDutyCycle = .5;
                }

                SetPoints();
            }
            if (n == 4) {
                mPhaseShift = ei.Value * Math.PI / 180;
            }
            if (n == 5) {
                mDutyCycle = ei.Value * .01;
            }
        }
    }
}
