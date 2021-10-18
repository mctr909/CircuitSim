using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Output {
    class AudioOutputElm : CircuitElm {
        readonly int[] SAMPLE_RATE_LIST = { 8000, 11025, 16000, 22050, 44100 };

        Button mButton;
        int mLabelNum;

        bool mDataFull;
        int mSamplingRate;
        int mDataCount;
        int mDataPtr;
        double[] mData;

        double mDuration;
        double mSampleStep;
        double mDataStart;
        double mDataSample;
        double mNextDataSample = 0;
        int mDataSampleCount = 0;

        static int mLastSamplingRate = 8000;
        static bool mOkToChangeTimeStep;

        class Wav {
            int _sampleRate;
            short _channels;
            short[] _buffer;
            int _bufferNeedle = 0;
            bool _eof = true;

            public Wav(int sampleRate = 44100, short channels = 1) {
                _sampleRate = sampleRate;
                _channels = channels;
            }

            public void setBuffer(short[] buffer) {
                _buffer = getWavInt16Array(buffer);
                _bufferNeedle = 0;
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

        public AudioOutputElm(Point pos) : base(pos) {
            mDuration = 1;
            mSamplingRate = mLastSamplingRate;
            mLabelNum = getNextLabelNum();
            setDataCount();
            createButton();
        }

        public AudioOutputElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mDuration = st.nextTokenDouble();
            mSamplingRate = st.nextTokenInt();
            mLabelNum = st.nextTokenInt();
            setDataCount();
            createButton();
        }

        public override int PostCount { get { return 1; } }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.WAVE_OUT; } }

        protected override string dump() {
            return mDuration + " " + mSamplingRate + " " + mLabelNum;
        }

        void draggingDone() {
            setTimeStep();
        }

        /* get next unused labelNum value */
        int getNextLabelNum() {
            int i;
            int num = 1;
            if (CirSim.Sim.ElmList == null) {
                return 0;
            }
            for (i = 0; i != CirSim.Sim.ElmList.Count; i++) {
                var ce = CirSim.Sim.getElm(i);
                if (!(ce is AudioOutputElm)) {
                    continue;
                }
                int ln = ((AudioOutputElm)ce).mLabelNum;
                if (ln >= num) {
                    num = ln + 1;
                }
            }
            return num;
        }

        public override void Reset() {
            mDataPtr = 0;
            mDataFull = false;
            mDataSampleCount = 0;
            mNextDataSample = 0;
            mDataSample = 0;
        }

        public override void StepFinished() {
            mDataSample += Volts[0];
            mDataSampleCount++;
            if (CirSim.Sim.Time >= mNextDataSample) {
                mNextDataSample += mSampleStep;
                mData[mDataPtr++] = mDataSample / mDataSampleCount;
                mDataSampleCount = 0;
                mDataSample = 0;
                if (mDataPtr >= mDataCount) {
                    mDataPtr = 0;
                    mDataFull = true;
                }
            }
        }

        public override void SetPoints() {
            base.SetPoints();
            mLead1 = new Point();
        }

        public override void Delete() {
            ControlPanel.RemoveSlider(mButton);
            base.Delete();
        }

        public override void Draw(CustomGraphics g) {
            var selected = NeedsHighlight;
            var s = "Audio Out";
            if (mLabelNum > 1) {
                s = "Audio " + mLabelNum;
            }
            int textWidth = (int)g.GetTextSize(s).Width;
            g.LineColor = CustomGraphics.GrayColor;
            int pct = mDataFull ? textWidth : textWidth * mDataPtr / mDataCount;
            g.FillRectangle(P2.X - textWidth / 2, P2.Y - 10, pct, 20);
            g.LineColor = selected ? CustomGraphics.SelectColor : CustomGraphics.WhiteColor;
            setLead1(1 - (textWidth / 2.0) / mLen);
            setBbox(mPoint1, mLead1, 0);
            drawCenteredText(g, s, P2.X, P2.Y, true);
            if (selected) {
                g.ThickLineColor = CustomGraphics.SelectColor;
            } else {
                g.ThickLineColor = getVoltageColor(Volts[0]);
            }
            g.DrawThickLine(mPoint1, mLead1);
            drawPosts(g);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "audio output";
            arr[1] = "V = " + Utils.VoltageText(Volts[0]);
            int ct = (mDataFull ? mDataCount : mDataPtr);
            double dur = mSampleStep * ct;
            arr[2] = "start = " + Utils.UnitText(mDataFull ? CirSim.Sim.Time - mDuration : mDataStart, "s");
            arr[3] = "dur = " + Utils.UnitText(dur, "s");
            arr[4] = "samples = " + ct + (mDataFull ? "" : "/" + mDataCount);
        }

        void setDataCount() {
            mDataCount = (int)(mSamplingRate * mDuration);
            mData = new double[mDataCount];
            mDataStart = CirSim.Sim.Time;
            mDataPtr = 0;
            mDataFull = false;
            mSampleStep = 1.0 / mSamplingRate;
            mNextDataSample = CirSim.Sim.Time + mSampleStep;
        }

        void setTimeStep() {
            double target = mSampleStep / 8;
            if (ControlPanel.TimeStep != target) {
                if (mOkToChangeTimeStep || MessageBox.Show("Adjust timestep for best audio quality and performance?", "", MessageBoxButtons.OKCancel) == DialogResult.OK) {
                    ControlPanel.TimeStep = target;
                    mOkToChangeTimeStep = true;
                }
            }
        }

        void createButton() {
            string label = "Play Audio";
            if (mLabelNum > 1) {
                label += " " + mLabelNum;
            }
            ControlPanel.AddSlider(mButton = new Button() { Text = label });
            mButton.Click += new EventHandler((s, e) => {
                play();
            });
        }

        void play() {
            int ct = mDataPtr;
            int _base = 0;
            if (mDataFull) {
                ct = mDataCount;
                _base = mDataPtr;
            }
            if (ct * mSampleStep < .05) {
                MessageBox.Show("Audio data is not ready yet.\r\nIncrease simulation speed to make data ready sooner.");
                return;
            }

            /* rescale data to maximize */
            double max = -1e8;
            double min = 1e8;
            for (int i = 0; i != ct; i++) {
                if (mData[i] > max) max = mData[i];
                if (mData[i] < min) min = mData[i];
            }

            double adj = -(max + min) / 2;
            double mult = (.25 * 32766) / (max + adj);

            /* fade in over 1/20 sec */
            int fadeLen = mSamplingRate / 20;
            int fadeOut = ct - fadeLen;

            double fadeMult = mult / fadeLen;
            var samples = new short[ct];
            for (int i = 0; i != ct; i++) {
                double fade = (i < fadeLen) ? i * fadeMult : (i > fadeOut) ? (ct - i) * fadeMult : mult;
                samples[i] = (short)((mData[(i + _base) % mDataCount] + adj) * fade);
            }

            var wav = new Wav(mSamplingRate);
            wav.setBuffer(samples);
            var srclist = new List<short>();
            while (!wav.eof()) {
                srclist.AddRange(wav.getBuffer(1000));
            }
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo("録音時間(sec)", mDuration, 0, 5);
                return ei;
            }
            if (n == 1) {
                var ei = new ElementInfo("サンプリング周波数(Hz)", 0, -1, -1);
                ei.Choice = new ComboBox();
                for (int i = 0; i != SAMPLE_RATE_LIST.Length; i++) {
                    ei.Choice.Items.Add(SAMPLE_RATE_LIST[i] + "");
                    if (SAMPLE_RATE_LIST[i] == mSamplingRate) {
                        ei.Choice.SelectedIndex = i;
                    }
                }
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0 && ei.Value > 0) {
                mDuration = ei.Value;
                setDataCount();
            }
            if (n == 1) {
                int nsr = SAMPLE_RATE_LIST[ei.Choice.SelectedIndex];
                if (nsr != mSamplingRate) {
                    mSamplingRate = nsr;
                    mLastSamplingRate = nsr;
                    setDataCount();
                    setTimeStep();
                }
            }
        }
    }
}
