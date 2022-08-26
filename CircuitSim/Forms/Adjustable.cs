using System;
using System.Windows.Forms;

using Circuit.Elements;
using Circuit.Elements.Passive;
using Circuit.Elements.Input;

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
            var ei = UI.GetElementInfo(EditItem);
            CreateSlider(ei);
        }

        public void CreateSlider(ElementInfo ei) {
            int intValue = (int)((ei.Value - MinValue) * 100 / (MaxValue - MinValue));
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
            mSlider.ValueChanged += UI.CreateSlider(ei, this);
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
