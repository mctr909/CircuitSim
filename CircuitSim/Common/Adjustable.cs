using System.Windows.Forms;

using Circuit.Elements;

namespace Circuit {
    class Adjustable {
        public CircuitElm elm;
        public double minValue;
        public double maxValue;
        public string sliderText;

        /* index of value in getEditInfo() list that this slider controls */
        public int editItem { get; private set; }

        public Label label;
        TrackBar slider;
        bool settingValue;

        public Adjustable(CircuitElm ce, int item) {
            minValue = 1;
            maxValue = 1000;
            elm = ce;
            editItem = item;
        }

        public Adjustable(StringTokenizer st, CirSim sim) {
            int e = st.nextTokenInt();
            if (e == -1) {
                return;
            }
            elm = sim.getElm(e);
            editItem = st.nextTokenInt();
            minValue = st.nextTokenDouble();
            maxValue = st.nextTokenDouble();
            sliderText = CustomLogicModel.unescape(st.nextToken());
        }

        public void createSlider(CirSim sim) {
            double value = elm.GetEditInfo(editItem).Value;
            createSlider(sim, value);
        }

        public void createSlider(CirSim sim, double value) {
            int intValue = (int)((value - minValue) * 100 / (maxValue - minValue));
            sim.addWidgetToVerticalPanel(label = new Label() { Text = sliderText });
            sim.addWidgetToVerticalPanel(slider = new TrackBar() {
                SmallChange = 1,
                LargeChange = 10,
                Minimum = 0,
                Maximum = 101,
                Value = intValue,
                Width = 100
            });
        }

        public void setSliderValue(double value) {
            int intValue = (int)((value - minValue) * 100 / (maxValue - minValue));
            settingValue = true; /* don't recursively set value again in execute() */
            slider.Value = intValue;
            settingValue = false;
        }

        public void execute() {
            CircuitElm.Sim.analyzeFlag = true;
            if (settingValue) {
                return;
            }
            var ei = elm.GetEditInfo(editItem);
            ei.Value = getSliderValue();
            elm.SetEditValue(editItem, ei);
            CircuitElm.Sim.repaint();
        }

        double getSliderValue() {
            return minValue + (maxValue - minValue) * slider.Value / 100;
        }

        public void deleteSlider(CirSim sim) {
            sim.removeWidgetFromVerticalPanel(label);
            sim.removeWidgetFromVerticalPanel(slider);
        }

        public string dump() {
            return CircuitElm.Sim.locateElm(elm)
                + " " + editItem
                + " " + minValue
                + " " + maxValue
                + " " + CustomLogicModel.escape(sliderText);
        }
    }
}
