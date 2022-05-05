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

        Point mPs1;
        Point mPs2;
        Point mTextPos;

        protected VoltageElm(Point pos, VoltageElmE.WAVEFORM wf) : base(pos) {
            CirElm = new VoltageElmE(wf);
        }

        public VoltageElm(Point p1, Point p2, int f) : base(p1, p2, f) { }

        public VoltageElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            CirElm = new VoltageElmE(st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.VOLTAGE; } }

        protected override string dump() {
            var elm = (VoltageElmE)CirElm;
            /* set flag so we know if duty cycle is correct for pulse waveforms */
            if (elm.waveform == VoltageElmE.WAVEFORM.PULSE) {
                mFlags |= FLAG_PULSE_DUTY;
            } else {
                mFlags &= ~FLAG_PULSE_DUTY;
            }

            return elm.waveform
                + " " + elm.mFrequency
                + " " + elm.mMaxVoltage
                + " " + elm.mBias
                + " " + elm.mPhaseShift
                + " " + elm.mDutyCycle;
            /* VarRailElm adds text at the end */
        }

        public override void SetPoints() {
            base.SetPoints();

            var elm = (VoltageElmE)CirElm;
            calcLeads((elm.waveform == VoltageElmE.WAVEFORM.DC) ? BODY_LEN_DC : BODY_LEN);

            int sign;
            if (mPoint1.Y == mPoint2.Y) {
                sign = -mDsign;
            } else {
                sign = mDsign;
            }
            if (elm.waveform == VoltageElmE.WAVEFORM.DC) {
                interpPoint(ref mTextPos, 0.5, -2 * BODY_LEN_DC * sign);
            } else {
                interpPoint(ref mTextPos, (mLen / 2 + 0.7 * BODY_LEN) / mLen, 10 * sign);
            }
        }

        public override void Draw(CustomGraphics g) {
            setBbox(P1, P2);
            draw2Leads();
            var elm = (VoltageElmE)CirElm;
            if (elm.waveform == VoltageElmE.WAVEFORM.DC) {
                int hs = 12;
                setBbox(mPoint1, mPoint2, hs);

                interpLeadAB(ref mPs1, ref mPs2, 0, hs * 0.5);
                drawLead(mPs1, mPs2);

                interpLeadAB(ref mPs1, ref mPs2, 1, hs);
                drawLead(mPs1, mPs2);

                string s = Utils.UnitText(elm.mMaxVoltage, "V");
                g.DrawRightText(s, mTextPos.X, mTextPos.Y);
            } else {
                setBbox(mPoint1, mPoint2, BODY_LEN);
                interpLead(ref mPs1, 0.5);
                drawWaveform(g, mPs1);
                string inds;
                if (0 < elm.mBias || (0 == elm.mBias && VoltageElmE.WAVEFORM.PULSE == elm.waveform)) {
                    inds = "+";
                } else {
                    inds = "*";
                }
                drawCenteredLText(inds, mTextPos, true);
            }

            CirElm.cirUpdateDotCount();

            if (CirSim.Sim.DragElm != this) {
                if (elm.waveform == VoltageElmE.WAVEFORM.DC) {
                    drawDots(mPoint1, mPoint2, CirElm.CurCount);
                } else {
                    drawDots(mPoint1, mLead1, CirElm.CurCount);
                    drawDots(mPoint2, mLead2, -CirElm.CurCount);
                }
            }
            drawPosts();
        }

        protected void drawWaveform(CustomGraphics g, Point center) {
            var x = center.X;
            var y = center.Y;
            var elm = (VoltageElmE)CirElm;

            if (elm.waveform != VoltageElmE.WAVEFORM.NOISE) {
                g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
                g.DrawCircle(center, BODY_LEN / 2);
            }

            adjustBbox(
                x - BODY_LEN, y - BODY_LEN,
                x + BODY_LEN, y + BODY_LEN
            );

            var h = 7;
            var xd = (int)(h * 2 * elm.mDutyCycle - h + x);
            xd = Math.Max(x - h + 1, Math.Min(x + h - 1, xd));

            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;

            switch (elm.waveform) {
            case VoltageElmE.WAVEFORM.DC: {
                break;
            }
            case VoltageElmE.WAVEFORM.SQUARE:
                if (elm.mMaxVoltage < 0) {
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
            case VoltageElmE.WAVEFORM.PULSE:
                if (elm.mMaxVoltage < 0) {
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
            case VoltageElmE.WAVEFORM.SAWTOOTH:
                g.DrawLine(x, y - h, x - h, y    );
                g.DrawLine(x, y - h, x    , y + h);
                g.DrawLine(x, y + h, x + h, y    );
                break;
            case VoltageElmE.WAVEFORM.TRIANGLE: {
                int xl = 5;
                g.DrawLine(x - xl * 2, y    , x - xl    , y - h);
                g.DrawLine(x - xl    , y - h, x         , y    );
                g.DrawLine(x         , y    , x + xl    , y + h);
                g.DrawLine(x + xl    , y + h, x + xl * 2, y    );
                break;
            }
            case VoltageElmE.WAVEFORM.NOISE: {
                drawCenteredText("Noise", center, true);
                break;
            }
            case VoltageElmE.WAVEFORM.AC: {
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

            if (ControlPanel.ChkShowValues.Checked && elm.waveform != VoltageElmE.WAVEFORM.NOISE) {
                var s = Utils.UnitText(elm.mMaxVoltage, "V\r\n");
                s += Utils.UnitText(elm.mFrequency, "Hz\r\n");
                s += Utils.UnitText(elm.mPhaseShift * 180 / Math.PI, "°");
                drawValues(s, 0, 5);
            }
        }

        public override void GetInfo(string[] arr) {
            var elm = (VoltageElmE)CirElm;
            switch (elm.waveform) {
            case VoltageElmE.WAVEFORM.DC:
            case VoltageElmE.WAVEFORM.AC:
            case VoltageElmE.WAVEFORM.SQUARE:
            case VoltageElmE.WAVEFORM.PULSE:
            case VoltageElmE.WAVEFORM.SAWTOOTH:
            case VoltageElmE.WAVEFORM.TRIANGLE:
            case VoltageElmE.WAVEFORM.NOISE:
            case VoltageElmE.WAVEFORM.PWM_BOTH:
                arr[0] = elm.waveform.ToString(); break;
            }

            arr[1] = "I = " + Utils.CurrentText(elm.Current);
            arr[2] = ((this is RailElm) ? "V = " : "Vd = ") + Utils.VoltageText(elm.VoltageDiff);
            int i = 3;
            if (elm.waveform != VoltageElmE.WAVEFORM.DC && elm.waveform != VoltageElmE.WAVEFORM.NOISE) {
                arr[i++] = "f = " + Utils.UnitText(elm.mFrequency, "Hz");
                arr[i++] = "Vmax = " + Utils.VoltageText(elm.mMaxVoltage);
                if (elm.waveform == VoltageElmE.WAVEFORM.AC && elm.mBias == 0) {
                    arr[i++] = "V(rms) = " + Utils.VoltageText(elm.mMaxVoltage / 1.41421356);
                }
                if (elm.mBias != 0) {
                    arr[i++] = "Voff = " + Utils.VoltageText(elm.mBias);
                } else if (elm.mFrequency > 500) {
                    arr[i++] = "wavelength = " + Utils.UnitText(2.9979e8 / elm.mFrequency, "m");
                }
            }
            if (elm.waveform == VoltageElmE.WAVEFORM.DC && elm.Current != 0 && BaseElement.mCir.ShowResistanceInVoltageSources) {
                arr[i++] = "(R = " + Utils.UnitText(elm.mMaxVoltage / elm.Current, CirSim.OHM_TEXT) + ")";
            }
            arr[i++] = "P = " + Utils.UnitText(elm.Power, "W");
        }

        public override ElementInfo GetElementInfo(int n) {
            var elm = (VoltageElmE)CirElm;
            if (n == 0) {
                return new ElementInfo(elm.waveform == VoltageElmE.WAVEFORM.DC ? "電圧(V)" : "振幅(V)", elm.mMaxVoltage, -20, 20);
            }
            if (n == 1) {
                var ei = new ElementInfo("波形", (int)elm.waveform, -1, -1);
                ei.Choice = new ComboBox();
                ei.Choice.Items.Add(VoltageElmE.WAVEFORM.DC);
                ei.Choice.Items.Add(VoltageElmE.WAVEFORM.AC);
                ei.Choice.Items.Add(VoltageElmE.WAVEFORM.SQUARE);
                ei.Choice.Items.Add(VoltageElmE.WAVEFORM.TRIANGLE);
                ei.Choice.Items.Add(VoltageElmE.WAVEFORM.SAWTOOTH);
                ei.Choice.Items.Add(VoltageElmE.WAVEFORM.PULSE);
                ei.Choice.Items.Add(VoltageElmE.WAVEFORM.PWM_BOTH);
                ei.Choice.Items.Add(VoltageElmE.WAVEFORM.PWM_POSITIVE);
                ei.Choice.Items.Add(VoltageElmE.WAVEFORM.PWM_NEGATIVE);
                ei.Choice.Items.Add(VoltageElmE.WAVEFORM.NOISE);
                ei.Choice.SelectedIndex = (int)elm.waveform;
                return ei;
            }
            if (n == 2) {
                return new ElementInfo("オフセット電圧(V)", elm.mBias, -20, 20);
            }
            if (elm.waveform == VoltageElmE.WAVEFORM.DC || elm.waveform == VoltageElmE.WAVEFORM.NOISE) {
                return null;
            }
            if (n == 3) {
                return new ElementInfo("周波数(Hz)", elm.mFrequency, 4, 500);
            }
            if (n == 4) {
                return new ElementInfo("位相(degrees)", double.Parse((elm.mPhaseShift * 180 / Math.PI).ToString("0.00")), -180, 180).SetDimensionless();
            }
            if (n == 5 && (elm.waveform == VoltageElmE.WAVEFORM.PULSE
                || elm.waveform == VoltageElmE.WAVEFORM.SQUARE
                || elm.waveform == VoltageElmE.WAVEFORM.PWM_BOTH
                || elm.waveform == VoltageElmE.WAVEFORM.PWM_POSITIVE
                || elm.waveform == VoltageElmE.WAVEFORM.PWM_NEGATIVE)) {
                return new ElementInfo("デューティ比", elm.mDutyCycle * 100, 0, 100).SetDimensionless();
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var elm = (VoltageElmE)CirElm;
            if (n == 0) {
                elm.mMaxVoltage = ei.Value;
            }
            if (n == 2) {
                elm.mBias = ei.Value;
            }
            if (n == 3) {
                /* adjust time zero to maintain continuity ind the waveform
                 * even though the frequency has changed. */
                double oldfreq = elm.mFrequency;
                elm.mFrequency = ei.Value;
                double maxfreq = 1 / (8 * ControlPanel.TimeStep);
                if (maxfreq < elm.mFrequency) {
                    if (MessageBox.Show("Adjust timestep to allow for higher frequencies?", "", MessageBoxButtons.OKCancel) == DialogResult.OK) {
                        ControlPanel.TimeStep = 1 / (32 * elm.mFrequency);
                    } else {
                        elm.mFrequency = maxfreq;
                    }
                }
                double adj = elm.mFrequency - oldfreq;
            }
            if (n == 1) {
                var ow = elm.waveform;
                elm.waveform = (VoltageElmE.WAVEFORM)ei.Choice.SelectedIndex;
                if (elm.waveform == VoltageElmE.WAVEFORM.DC && ow != VoltageElmE.WAVEFORM.DC) {
                    ei.NewDialog = true;
                    elm.mBias = 0;
                } else if (elm.waveform != ow) {
                    ei.NewDialog = true;
                }

                /* change duty cycle if we're changing to or from pulse */
                if (elm.waveform == VoltageElmE.WAVEFORM.PULSE && ow != VoltageElmE.WAVEFORM.PULSE) {
                    elm.mDutyCycle = DEFAULT_PULSE_DUTY;
                } else if (ow == VoltageElmE.WAVEFORM.PULSE && elm.waveform != VoltageElmE.WAVEFORM.PULSE) {
                    elm.mDutyCycle = .5;
                }

                SetPoints();
            }
            if (n == 4) {
                elm.mPhaseShift = ei.Value * Math.PI / 180;
            }
            if (n == 5) {
                elm.mDutyCycle = ei.Value * .01;
            }
        }
    }
}
