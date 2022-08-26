using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Input {
    class VoltageUI : BaseUI {
        const int FLAG_COS = 2;
        const int FLAG_PULSE_DUTY = 4;

        const double DEFAULT_PULSE_DUTY = 0.5;

        protected const int BODY_LEN = 28;
        const int BODY_LEN_DC = 6;

        Point mPs1;
        Point mPs2;
        Point mTextPos;

        protected VoltageUI(Point pos, VoltageElm.WAVEFORM wf) : base(pos) {
            Elm = new VoltageElm(wf);
        }

        public VoltageUI(Point p1, Point p2, int f) : base(p1, p2, f) { }

        public VoltageUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new VoltageElm(st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.VOLTAGE; } }

        protected override void dump(List<object> optionList) {
            var elm = (VoltageElm)Elm;
            /* set flag so we know if duty cycle is correct for pulse waveforms */
            if (elm.waveform == VoltageElm.WAVEFORM.PULSE ||
                elm.waveform == VoltageElm.WAVEFORM.PULSE_BOTH) {
                DumpInfo.Flags |= FLAG_PULSE_DUTY;
            } else {
                DumpInfo.Flags &= ~FLAG_PULSE_DUTY;
            }
            optionList.Add(elm.waveform);
            optionList.Add(elm.mFrequency);
            optionList.Add(elm.mMaxVoltage);
            optionList.Add(elm.mBias);
            optionList.Add((elm.mPhaseShift * 180 / Math.PI).ToString("0"));
            optionList.Add(elm.mDutyCycle.ToString("0.00"));
        }

        public override void SetPoints() {
            base.SetPoints();

            var elm = (VoltageElm)Elm;
            calcLeads((elm.waveform == VoltageElm.WAVEFORM.DC) ? BODY_LEN_DC : BODY_LEN);

            int sign;
            if (mPost1.Y == mPost2.Y) {
                sign = -mDsign;
            } else {
                sign = mDsign;
            }
            if (elm.waveform == VoltageElm.WAVEFORM.DC) {
                interpPoint(ref mTextPos, 0.5, -2 * BODY_LEN_DC * sign);
            } else {
                interpPoint(ref mTextPos, (mLen / 2 + 0.7 * BODY_LEN) / mLen, 10 * sign);
            }
        }

        public override void Draw(CustomGraphics g) {
            DumpInfo.SetBbox(DumpInfo.P1, DumpInfo.P2);
            draw2Leads();
            var elm = (VoltageElm)Elm;
            if (elm.waveform == VoltageElm.WAVEFORM.DC) {
                int hs = 10;
                setBbox(mPost1, mPost2, hs);

                interpLeadAB(ref mPs1, ref mPs2, 0, hs * 0.5);
                drawLead(mPs1, mPs2);

                interpLeadAB(ref mPs1, ref mPs2, 1, hs);
                drawLead(mPs1, mPs2);

                string s = Utils.UnitText(elm.mMaxVoltage, "V");
                g.DrawRightText(s, mTextPos.X, mTextPos.Y);
            } else {
                setBbox(mPost1, mPost2, BODY_LEN);
                interpLead(ref mPs1, 0.5);
                drawWaveform(g, mPs1);
                string inds;
                if (0 < elm.mBias || (0 == elm.mBias &&
                    (VoltageElm.WAVEFORM.PULSE == elm.waveform || VoltageElm.WAVEFORM.PULSE_BOTH == elm.waveform))) {
                    inds = "+";
                } else {
                    inds = "*";
                }
                drawCenteredLText(inds, mTextPos, true);
            }

            updateDotCount();

            if (CirSimForm.Sim.DragElm != this) {
                if (elm.waveform == VoltageElm.WAVEFORM.DC) {
                    drawDots(mPost1, mPost2, Elm.CurCount);
                } else {
                    drawDots(mPost1, mLead1, Elm.CurCount);
                    drawDots(mPost2, mLead2, -Elm.CurCount);
                }
            }
            drawPosts();
        }

        protected void drawWaveform(CustomGraphics g, Point center) {
            var x = center.X;
            var y = center.Y;
            var elm = (VoltageElm)Elm;

            if (elm.waveform != VoltageElm.WAVEFORM.NOISE) {
                g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
                g.DrawCircle(center, BODY_LEN / 2);
            }

            DumpInfo.AdjustBbox(
                x - BODY_LEN, y - BODY_LEN,
                x + BODY_LEN, y + BODY_LEN
            );

            var h = 7;
            var xd = (int)(h * 2 * elm.mDutyCycle - h + x);
            xd = Math.Max(x - h + 1, Math.Min(x + h - 1, xd));

            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;

            switch (elm.waveform) {
            case VoltageElm.WAVEFORM.DC: {
                break;
            }
            case VoltageElm.WAVEFORM.SQUARE:
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
            case VoltageElm.WAVEFORM.PULSE:
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
            case VoltageElm.WAVEFORM.PULSE_BOTH:
                if (elm.mMaxVoltage < 0) {
                    g.DrawLine(x + h, y, x + h, y);
                    g.DrawLine(x + h, y, xd, y);
                    g.DrawLine(xd, y + h, xd, y);
                    g.DrawLine(x - h, y + h, xd, y + h);
                    g.DrawLine(x - h, y + h, x - h, y);
                } else {
                    g.DrawLine(x - h, y - h, x - h, y);
                    g.DrawLine(x - h, y - h, xd, y - h);
                    g.DrawLine(xd, y - h, xd, y);
                    g.DrawLine(x + h, y, xd, y);
                    g.DrawLine(x + h, y, x + h, y);
                }
                break;
            case VoltageElm.WAVEFORM.SAWTOOTH:
                g.DrawLine(x, y - h, x - h, y    );
                g.DrawLine(x, y - h, x    , y + h);
                g.DrawLine(x, y + h, x + h, y    );
                break;
            case VoltageElm.WAVEFORM.TRIANGLE: {
                int xl = 5;
                g.DrawLine(x - xl * 2, y    , x - xl    , y - h);
                g.DrawLine(x - xl    , y - h, x         , y    );
                g.DrawLine(x         , y    , x + xl    , y + h);
                g.DrawLine(x + xl    , y + h, x + xl * 2, y    );
                break;
            }
            case VoltageElm.WAVEFORM.NOISE: {
                drawCenteredText("Noise", center, true);
                break;
            }
            case VoltageElm.WAVEFORM.AC: {
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

            if (ControlPanel.ChkShowValues.Checked && elm.waveform != VoltageElm.WAVEFORM.NOISE) {
                var s = Utils.UnitText(elm.mMaxVoltage, "V\r\n");
                s += Utils.UnitText(elm.mFrequency, "Hz\r\n");
                s += Utils.UnitText(elm.mPhaseShift * 180 / Math.PI, "°");
                drawValues(s, 0, 5);
            }
        }

        public override void GetInfo(string[] arr) {
            var elm = (VoltageElm)Elm;
            switch (elm.waveform) {
            case VoltageElm.WAVEFORM.DC:
            case VoltageElm.WAVEFORM.AC:
            case VoltageElm.WAVEFORM.SQUARE:
            case VoltageElm.WAVEFORM.PULSE:
            case VoltageElm.WAVEFORM.PULSE_BOTH:
            case VoltageElm.WAVEFORM.SAWTOOTH:
            case VoltageElm.WAVEFORM.TRIANGLE:
            case VoltageElm.WAVEFORM.NOISE:
            case VoltageElm.WAVEFORM.PWM_BOTH:
                arr[0] = elm.waveform.ToString(); break;
            }

            arr[1] = "I = " + Utils.CurrentText(elm.Current);
            arr[2] = ((this is RailUI) ? "V = " : "Vd = ") + Utils.VoltageText(elm.VoltageDiff);
            int i = 3;
            if (elm.waveform != VoltageElm.WAVEFORM.DC && elm.waveform != VoltageElm.WAVEFORM.NOISE) {
                arr[i++] = "f = " + Utils.UnitText(elm.mFrequency, "Hz");
                arr[i++] = "Vmax = " + Utils.VoltageText(elm.mMaxVoltage);
                if (elm.waveform == VoltageElm.WAVEFORM.AC && elm.mBias == 0) {
                    arr[i++] = "V(rms) = " + Utils.VoltageText(elm.mMaxVoltage / 1.41421356);
                }
                if (elm.mBias != 0) {
                    arr[i++] = "Voff = " + Utils.VoltageText(elm.mBias);
                } else if (elm.mFrequency > 500) {
                    arr[i++] = "wavelength = " + Utils.UnitText(2.9979e8 / elm.mFrequency, "m");
                }
            }
            if (elm.waveform == VoltageElm.WAVEFORM.DC && elm.Current != 0 && Circuit.ShowResistanceInVoltageSources) {
                arr[i++] = "(R = " + Utils.UnitText(elm.mMaxVoltage / elm.Current, CirSimForm.OHM_TEXT) + ")";
            }
            arr[i++] = "P = " + Utils.UnitText(elm.Power, "W");
        }

        public override ElementInfo GetElementInfo(int n) {
            var elm = (VoltageElm)Elm;
            if (n == 0) {
                return new ElementInfo(elm.waveform == VoltageElm.WAVEFORM.DC ? "電圧(V)" : "振幅(V)", elm.mMaxVoltage, -20, 20);
            }
            if (n == 1) {
                var ei = new ElementInfo("波形", (int)elm.waveform, -1, -1);
                ei.Choice = new ComboBox();
                ei.Choice.Items.Add(VoltageElm.WAVEFORM.DC);
                ei.Choice.Items.Add(VoltageElm.WAVEFORM.AC);
                ei.Choice.Items.Add(VoltageElm.WAVEFORM.SQUARE);
                ei.Choice.Items.Add(VoltageElm.WAVEFORM.TRIANGLE);
                ei.Choice.Items.Add(VoltageElm.WAVEFORM.SAWTOOTH);
                ei.Choice.Items.Add(VoltageElm.WAVEFORM.PULSE);
                ei.Choice.Items.Add(VoltageElm.WAVEFORM.PULSE_BOTH);
                ei.Choice.Items.Add(VoltageElm.WAVEFORM.PWM_BOTH);
                ei.Choice.Items.Add(VoltageElm.WAVEFORM.PWM_POSITIVE);
                ei.Choice.Items.Add(VoltageElm.WAVEFORM.PWM_NEGATIVE);
                ei.Choice.Items.Add(VoltageElm.WAVEFORM.NOISE);
                ei.Choice.SelectedIndex = (int)elm.waveform;
                return ei;
            }
            if (n == 2) {
                return new ElementInfo("オフセット電圧(V)", elm.mBias, -20, 20);
            }
            if (elm.waveform == VoltageElm.WAVEFORM.DC || elm.waveform == VoltageElm.WAVEFORM.NOISE) {
                return null;
            }
            if (n == 3) {
                return new ElementInfo("周波数(Hz)", elm.mFrequency, 4, 500);
            }
            if (n == 4) {
                return new ElementInfo("位相(degrees)", double.Parse((elm.mPhaseShift * 180 / Math.PI).ToString("0.00")), -180, 180).SetDimensionless();
            }
            if (n == 5 && (elm.waveform == VoltageElm.WAVEFORM.PULSE
                || elm.waveform == VoltageElm.WAVEFORM.PULSE_BOTH
                || elm.waveform == VoltageElm.WAVEFORM.SQUARE
                || elm.waveform == VoltageElm.WAVEFORM.PWM_BOTH
                || elm.waveform == VoltageElm.WAVEFORM.PWM_POSITIVE
                || elm.waveform == VoltageElm.WAVEFORM.PWM_NEGATIVE)) {
                return new ElementInfo("デューティ比", elm.mDutyCycle * 100, 0, 100).SetDimensionless();
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var elm = (VoltageElm)Elm;
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
                elm.waveform = (VoltageElm.WAVEFORM)ei.Choice.SelectedIndex;
                if (elm.waveform == VoltageElm.WAVEFORM.DC && ow != VoltageElm.WAVEFORM.DC) {
                    ei.NewDialog = true;
                    elm.mBias = 0;
                } else if (elm.waveform != ow) {
                    ei.NewDialog = true;
                }

                /* change duty cycle if we're changing to or from pulse */
                if (elm.waveform == VoltageElm.WAVEFORM.PULSE && ow != VoltageElm.WAVEFORM.PULSE) {
                    elm.mDutyCycle = DEFAULT_PULSE_DUTY;
                } else if (ow == VoltageElm.WAVEFORM.PULSE && elm.waveform != VoltageElm.WAVEFORM.PULSE) {
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
