using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Circuit.Elements.Input;

namespace Circuit.UI.Input {
    class Voltage : BaseUI {
        const int FLAG_COS = 2;
        const int FLAG_PULSE_DUTY = 4;

        const double DEFAULT_PULSE_DUTY = 0.5;

        protected const int BODY_LEN = 28;
        const int BODY_LEN_DC = 6;

        public const string VALUE_NAME_V = "電圧(V)";
        public const string VALUE_NAME_AMP = "振幅(V)";
        public const string VALUE_NAME_BIAS = "バイアス電圧(V)";
        public const string VALUE_NAME_HZ = "周波数(Hz)";
        public const string VALUE_NAME_PHASE = "位相(deg.)";
        public const string VALUE_NAME_PHASE_OFS = "オフセット位相(deg.)";
        public const string VALUE_NAME_DUTY = "デューティ比";

        Point mPs1;
        Point mPs2;
        Point mTextPos;

        public Voltage(Point pos, ElmVoltage.WAVEFORM wf) : base(pos) {
            Elm = new ElmVoltage(wf);
            DumpInfo.ReferenceName = "";
        }

        public Voltage(Point p1, Point p2, int f) : base(p1, p2, f) { }

        public Voltage(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmVoltage(st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.VOLTAGE; } }

        protected override void dump(List<object> optionList) {
            var elm = (ElmVoltage)Elm;
            /* set flag so we know if duty cycle is correct for pulse waveforms */
            if (elm.WaveForm == ElmVoltage.WAVEFORM.PULSE ||
                elm.WaveForm == ElmVoltage.WAVEFORM.PULSE_BOTH) {
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
            optionList.Add(elm.LinkBias);
            optionList.Add(elm.LinkFrequency);
            optionList.Add(elm.LinkPhaseOffset);
        }

        public override void SetPoints() {
            base.SetPoints();

            var elm = (ElmVoltage)Elm;
            calcLeads((elm.WaveForm == ElmVoltage.WAVEFORM.DC) ? BODY_LEN_DC : BODY_LEN);

            int sign;
            if (mPost1.Y == mPost2.Y) {
                sign = -mDsign;
            } else {
                sign = mDsign;
            }
            if (elm.WaveForm == ElmVoltage.WAVEFORM.DC) {
                interpPoint(ref mTextPos, 0.5, -2 * BODY_LEN_DC * sign);
            } else {
                interpPoint(ref mTextPos, (mLen / 2 + 0.6 * BODY_LEN) / mLen, 7 * sign);
            }
        }

        public override void Draw(CustomGraphics g) {
            DumpInfo.SetBbox(DumpInfo.P1, DumpInfo.P2);
            draw2Leads();
            var elm = (ElmVoltage)Elm;
            if (elm.WaveForm == ElmVoltage.WAVEFORM.DC) {
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
                    (ElmVoltage.WAVEFORM.PULSE == elm.WaveForm || ElmVoltage.WAVEFORM.PULSE_BOTH == elm.WaveForm))) {
                    inds = "+";
                } else {
                    inds = "*";
                }
                drawCenteredLText(inds, mTextPos, true);
            }

            updateDotCount();

            if (CirSimForm.DragElm != this) {
                if (elm.WaveForm == ElmVoltage.WAVEFORM.DC) {
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
            var elm = (ElmVoltage)Elm;

            if (elm.WaveForm != ElmVoltage.WAVEFORM.NOISE) {
                g.DrawColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
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

            g.DrawColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;

            switch (elm.WaveForm) {
            case ElmVoltage.WAVEFORM.DC: {
                break;
            }
            case ElmVoltage.WAVEFORM.SQUARE:
                if (elm.MaxVoltage < 0) {
                    g.DrawLine(x - h, y + h, x - h, y);
                    g.DrawLine(x - h, y + h, xd, y + h);
                    g.DrawLine(xd, y + h, xd, y - h);
                    g.DrawLine(x + h, y - h, xd, y - h);
                    g.DrawLine(x + h, y, x + h, y - h);
                } else {
                    g.DrawLine(x - h, y - h, x - h, y);
                    g.DrawLine(x - h, y - h, xd, y - h);
                    g.DrawLine(xd, y - h, xd, y + h);
                    g.DrawLine(x + h, y + h, xd, y + h);
                    g.DrawLine(x + h, y, x + h, y + h);
                }
                break;
            case ElmVoltage.WAVEFORM.PULSE:
                if (elm.MaxVoltage < 0) {
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
            case ElmVoltage.WAVEFORM.PULSE_BOTH:
                g.DrawLine(x - h, y - h, x - h, y);
                g.DrawLine(x - h, y - h, hd, y - h);
                g.DrawLine(hd, y - h, hd, y);
                g.DrawLine(hd, y, x, y);
                g.DrawLine(x, y, x, y + h);
                g.DrawLine(x, y + h, hd + h, y + h);
                g.DrawLine(hd + h, y + h, hd + h, y);
                g.DrawLine(hd + h, y, x + h, y);
                break;
            case ElmVoltage.WAVEFORM.SAWTOOTH:
                g.DrawLine(x, y - h, x - h, y);
                g.DrawLine(x, y - h, x, y + h);
                g.DrawLine(x, y + h, x + h, y);
                break;
            case ElmVoltage.WAVEFORM.TRIANGLE: {
                int xl = 5;
                g.DrawLine(x - xl * 2, y, x - xl, y - h);
                g.DrawLine(x - xl, y - h, x, y);
                g.DrawLine(x, y, x + xl, y + h);
                g.DrawLine(x + xl, y + h, x + xl * 2, y);
                break;
            }
            case ElmVoltage.WAVEFORM.NOISE: {
                drawCenteredText("Noise", center, true);
                break;
            }
            case ElmVoltage.WAVEFORM.AC: {
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

            if (elm.WaveForm != ElmVoltage.WAVEFORM.NOISE) {
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
            var elm = (ElmVoltage)Elm;
            switch (elm.WaveForm) {
            case ElmVoltage.WAVEFORM.DC:
            case ElmVoltage.WAVEFORM.AC:
            case ElmVoltage.WAVEFORM.SQUARE:
            case ElmVoltage.WAVEFORM.PULSE:
            case ElmVoltage.WAVEFORM.PULSE_BOTH:
            case ElmVoltage.WAVEFORM.SAWTOOTH:
            case ElmVoltage.WAVEFORM.TRIANGLE:
            case ElmVoltage.WAVEFORM.NOISE:
            case ElmVoltage.WAVEFORM.PWM_BOTH:
                arr[0] = elm.WaveForm.ToString(); break;
            }

            arr[1] = "I = " + Utils.CurrentText(elm.Current);
            arr[2] = ((this is Rail) ? "V = " : "Vd = ") + Utils.VoltageText(elm.VoltageDiff);
            int i = 3;
            if (elm.WaveForm != ElmVoltage.WAVEFORM.DC && elm.WaveForm != ElmVoltage.WAVEFORM.NOISE) {
                arr[i++] = "f = " + Utils.UnitText(elm.Frequency, "Hz");
                arr[i++] = "Vmax = " + Utils.VoltageText(elm.MaxVoltage);
                if (elm.WaveForm == ElmVoltage.WAVEFORM.AC && elm.Bias == 0) {
                    arr[i++] = "V(rms) = " + Utils.VoltageText(elm.MaxVoltage / 1.41421356);
                }
                if (elm.Bias != 0) {
                    arr[i++] = "Voff = " + Utils.VoltageText(elm.Bias);
                } else if (elm.Frequency > 500) {
                    arr[i++] = "wavelength = " + Utils.UnitText(2.9979e8 / elm.Frequency, "m");
                }
            }
            if (elm.WaveForm == ElmVoltage.WAVEFORM.DC && elm.Current != 0 && Circuit.ShowResistanceInVoltageSources) {
                arr[i++] = "(R = " + Utils.UnitText(elm.MaxVoltage / elm.Current, CirSimForm.OHM_TEXT) + ")";
            }
            arr[i++] = "P = " + Utils.UnitText(elm.Power, "W");
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var elm = (ElmVoltage)Elm;

            if (c == 0) {
                if (r == 0) {
                    var ei = new ElementInfo("波形");
                    ei.Choice = new ComboBox();
                    ei.Choice.Width = 100;
                    ei.Choice.Items.Add(ElmVoltage.WAVEFORM.DC);
                    ei.Choice.Items.Add(ElmVoltage.WAVEFORM.AC);
                    ei.Choice.Items.Add(ElmVoltage.WAVEFORM.SQUARE);
                    ei.Choice.Items.Add(ElmVoltage.WAVEFORM.TRIANGLE);
                    ei.Choice.Items.Add(ElmVoltage.WAVEFORM.SAWTOOTH);
                    ei.Choice.Items.Add(ElmVoltage.WAVEFORM.PULSE);
                    ei.Choice.Items.Add(ElmVoltage.WAVEFORM.PULSE_BOTH);
                    ei.Choice.Items.Add(ElmVoltage.WAVEFORM.PWM);
                    ei.Choice.Items.Add(ElmVoltage.WAVEFORM.PWM_BOTH);
                    ei.Choice.Items.Add(ElmVoltage.WAVEFORM.NOISE);
                    ei.Choice.SelectedIndex = (int)elm.WaveForm;
                    return ei;
                }
                if (r == 1) {
                    return new ElementInfo("名前", DumpInfo.ReferenceName);
                }
                if (r == 2) {
                    return new ElementInfo(elm.WaveForm == ElmVoltage.WAVEFORM.DC ? VALUE_NAME_V : VALUE_NAME_AMP, elm.MaxVoltage, -20, 20);
                }
                if (r == 3) {
                    return new ElementInfo(VALUE_NAME_BIAS, elm.Bias, -20, 20);
                }
                if (r == 4) {
                    if (elm.WaveForm == ElmVoltage.WAVEFORM.DC || elm.WaveForm == ElmVoltage.WAVEFORM.NOISE) {
                        return null;
                    } else {
                        return new ElementInfo(VALUE_NAME_HZ, elm.Frequency, 4, 500);
                    }
                }
                if (r == 5) {
                    return new ElementInfo(VALUE_NAME_PHASE, double.Parse((elm.Phase * 180 / Math.PI).ToString("0.00")), -180, 180).SetDimensionless();
                }
                if (r == 6) {
                    return new ElementInfo(VALUE_NAME_PHASE_OFS, double.Parse((elm.PhaseOffset * 180 / Math.PI).ToString("0.00")), -180, 180).SetDimensionless();
                }
                if (r == 7 && (elm.WaveForm == ElmVoltage.WAVEFORM.PULSE
                    || elm.WaveForm == ElmVoltage.WAVEFORM.PULSE_BOTH
                    || elm.WaveForm == ElmVoltage.WAVEFORM.SQUARE
                    || elm.WaveForm == ElmVoltage.WAVEFORM.PWM
                    || elm.WaveForm == ElmVoltage.WAVEFORM.PWM_BOTH)) {
                    return new ElementInfo(VALUE_NAME_DUTY, elm.DutyCycle * 100, 0, 100).SetDimensionless();
                }
            }
            if (c == 1) {
                if (r == 3) {
                    return new ElementInfo("連動グループ", elm.LinkBias);
                }
                if (r == 4) {
                    return new ElementInfo("連動グループ", elm.LinkFrequency);
                }
                if (r == 6) {
                    return new ElementInfo("連動グループ", elm.LinkPhaseOffset);
                }
                if (r < 7) {
                    return new ElementInfo();
                }
            }
            return null;
        }

        public override void SetElementValue(int r, int c, ElementInfo ei) {
            var elm = (ElmVoltage)Elm;
            if (c == 0) {
                if (r == 0) {
                    var ow = elm.WaveForm;
                    elm.WaveForm = (ElmVoltage.WAVEFORM)ei.Choice.SelectedIndex;
                    if (elm.WaveForm == ElmVoltage.WAVEFORM.DC && ow != ElmVoltage.WAVEFORM.DC) {
                        ei.NewDialog = true;
                        elm.Bias = 0;
                    } else if (elm.WaveForm != ow) {
                        ei.NewDialog = true;
                    }

                    /* change duty cycle if we're changing to or from pulse */
                    if (elm.WaveForm == ElmVoltage.WAVEFORM.PULSE && ow != ElmVoltage.WAVEFORM.PULSE) {
                        elm.DutyCycle = DEFAULT_PULSE_DUTY;
                    } else if (ow == ElmVoltage.WAVEFORM.PULSE && elm.WaveForm != ElmVoltage.WAVEFORM.PULSE) {
                        elm.DutyCycle = .5;
                    }

                    SetPoints();
                }
                if (r == 1) {
                    DumpInfo.ReferenceName = ei.Textf.Text;
                }
                if (r == 2) {
                    elm.MaxVoltage = ei.Value;
                }
                if (r == 3) {
                    elm.Bias = ei.Value;
                }
                if (r == 4) {
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
                if (r == 5) {
                    elm.Phase = ei.Value * Math.PI / 180;
                }
                if (r == 6) {
                    elm.PhaseOffset = ei.Value * Math.PI / 180;
                }
                if (elm.WaveForm == ElmVoltage.WAVEFORM.PULSE
                   || elm.WaveForm == ElmVoltage.WAVEFORM.PULSE_BOTH
                   || elm.WaveForm == ElmVoltage.WAVEFORM.SQUARE
                   || elm.WaveForm == ElmVoltage.WAVEFORM.PWM
                   || elm.WaveForm == ElmVoltage.WAVEFORM.PWM_BOTH) {
                    if (r == 7) {
                        elm.DutyCycle = ei.Value * .01;
                    }
                }
            }
            if (c == 1) {
                if (r == 3) {
                    elm.LinkBias = (int)ei.Value;
                }
                if (r == 4) {
                    elm.LinkFrequency = (int)ei.Value;
                }
                if (r == 6) {
                    elm.LinkPhaseOffset = (int)ei.Value;
                }
            }
        }

        public override EventHandler CreateSlider(ElementInfo ei, Adjustable adj) {
            var trb = adj.Slider;
            var ce = (ElmVoltage)Elm;
            switch (ei.Name) {
            case VALUE_NAME_V:
            case VALUE_NAME_AMP:
                adj.MinValue = 0;
                adj.MaxValue = 5;
                break;
            case VALUE_NAME_BIAS:
                adj.MinValue = 0;
                adj.MaxValue = 5;
                break;
            case VALUE_NAME_HZ:
                adj.MinValue = 0;
                adj.MaxValue = 1000;
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
                case VALUE_NAME_BIAS:
                    ce.Bias = val;
                    if (ce.LinkBias != 0) {
                        for (int i = 0; i != CirSimForm.ElmCount; i++) {
                            var o = CirSimForm.GetElm(i).Elm;
                            if (o is ElmVoltage) {
                                var s2 = (ElmVoltage)o;
                                if (s2.LinkBias == ce.LinkBias) {
                                    s2.Bias = ce.Bias;
                                }
                            }
                        }
                    }
                    break;
                case VALUE_NAME_HZ:
                    ce.Frequency = val;
                    if (ce.LinkFrequency != 0) {
                        for (int i = 0; i != CirSimForm.ElmCount; i++) {
                            var o = CirSimForm.GetElm(i).Elm;
                            if (o is ElmVoltage) {
                                var s2 = (ElmVoltage)o;
                                if (s2.LinkFrequency == ce.LinkFrequency) {
                                    s2.Frequency = ce.Frequency;
                                }
                            }
                        }
                    }
                    break;
                case VALUE_NAME_PHASE:
                    ce.Phase = val * Math.PI / 180;
                    break;
                case VALUE_NAME_PHASE_OFS:
                    ce.PhaseOffset = val * Math.PI / 180;
                    if (ce.LinkPhaseOffset != 0) {
                        for (int i = 0; i != CirSimForm.ElmCount; i++) {
                            var o = CirSimForm.GetElm(i).Elm;
                            if (o is ElmVoltage) {
                                var s2 = (ElmVoltage)o;
                                if (s2.LinkPhaseOffset == ce.LinkPhaseOffset) {
                                    s2.PhaseOffset = ce.PhaseOffset;
                                }
                            }
                        }
                    }
                    break;
                case VALUE_NAME_DUTY:
                    ce.DutyCycle = val;
                    break;
                }
                CirSimForm.NeedAnalyze();
            });
        }
    }
}
