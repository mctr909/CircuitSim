using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Circuit.Elements;
using Circuit.Elements.Input;

namespace Circuit.UI.Input {
    class VoltageLink : BaseLink {
        public const int BIAS = 0;
        public const int FREQUENCY = 1;
        public const int PHASE_OFFSET = 2;
        public int Bias = 0;
        public int Frequency = 0;
        public int PhaseOffset = 0;
        public override int GetGroup(int id) {
            switch (id) {
            case BIAS: return Bias;
            case FREQUENCY: return Frequency;
            case PHASE_OFFSET: return PhaseOffset;
            default: return 0;
            }
        }
        public override void SetValue(BaseElement element, int linkID, double value) {
            var elm = (ElmVoltage)element;
            switch (linkID) {
            case BIAS:
                elm.Bias = value;
                break;
            case FREQUENCY:
                elm.Frequency = value;
                break;
            case PHASE_OFFSET:
                elm.PhaseOffset = value * Math.PI / 180;
                break;
            }
        }
        public override void Load(StringTokenizer st) {
            Bias = st.nextTokenInt();
            Frequency = st.nextTokenInt();
            PhaseOffset = st.nextTokenInt();
        }
        public override void Dump(List<object> optionList) {
            optionList.Add(Bias);
            optionList.Add(Frequency);
            optionList.Add(PhaseOffset);
        }
    }

    class Voltage : BaseUI {
        const int FLAG_PULSE_DUTY = 4;
        const double DEFAULT_PULSE_DUTY = 0.5;

        protected const int BODY_LEN = 30;
        const int BODY_LEN_DC = 6;

        public const string VALUE_NAME_V = "電圧(V)";
        public const string VALUE_NAME_AMP = "振幅(V)";
        public const string VALUE_NAME_BIAS = "バイアス電圧(V)";
        public const string VALUE_NAME_HZ = "周波数(Hz)";
        public const string VALUE_NAME_PHASE = "位相(deg.)";
        public const string VALUE_NAME_PHASE_OFS = "オフセット位相(deg.)";
        public const string VALUE_NAME_DUTY = "デューティ比";

        protected override BaseLink mLink { get; set; } = new VoltageLink();
        protected VoltageLink Link { get { return (VoltageLink)mLink; } }

        PointF mPs1;
        PointF mPs2;
        PointF[] mWaveFormPos;
        Point mTextPos;

        public Voltage(Point pos, ElmVoltage.WAVEFORM wf) : base(pos) {
            Elm = new ElmVoltage(wf);
            DumpInfo.ReferenceName = "";
        }

        public Voltage(Point p1, Point p2, int f) : base(p1, p2, f) { }

        public Voltage(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmVoltage(st);
            Link.Load(st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.VOLTAGE; } }

        protected override void dump(List<object> optionList) {
            var elm = (ElmVoltage)Elm;
            /* set flag so we know if duty cycle is correct for pulse waveforms */
            if (elm.WaveForm == ElmVoltage.WAVEFORM.PULSE_MONOPOLE ||
                elm.WaveForm == ElmVoltage.WAVEFORM.PULSE_DIPOLE) {
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

            var elm = (ElmVoltage)Elm;
            calcLeads((elm.WaveForm == ElmVoltage.WAVEFORM.DC) ? BODY_LEN_DC : BODY_LEN);

            int sign;
            if (mHorizontal) {
                sign = -mDsign;
            } else {
                sign = mDsign;
            }
            if (elm.WaveForm == ElmVoltage.WAVEFORM.DC) {
                interpPost(ref mTextPos, 0.5, -2 * BODY_LEN_DC * sign);
            } else {
                interpPost(ref mTextPos, (mLen / 2 + 0.6 * BODY_LEN) / mLen, 7 * sign);
            }
        }

        public override void Draw(CustomGraphics g) {
            DumpInfo.SetBbox(DumpInfo.P1, DumpInfo.P2);
            draw2Leads();
            var elm = (ElmVoltage)Elm;
            if (elm.WaveForm == ElmVoltage.WAVEFORM.DC) {
                int hs = 10;
                setBbox(hs);

                interpLeadAB(ref mPs1, ref mPs2, 0, hs * 0.5);
                drawLine(mPs1, mPs2);

                interpLeadAB(ref mPs1, ref mPs2, 1, hs);
                drawLine(mPs1, mPs2);

                string s = Utils.UnitText(elm.MaxVoltage, "V");
                g.DrawRightText(s, mTextPos.X, mTextPos.Y);
            } else {
                setBbox(BODY_LEN);
                interpLead(ref mPs1, 0.5);
                drawWaveform(mPs1);
                string inds;
                if (0 < elm.Bias || (0 == elm.Bias &&
                    (ElmVoltage.WAVEFORM.PULSE_MONOPOLE == elm.WaveForm || ElmVoltage.WAVEFORM.PULSE_DIPOLE == elm.WaveForm))) {
                    inds = "+";
                } else {
                    inds = "*";
                }
                drawCenteredLText(inds, mTextPos, true);
            }

            updateDotCount();

            if (CirSimForm.DragElm != this) {
                if (elm.WaveForm == ElmVoltage.WAVEFORM.DC) {
                    drawCurrent(Elm.Post[0], Elm.Post[1], CurCount);
                } else {
                    drawCurrentA(CurCount);
                    drawCurrentB(CurCount);
                }
            }
            drawPosts();
        }

        protected void drawWaveform(PointF p) {
            var x = p.X;
            var y = p.Y;
            var elm = (ElmVoltage)Elm;

            if (elm.WaveForm != ElmVoltage.WAVEFORM.NOISE) {
                drawCircle(p, BODY_LEN / 2);
            }

            DumpInfo.AdjustBbox(
                (int)(x - BODY_LEN), (int)(y - BODY_LEN),
                (int)(x + BODY_LEN), (int)(y + BODY_LEN)
            );

            switch (elm.WaveForm) {
            case ElmVoltage.WAVEFORM.DC:
                return;
            case ElmVoltage.WAVEFORM.NOISE:
                drawCenteredText("Noise", p, true);
                return;
            }

            const int DX = 12;
            const int DX_H = 6;
            const int H = 8;
            var duty = Math.Min(1, Math.Max(0, elm.DutyCycle));
            var w = (float)(x - DX + 2 * DX * duty);
            var wh = (float)(x - DX + DX * duty);
            var wp = (float)(DX * duty / 3.0);
            var ph = 0 < duty ? H : 0;

            switch (elm.WaveForm) {
            case ElmVoltage.WAVEFORM.SIN: {
                mWaveFormPos = new PointF[DX * 2 + 1];
                for (int t = -DX, c = 0; t <= DX; t++, c++) {
                    var yy = y + (int)(.95 * Math.Sin(t * Math.PI / DX) * H);
                    mWaveFormPos[c].X = x + t;
                    mWaveFormPos[c].Y = yy;
                }
                break;
            }
            case ElmVoltage.WAVEFORM.SQUARE:
                mWaveFormPos = new PointF[] {
                    new PointF(x - DX, y),
                    new PointF(x - DX, y - H),
                    new PointF(w, y - H),
                    new PointF(w, y + H),
                    new PointF(x + DX, y + H),
                    new PointF(x + DX, y)
                };
                break;
            case ElmVoltage.WAVEFORM.TRIANGLE:
                mWaveFormPos = new PointF[] {
                    new PointF(x - DX, y),
                    new PointF(x - DX_H, y - H),
                    new PointF(x, y),
                    new PointF(x + DX_H, y + H),
                    new PointF(x + DX, y)
                };
                break;
            case ElmVoltage.WAVEFORM.SAWTOOTH:
                mWaveFormPos = new PointF[] {
                    new PointF(x - DX, y),
                    new PointF(x, y - H),
                    new PointF(x, y + H),
                    new PointF(x + DX, y)
                };
                break;
            case ElmVoltage.WAVEFORM.PULSE_MONOPOLE:
                if (elm.MaxVoltage < 0) {
                    mWaveFormPos = new PointF[] {
                        new PointF(x - DX, y),
                        new PointF(x - DX, y + ph),
                        new PointF(w, y + ph),
                        new PointF(w, y),
                        new PointF(x + DX, y)
                    };
                } else {
                    mWaveFormPos = new PointF[] {
                        new PointF(x - DX, y),
                        new PointF(x - DX, y - ph),
                        new PointF(w, y - ph),
                        new PointF(w, y),
                        new PointF(x + DX, y)
                    };
                }
                break;
            case ElmVoltage.WAVEFORM.PULSE_DIPOLE:
                mWaveFormPos = new PointF[] {
                    new PointF(x - DX, y),
                    new PointF(x - DX, y - ph),
                    new PointF(wh, y - ph),
                    new PointF(wh, y),
                    new PointF(x, y),
                    new PointF(x, y + ph),
                    new PointF(wh + DX, y + ph),
                    new PointF(wh + DX, y),
                    new PointF(x + DX, y)
                };
                break;
            case ElmVoltage.WAVEFORM.PWM_MONOPOLE:
                mWaveFormPos = new PointF[] {
                    new PointF(x - DX, y),
                    new PointF(x + DX, y)
                };
                break;
            case ElmVoltage.WAVEFORM.PWM_DIPOLE:
                mWaveFormPos = new PointF[] {
                    new PointF(x - DX, y),
                    new PointF(x - DX + 1, y),
                    new PointF(x - DX + 1, y - ph),
                    new PointF(x - DX + 1, y),
                    new PointF(x - DX_H - wp, y),
                    new PointF(x - DX_H - wp, y - ph),
                    new PointF(x - DX_H + wp, y - ph),
                    new PointF(x - DX_H + wp, y),
                    new PointF(x - 1, y),
                    new PointF(x - 1, y - ph),
                    new PointF(x - 1, y),
                    new PointF(x + 1, y),
                    new PointF(x + 1, y + ph),
                    new PointF(x + 1, y),
                    new PointF(x + DX_H - wp, y),
                    new PointF(x + DX_H - wp, y + ph),
                    new PointF(x + DX_H + wp, y + ph),
                    new PointF(x + DX_H + wp, y),
                    new PointF(x + DX - 1, y),
                    new PointF(x + DX - 1, y + ph),
                    new PointF(x + DX - 1, y),
                    new PointF(x + DX, y)
                };
                break;
            case ElmVoltage.WAVEFORM.PWM_POSITIVE:
                mWaveFormPos = new PointF[] {
                    new PointF(x - DX, y),
                    new PointF(x - DX + 1, y),
                    new PointF(x - DX + 1, y - ph),
                    new PointF(x - DX + 1, y),
                    new PointF(x - DX_H - wp, y),
                    new PointF(x - DX_H - wp, y - ph),
                    new PointF(x - DX_H + wp, y - ph),
                    new PointF(x - DX_H + wp, y),
                    new PointF(x - 1, y),
                    new PointF(x - 1, y - ph),
                    new PointF(x - 1, y),
                    new PointF(x + DX, y)
                };
                break;
            case ElmVoltage.WAVEFORM.PWM_NEGATIVE:
                mWaveFormPos = new PointF[] {
                    new PointF(x - DX, y),
                    new PointF(x + 1, y),
                    new PointF(x + 1, y - ph),
                    new PointF(x + 1, y),
                    new PointF(x + DX_H - wp, y),
                    new PointF(x + DX_H - wp, y - ph),
                    new PointF(x + DX_H + wp, y - ph),
                    new PointF(x + DX_H + wp, y),
                    new PointF(x + DX - 1, y),
                    new PointF(x + DX - 1, y - ph),
                    new PointF(x + DX - 1, y),
                    new PointF(x + DX, y)
                };
                break;
            default:
                mWaveFormPos = new PointF[] {
                    new PointF(x - DX, y),
                    new PointF(x + DX, y)
                };
                break;
            }
            drawPolygon(mWaveFormPos);

            if (elm.WaveForm != ElmVoltage.WAVEFORM.NOISE) {
                if (ControlPanel.ChkShowValues.Checked) {
                    var s = Utils.UnitText(elm.MaxVoltage, "V\r\n");
                    s += Utils.UnitText(elm.Frequency, "Hz\r\n");
                    s += Utils.UnitText((elm.Phase + elm.PhaseOffset) * 180 / Math.PI, "deg");
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
            case ElmVoltage.WAVEFORM.SIN:
            case ElmVoltage.WAVEFORM.SQUARE:
            case ElmVoltage.WAVEFORM.PULSE_MONOPOLE:
            case ElmVoltage.WAVEFORM.PULSE_DIPOLE:
            case ElmVoltage.WAVEFORM.SAWTOOTH:
            case ElmVoltage.WAVEFORM.TRIANGLE:
            case ElmVoltage.WAVEFORM.NOISE:
            case ElmVoltage.WAVEFORM.PWM_DIPOLE:
            case ElmVoltage.WAVEFORM.PWM_POSITIVE:
            case ElmVoltage.WAVEFORM.PWM_NEGATIVE:
                arr[0] = elm.WaveForm.ToString(); break;
            }

            arr[1] = "I = " + Utils.CurrentText(elm.Current);
            arr[2] = ((this is Rail) ? "V = " : "Vd = ") + Utils.VoltageText(elm.GetVoltageDiff());
            int i = 3;
            if (elm.WaveForm != ElmVoltage.WAVEFORM.DC && elm.WaveForm != ElmVoltage.WAVEFORM.NOISE) {
                arr[i++] = "f = " + Utils.UnitText(elm.Frequency, "Hz");
                arr[i++] = "Vmax = " + Utils.VoltageText(elm.MaxVoltage);
                if (elm.WaveForm == ElmVoltage.WAVEFORM.SIN && elm.Bias == 0) {
                    arr[i++] = "V(rms) = " + Utils.VoltageText(elm.MaxVoltage / 1.41421356);
                }
                if (elm.Bias != 0) {
                    arr[i++] = "Voff = " + Utils.VoltageText(elm.Bias);
                } else if (elm.Frequency > 500) {
                    arr[i++] = "wavelength = " + Utils.UnitText(2.9979e8 / elm.Frequency, "m");
                }
            }
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var elm = (ElmVoltage)Elm;

            if (c == 0) {
                if (r == 0) {
                    var ei = new ElementInfo("波形");
                    ei.Choice = new ComboBox();
                    ei.Choice.Items.Add(ElmVoltage.WAVEFORM.DC);
                    ei.Choice.Items.Add(ElmVoltage.WAVEFORM.SIN);
                    ei.Choice.Items.Add(ElmVoltage.WAVEFORM.SQUARE);
                    ei.Choice.Items.Add(ElmVoltage.WAVEFORM.TRIANGLE);
                    ei.Choice.Items.Add(ElmVoltage.WAVEFORM.SAWTOOTH);
                    ei.Choice.Items.Add(ElmVoltage.WAVEFORM.PULSE_MONOPOLE);
                    ei.Choice.Items.Add(ElmVoltage.WAVEFORM.PULSE_DIPOLE);
                    ei.Choice.Items.Add(ElmVoltage.WAVEFORM.PWM_MONOPOLE);
                    ei.Choice.Items.Add(ElmVoltage.WAVEFORM.PWM_DIPOLE);
                    ei.Choice.Items.Add(ElmVoltage.WAVEFORM.PWM_POSITIVE);
                    ei.Choice.Items.Add(ElmVoltage.WAVEFORM.PWM_NEGATIVE);
                    ei.Choice.Items.Add(ElmVoltage.WAVEFORM.NOISE);
                    ei.Choice.SelectedIndex = (int)elm.WaveForm;
                    return ei;
                }
                if (r == 1) {
                    return new ElementInfo("名前", DumpInfo.ReferenceName);
                }
                if (r == 2) {
                    return new ElementInfo(elm.WaveForm == ElmVoltage.WAVEFORM.DC ? VALUE_NAME_V : VALUE_NAME_AMP, elm.MaxVoltage);
                }
                if (r == 3) {
                    return new ElementInfo(VALUE_NAME_BIAS, elm.Bias);
                }
                if (r == 4) {
                    if (elm.WaveForm == ElmVoltage.WAVEFORM.DC || elm.WaveForm == ElmVoltage.WAVEFORM.NOISE) {
                        return null;
                    } else {
                        return new ElementInfo(VALUE_NAME_HZ, elm.Frequency);
                    }
                }
                if (r == 5) {
                    return new ElementInfo(VALUE_NAME_PHASE, double.Parse((elm.Phase * 180 / Math.PI).ToString("0.00")));
                }
                if (r == 6) {
                    return new ElementInfo(VALUE_NAME_PHASE_OFS, double.Parse((elm.PhaseOffset * 180 / Math.PI).ToString("0.00")));
                }
                if (r == 7 && (elm.WaveForm == ElmVoltage.WAVEFORM.PULSE_MONOPOLE
                    || elm.WaveForm == ElmVoltage.WAVEFORM.PULSE_DIPOLE
                    || elm.WaveForm == ElmVoltage.WAVEFORM.SQUARE
                    || elm.WaveForm == ElmVoltage.WAVEFORM.PWM_MONOPOLE
                    || elm.WaveForm == ElmVoltage.WAVEFORM.PWM_DIPOLE
                    || elm.WaveForm == ElmVoltage.WAVEFORM.PWM_POSITIVE
                    || elm.WaveForm == ElmVoltage.WAVEFORM.PWM_NEGATIVE)) {
                    return new ElementInfo(VALUE_NAME_DUTY, elm.DutyCycle * 100);
                }
            }
            if (c == 1) {
                if (r == 3) {
                    return new ElementInfo("連動グループ", Link.Bias);
                }
                if (r == 4) {
                    return new ElementInfo("連動グループ", Link.Frequency);
                }
                if (r == 6) {
                    return new ElementInfo("連動グループ", Link.PhaseOffset);
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
                    if (elm.WaveForm == ElmVoltage.WAVEFORM.PULSE_MONOPOLE && ow != ElmVoltage.WAVEFORM.PULSE_MONOPOLE ||
                        elm.WaveForm != ElmVoltage.WAVEFORM.PULSE_MONOPOLE && ow == ElmVoltage.WAVEFORM.PULSE_MONOPOLE) {
                        elm.DutyCycle = DEFAULT_PULSE_DUTY;
                    }
                    SetPoints();
                }
                if (r == 1) {
                    DumpInfo.ReferenceName = ei.Text;
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
                if (elm.WaveForm == ElmVoltage.WAVEFORM.PULSE_MONOPOLE
                   || elm.WaveForm == ElmVoltage.WAVEFORM.PULSE_DIPOLE
                   || elm.WaveForm == ElmVoltage.WAVEFORM.SQUARE
                   || elm.WaveForm == ElmVoltage.WAVEFORM.PWM_MONOPOLE
                   || elm.WaveForm == ElmVoltage.WAVEFORM.PWM_DIPOLE
                   || elm.WaveForm == ElmVoltage.WAVEFORM.PWM_POSITIVE
                   || elm.WaveForm == ElmVoltage.WAVEFORM.PWM_NEGATIVE) {
                    if (r == 7) {
                        elm.DutyCycle = ei.Value * .01;
                    }
                }
            }
            if (c == 1) {
                if (r == 3) {
                    Link.Bias = (int)ei.Value;
                }
                if (r == 4) {
                    Link.Frequency = (int)ei.Value;
                }
                if (r == 6) {
                    Link.PhaseOffset = (int)ei.Value;
                }
            }
        }

        public override EventHandler CreateSlider(ElementInfo ei, Adjustable adj) {
            var trb = adj.Slider;
            var e1 = (ElmVoltage)Elm;
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
                    e1.MaxVoltage = val;
                    break;
                case VALUE_NAME_BIAS:
                    setLinkedValues<Voltage>(VoltageLink.BIAS, val);
                    break;
                case VALUE_NAME_HZ:
                    setLinkedValues<Voltage>(VoltageLink.FREQUENCY, val);
                    break;
                case VALUE_NAME_PHASE:
                    e1.Phase = val * Math.PI / 180;
                    break;
                case VALUE_NAME_PHASE_OFS:
                    setLinkedValues<Voltage>(VoltageLink.PHASE_OFFSET, val);
                    break;
                case VALUE_NAME_DUTY:
                    e1.DutyCycle = val;
                    break;
                }
                CirSimForm.NeedAnalyze();
            });
        }
    }
}
