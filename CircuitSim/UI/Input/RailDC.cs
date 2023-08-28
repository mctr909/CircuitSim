using System;
using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.UI.Input {
    class RailDC : Rail {
        public RailDC(Point pos) : base(pos, ElmVoltage.WAVEFORM.DC) { }

        public RailDC(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) { }

        public override DUMP_ID DumpType { get { return DUMP_ID.RAIL_DC; } }

        public override void GetInfo(string[] arr) {
            var elm = (ElmVoltage)Elm;
            arr[0] = "直流電源";
            arr[1] = "電圧：" + Utils.VoltageText(elm.GetVoltageDiff() + elm.Bias);
            arr[2] = "電流：" + Utils.CurrentText(elm.Current);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var elm = (ElmVoltage)Elm;
            if (c == 0) {
                if (r == 0) {
                    return new ElementInfo(VALUE_NAME_V, elm.MaxVoltage);
                }
                if (r == 1) {
                    return new ElementInfo(VALUE_NAME_BIAS, elm.Bias);
                }
            }
            if (c == 1) {
                if (r == 0) {
                    return new ElementInfo("連動グループ", Link.Voltage);
                }
                if (r == 1) {
                    return new ElementInfo("連動グループ", Link.Bias);
                }
                if (r < 1) {
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
            }
            if (c == 1) {
                if (r == 0) {
                    Link.Voltage = (int)ei.Value;
                }
                if (r == 1) {
                    Link.Bias = (int)ei.Value;
                }
            }
        }

        public override EventHandler CreateSlider(ElementInfo ei, Adjustable adj) {
            var trb = adj.Slider;
            var ce = (ElmVoltage)Elm;
            switch (ei.Name) {
            case VALUE_NAME_V:
                adj.MinValue = 0;
                adj.MaxValue = 5;
                break;
            case VALUE_NAME_BIAS:
                adj.MinValue = 0;
                adj.MaxValue = 5;
                break;
            }
            return new EventHandler((s, e) => {
                var val = adj.MinValue + (adj.MaxValue - adj.MinValue) * trb.Value / trb.Maximum;
                switch (ei.Name) {
                case VALUE_NAME_V:
                    setLinkedValues<Voltage>(VoltageLink.VOLTAGE, val);
                    break;
                case VALUE_NAME_BIAS:
                    setLinkedValues<Voltage>(VoltageLink.BIAS, val);
                    break;
                }
            });
        }
    }
}
