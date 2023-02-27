using System;
using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.UI.Input {
    class VoltageAC : Voltage {
        public VoltageAC(Point pos) : base(pos, ElmVoltage.WAVEFORM.AC) { }

        public VoltageAC(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) { }

        public override DUMP_ID DumpType { get { return DUMP_ID.AC; } }

        public override void GetInfo(string[] arr) {
            var elm = (ElmVoltage)Elm;
            arr[0] = "AC";
            arr[1] = "I = " + Utils.CurrentText(elm.Current);
            arr[2] = "Vd = " + Utils.VoltageText(elm.VoltageDiff);
            int i = 3;
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
            arr[i++] = "P = " + Utils.UnitText(elm.Power, "W");
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var elm = (ElmVoltage)Elm;
            if (c == 0) {
                if (r == 0) {
                    return new ElementInfo("名前", DumpInfo.ReferenceName);
                }
                if (r == 1) {
                    return new ElementInfo(VALUE_NAME_AMP, elm.MaxVoltage);
                }
                if (r == 2) {
                    return new ElementInfo(VALUE_NAME_BIAS, elm.Bias);
                }
                if (r == 3) {
                    return new ElementInfo(VALUE_NAME_HZ, elm.Frequency);
                }
                if (r == 4) {
                    return new ElementInfo(VALUE_NAME_PHASE, double.Parse((elm.Phase * 180 / Math.PI).ToString("0.00")));
                }
                if (r == 5) {
                    return new ElementInfo(VALUE_NAME_PHASE_OFS, double.Parse((elm.PhaseOffset * 180 / Math.PI).ToString("0.00")));
                }
            }
            if (c == 1) {
                if (r == 2) {
                    return new ElementInfo("連動グループ", elm.LinkBias, true);
                }
                if (r == 3) {
                    return new ElementInfo("連動グループ", elm.LinkFrequency, true);
                }
                if (r == 5) {
                    return new ElementInfo("連動グループ", elm.LinkPhaseOffset, true);
                }
                if (r < 5) {
                    return new ElementInfo();
                }
            }
            return null;
        }

        public override void SetElementValue(int r, int c, ElementInfo ei) {
            var elm = (ElmVoltage)Elm;
            if (c == 0) {
                if (r == 0) {
                    DumpInfo.ReferenceName = ei.Textf.Text;
                }
                if (r == 1) {
                    elm.MaxVoltage = ei.Value;
                }
                if (r == 2) {
                    elm.Bias = ei.Value;
                }
                if (r == 3) {
                    elm.Frequency = ei.Value;
                    var maxfreq = 1 / (8 * ControlPanel.TimeStep);
                    if (maxfreq < elm.Frequency) {
                        elm.Frequency = maxfreq;
                    }
                }
                if (r == 4) {
                    elm.Phase = ei.Value * Math.PI / 180;
                }
                if (r == 5) {
                    elm.PhaseOffset = ei.Value * Math.PI / 180;
                }
            }
            if (c == 1) {
                if (r == 2) {
                    elm.LinkBias = (int)ei.Value;
                }
                if (r == 3) {
                    elm.LinkFrequency = (int)ei.Value;
                }
                if (r == 5) {
                    elm.LinkPhaseOffset = (int)ei.Value;
                }
            }
        }

        public override EventHandler CreateSlider(ElementInfo ei, Adjustable adj) {
            var trb = adj.Slider;
            var ce = (ElmVoltage)Elm;
            switch (ei.Name) {
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
            }
            return new EventHandler((s, e) => {
                var val = adj.MinValue + (adj.MaxValue - adj.MinValue) * trb.Value / trb.Maximum;
                switch (ei.Name) {
                case VALUE_NAME_AMP:
                    ce.MaxVoltage = val;
                    break;
                case VALUE_NAME_BIAS:
                    ce.Bias = val;
                    if (ce.LinkBias != 0) {
                        for (int i = 0; i != CirSimForm.UICount; i++) {
                            var o = CirSimForm.GetUI(i).Elm;
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
                        for (int i = 0; i != CirSimForm.UICount; i++) {
                            var o = CirSimForm.GetUI(i).Elm;
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
                        for (int i = 0; i != CirSimForm.UICount; i++) {
                            var o = CirSimForm.GetUI(i).Elm;
                            if (o is ElmVoltage) {
                                var s2 = (ElmVoltage)o;
                                if (s2.LinkPhaseOffset == ce.LinkPhaseOffset) {
                                    s2.PhaseOffset = ce.PhaseOffset;
                                }
                            }
                        }
                    }
                    break;
                }
                CirSimForm.NeedAnalyze();
            });
        }
    }
}
