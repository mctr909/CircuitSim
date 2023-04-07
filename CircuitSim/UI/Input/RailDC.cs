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
            arr[0] = "DC";
            arr[1] = "I = " + Utils.CurrentText(elm.Current);
            arr[2] = "V = " + Utils.VoltageText(elm.GetVoltageDiff());
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var elm = (ElmVoltage)Elm;
            if (c == 0) {
                if (r == 0) {
                    return new ElementInfo("名前", DumpInfo.ReferenceName);
                }
                if (r == 1) {
                    return new ElementInfo(VALUE_NAME_V, elm.MaxVoltage);
                }
                if (r == 2) {
                    return new ElementInfo(VALUE_NAME_BIAS, elm.Bias);
                }
            }
            if (c == 1) {
                if (r < 2) {
                    return new ElementInfo();
                }
                if (r == 2) {
                    return new ElementInfo("連動グループ", elm.LinkBias, true);
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
            }
            if (c == 1) {
                if (r == 2) {
                    elm.LinkBias = (int)ei.Value;
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
                }
                CirSimForm.NeedAnalyze();
            });
        }
    }
}
