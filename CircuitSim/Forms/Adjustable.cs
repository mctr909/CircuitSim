﻿using System.Windows.Forms;

using Circuit.Elements;

namespace Circuit {
    public class Adjustable {
        /* index of value in getEditInfo() list that this slider controls */
        public int EditItemR { get; private set; }
        public int EditItemC { get; private set; }

        public double Value {
            get { return MinValue + (MaxValue - MinValue) * Slider.Value / 100; }
            set {
                int intValue = (int)((value - MinValue) * 100 / (MaxValue - MinValue));
                mSettingValue = true; /* don't recursively set value again in execute() */
                Slider.Value = (intValue < Slider.Minimum) ? Slider.Minimum :
                    (Slider.Maximum < intValue) ? Slider.Maximum : intValue;
                mSettingValue = false;
            }
        }

        public TrackBar Slider;
        public Label Label;
        public BaseUI UI;
        public double MinValue;
        public double MaxValue;
        public string SliderText;

        bool mSettingValue;

        public Adjustable(BaseUI ce, int itemR) {
            MinValue = 1;
            MaxValue = 1000;
            UI = ce;
            EditItemR = itemR;
            EditItemC = 0;
        }

        public Adjustable(StringTokenizer st) {
            int e = st.nextTokenInt();
            if (e == -1) {
                return;
            }
            UI = CirSimForm.GetElm(e);
            EditItemR = st.nextTokenInt();
            EditItemC = 0;
            MinValue = st.nextTokenDouble();
            MaxValue = st.nextTokenDouble();
            SliderText = Utils.Unescape(st.nextToken());
        }

        public void CreateSlider() {
            var ei = UI.GetElementInfo(EditItemR, EditItemC);
            CreateSlider(ei);
        }

        public void CreateSlider(ElementInfo ei) {
            int intValue = (int)((ei.Value - MinValue) * 100 / (MaxValue - MinValue));
            ControlPanel.AddSlider(Label = new Label() { Text = SliderText });
            ControlPanel.AddSlider(Slider = new TrackBar() {
                SmallChange = 1,
                LargeChange = 10,
                TickFrequency = 10,
                TickStyle = TickStyle.TopLeft,
                Minimum = 0,
                Maximum = 100,
                Value = (intValue < 0) ? 0 : (100 < intValue) ? 100 : intValue,
                Width = 175,
                Height = 23
            });
            Slider.ValueChanged += UI.CreateSlider(ei, this);
        }

        public void DeleteSlider() {
            ControlPanel.RemoveSlider(Label);
            ControlPanel.RemoveSlider(Slider);
        }

        public void Execute() {
            CirSimForm.NeedAnalyze();
            if (mSettingValue) {
                return;
            }
            var ei = UI.GetElementInfo(EditItemR, EditItemC);
            ei.Value = Value;
            UI.SetElementValue(EditItemR, EditItemC, ei);
            CirSimForm.Repaint();
        }

        public string Dump() {
            return CirSimForm.GetElmIndex(UI)
                + " " + EditItemR
                + " " + MinValue
                + " " + MaxValue
                + " " + Utils.Escape(SliderText);
        }
    }
}
