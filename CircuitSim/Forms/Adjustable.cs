using System;
using System.Windows.Forms;

using Circuit.Elements;
using Circuit.Elements.Passive;

namespace Circuit {
    public class Adjustable {
        /* index of value in getEditInfo() list that this slider controls */
        public int EditItem { get; private set; }

        public double Value {
            get { return MinValue + (MaxValue - MinValue) * mSlider.Value / 100; }
            set {
                int intValue = (int)((value - MinValue) * 100 / (MaxValue - MinValue));
                mSettingValue = true; /* don't recursively set value again in execute() */
                mSlider.Value = intValue;
                mSettingValue = false;
            }
        }

        public Label Label;
        public BaseUI UI;
        public double MinValue;
        public double MaxValue;
        public string SliderText;

        TrackBar mSlider;
        bool mSettingValue;

        public Adjustable(BaseUI ce, int item) {
            MinValue = 1;
            MaxValue = 1000;
            UI = ce;
            EditItem = item;
        }

        public Adjustable(StringTokenizer st, CirSimForm sim) {
            int e = st.nextTokenInt();
            if (e == -1) {
                return;
            }
            UI = sim.GetElm(e);
            EditItem = st.nextTokenInt();
            MinValue = st.nextTokenDouble();
            MaxValue = st.nextTokenDouble();
            SliderText = Utils.Unescape(st.nextToken());
        }

        public void CreateSlider() {
            double value = UI.GetElementInfo(EditItem).Value;
            CreateSlider(value);
        }

        public void CreateSlider(double value) {
            int intValue = (int)((value - MinValue) * 100 / (MaxValue - MinValue));
            ControlPanel.AddSlider(Label = new Label() { Text = SliderText });
            ControlPanel.AddSlider(mSlider = new TrackBar() {
                SmallChange = 1,
                LargeChange = 10,
                TickFrequency = 10,
                TickStyle = TickStyle.TopLeft,
                Minimum = 0,
                Maximum = 100,
                Value = (100 < intValue) ? 100 : intValue,
                Width = 175,
                Height = 23
            });
            if (UI is ResistorUI) {
                mSlider.ValueChanged += new EventHandler((s, e) => {
                    var trb = (TrackBar)s;
                    ((ResistorElm)UI.Elm).Resistance = MinValue + (MaxValue - MinValue) * trb.Value / trb.Maximum;
                    CirSimForm.Sim.NeedAnalyze();
                });
            }
            if (UI is CapacitorUI) {
                mSlider.ValueChanged += new EventHandler((s, e) => {
                    var trb = (TrackBar)s;
                    ((CapacitorElm)UI.Elm).Capacitance = MinValue + (MaxValue - MinValue) * trb.Value / trb.Maximum;
                    CirSimForm.Sim.NeedAnalyze();
                });
            }
            if (UI is InductorUI) {
                mSlider.ValueChanged += new EventHandler((s, e) => {
                    var trb = (TrackBar)s;
                    ((InductorElm)UI.Elm).Inductance = MinValue + (MaxValue - MinValue) * trb.Value / trb.Maximum;
                    CirSimForm.Sim.NeedAnalyze();
                });
            }
        }

        public void DeleteSlider() {
            ControlPanel.RemoveSlider(Label);
            ControlPanel.RemoveSlider(mSlider);
        }

        public void Execute() {
            CirSimForm.Sim.NeedAnalyze();
            if (mSettingValue) {
                return;
            }
            var ei = UI.GetElementInfo(EditItem);
            ei.Value = Value;
            UI.SetElementValue(EditItem, ei);
            CirSimForm.Sim.Repaint();
        }

        public string Dump() {
            return CirSimForm.Sim.LocateElm(UI)
                + " " + EditItem
                + " " + MinValue
                + " " + MaxValue
                + " " + Utils.Escape(SliderText);
        }
    }
}
