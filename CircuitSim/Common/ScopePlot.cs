using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.UI;

namespace Circuit.Common {
    public class ScopePlot {
        const int SCALE_INFO_WIDTH = 45;
        const int SPEED_MAX = 1024;
        const double SCALE_MIN = 1e-9;
        const double FFT_RANGE = 60.0;
        static readonly Color[] COLORS = {
            Color.FromArgb(0xCF, 0x00, 0x00), //RED,
            Color.FromArgb(0x00, 0xCF, 0x00), //GREEN,
            Color.FromArgb(0x1F, 0x1F, 0xEF), //BLUE,
            Color.FromArgb(0xBF, 0x00, 0xBF), //PURPLE,
            Color.FromArgb(0xFF, 0x00, 0x8F), //MAGENTA,
            Color.FromArgb(0x00, 0xBF, 0xBF), //CYAN,
            Color.FromArgb(0xBF, 0xBF, 0x00), //YELLOW,
            Color.FromArgb(0xA0, 0xA0, 0xA0)  //GRAY
        };

        public enum E_COLOR : int {
            RED,
            GREEN,
            BLUE,
            PURPLE,
            MAGENTA,
            CYAN,
            YELLOW,
            GRAY,
            INVALID
        }

        public List<ScopeWave> Waves = new List<ScopeWave>();

        int mFlags {
            set {
                ShowVoltage = (value & 2) != 0;
                ShowFreq = (value & 8) != 0;
                ManualScale = (value & 16) != 0;
                ShowScale = (value & 512) != 0;
                ShowFFT = (value & 1024) != 0;
                Normarize = (value & 8192) != 0;
                ShowRMS = (value & 16384) != 0;
                LogSpectrum = (value & 65536) != 0;
            }
            get {
                return (ShowVoltage ? 2 : 0)
                    | (ShowFreq ? 8 : 0)
                    | (ManualScale ? 16 : 0)
                    | (ShowScale ? 512 : 0)
                    | (ShowFFT ? 1024 : 0)
                    | (Normarize ? 8192 : 0)
                    | (ShowRMS ? 16384 : 0)
                    | (LogSpectrum ? 65536 : 0);
            }
        }

        #region [private variable]
        CustomGraphics mContext;
        FFT mFft = new FFT(16);
        double[] mReal = new double[16];
        double[] mImag = new double[16];
        double mFftMax = 0.0;
        double mFftMainMax;
        Rectangle mFFTBoundingBox;
        bool mShowNegative;
        bool mSomethingSelected;
        double mGridDivX;
        double mGridStepX;
        double mGridStepY;
        double mScopeTimeStep;
        double mMainGridMult;
        double mMainGridMid;
        double mMaxValue;
        double mMinValue;
        int mScopePointCount;
        #endregion

        #region [public property]
        public int MouseCursorX { get; set; } = -1;
        public int MouseCursorY { get; set; } = -1;
        public Rectangle BoundingBox { get; private set; } = new Rectangle(0, 0, 1, 1);
        public int Right { get { return BoundingBox.X + BoundingBox.Width; } }
        public int Index { get; set; } = 0;
        public int StackCount { get; set; } = 0;
        public int SelectedWave { get; private set; } = -1;
        public bool CanMenu { get { return Waves[0].UI != null; } }
        public bool NeedToRemove {
            get {
                bool ret = true;
                for (int i = 0; i != Waves.Count; i++) {
                    var plot = Waves[i];
                    if (CirSimForm.GetUIIndex(plot.UI) < 0) {
                        Waves.RemoveAt(i--);
                    } else {
                        ret = false;
                    }
                }
                return ret;
            }
        }

        public int Speed { get; private set; } = 64;
        public double Scale { get; private set; } = SCALE_MIN;
        public bool Normarize { get; private set; } = true;
        public bool ManualScale { get; set; }
        public bool ShowScale { get; set; }
        public bool ShowRMS { get; set; }
        public bool ShowFreq { get; set; }
        public bool ShowVoltage { get; private set; }
        public bool ShowFFT { get; set; }
        public bool LogSpectrum { get; set; }
        public string Text { get; set; }
        #endregion

        public ScopePlot() {
            allocImage();
            initialize();
        }

        #region [get/set method]
        public BaseUI GetUI() {
            if (0 <= SelectedWave && SelectedWave < Waves.Count) {
                return Waves[SelectedWave].UI;
            }
            return 0 < Waves.Count ? Waves[0].UI : null;
        }
        public void SetRect(Rectangle rect) {
            int w = BoundingBox.Width;
            rect.Width -= SCALE_INFO_WIDTH;
            BoundingBox = rect;
            if (BoundingBox.Width != w) {
                ResetGraph();
            }
            mFFTBoundingBox = new Rectangle(40, 0, rect.Width - 40, rect.Height - 16);
        }
        public void SetSpeed(int speed) {
            if (speed < 1) {
                speed = 1;
            }
            if (1024 < speed) {
                speed = 1024;
            }
            Speed = speed;
            ResetGraph();
        }
        public void SetScale(double scale) {
            if (Waves.Count == 0) {
                return;
            }
            Scale = Math.Max(SCALE_MIN, scale);
        }
        public void SetShowVoltage(bool show) {
            if (show && !ShowVoltage) {
                setPlot();
            }
            ShowVoltage = show;
        }
        #endregion

        #region [public method]
        public string Dump() {
            var vPlot = Waves[0];
            if (vPlot.UI == null) {
                return null;
            }
            var dumpList = new List<object>() {
                "o",
                vPlot.Speed,
                mFlags,
                Scale.ToString("g3"),
                Index,
                Waves.Count
            };
            foreach (var p in Waves) {
                dumpList.Add(CirSimForm.GetUIIndex(p.UI) + "_" + p.Color);
            }
            if (!string.IsNullOrWhiteSpace(Text)) {
                dumpList.Add(Utils.Escape(Text));
            }
            return string.Join(" ", dumpList.ToArray());
        }
        public void Undump(StringTokenizer st) {
            initialize();
            Waves = new List<ScopeWave>();

            Speed = st.nextTokenInt(1);
            ResetGraph();

            mFlags = st.nextTokenInt();
            Scale = st.nextTokenDouble();
            Index = st.nextTokenInt();

            try {
                var plotCount = st.nextTokenInt();
                for (int i = 0; i != plotCount; i++) {
                    string temp;
                    st.nextToken(out temp);
                    var subElmCol = temp.Split('_');
                    var subElmIdx = int.Parse(subElmCol[0]);
                    var subElm = CirSimForm.UIList[subElmIdx];
                    var color = (int)Enum.Parse(typeof(E_COLOR), subElmCol[1]);
                    var p = new ScopeWave(subElm);
                    p.Speed = Speed;
                    p.SetColor(color);
                    Waves.Add(p);
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw ex;
            }

            if (st.HasMoreTokens) {
                string temp;
                st.nextToken(out temp);
                Text = Utils.Unescape(temp);
            } else {
                Text = "";
            }
        }
        public void Setup(BaseUI ui) {
            setPlot(ui);
            initialize();
        }
        public double CalcGridTime() {
            var baseT = 10 * ControlPanel.TimeStep * Speed;
            mGridStepX = 1e-9;
            mGridDivX = 10;
            for (int i = 0; mGridStepX < baseT; i++) {
                var m = i % 2;
                var exp = Math.Pow(10, (i - m) / 2);
                switch (m) {
                case 0:
                    mGridStepX = 1e-9 * exp;
                    mGridDivX = 10;
                    break;
                case 1:
                    mGridStepX = 2e-9 * exp;
                    mGridDivX = 5;
                    break;
                }
            }
            return mGridStepX;
        }
        public void Combine(ScopePlot plot) {
            var wc = Waves.Count;
            foreach (var wave in plot.Waves) {
                wave.SetColor(wc++);
                Waves.Add(wave);
            }
            plot.Waves.Clear();
        }
        public void SpeedUp() {
            if (1 < Speed) {
                Speed >>= 1;
                ResetGraph();
            }
        }
        public void SlowDown() {
            if (Speed < SPEED_MAX) {
                Speed <<= 1;
            }
            ResetGraph();
        }
        public void MaxScale() {
            Normarize = !Normarize;
            mShowNegative = false;
        }
        public void ResetGraph(bool full = false) {
            mScopePointCount = 1;
            while (mScopePointCount <= BoundingBox.Width) {
                mScopePointCount <<= 1;
            }
            if (Waves == null) {
                Waves = new List<ScopeWave>();
            }
            mShowNegative = false;
            for (int i = 0; i != Waves.Count; i++) {
                var p = Waves[i];
                p.Reset(mScopePointCount, Speed, full);
                if (p.Color == E_COLOR.INVALID) {
                    p.SetColor(i);
                }
            }
            mScopeTimeStep = ControlPanel.TimeStep;
            allocImage();
        }
        public void TimeStep() {
            foreach (var wave in Waves) {
                wave.TimeStep();
            }
        }
        public void Draw(CustomGraphics g, bool isFloat = false) {
            if (Waves.Count == 0) {
                return;
            }

            /* reset if timestep changed */
            if (mScopeTimeStep != ControlPanel.TimeStep) {
                mScopeTimeStep = ControlPanel.TimeStep;
                ResetGraph();
            }

            if (Normarize) {
                Scale = SCALE_MIN;
            }

            mSomethingSelected = false;
            foreach (var p in Waves) {
                calcScale(p);
                if (p.UI != null && p.UI.IsMouseElm) {
                    mSomethingSelected = true;
                }
            }

            checkSelection();
            if (SelectedWave >= 0) {
                mSomethingSelected = true;
            }

            if (Waves.Count > 0) {
                calcMaxAndMin();
            }

            if (isFloat) {
                g.SetPlotPos(BoundingBox.Location);
                g.DrawRectangle(new Rectangle(0, 0, BoundingBox.Width, BoundingBox.Height));
            } else {
                g.SetPlotPos(BoundingBox.Location);
            }

            {
                if (ShowFFT) {
                    drawFFTGridLines(g);
                    for (int i = 0; i != Waves.Count; i++) {
                        drawFFT(g, i);
                    }
                }
                if (ShowVoltage) {
                    /* Vertical (T) gridlines */
                    CalcGridTime();
                    /* draw volts on top (last), then current underneath, then everything else */
                    for (int i = 0; i != Waves.Count; i++) {
                        drawWave(g, i);
                    }
                }
                if (Waves.Count > 0) {
                    drawInfoTexts(g);
                }
            }
            g.ClearTransform();

            drawCrosshairs(g);

            if (5 < Waves[0].Pointer && (!ManualScale || Normarize)) {
                if (SCALE_MIN < Scale) {
                    Scale /= 2;
                }
            }
        }
        #endregion

        #region [private method]
        void allocImage() {
            if (mContext == null) {
                mContext = CustomGraphics.FromImage(BoundingBox.Width, BoundingBox.Height);
            }
        }
        void initialize() {
            ResetGraph();
            Scale = 0.1;
            Speed = 64;
            ShowVoltage = true;
            ShowScale = ShowFreq = ManualScale = ShowFFT = false;
        }
        void setPlot(BaseUI ui) {
            if (null == ui) {
                Waves.Clear();
                return;
            }
            Waves = new List<ScopeWave>() { new ScopeWave(ui) };
            ShowVoltage = true;
            ResetGraph();
        }
        void setPlot() {
            if (Waves.Count == 0) {
                return;
            }
            var uiList = new List<BaseUI>();
            foreach (var wave in Waves) {
                if (null == wave.UI) {
                    continue;
                }
                uiList.Add(wave.UI);
            }
            Waves.Clear();
            foreach (var ui in uiList) {
                Waves.Add(new ScopeWave(ui));
            }
            ShowVoltage = true;
            ResetGraph();
        }
        void checkSelection() {
            if (!BoundingBox.Contains(MouseCursorX, MouseCursorY)) {
                SelectedWave = -1;
                return;
            }
            if (ShowFFT) {
                SelectedWave = 0;
                return;
            }
            var ipa = Waves[0].StartIndex(BoundingBox.Width);
            var pointer = (MouseCursorX - BoundingBox.X + ipa) & (mScopePointCount - 1);
            var maxy = (BoundingBox.Height - 1) / 2;
            int bestdist = int.MaxValue;
            int best = -1;
            for (int i = 0; i != Waves.Count; i++) {
                var wave = Waves[i];
                var maxvy = (int)Math.Min(BoundingBox.Y, maxy / Scale * wave.MaxValues[pointer]);
                var dist = Math.Abs(MouseCursorY - (BoundingBox.Y + maxy - maxvy));
                if (dist < bestdist) {
                    bestdist = dist;
                    best = i;
                }
            }
            SelectedWave = best;
        }
        void calcMaxAndMin() {
            mMaxValue = double.MinValue;
            mMinValue = double.MaxValue;
            for (int si = 0; si != Waves.Count; si++) {
                var wave = Waves[si];
                var ipa = wave.StartIndex(BoundingBox.Width);
                var maxV = wave.MaxValues;
                var minV = wave.MinValues;
                for (int i = 0; i != BoundingBox.Width; i++) {
                    int ip = (i + ipa) & (mScopePointCount - 1);
                    if (maxV[ip] > mMaxValue) {
                        mMaxValue = maxV[ip];
                    }
                    if (minV[ip] < mMinValue) {
                        mMinValue = minV[ip];
                    }
                }
            }
        }
        void calcScale(ScopeWave wave) {
            if (ManualScale && !Normarize) {
                return;
            }
            var ipa = wave.StartIndex(BoundingBox.Width);
            var maxV = wave.MaxValues;
            var minV = wave.MinValues;
            double max = 0;
            double gridMax = Scale;
            for (int i = 0; i != BoundingBox.Width; i++) {
                int ip = (i + ipa) & (mScopePointCount - 1);
                if (max < maxV[ip]) {
                    max = maxV[ip];
                }
                if (minV[ip] < -max) {
                    max = -minV[ip];
                }
            }
            /* scale fixed at maximum? */
            if (Normarize) {
                gridMax = Math.Max(max, gridMax);
            } else {
                /* adjust in powers of two */
                while (gridMax < max) {
                    gridMax *= 2;
                }
            }
            Scale = gridMax;
        }
        string calcRMS() {
            var wave = Waves[0];
            int i;
            double avg = 0;
            var ipa = wave.Pointer + mScopePointCount - BoundingBox.Width;
            var maxV = wave.MaxValues;
            var minV = wave.MinValues;
            var mid = (mMaxValue + mMinValue) / 2;
            int state = -1;

            /* skip zeroes */
            for (i = 0; i != BoundingBox.Width; i++) {
                int ip = (i + ipa) & (mScopePointCount - 1);
                if (maxV[ip] != 0) {
                    if (maxV[ip] > mid) {
                        state = 1;
                    }
                    break;
                }
            }
            int firstState = -state;
            int start = i;
            int end = 0;
            int waveCount = 0;
            double endAvg = 0;
            for (; i != BoundingBox.Width; i++) {
                int ip = (i + ipa) & (mScopePointCount - 1);
                bool sw = false;

                /* switching polarity? */
                if (state == 1) {
                    if (maxV[ip] < mid) {
                        sw = true;
                    }
                } else if (minV[ip] > mid) {
                    sw = true;
                }

                if (sw) {
                    state = -state;
                    /* completed a full cycle? */
                    if (firstState == state) {
                        if (waveCount == 0) {
                            start = i;
                            firstState = state;
                            avg = 0;
                        }
                        waveCount++;
                        end = i;
                        endAvg = avg;
                    }
                }
                if (waveCount > 0) {
                    var m = (maxV[ip] + minV[ip]) * .5;
                    avg += m * m;
                }
            }
            if (1 < waveCount) {
                var rms = Math.Sqrt(endAvg / (end - start));
                return Utils.VoltageText(rms) + "rms";
            } else {
                return "";
            }
        }
        string calcFrequency() {
            /* try to get frequency
             * get average */
            double avg = 0;
            int i;
            var wave = Waves[0];
            var ipa = wave.Pointer + mScopePointCount - BoundingBox.Width;
            var minV = wave.MinValues;
            var maxV = wave.MaxValues;
            for (i = 0; i != BoundingBox.Width; i++) {
                int ip = (i + ipa) & (mScopePointCount - 1);
                avg += minV[ip] + maxV[ip];
            }
            avg /= i * 2;
            int state = 0;
            double thresh = avg * .05;
            int oi = 0;
            double avperiod = 0;
            int periodct = -1;
            double avperiod2 = 0;
            /* count period lengths */
            for (i = 0; i != BoundingBox.Width; i++) {
                int ip = (i + ipa) & (mScopePointCount - 1);
                double q = maxV[ip] - avg;
                int os = state;
                if (q < thresh) {
                    state = 1;
                } else if (q > -thresh) {
                    state = 2;
                }
                if (state == 2 && os == 1) {
                    int pd = i - oi;
                    oi = i;
                    /* short periods can't be counted properly */
                    if (pd < 12) {
                        continue;
                    }
                    /* skip first period, it might be too short */
                    if (periodct >= 0) {
                        avperiod += pd;
                        avperiod2 += pd * pd;
                    }
                    periodct++;
                }
            }
            avperiod /= periodct;
            avperiod2 /= periodct;
            var periodstd = Math.Sqrt(avperiod2 - avperiod * avperiod);
            var freq = 1 / (avperiod * ControlPanel.TimeStep * Speed);
            /* don't show freq if standard deviation is too great */
            if (periodct < 1 || periodstd > 2) {
                freq = 0;
            }
            /* Console.WriteLine(freq + " " + periodstd + " " + periodct); */
            if (0 == freq) {
                return "";
            } else {
                return Utils.FrequencyText(freq);
            }
        }
        #endregion

        #region [draw method]
        void drawCrosshairs(CustomGraphics g) {
            if (!BoundingBox.Contains(MouseCursorX, MouseCursorY)) {
                return;
            }
            if (SelectedWave < 0 && !ShowFFT) {
                return;
            }

            var info = new string[4];
            int ct = 0;

            if (ShowVoltage) {
                int maxy = (BoundingBox.Height - 1) / 2;
                int ipa = Waves[0].StartIndex(BoundingBox.Width);
                int pointer = (MouseCursorX - BoundingBox.X + ipa) & (mScopePointCount - 1);
                if (SelectedWave >= 0) {
                    var wave = Waves[SelectedWave];
                    info[ct++] = Utils.VoltageText(wave.MaxValues[pointer]);
                    var maxvy = (int)(mMainGridMult * (wave.MaxValues[pointer] - mMainGridMid));
                    maxvy = Math.Max(-maxy, maxvy);
                    maxvy = Math.Min(maxy, maxvy);
                    g.FillColor = COLORS[(int)wave.Color];
                    g.FillCircle(MouseCursorX, BoundingBox.Y + maxy - maxvy, 3);
                }
                if (Waves.Count > 0) {
                    var t = Circuit.Time - ControlPanel.TimeStep * Speed * (BoundingBox.X + BoundingBox.Width - MouseCursorX);
                    info[ct++] = Utils.TimeText(t);
                }
            }

            if (ShowFFT) {
                double maxFrequency = 1 / (ControlPanel.TimeStep * Speed * 2);
                var posX = MouseCursorX - mFFTBoundingBox.X;
                if (posX < 0) {
                    posX = 0;
                }
                info[ct++] = Utils.UnitText(maxFrequency * posX / mFFTBoundingBox.Width, "Hz");
            }

            int szw = 0, szh = 15 * ct;
            for (int i = 0; i != ct; i++) {
                int w = (int)g.GetTextSize(info[i]).Width;
                if (w > szw) {
                    szw = w;
                }
            }

            g.DrawColor = CustomGraphics.WhiteColor;
            g.DrawLine(MouseCursorX, BoundingBox.Y, MouseCursorX, BoundingBox.Y + BoundingBox.Height);
            int bx = MouseCursorX;
            if (bx < szw / 2) {
                bx = szw / 2;
            }

            g.FillColor = ControlPanel.ChkPrintable.Checked ? Color.White : Color.Black;
            g.FillRectangle(bx - szw / 2, BoundingBox.Y - szh, szw, szh);
            for (int i = 0; i != ct; i++) {
                int w = (int)g.GetTextSize(info[i]).Width;
                g.DrawLeftText(info[i], bx - w / 2, BoundingBox.Y - 2 - (ct - 1 - i) * 15);
            }
        }
        void drawWave(CustomGraphics g, int waveIndex) {
            var wave = Waves[waveIndex];
            if (wave.UI == null) {
                return;
            }

            var centerY = (BoundingBox.Height - 1) / 2.0f;
            double graphMid;
            double graphMult;
            {
                /* if we don't have overlapping scopes of different units, we can move zero around.
                 * Put it at the bottom if the scope is never negative. */
                var mx = Scale;
                var mn = 0.0;
                if (Normarize) {
                    /* scale is maxed out, so fix boundaries of scope at maximum and minimum. */
                    mx = mMaxValue;
                    mn = mMinValue;
                }
                var gridMax = (mx - mn) * 0.55;  /* leave space at top and bottom */
                if (gridMax * gridMax < SCALE_MIN * SCALE_MIN) {
                    gridMax = SCALE_MIN;
                } else if (mShowNegative || mMinValue < (mx + mn) * .5 - (mx - mn) * .55) {
                    mn = -Scale;
                    mShowNegative = true;
                }
                graphMid = (mx + mn) * 0.5;
                graphMult = centerY / gridMax;
                if (waveIndex == 0) {
                    mMainGridMult = graphMult;
                    mMainGridMid = graphMid;
                }

                mGridStepY = 1e-12;
                for (int i = 0; mGridStepY < 10 * gridMax / centerY; i++) {
                    var m = i % 3;
                    var exp = Math.Pow(10, (i - m) / 3);
                    switch (m) {
                    case 0:
                        mGridStepY = 1e-12 * exp;
                        break;
                    case 1:
                        mGridStepY = 2e-12 * exp;
                        break;
                    case 2:
                        mGridStepY = 5e-12 * exp;
                        break;
                    }
                }
            }

            if (waveIndex == 0) {
                Color minorDiv;
                Color majorDiv;
                if (ControlPanel.ChkPrintable.Checked) {
                    minorDiv = Color.FromArgb(0xCF, 0xCF, 0xCF);
                    majorDiv = Color.FromArgb(0x7F, 0x7F, 0x7F);
                } else {
                    minorDiv = Color.FromArgb(0x30, 0x30, 0x30);
                    majorDiv = Color.FromArgb(0x7F, 0x7F, 0x7F);
                }

                /* horizontal gridlines */
                g.DrawColor = minorDiv;
                var gridStepY = mGridStepY * graphMult;
                var gridDivY = (int)(centerY / gridStepY);
                var showGridlines = mGridStepY != 0;
                for (int ll = -gridDivY; ll <= gridDivY && showGridlines; ll++) {
                    var ly = (float)(centerY - ll * gridStepY);
                    g.DrawLine(0, ly, BoundingBox.Width - 1, ly);
                }

                /* vertical gridlines */
                var baseT = ControlPanel.TimeStep * Speed;
                var beginT = Circuit.Time - BoundingBox.Width * baseT;
                var endT = Circuit.Time - (Circuit.Time % mGridStepX);
                g.DrawColor = minorDiv;
                for (int ll = 0; ; ll++) {
                    var t = endT - mGridStepX * ll;
                    var lx = (float)((t - beginT) / baseT);
                    if (lx < 0) {
                        break;
                    }
                    if (t < 0 || BoundingBox.Width <= lx) {
                        continue;
                    }
                    if (((t + mGridStepX / 4) % (mGridStepX * mGridDivX)) < mGridStepX) {
                    } else {
                        g.DrawLine(lx, 0, lx, BoundingBox.Height - 1);
                    }
                }
                g.DrawColor = majorDiv;
                for (int ll = 0; ; ll++) {
                    var t = endT - mGridStepX * ll;
                    var lx = (float)((t - beginT) / baseT);
                    if (lx < 0) {
                        break;
                    }
                    if (t < 0 || BoundingBox.Width <= lx) {
                        continue;
                    }
                    if (((t + mGridStepX / 4) % (mGridStepX * mGridDivX)) < mGridStepX) {
                        g.DrawLine(lx, 0, lx, BoundingBox.Height - 1);
                    }
                }

                if (Normarize) {
                    g.DrawColor = majorDiv;
                    g.DrawLine(0, centerY, BoundingBox.Width - 1, centerY);
                } else {
                    var ly = (float)(centerY + graphMid * graphMult);
                    if (0 <= ly && ly < BoundingBox.Height) {
                        g.DrawColor = majorDiv;
                        g.DrawLine(0, ly, BoundingBox.Width - 1, ly);
                    }
                }
            }

            var idxBegin = wave.StartIndex(BoundingBox.Width);
            var vMax = wave.MaxValues;
            var vMin = wave.MinValues;
            var yMax = BoundingBox.Height - 1;
            var yMin = 1;
            var rect = new PointF[BoundingBox.Width * 2 + 1];
            for (int x = 0; x != BoundingBox.Width; x++) {
                var idx = (x + idxBegin) & (mScopePointCount - 1);
                var v = (float)(graphMult * (vMax[idx] - graphMid));
                var y = centerY - v - 0.5f;
                y = Math.Max(yMin, y);
                y = Math.Min(yMax, y);
                rect[x].X = x;
                rect[x].Y = y;
            }
            for (int x = BoundingBox.Width - 1, i = BoundingBox.Width; 0 <= x; x--, i++) {
                var idx = (x + idxBegin) & (mScopePointCount - 1);
                var v = (float)(graphMult * (vMin[idx] - graphMid));
                var y = centerY - v + 0.5f;
                y = Math.Max(yMin, y);
                y = Math.Min(yMax, y);
                rect[i].X = x;
                rect[i].Y = y;
            }
            rect[BoundingBox.Width * 2] = rect[0];
            if (ControlPanel.ChkPrintable.Checked) {
                g.FillColor = COLORS[(int)wave.Color];
            } else {
                if (waveIndex == SelectedWave || wave.UI.IsMouseElm) {
                    g.FillColor = CustomGraphics.SelectColor;
                } else {
                    g.FillColor = mSomethingSelected ? COLORS[(int)E_COLOR.GRAY] : COLORS[(int)wave.Color];
                }
            }
            g.FillPolygon(rect);
        }
        void drawFFTGridLines(CustomGraphics g) {
            const int xDivs = 20;
            const int yDivs = 10;
            int prevEnd = 0;
            double maxFrequency = 1 / (ControlPanel.TimeStep * Speed * xDivs * 2);
            var gridBottom = mFFTBoundingBox.Height - 1;
            g.DrawColor = CustomGraphics.LineColor;
            g.DrawLine(0, 0, BoundingBox.Width, 0);
            g.DrawLine(0, gridBottom, BoundingBox.Width, gridBottom);
            g.DrawLine(mFFTBoundingBox.X, 0, mFFTBoundingBox.X, BoundingBox.Height);
            for (int i = 0; i < xDivs; i++) {
                int x = mFFTBoundingBox.X + mFFTBoundingBox.Width * i / xDivs;
                if (x < prevEnd) {
                    continue;
                }
                string s = Utils.UnitText((int)Math.Round(i * maxFrequency), "Hz");
                int sWidth = (int)Math.Ceiling(g.GetTextSize(s).Width);
                prevEnd = x + sWidth + 4;
                if (i > 0) {
                    g.DrawLine(x, 0, x, BoundingBox.Height);
                }
                g.DrawLeftText(s, x, BoundingBox.Height - 10);
            }
            if (Waves.Count == 1) {
                mFftMax = 0;
            } else {
                mFftMax = 20;
            }
            for (int i = 1; i < yDivs; i++) {
                int y = mFFTBoundingBox.Height * i / yDivs;
                string s;
                if (LogSpectrum) {
                    s = (mFftMax - FFT_RANGE * i / yDivs).ToString() + "db";
                } else {
                    s = (1.0 * (yDivs - i) / yDivs).ToString();
                }
                if (i > 0) {
                    g.DrawLine(0, y, BoundingBox.Width, y);
                }
                g.DrawLeftText(s, 0, y + 8);
            }
        }
        void drawFFT(CustomGraphics g, int waveIndex) {
            if (mFft.Size != mScopePointCount) {
                mFft = new FFT(mScopePointCount);
                mReal = new double[mScopePointCount];
                mImag = new double[mScopePointCount];
            }
            var wave = Waves[waveIndex];
            var maxV = wave.MaxValues;
            var minV = wave.MinValues;
            int ptr = wave.Pointer;
            for (int i = 0; i < mScopePointCount; i++) {
                var ii = (ptr - i + mScopePointCount) % mScopePointCount;
                mReal[i] = 0.5 * (maxV[ii] + minV[ii]) * (0.5 - 0.5 * Math.Cos(2.0 * Math.PI * i / mScopePointCount));
                mImag[i] = 0;
            }
            mFft.Exec(mReal, mImag);
            if (0 == waveIndex) {
                mFftMainMax = SCALE_MIN;
                for (int i = 0; i < mScopePointCount / 2; i++) {
                    var m = mFft.Magnitude(mReal[i], mImag[i]);
                    if (m > mFftMainMax) {
                        mFftMainMax = m;
                    }
                }
            }
            var bottom = mFFTBoundingBox.Height - 1;
            var scaleX = 2.0f * mFFTBoundingBox.Width / mScopePointCount;
            var x0 = 1.0f * mFFTBoundingBox.X;
            var x1 = x0;
            var y0 = 0.0f;
            g.DrawColor = COLORS[(int)wave.Color];
            if (LogSpectrum) {
                var ymult = bottom / FFT_RANGE;
                for (int i = 0; i < mScopePointCount / 2; i++) {
                    var mag = mFft.Magnitude(mReal[i], mImag[i]);
                    if (mag < SCALE_MIN) {
                        mag = SCALE_MIN;
                    }
                    var db = 20 * Math.Log10(mag / mFftMainMax);
                    if (db < mFftMax - FFT_RANGE) {
                        db = mFftMax - FFT_RANGE;
                    }
                    var y1 = (float)((mFftMax - db) * ymult);
                    x1 += scaleX;
                    if (0 == i) {
                        g.DrawLine(x0, y1, x1, y1);
                    } else {
                        g.DrawLine(x0, y0, x1, y1);
                    }
                    y0 = y1;
                    x0 = x1;
                }
            } else {
                for (int i = 0; i < mScopePointCount / 2; i++) {
                    var mag = mFft.Magnitude(mReal[i], mImag[i]);
                    var y1 = bottom - (float)(mag * bottom / mFftMainMax);
                    x1 += scaleX;
                    if (0 == i) {
                        g.DrawLine(x0, y1, x1, y1);
                    } else {
                        g.DrawLine(x0, y0, x1, y1);
                    }
                    y0 = y1;
                    x0 = x1;
                }
            }
        }
        void drawInfoTexts(CustomGraphics g) {
            string t = Text;
            var textY = 8;
            if (!string.IsNullOrEmpty(t)) {
                g.DrawLeftText(t, 0, textY);
                textY += 12;
            }

            if (ShowVoltage) {
                if (ShowScale) {
                    string vScaleText = "";
                    if (mGridStepY != 0) {
                        vScaleText = ", V=" + Utils.VoltageAbsText(mGridStepY) + "/div";
                    }
                    g.DrawLeftText("H=" + Utils.TimeText(mGridStepX) + "/div" + vScaleText, 0, textY);
                    textY += 12;
                }
                g.DrawLeftText(Utils.VoltageText(mMaxValue), BoundingBox.Width, 6);
                int ym = BoundingBox.Height - 6;
                g.DrawLeftText(Utils.VoltageText(mMinValue), BoundingBox.Width, ym);
                if (Normarize) {
                    var centerY = (BoundingBox.Height - 1) / 2.0f;
                    g.DrawLeftText(Utils.VoltageText(mMainGridMid), BoundingBox.Width, centerY);
                }
            }

            if (ShowRMS) {
                g.DrawLeftText(calcRMS(), 0, textY);
                textY += 12;
            }
            if (ShowFreq) {
                g.DrawLeftText(calcFrequency(), 0, textY);
            }
        }
        #endregion
    }
}
