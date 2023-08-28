using System;
using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.UI.Input {
    class VoltageAC : Voltage {
        public VoltageAC(Point pos) : base(pos, ElmVoltage.WAVEFORM.SIN) { }

        public VoltageAC(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) { }

        public override DUMP_ID DumpType { get { return DUMP_ID.AC; } }

        public override void GetInfo(string[] arr) {
            var elm = (ElmVoltage)Elm;
            arr[0] = "交流電源";
            arr[1] = "電流：" + Utils.CurrentText(elm.Current);
            arr[2] = "振幅：" + Utils.VoltageText(elm.MaxVoltage);
            arr[3] = "周波数：" + Utils.UnitText(elm.Frequency, "Hz");
            var phase = elm.Phase + elm.PhaseOffset;
            phase %= 2 * Math.PI;
            arr[4] = "位相：" + Utils.UnitText3digit(phase * 180 / Math.PI, "deg");
            if (elm.Bias != 0) {
                arr[5] = "バイアス：" + Utils.VoltageText(elm.Bias);
            }
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var elm = (ElmVoltage)Elm;
            if (c == 0) {
                if (r == 0) {
                    return new ElementInfo(VALUE_NAME_AMP, elm.MaxVoltage);
                }
                if (r == 1) {
                    return new ElementInfo(VALUE_NAME_BIAS, elm.Bias);
                }
                if (r == 2) {
                    return new ElementInfo(VALUE_NAME_HZ, elm.Frequency);
                }
                if (r == 3) {
                    return new ElementInfo(VALUE_NAME_PHASE, double.Parse((elm.Phase * 180 / Math.PI).ToString("0.00")));
                }
                if (r == 4) {
                    return new ElementInfo(VALUE_NAME_PHASE_OFS, double.Parse((elm.PhaseOffset * 180 / Math.PI).ToString("0.00")));
                }
            }
            if (c == 1) {
                if (r == 0) {
                    return new ElementInfo("連動グループ", Link.Voltage);
                }
                if (r == 1) {
                    return new ElementInfo("連動グループ", Link.Bias);
                }
                if (r == 2) {
                    return new ElementInfo("連動グループ", Link.Frequency);
                }
                if (r == 4) {
                    return new ElementInfo("連動グループ", Link.PhaseOffset);
                }
                if (r < 4) {
                    return new ElementInfo();
                }
            }
            return null;
        }

        public override void SetElementValue(int r, int c, ElementInfo ei) {
            var elm = (ElmVoltage)Elm;
            if (c == 0) {
                if (r == 0) {
                    elm.MaxVoltage = ei.Value;
                }
                if (r == 1) {
                    elm.Bias = ei.Value;
                }
                if (r == 2) {
                    elm.Frequency = ei.Value;
                    var maxfreq = 1 / (8 * ControlPanel.TimeStep);
                    if (maxfreq < elm.Frequency) {
                        elm.Frequency = maxfreq;
                    }
                }
                if (r == 3) {
                    elm.Phase = ei.Value * Math.PI / 180;
                }
                if (r == 4) {
                    elm.PhaseOffset = ei.Value * Math.PI / 180;
                }
            }
            if (c == 1) {
                if (r == 0) {
                    Link.Voltage = (int)ei.Value;
                }
                if (r == 1) {
                    Link.Bias = (int)ei.Value;
                }
                if (r == 2) {
                    Link.Frequency = (int)ei.Value;
                }
                if (r == 4) {
                    Link.PhaseOffset = (int)ei.Value;
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
                    setLinkedValues<Voltage>(VoltageLink.VOLTAGE, val);
                    break;
                case VALUE_NAME_BIAS:
                    setLinkedValues<Voltage>(VoltageLink.BIAS, val);
                    break;
                case VALUE_NAME_HZ:
                    setLinkedValues<Voltage>(VoltageLink.FREQUENCY, val);
                    break;
                case VALUE_NAME_PHASE:
                    ce.Phase = val * Math.PI / 180;
                    break;
                case VALUE_NAME_PHASE_OFS:
                    setLinkedValues<Voltage>(VoltageLink.PHASE_OFFSET, val);
                    break;
                }
            });
        }
    }
}
