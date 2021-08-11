using System;
using System.Collections.Generic;
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

        public AudioOutputElm(Point pos) : base(pos) {
            duration = 1;
            samplingRate = lastSamplingRate;
            labelNum = getNextLabelNum();
            setDataCount();
            createButton();
        }

        public AudioOutputElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
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
            if (Sim.ElmList == null) {
                return 0;
            }
            for (i = 0; i != Sim.ElmList.Count; i++) {
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
            g.FillRectangle(P2.X - textWidth / 2, P2.Y - 10, pct, 20);
            g.LineColor = selected ? SelectColor : WhiteColor;
            Utils.InterpPoint(mPoint1, mPoint2, ref mLead1, 1 - (textWidth / 2.0 + 8) / mLen);
            setBbox(mPoint1, mLead1, 0);
            drawCenteredText(g, s, P2.X, P2.Y, true);
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
            arr[2] = "start = " + Utils.UnitText(dataFull ? Sim.Time - duration : dataStart, "s");
            arr[3] = "dur = " + Utils.UnitText(dur, "s");
            arr[4] = "samples = " + ct + (dataFull ? "" : "/" + dataCount);
        }

        public override void StepFinished() {
            dataSample += Volts[0];
            dataSampleCount++;
            if (Sim.Time >= nextDataSample) {
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
            ControlPanel.RemoveSlider(button);
            base.Delete();
        }

        void setDataCount() {
            dataCount = (int)(samplingRate * duration);
            data = new double[dataCount];
            dataStart = Sim.Time;
            dataPtr = 0;
            dataFull = false;
            sampleStep = 1.0 / samplingRate;
            nextDataSample = Sim.Time + sampleStep;
        }

        void setTimeStep() {
            double target = sampleStep / 8;
            if (ControlPanel.TimeStep != target) {
                if (okToChangeTimeStep || MessageBox.Show("Adjust timestep for best audio quality and performance?", "", MessageBoxButtons.OKCancel) == DialogResult.OK) {
                    ControlPanel.TimeStep = target;
                    okToChangeTimeStep = true;
                }
            }
        }

        void createButton() {
            string label = "Play Audio";
            if (labelNum > 1) {
                label += " " + labelNum;
            }
            ControlPanel.AddSlider(button = new Button() { Text = label });
            button.Click += new EventHandler((s, e) => {
                play();
            });
        }

        void play() {
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
            for (int i = 0; i != ct; i++) {
                if (data[i] > max) max = data[i];
                if (data[i] < min) min = data[i];
            }

            double adj = -(max + min) / 2;
            double mult = (.25 * 32766) / (max + adj);

            /* fade in over 1/20 sec */
            int fadeLen = samplingRate / 20;
            int fadeOut = ct - fadeLen;

            double fadeMult = mult / fadeLen;
            var samples = new short[ct];
            for (int i = 0; i != ct; i++) {
                double fade = (i < fadeLen) ? i * fadeMult : (i > fadeOut) ? (ct - i) * fadeMult : mult;
                samples[i] = (short)((data[(i + _base) % dataCount] + adj) * fade);
            }

            var wav = new Wav(samplingRate);
            wav.setBuffer(samples);
            var srclist = new List<short>();
            while (!wav.eof()) {
                srclist.AddRange(wav.getBuffer(1000));
            }
        }

        class Wav {
            int _sampleRate;
            short _channels;
            short[] _buffer;
            int _bufferNeedle = 0;
            int[] _internalBuffer;
            bool _hasOutputHeader;
            bool _eof = true;

            public Wav(int sampleRate = 44100, short channels = 1) {
                _sampleRate = sampleRate;
                _channels = channels;
            }

            public void setBuffer(short[] buffer) {
                _buffer = getWavInt16Array(buffer);
                _bufferNeedle = 0;
                _internalBuffer = null;
                _hasOutputHeader = false;
                _eof = false;
            }

            public short[] getBuffer(int len) {
                short[] rt;
                if (_bufferNeedle + len >= _buffer.Length) {
                    rt = new short[_buffer.Length - _bufferNeedle];
                    _eof = true;
                } else {
                    rt = new short[len];
                }
                for (var i = 0; i < rt.Length; i++) {
                    rt[i] = _buffer[i + _bufferNeedle];
                }
                _bufferNeedle += rt.Length;
                return rt;
            }

            public bool eof() {
                return _eof;
            }

            short[] getWavInt16Array(short[] buffer) {
                var intBuffer = new short[buffer.Length + 23];

                intBuffer[0] = 0x4952; // "RI"
                intBuffer[1] = 0x4646; // "FF"

                intBuffer[2] = (short)((2 * buffer.Length + 15) & 0x0000ffff); // RIFF size
                intBuffer[3] = (short)(((2 * buffer.Length + 15) & 0xffff0000) >> 16); // RIFF size

                intBuffer[4] = 0x4157; // "WA"
                intBuffer[5] = 0x4556; // "VE"

                intBuffer[6] = 0x6d66; // "fm"
                intBuffer[7] = 0x2074; // "t "

                intBuffer[8] = 0x0012; // fmt chunksize: 18
                intBuffer[9] = 0x0000; //

                intBuffer[10] = 0x0001; // format tag : 1
                intBuffer[11] = _channels; // channels: 2

                intBuffer[12] = (short)(_sampleRate & 0x0000ffff); // sample per sec
                intBuffer[13] = (short)((_sampleRate & 0xffff0000) >> 16); // sample per sec

                intBuffer[14] = (short)((2 * _channels * _sampleRate) & 0x0000ffff); // byte per sec
                intBuffer[15] = (short)(((2 * _channels * _sampleRate) & 0xffff0000) >> 16); // byte per sec

                intBuffer[16] = 0x0004; // block align
                intBuffer[17] = 0x0010; // bit per sample
                intBuffer[18] = 0x0000; // cb size
                intBuffer[19] = 0x6164; // "da"
                intBuffer[20] = 0x6174; // "ta"
                intBuffer[21] = (short)((2 * buffer.Length) & 0x0000ffff); // data size[byte]
                intBuffer[22] = (short)(((2 * buffer.Length) & 0xffff0000) >> 16); // data size[byte]

                for (int i = 0; i < buffer.Length; i++) {
                    intBuffer[i + 23] = buffer[i];
                }
                return intBuffer;
            }
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo("Duration (s)", duration, 0, 5);
                return ei;
            }
            if (n == 1) {
                var ei = new ElementInfo("Sampling Rate", 0, -1, -1);
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

        public override void SetElementValue(int n, ElementInfo ei) {
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
