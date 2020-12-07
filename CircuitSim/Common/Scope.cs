using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

using Circuit.Elements;

namespace Circuit {
    enum SCOPE_MENU {
        maxscale,
        showvoltage,
        showcurrent,
        showscale,
        showpeak,
        shownegpeak,
        showfreq,
        showfft,
        logspectrum,
        showrms,
        showduty,
        showpower,
        showib,
        showic,
        showie,
        showvbe,
        showvbc,
        showvce,
        showvcevsic,
        showvvsi,
        manualscale,
        plotxy,
        showresistance
    }

    class ScopeCheckBox : CheckBox {
        public SCOPE_MENU Menu;
        public ScopeCheckBox(string text, SCOPE_MENU menu) : base() {
            AutoSize = true;
            Text = text;
            Menu = menu;
        }
    }

    class Scope {
        #region CONST
        const int FLAG_YELM = 32;

        /* bunch of other flags go here, see dump() */
        const int FLAG_IVALUE = 2048; /* Flag to indicate if IVALUE is included in dump */
        const int FLAG_PLOTS = 4096;  /* new-style dump with multiple plots */
                                      /* other flags go here too, see dump() */

        readonly double[] MULTA = new double[] { 2.0, 2.5, 2.0 };

        public const int VAL_POWER = 7;
        public const int VAL_POWER_OLD = 1;
        public const int VAL_CURRENT = 3;
        public const int VAL_IB = 1;
        public const int VAL_IC = 2;
        public const int VAL_IE = 3;
        public const int VAL_VBE = 4;
        public const int VAL_VBC = 5;
        public const int VAL_VCE = 6;
        public const int VAL_R = 2;

        public const int UNITS_V = 0;
        public const int UNITS_A = 1;
        public const int UNITS_W = 2;
        public const int UNITS_OHMS = 3;
        public const int UNITS_COUNT = 4;
        #endregion

        #region dynamic variable
        CirSim mSim;
        FFT mFft;

        CustomGraphics mContext;

        List<ScopePlot> mPlots;
        List<ScopePlot> mVisiblePlots;

        double mScopeTimeStep;

        double[] mScale;
        bool[] mReduceRange;
        int mScopePointCount = 128;
        int mAlphaDiv = 0;

        double mScaleX;  /* for X-Y plots */
        double mScaleY;
        int mWheelDeltaY;

        int mDrawOx;
        int mDrawOy;

        double mGridStepX;
        double mGridStepY;
        double mMaxValue;
        double mMinValue;
        double mMainGridMult;
        double mMainGridMid;

        int mTextY;

        bool mDrawGridLines;
        bool mSomethingSelected;
        bool mMaxScale;
        bool mShowNegative;
        bool mShowDutyCycle;
        #endregion

        #region property
        public int Position { get; set; }
        public int Speed { get; set; }
        public int StackCount { get; set; } /* number of scopes in this column */
        public string Text { get; set; }

        public Rectangle BoundingBox { get; private set; }
        public int SelectedPlot { get; private set; }

        public bool ShowMax { get; set; }
        public bool ShowMin { get; set; }
        public bool ShowI { get; private set; }
        public bool ShowV { get; private set; }
        public bool ShowScale { get; private set; }
        public bool ShowFreq { get; private set; }
        public bool LockScale { get; private set; }
        public bool Plot2d { get; private set; }
        public bool PlotXY { get; private set; }
        public bool ShowFFT { get; private set; }
        public bool LogSpectrum { get; private set; }
        public bool ShowRMS { get; private set; }
        #endregion

        public Scope(CirSim s) {
            mSim = s;
            mScale = new double[UNITS_COUNT];
            mReduceRange = new bool[UNITS_COUNT];

            BoundingBox = new Rectangle(0, 0, 1, 1);

            allocImage();
            initialize();
        }

        void showCurrent(bool b) {
            ShowI = b;
            if (b && !showingVoltageAndMaybeCurrent()) {
                setValue(0);
            }
            calcVisiblePlots();
        }

        void showVoltage(bool b) {
            ShowV = b;
            if (b && !showingVoltageAndMaybeCurrent()) {
                setValue(0);
            }
            calcVisiblePlots();
        }

        void showFFT(bool b) {
            ShowFFT = b;
            if (!ShowFFT) {
                mFft = null;
            }
        }

        void setManualScale(bool b) { LockScale = b; }

        public void resetGraph() { resetGraph(false); }

        public void resetGraph(bool full) {
            mScopePointCount = 1;
            while (mScopePointCount <= BoundingBox.Width) {
                mScopePointCount *= 2;
            }
            if (mPlots == null) {
                mPlots = new List<ScopePlot>();
            }
            mShowNegative = false;
            int i;
            for (i = 0; i != mPlots.Count; i++) {
                mPlots[i].Reset(mScopePointCount, Speed, full);
            }
            calcVisiblePlots();
            mScopeTimeStep = mSim.timeStep;
            allocImage();
        }

        public void setManualScaleValue(double d) {
            if (mVisiblePlots.Count == 0) {
                return;
            }
            var p = mVisiblePlots[0];
            mScale[p.Units] = d;
        }

        public double getScaleValue() {
            if (mVisiblePlots.Count == 0) {
                return 0;
            }
            var p = mVisiblePlots[0];
            return mScale[p.Units];
        }

        public string getScaleUnitsText() {
            if (mVisiblePlots.Count == 0) {
                return "V";
            }
            var p = mVisiblePlots[0];
            switch (p.Units) {
            case UNITS_A:
                return "A";
            case UNITS_OHMS:
                return CirSim.ohmString;
            case UNITS_W:
                return "W";
            default:
                return "V";
            }
        }

        bool active() { return mPlots.Count > 0 && mPlots[0].Elm != null; }

        void initialize() {
            resetGraph();
            mScale[UNITS_W] = mScale[UNITS_OHMS] = mScale[UNITS_V] = 5;
            mScale[UNITS_A] = .1;
            mScaleX = 5;
            mScaleY = .1;
            Speed = 64;
            ShowMax = true;
            ShowV = ShowI = false;
            ShowScale = ShowFreq = LockScale = ShowMin = false;
            ShowFFT = false;
            Plot2d = false;
            if (!loadDefaults()) {
                /* set showV and showI appropriately depending on what plots are present */
                int i;
                for (i = 0; i != mPlots.Count; i++) {
                    var plot = mPlots[i];
                    if (plot.Units == UNITS_V) {
                        ShowV = true;
                    }
                    if (plot.Units == UNITS_A) {
                        ShowI = true;
                    }
                }
            }
        }

        void calcVisiblePlots() {
            mVisiblePlots = new List<ScopePlot>();
            int vc = 0, ac = 0, oc = 0;
            for (int i = 0; i != mPlots.Count; i++) {
                var plot = mPlots[i];
                if (plot.Units == UNITS_V) {
                    if (ShowV) {
                        mVisiblePlots.Add(plot);
                        plot.AssignColor(vc++);
                    }
                } else if (plot.Units == UNITS_A) {
                    if (ShowI) {
                        mVisiblePlots.Add(plot);
                        plot.AssignColor(ac++);
                    }
                } else {
                    mVisiblePlots.Add(plot);
                    plot.AssignColor(oc++);
                }
            }
        }

        public void setRect(Rectangle r) {
            int w = BoundingBox.Width;
            BoundingBox = r;
            if (BoundingBox.Width != w) {
                resetGraph();
            }
        }

        int getWidth() { return BoundingBox.Width; }

        public int rightEdge() { return BoundingBox.X + BoundingBox.Width; }

        public void setElm(CircuitElm ce) {
            mPlots = new List<ScopePlot>();
            if (null != ce && (ce is TransistorElm)) {
                setValue(VAL_VCE, ce);
            } else {
                setValue(0, ce);
            }
            initialize();
        }

        void setValue(int val) {
            if (mPlots.Count > 2 || mPlots.Count == 0) {
                return;
            }
            var ce = mPlots[0].Elm;
            if (mPlots.Count == 2 && !mPlots[1].Elm.Equals(ce)) {
                return;
            }
            Plot2d = PlotXY = false;
            setValue(val, ce);
        }

        void setValue(int val, CircuitElm ce) {
            mPlots = new List<ScopePlot>();
            if (val == 0) {
                mPlots.Add(new ScopePlot(ce, UNITS_V, 0));
                /* create plot for current if applicable */
                if (ce != null && !((ce is OutputElm) || (ce is LogicOutputElm) || (ce is _AudioOutputElm) || (ce is ProbeElm))) {
                    mPlots.Add(new ScopePlot(ce, UNITS_A, VAL_CURRENT));
                }
            } else {
                int u = ce.GetScopeUnits(val);
                mPlots.Add(new ScopePlot(ce, u, val));
                if (u == UNITS_V) {
                    ShowV = true;
                }
                if (u == UNITS_A) {
                    ShowI = true;
                }
            }
            calcVisiblePlots();
            resetGraph();
        }

        void setValues(int val, int ival, CircuitElm ce, CircuitElm yelm) {
            if (ival > 0) {
                mPlots = new List<ScopePlot>();
                mPlots.Add(new ScopePlot(ce, ce.GetScopeUnits(val), val));
                mPlots.Add(new ScopePlot(ce, ce.GetScopeUnits(ival), ival));
                return;
            }
            if (yelm != null) {
                mPlots = new List<ScopePlot>();
                mPlots.Add(new ScopePlot(ce, ce.GetScopeUnits(val), 0));
                mPlots.Add(new ScopePlot(yelm, ce.GetScopeUnits(ival), 0));
                return;
            }
            setValue(val);
        }

        public bool showingValue(int v) {
            for (int i = 0; i != mPlots.Count; i++) {
                var sp = mPlots[i];
                if (sp.Value != v) {
                    return false;
                }
            }
            return true;
        }

        /* returns true if we have a plot of voltage and nothing else (except current).
        /* The default case is a plot of voltage and current, so we're basically checking if that case is true. */
        bool showingVoltageAndMaybeCurrent() {
            bool gotv = false;
            for (int i = 0; i != mPlots.Count; i++) {
                var sp = mPlots[i];
                if (sp.Value == 0) {
                    gotv = true;
                } else if (sp.Value != VAL_CURRENT) {
                    return false;
                }
            }
            return gotv;
        }

        public void combine(Scope s) {
            mPlots = mVisiblePlots;
            mPlots.AddRange(s.mVisiblePlots);
            s.mPlots.Clear();
            calcVisiblePlots();
        }

        /* separate this scope's plots into separate scopes and return them in arr[pos], arr[pos+1], etc.
         * return new length of array. */
        public int separate(List<Scope> arr, int pos) {
            ScopePlot lastPlot = null;
            for (int i = 0; i != mVisiblePlots.Count; i++) {
                if (pos >= arr.Count) {
                    return pos;
                }
                var s = new Scope(mSim);
                var sp = mVisiblePlots[i];
                if (lastPlot != null && lastPlot.Elm == sp.Elm && lastPlot.Value == 0 && sp.Value == VAL_CURRENT) {
                    continue;
                }
                s.setValue(sp.Value, sp.Elm);
                s.Position = pos;
                arr[pos++] = s;
                lastPlot = sp;
                s.setFlags(getFlags());
                s.setSpeed(Speed);
            }
            return pos;
        }

        public void removePlot(int plot) {
            if (plot < mVisiblePlots.Count) {
                var p = mVisiblePlots[plot];
                mPlots.Remove(p);
                calcVisiblePlots();
            }
        }

        /* called for each timestep */
        public void timeStep() {
            int i;
            for (i = 0; i != mPlots.Count; i++) {
                mPlots[i].TimeStep();
            }

            if (Plot2d && mContext != null) {
                bool newscale = false;
                if (mPlots.Count < 2) {
                    return;
                }
                double v = mPlots[0].LastValue;
                while (v > mScaleX || v < -mScaleX) {
                    mScaleX *= 2;
                    newscale = true;
                }
                double yval = mPlots[1].LastValue;
                while (yval > mScaleY || yval < -mScaleY) {
                    mScaleY *= 2;
                    newscale = true;
                }
                if (newscale) {
                    clear2dView();
                }
                double xa = v / mScaleX;
                double ya = yval / mScaleY;
                int x = (int)(BoundingBox.Width * (1 + xa) * .499);
                int y = (int)(BoundingBox.Height * (1 - ya) * .499);
                drawTo(x, y);
            }
        }

        /*
        void adjustScale(double x) {
        scale[UNITS_V] *= x;
        scale[UNITS_A] *= x;
        scale[UNITS_OHMS] *= x;
        scale[UNITS_W] *= x;
        scaleX *= x;
        scaleY *= x;
        }
        */
        public void maxScale() {
            if (Plot2d) {
                double x = 1e-8;
                mScale[UNITS_V] *= x;
                mScale[UNITS_A] *= x;
                mScale[UNITS_OHMS] *= x;
                mScale[UNITS_W] *= x;
                mScaleX *= x;
                mScaleY *= x;
                return;
            }
            /* toggle max scale.  This isn't on by default because, for the examples, we sometimes want two plots
             * matched to the same scale so we can show one is larger.  Also, for some fast-moving scopes
             * (like for AM detector), the amplitude varies over time but you can't see that if the scale is
             * constantly adjusting.  It's also nice to set the default scale to hide noise and to avoid
             * having the scale moving around a lot when a circuit starts up. */
            mMaxScale = !mMaxScale;
            mShowNegative = false;
        }

        bool showSettingsWheel() {
            return BoundingBox.Height > 100 && BoundingBox.Width > 100;
        }

        public bool cursorInSettingsWheel() {
            return showSettingsWheel() &&
                mSim.mouseCursorX >= BoundingBox.X &&
                mSim.mouseCursorX <= BoundingBox.X + 36 &&
                mSim.mouseCursorY >= BoundingBox.Y + BoundingBox.Height - 36 &&
                mSim.mouseCursorY <= BoundingBox.Y + BoundingBox.Height;
        }

        public void draw(CustomGraphics g) {
            if (mPlots.Count == 0) {
                return;
            }

            /* reset if timestep changed */
            if (mScopeTimeStep != mSim.timeStep) {
                mScopeTimeStep = mSim.timeStep;
                resetGraph();
            }

            g.SetTransform(new Matrix(1, 0, 0, 1, BoundingBox.X, BoundingBox.Y));

            if (Plot2d) {
                draw2d(g);
                return;
            }

            g.LineColor = Color.Red;

            drawSettingsWheel(g);

            if (ShowFFT) {
                drawFFTVerticalGridLines(g);
                drawFFT(g);
            }

            for (int i = 0; i != UNITS_COUNT; i++) {
                mReduceRange[i] = false;
                if (mMaxScale) {
                    mScale[i] = 1e-4;
                }
            }

            mSomethingSelected = false;  /* is one of our plots selected? */

            for (int si = 0; si != mVisiblePlots.Count; si++) {
                var plot = mVisiblePlots[si];
                calcPlotScale(plot);
                if (mSim.scopeSelected == -1 && plot.Elm != null && plot.Elm.IsMouseElm) {
                    mSomethingSelected = true;
                }
                mReduceRange[plot.Units] = true;
            }

            checkForSelection();
            if (SelectedPlot >= 0) {
                mSomethingSelected = true;
            }

            mDrawGridLines = true;
            bool hGridLines = true;
            for (int i = 1; i < mVisiblePlots.Count; i++) {
                if (mVisiblePlots[i].Units != mVisiblePlots[0].Units) {
                    hGridLines = false;
                }
            }

            if ((hGridLines || ShowMax || ShowMin) && mVisiblePlots.Count > 0) {
                calcMaxAndMin(mVisiblePlots[0].Units);
            }

            /* draw volts on top (last), then current underneath, then everything else */
            for (int i = 0; i != mVisiblePlots.Count; i++) {
                if (mVisiblePlots[i].Units > UNITS_A && i != SelectedPlot) {
                    drawPlot(g, mVisiblePlots[i], hGridLines, false);
                }
            }
            for (int i = 0; i != mVisiblePlots.Count; i++) {
                if (mVisiblePlots[i].Units == UNITS_A && i != SelectedPlot) {
                    drawPlot(g, mVisiblePlots[i], hGridLines, false);
                }
            }
            for (int i = 0; i != mVisiblePlots.Count; i++) {
                if (mVisiblePlots[i].Units == UNITS_V && i != SelectedPlot) {
                    drawPlot(g, mVisiblePlots[i], hGridLines, false);
                }
            }
            /* draw selection on top.  only works if selection chosen from scope */
            if (SelectedPlot >= 0 && SelectedPlot < mVisiblePlots.Count) {
                drawPlot(g, mVisiblePlots[SelectedPlot], hGridLines, true);
            } 

            if (mVisiblePlots.Count > 0) {
                drawInfoTexts(g);
            }

            drawCrosshairs(g);

            g.ClearTransform();

            if (mPlots[0].Pointer > 5 && !LockScale) {
                for (int i = 0; i != UNITS_COUNT; i++) {
                    if (mScale[i] > 1e-4 && mReduceRange[i]) {
                        mScale[i] /= 2;
                    }
                }
            }
        }

        /* calculate maximum and minimum values for all plots of given units */
        void calcMaxAndMin(int units) {
            mMaxValue = -1e8;
            mMinValue = 1e8;
            int i;
            for (int si = 0; si != mVisiblePlots.Count; si++) {
                var plot = mVisiblePlots[si];
                if (plot.Units != units) {
                    continue;
                }
                int ipa = plot.StartIndex(BoundingBox.Width);
                var maxV = plot.MaxValues;
                var minV = plot.MinValues;
                for (i = 0; i != BoundingBox.Width; i++) {
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

        /* adjust scale of a plot */
        void calcPlotScale(ScopePlot plot) {
            if (LockScale) {
                return;
            }
            int ipa = plot.StartIndex(BoundingBox.Width);
            var maxV = plot.MaxValues;
            var minV = plot.MinValues;
            double max = 0;
            double gridMax = mScale[plot.Units];
            for (int i = 0; i != BoundingBox.Width; i++) {
                int ip = (i + ipa) & (mScopePointCount - 1);
                if (maxV[ip] > max) {
                    max = maxV[ip];
                }
                if (minV[ip] < -max) {
                    max = -minV[ip];
                }
            }
            /* scale fixed at maximum? */
            if (mMaxScale) {
                gridMax = Math.Max(max, gridMax);
            } else {
                /* adjust in powers of two */
                while (max > gridMax) {
                    gridMax *= 2;
                }
            }
            mScale[plot.Units] = gridMax;
        }

        public double calcGridStepX() {
            int multptr = 0;
            double gsx = 1e-15;

            double ts = mSim.timeStep * Speed;
            while (gsx < ts * 20) {
                gsx *= MULTA[(multptr++) % 3];
            }
            return gsx;
        }

        /* find selected plot */
        void checkForSelection() {
            if (mSim.dialogIsShowing()) {
                return;
            }
            if (!BoundingBox.Contains(mSim.mouseCursorX, mSim.mouseCursorY)) {
                SelectedPlot = -1;
                return;
            }
            int ipa = mPlots[0].StartIndex(BoundingBox.Width);
            int ip = (mSim.mouseCursorX - BoundingBox.X + ipa) & (mScopePointCount - 1);
            int maxy = (BoundingBox.Height - 1) / 2;
            int y = maxy;
            int i;
            int bestdist = 10000;
            int best = -1;
            for (i = 0; i != mVisiblePlots.Count; i++) {
                var plot = mVisiblePlots[i];
                int maxvy = (int)((maxy / mScale[plot.Units]) * plot.MaxValues[ip]);
                int dist = Math.Abs(mSim.mouseCursorY - (BoundingBox.Y + y - maxvy));
                if (dist < bestdist) {
                    bestdist = dist;
                    best = i;
                }
            }
            SelectedPlot = best;
        }

        public bool canShowRMS() {
            if (mVisiblePlots.Count == 0) {
                return false;
            }
            var plot = mVisiblePlots[0];
            return (plot.Units == UNITS_V || plot.Units == UNITS_A);
        }

        string getScopeText() {
            /* stacked scopes?  don't show text */
            if (StackCount != 1) {
                return null;
            }

            /* multiple elms?  don't show text (unless one is selected) */
            if (SelectedPlot < 0 && getSingleElm() == null) {
                return null;
            }

            var plot = mVisiblePlots[0];
            if (SelectedPlot >= 0 && mVisiblePlots.Count > SelectedPlot) {
                plot = mVisiblePlots[SelectedPlot];
            }
            if (plot.Elm == null) {
                return "";
            } else {
                return plot.Elm.GetScopeText(plot.Value);
            }
        }

        public void setSpeed(int sp) {
            if (sp < 1) {
                sp = 1;
            }
            if (sp > 1024) {
                sp = 1024;
            }
            Speed = sp;
            resetGraph();
        }

        public void properties(int x, int y) {
            var fm = new ScopePropertiesDialog(mSim, this);
            fm.Show(x, y);
            CirSim.dialogShowing = fm;
        }

        public void speedUp() {
            if (Speed > 1) {
                Speed /= 2;
                resetGraph();
            }
        }

        public void slowDown() {
            if (Speed < 1024) {
                Speed *= 2;
            }
            resetGraph();
        }

        /* get scope element, returning null if there's more than one */
        public CircuitElm getSingleElm() {
            var elm = mPlots[0].Elm;
            for (int i = 1; i < mPlots.Count; i++) {
                if (!mPlots[i].Elm.Equals(elm)) {
                    return null;
                }
            }
            return elm;
        }

        public bool canMenu() {
            return mPlots[0].Elm != null;
        }

        public bool canShowResistance() {
            var elm = getSingleElm();
            return elm != null && elm.CanShowValueInScope(VAL_R);
        }

        public bool isShowingVceAndIc() {
            return Plot2d && mPlots.Count == 2 && mPlots[0].Value == VAL_VCE && mPlots[1].Value == VAL_IC;
        }

        int getFlags() {
            int flags
                = (ShowI ? 1 : 0)
                | (ShowV ? 2 : 0)
                | (ShowMax ? 0 : 4)   /* showMax used to be always on */
                | (ShowFreq ? 8 : 0)
                | (LockScale ? 16 : 0)
                | (Plot2d ? 64 : 0)
                | (PlotXY ? 128 : 0)
                | (ShowMin ? 256 : 0)
                | (ShowScale ? 512 : 0)
                | (ShowFFT ? 1024 : 0)
                | (mMaxScale ? 8192 : 0)
                | (ShowRMS ? 16384 : 0)
                | (mShowDutyCycle ? 32768 : 0)
                | (LogSpectrum ? 65536 : 0);
            flags |= FLAG_PLOTS;
            return flags;
        }

        public string dump() {
            var vPlot = mPlots[0];

            var elm = vPlot.Elm;
            if (elm == null) {
                return null;
            }
            int flags = getFlags();
            int eno = mSim.locateElm(elm);
            if (eno < 0) {
                return null;
            }
            string x = "o " + eno
                + " " + vPlot.Speed
                + " " + vPlot.Value
                + " " + flags
                + " " + mScale[UNITS_V]
                + " " + mScale[UNITS_A]
                + " " + Position
                + " " + mPlots.Count;
            for (int i = 0; i < mPlots.Count; i++) {
                var p = mPlots[i];
                if (i > 0) {
                    x += " " + mSim.locateElm(p.Elm) + " " + p.Value;
                }
                /* dump scale if units are not V or A */
                if (p.Units > UNITS_A) {
                    x += " " + mScale[p.Units];
                }
            }
            if (Text != null) {
                x += " " + CustomLogicModel.escape(Text);
            }
            return x;
        }

        public void undump(StringTokenizer st) {
            initialize();

            int e = st.nextTokenInt();
            if (e == -1) {
                return;
            }

            var ce = mSim.getElm(e);
            setElm(ce);
            Speed = st.nextTokenInt();
            int value = st.nextTokenInt();

            /* fix old value for VAL_POWER which doesn't work for transistors (because it's the same as VAL_IB) */
            if (!((ce is TransistorElm) && value == VAL_POWER_OLD)) {
                value = VAL_POWER;
            }

            int flags = st.nextTokenInt();
            mScale[UNITS_V] = st.nextTokenDouble();
            mScale[UNITS_A] = st.nextTokenDouble();

            if (mScale[UNITS_V] == 0) {
                mScale[UNITS_V] = .5;
            }
            if (mScale[UNITS_A] == 0) {
                mScale[UNITS_A] = 1;
            }
            mScaleX = mScale[UNITS_V];
            mScaleY = mScale[UNITS_A];
            mScale[UNITS_OHMS] = mScale[UNITS_W] = mScale[UNITS_V];
            Text = null;
            bool plot2dFlag = (flags & 64) != 0;
            if ((flags & FLAG_PLOTS) != 0) {
                /* new-style dump */
                try {
                    Position = st.nextTokenInt();
                    int sz = st.nextTokenInt();

                    int i;
                    setValue(value);
                    /* setValue(0) creates an extra plot for current, so remove that */
                    while (mPlots.Count > 1) {
                        mPlots.RemoveAt(1);
                    }

                    int u = mPlots[0].Units;
                    if (u > UNITS_A) {
                        mScale[u] = st.nextTokenDouble();
                    }

                    for (i = 0; i != sz; i++) {
                        int ne = st.nextTokenInt();
                        int val = st.nextTokenInt();

                        var elm = mSim.getElm(ne);
                        u = elm.GetScopeUnits(val);
                        if (u > UNITS_A) {
                            mScale[u] = st.nextTokenDouble();
                        }
                        mPlots.Add(new ScopePlot(elm, u, val));
                    }
                    while (st.hasMoreTokens()) {
                        if (Text == null) {
                            Text = st.nextToken();
                        } else {
                            Text += " " + st.nextToken();
                        }
                    }
                } catch (Exception ex) {
                    throw ex;
                }
            } else {
                /* old-style dump */
                CircuitElm yElm = null;
                int ivalue = 0;
                try {
                    Position = st.nextTokenInt();
                    int ye = -1;
                    if ((flags & FLAG_YELM) != 0) {
                        ye = st.nextTokenInt();
                        if (ye != -1) {
                            yElm = mSim.getElm(ye);
                        }
                        /* sinediode.txt has yElm set to something even though there's no xy plot...? */
                        if (!plot2dFlag) {
                            yElm = null;
                        }
                    }
                    if ((flags & FLAG_IVALUE) != 0) {
                        ivalue = st.nextTokenInt();
                    }
                    while (st.hasMoreTokens()) {
                        if (Text == null) {
                            Text = st.nextToken();
                        } else {
                            Text += " " + st.nextToken();
                        }
                    }
                } catch (Exception ex) {
                    throw ex;
                }
                setValues(value, ivalue, mSim.getElm(e), yElm);
            }
            if (Text != null) {
                Text = CustomLogicModel.unescape(Text);
            }
            Plot2d = plot2dFlag;
            setFlags(flags);
        }

        void setFlags(int flags) {
            ShowI = (flags & 1) != 0;
            ShowV = (flags & 2) != 0;
            ShowMax = (flags & 4) == 0;
            ShowFreq = (flags & 8) != 0;
            LockScale = (flags & 16) != 0;
            PlotXY = (flags & 128) != 0;
            ShowMin = (flags & 256) != 0;
            ShowScale = (flags & 512) != 0;
            showFFT((flags & 1024) != 0);
            mMaxScale = (flags & 8192) != 0;
            ShowRMS = (flags & 16384) != 0;
            mShowDutyCycle = (flags & 32768) != 0;
            LogSpectrum = (flags & 65536) != 0;
        }

        public void saveAsDefault() {
            var stor = Storage.getLocalStorageIfSupported();
            if (stor == null) {
                return;
            }
            var vPlot = mPlots[0];
            int flags = getFlags();

            /* store current scope settings as default.  1 is a version code */
            stor.setItem("scopeDefaults", "1 " + flags + " " + vPlot.Speed);
            Console.WriteLine("saved defaults " + flags);
        }

        bool loadDefaults() {
            var stor = Storage.getLocalStorageIfSupported();
            if (stor == null) {
                return false;
            }
            string str = stor.getItem("scopeDefaults");
            if (string.IsNullOrEmpty(str)) {
                return false;
            }
            var arr = str.Split(' ');
            int flags = int.Parse(arr[1]);
            setFlags(flags);
            Speed = int.Parse(arr[2]);
            return true;
        }

        public void handleMenu(SCOPE_MENU mi, bool state) {
            if (mi == SCOPE_MENU.maxscale)
                maxScale();
            if (mi == SCOPE_MENU.showvoltage)
                showVoltage(state);
            if (mi == SCOPE_MENU.showcurrent)
                showCurrent(state);
            if (mi == SCOPE_MENU.showscale)
                ShowScale = state;
            if (mi == SCOPE_MENU.showpeak)
                ShowMax = state;
            if (mi == SCOPE_MENU.shownegpeak)
                ShowMin = state;
            if (mi == SCOPE_MENU.showfreq)
                ShowFreq = state;
            if (mi == SCOPE_MENU.showfft)
                showFFT(state);
            if (mi == SCOPE_MENU.logspectrum)
                LogSpectrum = state;
            if (mi == SCOPE_MENU.showrms)
                ShowRMS = state;
            if (mi == SCOPE_MENU.showduty)
                mShowDutyCycle = state;
            if (mi == SCOPE_MENU.showpower)
                setValue(VAL_POWER);
            if (mi == SCOPE_MENU.showib)
                setValue(VAL_IB);
            if (mi == SCOPE_MENU.showic)
                setValue(VAL_IC);
            if (mi == SCOPE_MENU.showie)
                setValue(VAL_IE);
            if (mi == SCOPE_MENU.showvbe)
                setValue(VAL_VBE);
            if (mi == SCOPE_MENU.showvbc)
                setValue(VAL_VBC);
            if (mi == SCOPE_MENU.showvce)
                setValue(VAL_VCE);

            if (mi == SCOPE_MENU.showvcevsic) {
                Plot2d = true;
                PlotXY = false;
                setValues(VAL_VCE, VAL_IC, getElm(), null);
                resetGraph();
            }

            if (mi == SCOPE_MENU.showvvsi) {
                Plot2d = state;
                PlotXY = false;
                resetGraph();
            }

            if (mi == SCOPE_MENU.manualscale) {
                setManualScale(state);
            }

            if (mi == SCOPE_MENU.plotxy) {
                PlotXY = Plot2d = state;
                if (Plot2d) {
                    mPlots = mVisiblePlots;
                }
                if (Plot2d && mPlots.Count == 1) {
                    selectY();
                }
                resetGraph();
            }

            if (mi == SCOPE_MENU.showresistance) {
                setValue(VAL_R);
            }
        }

        public void selectY() {
            var yElm = (mPlots.Count == 2) ? mPlots[1].Elm : null;
            int e = (yElm == null) ? -1 : mSim.locateElm(yElm);
            int firstE = e;
            while (true) {
                for (e++; e < mSim.elmList.Count; e++) {
                    var ce = mSim.getElm(e);
                    if (((ce is OutputElm) || (ce is ProbeElm)) && !ce.Equals(mPlots[0].Elm)) {
                        yElm = ce;
                        if (mPlots.Count == 1) {
                            mPlots.Add(new ScopePlot(yElm, UNITS_V));
                        } else {
                            mPlots[1].Elm = yElm;
                            mPlots[1].Units = UNITS_V;
                        }
                        return;
                    }
                }
                if (firstE == -1) {
                    return;
                }
                e = firstE = -1;
            }
            /* not reached */
        }

        public void onMouseWheel(MouseEventArgs e) {
            mWheelDeltaY += e.Delta;
            if (mWheelDeltaY > 5) {
                slowDown();
                mWheelDeltaY = 0;
            }
            if (mWheelDeltaY < -5) {
                speedUp();
                mWheelDeltaY = 0;
            }
        }

        public CircuitElm getElm() {
            if (SelectedPlot >= 0 && mVisiblePlots.Count > SelectedPlot) {
                return mVisiblePlots[SelectedPlot].Elm;
            }
            return mVisiblePlots.Count > 0 ? mVisiblePlots[0].Elm : mPlots[0].Elm;
        }

        public bool viewingWire() {
            for (int i = 0; i != mPlots.Count; i++) {
                if (mPlots[i].Elm is WireElm) {
                    return true;
                }
            }
            return false;
        }

        public CircuitElm getXElm() {
            return getElm();
        }

        public CircuitElm getYElm() {
            if (mPlots.Count == 2) {
                return mPlots[1].Elm;
            }
            return null;
        }

        public bool needToRemove() {
            bool ret = true;
            bool removed = false;
            int i;
            for (i = 0; i != mPlots.Count; i++) {
                var plot = mPlots[i];
                if (mSim.locateElm(plot.Elm) < 0) {
                    mPlots.RemoveAt(i--);
                    removed = true;
                } else {
                    ret = false;
                }
            }
            if (removed) {
                calcVisiblePlots();
            }
            return ret;
        }

        #region Draw Utils
        void allocImage() {
            if (mContext == null) {
                mContext = CustomGraphics.FromImage(BoundingBox.Width, BoundingBox.Height);
            }
            clear2dView();
        }

        void clear2dView() {
            if (mContext != null) {
                mContext.Clear(Color.Black);
            }
            mDrawOx = mDrawOy = -1;
        }

        void drawCrosshairs(CustomGraphics g) {
            if (mSim.dialogIsShowing()) {
                return;
            }
            if (!BoundingBox.Contains(mSim.mouseCursorX, mSim.mouseCursorY)) {
                return;
            }
            if (SelectedPlot < 0 && !ShowFFT) {
                return;
            }
            var info = new string[4];
            int ipa = mPlots[0].StartIndex(BoundingBox.Width);
            int ip = (mSim.mouseCursorX - BoundingBox.X + ipa) & (mScopePointCount - 1);
            int ct = 0;
            int maxy = (BoundingBox.Height - 1) / 2;
            int y = maxy;
            if (SelectedPlot >= 0) {
                var plot = mVisiblePlots[SelectedPlot];
                info[ct++] = plot.GetUnitText(plot.MaxValues[ip]);
                int maxvy = (int)(mMainGridMult * (plot.MaxValues[ip] - mMainGridMid));
                g.LineColor = plot.Color;
                g.FillCircle(mSim.mouseCursorX, BoundingBox.Y + y - maxvy, 2.5f);
            }
            if (ShowFFT) {
                double maxFrequency = 1 / (mSim.timeStep * Speed * 2);
                info[ct++] = Utils.UnitText(maxFrequency * (mSim.mouseCursorX - BoundingBox.X) / BoundingBox.Width, "Hz");
            }
            if (mVisiblePlots.Count > 0) {
                double t = mSim.t - mSim.timeStep * Speed * (BoundingBox.X + BoundingBox.Width - mSim.mouseCursorX);
                info[ct++] = Utils.TimeText(t);
            }

            int szw = 0, szh = 15 * ct;
            for (int i = 0; i != ct; i++) {
                int w = (int)g.GetTextSize(info[i]).Width;
                if (w > szw) {
                    szw = w;
                }
            }

            g.LineColor = CircuitElm.WhiteColor;
            g.DrawLine(mSim.mouseCursorX, BoundingBox.Y, mSim.mouseCursorX, BoundingBox.Y + BoundingBox.Height);

            int bx = mSim.mouseCursorX;
            if (bx < szw / 2) {
                bx = szw / 2;
            }

            g.LineColor = mSim.chkPrintableCheckItem.Checked ? Color.White : Color.Black;
            g.FillRectangle(bx - szw / 2, BoundingBox.Y - szh, szw, szh);

            g.TextColor = CircuitElm.TextColor;
            for (int i = 0; i != ct; i++) {
                int w = (int)g.GetTextSize(info[i]).Width;
                g.DrawLeftText(info[i], bx - w / 2, BoundingBox.Y - 2 - (ct - 1 - i) * 15);
            }
        }

        void drawPlot(CustomGraphics g, ScopePlot plot, bool drawHGridLines, bool selected) {
            if (plot.Elm == null) {
                return;
            }
            int i;
            int multptr = 0;
            int x = 0;
            int maxy = (BoundingBox.Height - 1) / 2;
            int y = maxy;

            var color = (mSomethingSelected) ? Color.FromArgb(0xA0, 0xA0, 0xA0) : plot.Color;
            if (mSim.scopeSelected == -1 && plot.Elm.IsMouseElm) {
                color = Color.FromArgb(0x00, 0xFF, 0xFF);
            } else if (selected) {
                color = plot.Color;
            }
            int ipa = plot.StartIndex(BoundingBox.Width);
            var maxV = plot.MaxValues;
            var minV = plot.MinValues;
            double gridMax = mScale[plot.Units];
            double gridMid = 0;
            if (drawHGridLines) {
                /* if we don't have overlapping scopes of different units, we can move zero around.
                 * Put it at the bottom if the scope is never negative. */
                double mx = gridMax;
                double mn = 0;
                if (mMaxScale) {
                    /* scale is maxed out, so fix boundaries of scope at maximum and minimum. */
                    mx = mMaxValue;
                    mn = mMinValue;
                } else if (mShowNegative || mMinValue < (mx + mn) * .5 - (mx - mn) * .55) {
                    mn = -gridMax;
                    mShowNegative = true;
                }
                gridMid = (mx + mn) * .5;
                gridMax = (mx - mn) * .55;  /* leave space at top and bottom */
            }
            double gridMult = maxy / gridMax;
            if (selected) {
                mMainGridMult = gridMult;
                mMainGridMid = gridMid;
            }
            int minRangeLo = -10 - (int)(gridMid * gridMult);
            int minRangeHi = 10 - (int)(gridMid * gridMult);

            mGridStepY = 1e-8;
            while (mGridStepY < 20 * gridMax / maxy) {
                mGridStepY *= MULTA[(multptr++) % 3];
            }

            /* Horizontal gridlines */
            int ll;
            var minorDiv = Color.FromArgb(0x30, 0x30, 0x30);
            var majorDiv = Color.FromArgb(0xA0, 0xA0, 0xA0);
            if (mSim.chkPrintableCheckItem.Checked) {
                minorDiv = Color.FromArgb(0xD0, 0xD0, 0xD0);
                majorDiv = Color.FromArgb(0x80, 0x80, 0x80);
            }

            /* Vertical (T) gridlines */
            double ts = mSim.timeStep * Speed;
            mGridStepX = calcGridStepX();

            if (mDrawGridLines) {
                /* horizontal gridlines */

                /* don't show gridlines if lines are too close together (except for center line) */
                bool showGridLines = (mGridStepY != 0) && drawHGridLines;
                for (ll = -100; ll <= 100; ll++) {
                    if (ll != 0 && !showGridLines) {
                        continue;
                    }
                    int yl = maxy - (int)((ll * mGridStepY - gridMid) * gridMult);
                    if (yl < 0 || yl >= BoundingBox.Height - 1) {
                        continue;
                    }
                    g.LineColor = ll == 0 ? majorDiv : minorDiv;
                    g.DrawLine(0, yl, BoundingBox.Width - 1, yl);
                }

                /* vertical gridlines */
                double tstart = mSim.t - mSim.timeStep * Speed * BoundingBox.Width;
                double tx = mSim.t - (mSim.t % mGridStepX);

                for (ll = 0; ; ll++) {
                    double tl = tx - mGridStepX * ll;
                    int gx = (int)((tl - tstart) / ts);
                    if (gx < 0) {
                        break;
                    }
                    if (gx >= BoundingBox.Width) {
                        continue;
                    }
                    if (tl < 0) {
                        continue;
                    }
                    if (((tl + mGridStepX / 4) % (mGridStepX * 10)) < mGridStepX) {
                        g.LineColor = majorDiv;
                    } else {
                        g.LineColor = minorDiv;
                    }
                    g.DrawLine(gx, 0, gx, BoundingBox.Height - 1);
                }
            }

            /* only need gridlines drawn once */
            mDrawGridLines = false;

            g.LineColor = color;
            int ox = -1;
            int oy = -1;
            for (i = 0; i != BoundingBox.Width; i++) {
                int nx = x + i;
                int ip = (i + ipa) & (mScopePointCount - 1);
                int minvy = (int)(gridMult * (minV[ip] - gridMid));
                int maxvy = (int)(gridMult * (maxV[ip] - gridMid));
                if (minvy <= maxy) {
                    if (minvy < minRangeLo || maxvy > minRangeHi) {
                        /* we got a value outside min range, so we don't need to rescale later */
                        mReduceRange[plot.Units] = false;
                        minRangeLo = -1000;
                        minRangeHi = 1000; /* avoid triggering this test again */
                    }
                    if (ox != -1) {
                        if (minvy == oy && maxvy == oy) {
                            continue;
                        }
                        g.DrawLine(ox, y - oy, nx, y - oy);
                        ox = oy = -1;
                    }
                    if (minvy == maxvy) {
                        ox = nx;
                        oy = minvy;
                        continue;
                    }
                    g.DrawLine(nx, y - minvy, nx, y - maxvy);
                }
            } /* for (i=0...) */
            if (ox != -1) {
                g.DrawLine(ox, y - oy, x + i, y - oy); /* Horizontal */
            }
        }

        void drawFFTVerticalGridLines(CustomGraphics g) {
            /* Draw x-grid lines and label the frequencies in the FFT that they point to. */
            int prevEnd = 0;
            int divs = 20;
            double maxFrequency = 1 / (mSim.timeStep * Speed * divs * 2);
            g.LineColor = Color.FromArgb(0x88, 0x00, 0x00);
            g.TextColor = CircuitElm.TextColor;
            for (int i = 0; i < divs; i++) {
                int x = BoundingBox.Width * i / divs;
                if (x < prevEnd) {
                    continue;
                }
                string s = ((int)Math.Round(i * maxFrequency)) + "Hz";
                int sWidth = (int)Math.Ceiling(g.GetTextSize(s).Width);
                prevEnd = x + sWidth + 4;
                if (i > 0) {
                    g.DrawLine(x, 0, x, BoundingBox.Height);
                }
                g.DrawLeftText(s, x + 2, BoundingBox.Height);
            }
        }

        void drawFFT(CustomGraphics g) {
            if (mFft == null || mFft.Size != mScopePointCount) {
                mFft = new FFT(mScopePointCount);
            }
            var real = new double[mScopePointCount];
            var imag = new double[mScopePointCount];
            var plot = (mVisiblePlots.Count == 0) ? mPlots[0] : mVisiblePlots[0];
            var maxV = plot.MaxValues;
            var minV = plot.MinValues;
            int ptr = plot.Pointer;
            for (int i = 0; i < mScopePointCount; i++) {
                int ii = (ptr - i + mScopePointCount) & (mScopePointCount - 1);
                /* need to average max and min or else it could cause average of function to be > 0, which
                /* produces spike at 0 Hz that hides rest of spectrum */
                real[i] = .5 * (maxV[ii] + minV[ii]);
                imag[i] = 0;
            }
            mFft.Exec(real, imag);
            double maxM = 1e-8;
            for (int i = 0; i < mScopePointCount / 2; i++) {
                double m = mFft.Magnitude(real[i], imag[i]);
                if (m > maxM) {
                    maxM = m;
                }
            }
            int prevX = 0;
            g.LineColor = Color.Red;
            if (!LogSpectrum) {
                int prevHeight = 0;
                int y = (BoundingBox.Height - 1) - 12;
                for (int i = 0; i < mScopePointCount / 2; i++) {
                    int x = 2 * i * BoundingBox.Width / mScopePointCount;
                    /* rect.width may be greater than or less than scopePointCount/2,
                     * so x may be greater than or equal to prevX. */
                    double magnitude = mFft.Magnitude(real[i], imag[i]);
                    int height = (int)((magnitude * y) / maxM);
                    if (x != prevX) {
                        g.DrawLine(prevX, y - prevHeight, x, y - height);
                    }
                    prevHeight = height;
                    prevX = x;
                }
            } else {
                int y0 = 5;
                int prevY = 0;
                double ymult = BoundingBox.Height / 10;
                double val0 = Math.Log(mScale[plot.Units]) * ymult;
                for (int i = 0; i < mScopePointCount / 2; i++) {
                    int x = 2 * i * BoundingBox.Width / mScopePointCount;
                    /* rect.width may be greater than or less than scopePointCount/2,
                     * so x may be greater than or equal to prevX. */
                    var mag = mFft.Magnitude(real[i], imag[i]);
                    if (0 == mag) {
                        mag = 1;
                    }
                    double val = Math.Log(mag);
                    int y = y0 - (int)(val * ymult - val0);
                    if (x != prevX) {
                        g.DrawLine(prevX, prevY, x, y);
                    }
                    prevY = y;
                    prevX = x;
                }
            }
        }

        void drawSettingsWheel(CustomGraphics g) {
            const int outR = 8 * 12 / 8;
            const int inR = 5 * 12 / 8;
            const int inR45 = 4 * 12 / 8;
            const int outR45 = 6 * 12 / 8;
            if (showSettingsWheel()) {
                if (cursorInSettingsWheel()) {
                    g.LineColor = Color.Cyan;
                } else {
                    g.LineColor = Color.DarkGray;
                }
                g.SetTransform(new Matrix(1, 0, 0, 1, BoundingBox.X + 18, BoundingBox.Y + BoundingBox.Height - 18));
                g.DrawCircle(0, 0, inR);
                g.DrawLine(  -outR,       0,   -inR,      0);
                g.DrawLine(   outR,       0,    inR,      0);
                g.DrawLine(      0,   -outR,      0,   -inR);
                g.DrawLine(      0,    outR,      0,    inR);
                g.DrawLine(-outR45, -outR45, -inR45, -inR45);
                g.DrawLine( outR45, -outR45,  inR45, -inR45);
                g.DrawLine(-outR45,  outR45, -inR45,  inR45);
                g.DrawLine( outR45,  outR45,  inR45,  inR45);
                g.ClearTransform();
            }
        }

        void draw2d(CustomGraphics g) {
            if (mContext == null) {
                return;
            }

            mAlphaDiv++;
            if (mAlphaDiv > 2) {
                mAlphaDiv = 0;
                if (mSim.chkPrintableCheckItem.Checked) {
                    g.LineColor = Color.FromArgb(0x0F, 0xFF, 0xFF, 0xFF);
                } else {
                    g.LineColor = Color.FromArgb(0x0F, 0, 0, 0);
                }
                g.FillRectangle(0, 0, BoundingBox.Width, BoundingBox.Height);
            }

            g.LineColor = CircuitElm.WhiteColor;
            g.FillCircle(mDrawOx, mDrawOy, 2.5f);
            int yt = 10;
            int x = 0;
            if (Text != null && BoundingBox.Height > yt + 5) {
                g.TextColor = CircuitElm.TextColor;
                g.DrawLeftText(Text, x, yt);
                yt += 15;
            }

            g.LineColor = Color.Green;
            g.DrawLine(0, BoundingBox.Height / 2, BoundingBox.Width - 1, BoundingBox.Height / 2);
            if (!PlotXY) {
                g.LineColor = Color.Yellow;
            }
            g.DrawLine(BoundingBox.Width / 2, 0, BoundingBox.Width / 2, BoundingBox.Height - 1);

            g.ClearTransform();
            drawSettingsWheel(g);
        }

        void drawTo(int x2, int y2) {
            if (mDrawOx == -1) {
                mDrawOx = x2;
                mDrawOy = y2;
            }
            mContext.LineColor = Color.GreenYellow;
            mContext.DrawLine(mDrawOx, mDrawOy, x2, y2);
            mDrawOx = x2;
            mDrawOy = y2;
        }

        /* calc RMS and display it */
        void drawRMS(CustomGraphics g) {
            if (!canShowRMS()) {
                drawAverage(g);
                return;
            }
            var plot = mVisiblePlots[0];
            int i;
            double avg = 0;
            int ipa = plot.Pointer + mScopePointCount - BoundingBox.Width;
            var maxV = plot.MaxValues;
            var minV = plot.MinValues;
            double mid = (mMaxValue + mMinValue) / 2;
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
                    double m = (maxV[ip] + minV[ip]) * .5;
                    avg += m * m;
                }
            }
            double rms;
            if (waveCount > 1) {
                rms = Math.Sqrt(endAvg / (end - start));
                drawInfoText(g, plot.GetUnitText(rms) + "rms");
            }
        }

        void drawAverage(CustomGraphics g) {
            var plot = mVisiblePlots[0];
            int i;
            double avg = 0;
            int ipa = plot.Pointer + mScopePointCount - BoundingBox.Width;
            var maxV = plot.MaxValues;
            var minV = plot.MinValues;
            double mid = (mMaxValue + mMinValue) / 2;
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
                    double m = (maxV[ip] + minV[ip]) * .5;
                    avg += m;
                }
            }
            if (waveCount > 1) {
                avg = (endAvg / (end - start));
                drawInfoText(g, plot.GetUnitText(avg) + " average");
            }
        }

        void drawDutyCycle(CustomGraphics g) {
            var plot = mVisiblePlots[0];
            int i;
            int ipa = plot.Pointer + mScopePointCount - BoundingBox.Width;
            var maxV = plot.MaxValues;
            var minV = plot.MinValues;
            double mid = (mMaxValue + mMinValue) / 2;
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
            int firstState = 1;
            int start = i;
            int end = 0;
            int waveCount = 0;
            int dutyLen = 0;
            int middle = 0;
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
                            start = end = i;
                        } else {
                            end = start;
                            start = i;
                            dutyLen = end - middle;
                        }
                        waveCount++;
                    } else
                        middle = i;
                }
            }
            if (waveCount > 1) {
                int duty = 100 * dutyLen / (end - start);
                drawInfoText(g, "Duty cycle " + duty + "%");
            }
        }

        /* calc frequency if possible and display it */
        void drawFrequency(CustomGraphics g) {
            /* try to get frequency
             * get average */
            double avg = 0;
            int i;
            var plot = mVisiblePlots[0];
            int ipa = plot.Pointer + mScopePointCount - BoundingBox.Width;
            var minV = plot.MinValues;
            var maxV = plot.MaxValues;
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
            double periodstd = Math.Sqrt(avperiod2 - avperiod * avperiod);
            double freq = 1 / (avperiod * mSim.timeStep * Speed);
            /* don't show freq if standard deviation is too great */
            if (periodct < 1 || periodstd > 2) {
                freq = 0;
            }
            /* Console.WriteLine(freq + " " + periodstd + " " + periodct); */
            if (freq != 0) {
                drawInfoText(g, Utils.UnitText(freq, "Hz"));
            }
        }

        void drawInfoText(CustomGraphics g, string text) {
            if (BoundingBox.Y + BoundingBox.Height <= mTextY + 5) {
                return;
            }
            g.DrawLeftText(text, 0, mTextY);
            mTextY += 15;
        }

        void drawInfoTexts(CustomGraphics g) {
            g.TextColor = CircuitElm.TextColor;
            mTextY = 10;
            var plot = mVisiblePlots[0];
            if (ShowScale) {
                string vScaleText = "";
                if (mGridStepY != 0 && (!(ShowV && ShowI))) {
                    vScaleText = " V=" + plot.GetUnitText(mGridStepY) + "/div";
                }
                drawInfoText(g, "H=" + Utils.UnitText(mGridStepX, "s") + "/div" + vScaleText);
            }
            if (ShowMax) {
                drawInfoText(g, plot.GetUnitText(mMaxValue));
            }
            if (ShowMin) {
                int ym = BoundingBox.Height - 5;
                g.DrawLeftText(plot.GetUnitText(mMinValue), 0, ym);
            }
            if (ShowRMS) {
                drawRMS(g);
            }
            if (mShowDutyCycle) {
                drawDutyCycle(g);
            }
            string t = Text;
            if (t == null) {
                t = getScopeText();
            }
            if (t != null) {
                drawInfoText(g, t);
            }
            if (ShowFreq) {
                drawFrequency(g);
            }
        }
        #endregion
    }
}
