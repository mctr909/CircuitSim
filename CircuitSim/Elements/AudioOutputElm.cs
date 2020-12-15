using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class AudioOutputElm : CircuitElm {
        int dataCount;
        int dataPtr;
        double[] data;
        bool dataFull;
        Button button;
        int samplingRate;
        int labelNum;
        double duration;
        double sampleStep;
        double dataStart;
        static int lastSamplingRate = 8000;
        static bool okToChangeTimeStep;

        int dataSampleCount = 0;
        double nextDataSample = 0;
        double dataSample;

        int[] samplingRateChoices = { 8000, 11025, 16000, 22050, 44100 };

        public AudioOutputElm(int xx, int yy) : base(xx, yy) {
            duration = 1;
            samplingRate = lastSamplingRate;
            labelNum = getNextLabelNum();
            setDataCount();
            createButton();
        }

        public AudioOutputElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            duration = st.nextTokenDouble();
            samplingRate = st.nextTokenInt();
            labelNum = st.nextTokenInt();
            setDataCount();
            createButton();
        }

        public override int PostCount { get { return 1; } }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.WAVE_OUT; } }

        protected override string dump() {
            return duration + " " + samplingRate + " " + labelNum;
        }

        void draggingDone() {
            setTimeStep();
        }

        /* get next unused labelNum value */
        int getNextLabelNum() {
            int i;
            int num = 1;
            if (Sim.elmList == null) {
                return 0;
            }
            for (i = 0; i != Sim.elmList.Count; i++) {
                var ce = Sim.getElm(i);
                if (!(ce is AudioOutputElm)) {
                    continue;
                }
                int ln = ((AudioOutputElm)ce).labelNum;
                if (ln >= num) {
                    num = ln + 1;
                }
            }
            return num;
        }

        public override void Reset() {
            dataPtr = 0;
            dataFull = false;
            dataSampleCount = 0;
            nextDataSample = 0;
            dataSample = 0;
        }

        public override void SetPoints() {
            base.SetPoints();
            mLead1 = new Point();
        }

        public override void Draw(CustomGraphics g) {
            var selected = NeedsHighlight;
            var s = "Audio Out";
            if (labelNum > 1) {
                s = "Audio " + labelNum;
            }
            int textWidth = (int)g.GetTextSize(s).Width;
            g.LineColor = GrayColor;
            int pct = dataFull ? textWidth : textWidth * dataPtr / dataCount;
            g.FillRectangle(X2 - textWidth / 2, Y2 - 10, pct, 20);
            g.LineColor = selected ? SelectColor : WhiteColor;
            Utils.InterpPoint(mPoint1, mPoint2, ref mLead1, 1 - (textWidth / 2.0 + 8) / mLen);
            setBbox(mPoint1, mLead1, 0);
            drawCenteredText(g, s, X2, Y2, true);
            if (selected) {
                g.ThickLineColor = SelectColor;
            } else {
                g.ThickLineColor = getVoltageColor(Volts[0]);
            }
            g.DrawThickLine(mPoint1, mLead1);
            drawPosts(g);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "audio output";
            arr[1] = "V = " + Utils.VoltageText(Volts[0]);
            int ct = (dataFull ? dataCount : dataPtr);
            double dur = sampleStep * ct;
            arr[2] = "start = " + Utils.UnitText(dataFull ? Sim.t - duration : dataStart, "s");
            arr[3] = "dur = " + Utils.UnitText(dur, "s");
            arr[4] = "samples = " + ct + (dataFull ? "" : "/" + dataCount);
        }

        public override void StepFinished() {
            dataSample += Volts[0];
            dataSampleCount++;
            if (Sim.t >= nextDataSample) {
                nextDataSample += sampleStep;
                data[dataPtr++] = dataSample / dataSampleCount;
                dataSampleCount = 0;
                dataSample = 0;
                if (dataPtr >= dataCount) {
                    dataPtr = 0;
                    dataFull = true;
                }
            }
        }

        public override void Delete() {
            Sim.RemoveWidgetFromVerticalPanel(button);
            base.Delete();
        }

        void setDataCount() {
            dataCount = (int)(samplingRate * duration);
            data = new double[dataCount];
            dataStart = Sim.t;
            dataPtr = 0;
            dataFull = false;
            sampleStep = 1.0 / samplingRate;
            nextDataSample = Sim.t + sampleStep;
        }

        void setTimeStep() {
            /*
            // timestep must be smaller than 1/sampleRate
            if (sim.timeStep > sampleStep)
            sim.timeStep = sampleStep;
            else {
            // make sure sampleStep/timeStep is an integer.  otherwise we get distortion
    //		int frac = (int)Math.round(sampleStep/sim.timeStep);
    //		sim.timeStep = sampleStep / frac;

            // actually, just make timestep = 1/sampleRate
            sim.timeStep = sampleStep;
            }
            */
            //	    int frac = (int)Math.round(Math.max(sampleStep*33000, 1));
            double target = sampleStep / 8;
            if (Sim.timeStep != target) {
                if (okToChangeTimeStep || MessageBox.Show("Adjust timestep for best audio quality and performance?") == DialogResult.OK) {
                    Sim.timeStep = target;
                    okToChangeTimeStep = true;
                }
            }
        }

        void createButton() {
            string label = "Play Audio";
            if (labelNum > 1) {
                label += " " + labelNum;
            }
            Sim.AddWidgetToVerticalPanel(button = new Button() { Text = label });
            button.Click += new EventHandler((s, e) => {
                play();
            });
        }

        void play() {
            int i;
            int ct = dataPtr;
            int _base = 0;
            if (dataFull) {
                ct = dataCount;
                _base = dataPtr;
            }
            if (ct * sampleStep < .05) {
                MessageBox.Show("Audio data is not ready yet.\r\nIncrease simulation speed to make data ready sooner.");
                return;
            }

            /* rescale data to maximize */
            double max = -1e8;
            double min = 1e8;
            for (i = 0; i != ct; i++) {
                if (data[i] > max) max = data[i];
                if (data[i] < min) min = data[i];
            }

            double adj = -(max + min) / 2;
            double mult = (.25 * 32766) / (max + adj);

            /* fade in over 1/20 sec */
            int fadeLen = samplingRate / 20;
            int fadeOut = ct - fadeLen;

            double fadeMult = mult / fadeLen;
            for (i = 0; i != ct; i++) {
                double fade = (i < fadeLen) ? i * fadeMult : (i > fadeOut) ? (ct - i) * fadeMult : mult;
                int s = (int)((data[(i + _base) % dataCount] + adj) * fade);
                // TODO:
                //arr.push(s);
            }
            // TODO:
            //playJS(arr, samplingRate);
        }

        public override EditInfo GetEditInfo(int n) {
            if (n == 0) {
                var ei = new EditInfo("Duration (s)", duration, 0, 5);
                return ei;
            }
            if (n == 1) {
                var ei = new EditInfo("Sampling Rate", 0, -1, -1);
                ei.Choice = new ComboBox();
                for (int i = 0; i != samplingRateChoices.Length; i++) {
                    ei.Choice.Items.Add(samplingRateChoices[i] + "");
                    if (samplingRateChoices[i] == samplingRate) {
                        ei.Choice.SelectedIndex = i;
                    }
                }
                return ei;
            }
            return null;
        }

        public override void SetEditValue(int n, EditInfo ei) {
            if (n == 0 && ei.Value > 0) {
                duration = ei.Value;
                setDataCount();
            }
            if (n == 1) {
                int nsr = samplingRateChoices[ei.Choice.SelectedIndex];
                if (nsr != samplingRate) {
                    samplingRate = nsr;
                    lastSamplingRate = nsr;
                    setDataCount();
                    setTimeStep();
                }
            }
        }
    }
}
