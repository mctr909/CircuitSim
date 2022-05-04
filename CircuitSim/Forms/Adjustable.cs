using System;
using System.Windows.Forms;

using Circuit.Elements;
using Circuit.Elements.Passive;

namespace Circuit {
    class Adjustable {
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
        public CircuitElm Elm;
        public double MinValue;
        public double MaxValue;
        public string SliderText;

        TrackBar mSlider;
        bool mSettingValue;

        public Adjustable(CircuitElm ce, int item) {
            MinValue = 1;
            MaxValue = 1000;
            Elm = ce;
            EditItem = item;
        }

        public Adjustable(StringTokenizer st, CirSim sim) {
            int e = st.nextTokenInt();
            if (e == -1) {
                return;
            }
            Elm = sim.getElm(e);
            EditItem = st.nextTokenInt();
            MinValue = st.nextTokenDouble();
            MaxValue = st.nextTokenDouble();
            SliderText = CustomLogicModel.unescape(st.nextToken());
        }

        public void CreateSlider() {
            double value = Elm.GetElementInfo(EditItem).Value;
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
            if (Elm is ResistorElm) {
                mSlider.ValueChanged += new EventHandler((s, e) => {
                    var trb = (TrackBar)s;
                    ((ResistorElmE)Elm.CirElm).Resistance = MinValue + (MaxValue - MinValue) * trb.Value / trb.Maximum;
                    CirSim.Sim.NeedAnalyze();
                });
            }
            if (Elm is CapacitorElm) {
                mSlider.ValueChanged += new EventHandler((s, e) => {
                    var trb = (TrackBar)s;
                    ((CapacitorElmE)Elm.CirElm).Capacitance = MinValue + (MaxValue - MinValue) * trb.Value / trb.Maximum;
                    CirSim.Sim.NeedAnalyze();
                });
            }
            if (Elm is InductorElm) {
                mSlider.ValueChanged += new EventHandler((s, e) => {
                    var trb = (TrackBar)s;
                    ((InductorElmE)Elm.CirElm).Inductance = MinValue + (MaxValue - MinValue) * trb.Value / trb.Maximum;
                    CirSim.Sim.NeedAnalyze();
                });
            }
        }

        public void DeleteSlider() {
            ControlPanel.RemoveSlider(Label);
            ControlPanel.RemoveSlider(mSlider);
        }

        public void Execute() {
            CirSim.Sim.NeedAnalyze();
            if (mSettingValue) {
                return;
            }
            var ei = Elm.GetElementInfo(EditItem);
            ei.Value = Value;
            Elm.SetElementValue(EditItem, ei);
            CirSim.Sim.Repaint();
        }

        public string Dump() {
            return CirSim.Sim.LocateElm(Elm)
                + " " + EditItem
                + " " + MinValue
                + " " + MaxValue
                + " " + CustomLogicModel.escape(SliderText);
        }
    }
}
