using System.Windows.Forms;

namespace Circuit.Elements {
    class VarRailElm : RailElm {
        TrackBar slider;
        Label label;
        string sliderText;

        public VarRailElm(int xx, int yy) : base(xx, yy, WF_VAR) {
            sliderText = "Voltage";
            frequency = maxVoltage;
            createSlider();
        }

        public VarRailElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f, st) {
            sliderText = st.nextToken();
            while (st.hasMoreTokens()) {
                sliderText += ' ' + st.nextToken();
            }
            sliderText = sliderText.Replace("%2[bB]", "+");
            createSlider();
        }

        public override string dump() {
            return base.dump() + " " + sliderText.Replace("\\+", "%2B");
        }

        public override DUMP_ID getDumpType() { return DUMP_ID.VAR_RAIL; }

        void createSlider() {
            waveform = WF_VAR;
            Sim.addWidgetToVerticalPanel(label = new Label() { Text = sliderText });
            int value = (int)((frequency - bias) * 100 / (maxVoltage - bias));
            Sim.addWidgetToVerticalPanel(slider = new TrackBar() {
                Minimum = 0,
                Maximum = 101,
                SmallChange = 1,
                Value = value
            });
            //	    sim.verticalPanel.validate();
        }

        public override double getVoltage() {
            frequency = slider.Value * (maxVoltage - bias) / 100.0 + bias;
            return frequency;
        }

        public override void delete() {
            Sim.removeWidgetFromVerticalPanel(label);
            Sim.removeWidgetFromVerticalPanel(slider);
            base.delete();
        }

        public override EditInfo getEditInfo(int n) {
            if (n == 0) {
                return new EditInfo("Min Voltage", bias, -20, 20);
            }
            if (n == 1) {
                return new EditInfo("Max Voltage", maxVoltage, -20, 20);
            }
            if (n == 2) {
                var ei = new EditInfo("Slider Text", 0, -1, -1);
                ei.Text = sliderText;
                return ei;
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (n == 0) {
                bias = ei.Value;
            }
            if (n == 1) {
                maxVoltage = ei.Value;
            }
            if (n == 2) {
                sliderText = ei.Textf.Text;
                label.Text = sliderText;
                Sim.setiFrameHeight();
            }
        }

        public override void setMouseElm(bool v) {
            base.setMouseElm(v);
        }
    }
}
