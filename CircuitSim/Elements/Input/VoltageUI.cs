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

        public const string VALUE_NAME_V = "電圧(V)";
        public const string VALUE_NAME_AMP = "振幅(V)";
        public const string VALUE_NAME_V_OFS = "オフセット電圧(V)";
        public const string VALUE_NAME_HZ = "周波数(Hz)";
        public const string VALUE_NAME_PHASE = "位相(degrees)";
        public const string VALUE_NAME_PHASE_OFS = "オフセット位相(degrees)";
        public const string VALUE_NAME_DUTY = "デューティ比";

        Point mPs1;
        Point mPs2;
        Point mTextPos;

        protected VoltageUI(Point pos, VoltageElm.WAVEFORM wf) : base(pos) {
            Elm = new VoltageElm(wf);
            DumpInfo.ReferenceName = "";
        }

        public VoltageUI(Point p1, Point p2, int f) : base(p1, p2, f) { }

        public VoltageUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new VoltageElm(st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.VOLTAGE; } }

        protected override void dump(List<object> optionList) {
            var elm = (VoltageElm)Elm;
            /* set flag so we know if duty cycle is correct for pulse waveforms */
            if (elm.WaveForm == VoltageElm.WAVEFORM.PULSE ||
                elm.WaveForm == VoltageElm.WAVEFORM.PULSE_BOTH) {
                DumpInfo.Flags |= FLAG_PULSE_DUTY;
            } else {
                DumpInfo.Flags &= ~FLAG_PULSE_DUTY;
            }
            optionList.Add(elm.WaveForm);
            optionList.Add(elm.Frequency);
            optionList.Add(elm.MaxVoltage);
            optionList.Add(elm.Bias);
            optionList.Add((elm.Phase * 180 / Math.PI).ToString("0"));
            optionList.Add((elm.PhaseOffset * 180 / Math.PI).ToString("0"));
            optionList.Add(elm.DutyCycle.ToString("0.00"));
        }

        public override void SetPoints() {
            base.SetPoints();

            var elm = (VoltageElm)Elm;
            calcLeads((elm.WaveForm == VoltageElm.WAVEFORM.DC) ? BODY_LEN_DC : BODY_LEN);

            int sign;
            if (mPost1.Y == mPost2.Y) {
                sign = -mDsign;
            } else {
                sign = mDsign;
            }
            if (elm.WaveForm == VoltageElm.WAVEFORM.DC) {
                interpPoint(ref mTextPos, 0.5, -2 * BODY_LEN_DC * sign);
            } else {
                interpPoint(ref mTextPos, (mLen / 2 + 0.6 * BODY_LEN) / mLen, 7 * sign);
            }
        }

        public override void Draw(CustomGraphics g) {
            DumpInfo.SetBbox(DumpInfo.P1, DumpInfo.P2);
            draw2Leads();
            var elm = (VoltageElm)Elm;
            if (elm.WaveForm == VoltageElm.WAVEFORM.DC) {
                int hs = 10;
                setBbox(mPost1, mPost2, hs);

                interpLeadAB(ref mPs1, ref mPs2, 0, hs * 0.5);
                drawLead(mPs1, mPs2);

                interpLeadAB(ref mPs1, ref mPs2, 1, hs);
                drawLead(mPs1, mPs2);

                string s = Utils.UnitText(elm.MaxVoltage, "V");
                g.DrawRightText(s, mTextPos.X, mTextPos.Y);
            } else {
                setBbox(mPost1, mPost2, BODY_LEN);
                interpLead(ref mPs1, 0.5);
                drawWaveform(g, mPs1);
                string inds;
                if (0 < elm.Bias || (0 == elm.Bias &&
                    (VoltageElm.WAVEFORM.PULSE == elm.WaveForm || VoltageElm.WAVEFORM.PULSE_BOTH == elm.WaveForm))) {
                    inds = "+";
                } else {
                    inds = "*";
                }
                drawCenteredLText(inds, mTextPos, true);
            }

            updateDotCount();

            if (CirSimForm.Sim.DragElm != this) {
                if (elm.WaveForm == VoltageElm.WAVEFORM.DC) {
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

            if (elm.WaveForm != VoltageElm.WAVEFORM.NOISE) {
                g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
                g.DrawCircle(center, BODY_LEN / 2);
            }

            DumpInfo.AdjustBbox(
                x - BODY_LEN, y - BODY_LEN,
                x + BODY_LEN, y + BODY_LEN
            );

            var h = 7;
            var xd = (int)(h * 2 * elm.DutyCycle - h + x);
            xd = Math.Max(x - h + 1, Math.Min(x + h - 1, xd));
            var hd = (int)(h * elm.DutyCycle - h + x);

            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;

            switch (elm.WaveForm) {
            case VoltageElm.WAVEFORM.DC: {
                break;
            }
            case VoltageElm.WAVEFORM.SQUARE:
                if (elm.MaxVoltage < 0) {
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
                if (elm.MaxVoltage < 0) {
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
                g.DrawLine(x - h , y - h, x - h , y);
                g.DrawLine(x - h , y - h, hd    , y - h);
                g.DrawLine(hd    , y - h, hd    , y);
                g.DrawLine(hd    , y    , x     , y);
                g.DrawLine(x     , y    , x     , y + h);
                g.DrawLine(x     , y + h, hd + h, y + h);
                g.DrawLine(hd + h, y + h, hd + h, y);
                g.DrawLine(hd + h, y    , x + h , y);
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

            if (elm.WaveForm != VoltageElm.WAVEFORM.NOISE) {
                if (ControlPanel.ChkShowValues.Checked) {
                    var s = Utils.UnitText(elm.MaxVoltage, "V\r\n");
                    s += Utils.UnitText(elm.Frequency, "Hz\r\n");
                    s += Utils.UnitText(elm.Phase * 180 / Math.PI, "deg");
                    drawValues(s, 0, 5);
                }
                if (ControlPanel.ChkShowName.Checked) {
                    drawName(DumpInfo.ReferenceName, 0, -11);
                }
            }
        }

        public override void GetInfo(string[] arr) {
            var elm = (VoltageElm)Elm;
            switch (elm.WaveForm) {
            case VoltageElm.WAVEFORM.DC:
            case VoltageElm.WAVEFORM.AC:
            case VoltageElm.WAVEFORM.SQUARE:
            case VoltageElm.WAVEFORM.PULSE:
            case VoltageElm.WAVEFORM.PULSE_BOTH:
            case VoltageElm.WAVEFORM.SAWTOOTH:
            case VoltageElm.WAVEFORM.TRIANGLE:
            case VoltageElm.WAVEFORM.NOISE:
            case VoltageElm.WAVEFORM.PWM_BOTH:
                arr[0] = elm.WaveForm.ToString(); break;
            }

            arr[1] = "I = " + Utils.CurrentText(elm.Current);
            arr[2] = ((this is RailUI) ? "V = " : "Vd = ") + Utils.VoltageText(elm.VoltageDiff);
            int i = 3;
            if (elm.WaveForm != VoltageElm.WAVEFORM.DC && elm.WaveForm != VoltageElm.WAVEFORM.NOISE) {
                arr[i++] = "f = " + Utils.UnitText(elm.Frequency, "Hz");
                arr[i++] = "Vmax = " + Utils.VoltageText(elm.MaxVoltage);
                if (elm.WaveForm == VoltageElm.WAVEFORM.AC && elm.Bias == 0) {
                    arr[i++] = "V(rms) = " + Utils.VoltageText(elm.MaxVoltage / 1.41421356);
                }
                if (elm.Bias != 0) {
                    arr[i++] = "Voff = " + Utils.VoltageText(elm.Bias);
                } else if (elm.Frequency > 500) {
                    arr[i++] = "wavelength = " + Utils.UnitText(2.9979e8 / elm.Frequency, "m");
                }
            }
            if (elm.WaveForm == VoltageElm.WAVEFORM.DC && elm.Current != 0 && Circuit.ShowResistanceInVoltageSources) {
                arr[i++] = "(R = " + Utils.UnitText(elm.MaxVoltage / elm.Current, CirSimForm.OHM_TEXT) + ")";
            }
            arr[i++] = "P = " + Utils.UnitText(elm.Power, "W");
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var elm = (VoltageElm)Elm;

            if (c == 0) {
                if (r == 0) {
                    var ei = new ElementInfo("波形", (int)elm.WaveForm, -1, -1);
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
                    ei.Choice.SelectedIndex = (int)elm.WaveForm;
                    return ei;
                }
                if (r == 1) {
                    return new ElementInfo("名前", DumpInfo.ReferenceName);
                }
                if (r == 2) {
                    return new ElementInfo(elm.WaveForm == VoltageElm.WAVEFORM.DC ? VALUE_NAME_V : VALUE_NAME_AMP, elm.MaxVoltage, -20, 20);
                }
                if (r == 3) {
                    if (elm.WaveForm == VoltageElm.WAVEFORM.DC || elm.WaveForm == VoltageElm.WAVEFORM.NOISE) {
                        return null;
                    } else {
                        return new ElementInfo(VALUE_NAME_HZ, elm.Frequency, 4, 500);
                    }
                }
                if (r == 4) {
                    return new ElementInfo(VALUE_NAME_PHASE, double.Parse((elm.Phase * 180 / Math.PI).ToString("0.00")), -180, 180).SetDimensionless();
                }
                if (r == 5 && (elm.WaveForm == VoltageElm.WAVEFORM.PULSE
                    || elm.WaveForm == VoltageElm.WAVEFORM.PULSE_BOTH
                    || elm.WaveForm == VoltageElm.WAVEFORM.SQUARE
                    || elm.WaveForm == VoltageElm.WAVEFORM.PWM_BOTH
                    || elm.WaveForm == VoltageElm.WAVEFORM.PWM_POSITIVE
                    || elm.WaveForm == VoltageElm.WAVEFORM.PWM_NEGATIVE)) {
                    return new ElementInfo(VALUE_NAME_DUTY, elm.DutyCycle * 100, 0, 100).SetDimensionless();
                }
            }
            if (c == 1) {
                if (r == 2) {
                    return new ElementInfo(VALUE_NAME_V_OFS, elm.Bias, -20, 20);
                }
                if (r == 4) {
                    if (elm.WaveForm == VoltageElm.WAVEFORM.DC || elm.WaveForm == VoltageElm.WAVEFORM.NOISE) {
                        return null;
                    } else {
                        return new ElementInfo(VALUE_NAME_PHASE_OFS, double.Parse((elm.PhaseOffset * 180 / Math.PI).ToString("0.00")), -180, 180).SetDimensionless();
                    }
                }
                if (r < 4) {
                    return new ElementInfo();
                }
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var elm = (VoltageElm)Elm;
            if (n == 0) {
                var ow = elm.WaveForm;
                elm.WaveForm = (VoltageElm.WAVEFORM)ei.Choice.SelectedIndex;
                if (elm.WaveForm == VoltageElm.WAVEFORM.DC && ow != VoltageElm.WAVEFORM.DC) {
                    ei.NewDialog = true;
                    elm.Bias = 0;
                } else if (elm.WaveForm != ow) {
                    ei.NewDialog = true;
                }

                /* change duty cycle if we're changing to or from pulse */
                if (elm.WaveForm == VoltageElm.WAVEFORM.PULSE && ow != VoltageElm.WAVEFORM.PULSE) {
                    elm.DutyCycle = DEFAULT_PULSE_DUTY;
                } else if (ow == VoltageElm.WAVEFORM.PULSE && elm.WaveForm != VoltageElm.WAVEFORM.PULSE) {
                    elm.DutyCycle = .5;
                }

                SetPoints();
            }
            if (n == 1) {
                DumpInfo.ReferenceName = ei.Textf.Text;
            }
            if (n == 2) {
                elm.MaxVoltage = ei.Value;
            }
            if (n == 3) {
                elm.Bias = ei.Value;
            }
            if (n == 4) {
                /* adjust time zero to maintain continuity ind the waveform
                 * even though the frequency has changed. */
                double oldfreq = elm.Frequency;
                elm.Frequency = ei.Value;
                double maxfreq = 1 / (8 * ControlPanel.TimeStep);
                if (maxfreq < elm.Frequency) {
                    if (MessageBox.Show("Adjust timestep to allow for higher frequencies?", "", MessageBoxButtons.OKCancel) == DialogResult.OK) {
                        ControlPanel.TimeStep = 1 / (32 * elm.Frequency);
                    } else {
                        elm.Frequency = maxfreq;
                    }
                }
            }
            if (n == 5) {
                elm.Phase = ei.Value * Math.PI / 180;
            }
            if (n == 6) {
                elm.PhaseOffset = ei.Value * Math.PI / 180;
            }
            if (n == 7) {
                elm.DutyCycle = ei.Value * .01;
            }
        }

        public override EventHandler CreateSlider(ElementInfo ei, Adjustable adj) {
            var trb = adj.Slider;
            var ce = (VoltageElm)Elm;
            switch (ei.Name) {
            case VALUE_NAME_V:
            case VALUE_NAME_AMP:
                adj.MinValue = 0;
                adj.MaxValue = 5;
                break;
            case VALUE_NAME_V_OFS:
                adj.MinValue = 0;
                adj.MaxValue = 5;
                break;
            case VALUE_NAME_HZ:
                adj.MinValue = 0;
                adj.MaxValue = 100;
                break;
            case VALUE_NAME_PHASE:
                adj.MinValue = -180;
                adj.MaxValue = 180;
                trb.Maximum = 360;
                trb.TickFrequency = 30;
                break;
            case VALUE_NAME_PHASE_OFS:
                adj.MinValue = -180;
                adj.MaxValue = 180;
                trb.Maximum = 360;
                trb.TickFrequency = 30;
                break;
            case VALUE_NAME_DUTY:
                adj.MinValue = 0;
                adj.MaxValue = 1;
                break;
            }
            return new EventHandler((s, e) => {
                var val = adj.MinValue + (adj.MaxValue - adj.MinValue) * trb.Value / trb.Maximum;
                switch (ei.Name) {
                case VALUE_NAME_V:
                case VALUE_NAME_AMP:
                    ce.MaxVoltage = val;
                    break;
                case VALUE_NAME_V_OFS:
                    ce.Bias = val;
                    break;
                case VALUE_NAME_HZ:
                    ce.Frequency = val;
                    break;
                case VALUE_NAME_PHASE:
                    ce.Phase = val * Math.PI / 180;
                    break;
                case VALUE_NAME_PHASE_OFS:
                    ce.PhaseOffset = val * Math.PI / 180;
                    break;
                case VALUE_NAME_DUTY:
                    ce.DutyCycle = val;
                    break;
                }
                CirSimForm.Sim.NeedAnalyze();
            });
        }
    }
}
