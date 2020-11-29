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
        public SCOPE_MENU menuCmd;
        public ScopeCheckBox(string text, SCOPE_MENU menu) : base() {
            AutoSize = true;
            Text = text;
            menuCmd = menu;
        }
    }

    class Scope {
        #region CONST
        const int FLAG_YELM = 32;

        /* bunch of other flags go here, see dump() */
        const int FLAG_IVALUE = 2048; /* Flag to indicate if IVALUE is included in dump */
        const int FLAG_PLOTS = 4096;  /* new-style dump with multiple plots */
                                      /* other flags go here too, see dump() */

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

        readonly double[] multa = new double[] { 2.0, 2.5, 2.0 };

        readonly Font LABEL_FONT = new Font("Meiryo UI", 9.0f);
        #endregion

        #region dynamic variable
        Pen LABEL_COLOR = new Pen(Brushes.Red, 1.0f);
        Pen LINE_COLOR = new Pen(Brushes.Red, 1.0f);

        int scopePointCount = 128;
        FFT fft;

        int alphadiv = 0;

        public int position;
        public int speed;
        public int stackCount; /* number of scopes in this column */

        string text;

        public int selectedPlot { get; private set; }
        List<ScopePlot> plots;
        List<ScopePlot> visiblePlots;

        double scopeTimeStep;

        CirSim sim;
        double[] scale;
        bool[] reduceRange;

        double scaleX;  /* for X-Y plots */
        double scaleY;
        int wheelDeltaY;

        int draw_ox;
        int draw_oy;

        public Rectangle rect { get; private set; }
        Bitmap imageCanvas;
        Graphics imageContext;

        Color curColor;
        Color voltColor;

        double gridStepX;
        double gridStepY;
        double maxValue;
        double minValue;
        double mainGridMult;
        double mainGridMid;

        int textY;

        bool drawGridLines;
        bool somethingSelected;

        public bool showMax;
        public bool showMin;
        public bool showI { get; private set; }
        public bool showV { get; private set; }
        public bool showScale { get; private set; }

        public bool showFreq { get; private set; }
        public bool lockScale { get; private set; }
        public bool plot2d { get; private set; }
        public bool plotXY { get; private set; }
        public bool _maxScale { get; private set; }
        public bool logSpectrum { get; private set; }
        public bool _showFFT { get; private set; }
        public bool showNegative { get; private set; }
        public bool showRMS { get; private set; }
        public bool showDutyCycle { get; private set; }
        #endregion

        public Scope(CirSim s) {
            sim = s;
            scale = new double[UNITS_COUNT];
            reduceRange = new bool[UNITS_COUNT];

            rect = new Rectangle(0, 0, 1, 1);

            allocImage();
            initialize();
        }

        void showCurrent(bool b) {
            showI = b;
            if (b && !showingVoltageAndMaybeCurrent()) {
                setValue(0);
            }
            calcVisiblePlots();
        }

        void showVoltage(bool b) {
            showV = b;
            if (b && !showingVoltageAndMaybeCurrent()) {
                setValue(0);
            }
            calcVisiblePlots();
        }

        void showFFT(bool b) {
            _showFFT = b;
            if (!_showFFT) {
                fft = null;
            }
        }

        void setManualScale(bool b) { lockScale = b; }

        public void resetGraph() { resetGraph(false); }

        public void resetGraph(bool full) {
            scopePointCount = 1;
            while (scopePointCount <= rect.Width) {
                scopePointCount *= 2;
            }
            if (plots == null) {
                plots = new List<ScopePlot>();
            }
            showNegative = false;
            int i;
            for (i = 0; i != plots.Count; i++) {
                plots[i].reset(scopePointCount, speed, full);
            }
            calcVisiblePlots();
            scopeTimeStep = sim.timeStep;
            allocImage();
        }

        public void setManualScaleValue(double d) {
            if (visiblePlots.Count == 0) {
                return;
            }
            var p = visiblePlots[0];
            scale[p.units] = d;
        }

        public double getScaleValue() {
            if (visiblePlots.Count == 0) {
                return 0;
            }
            var p = visiblePlots[0];
            return scale[p.units];
        }

        public string getScaleUnitsText() {
            if (visiblePlots.Count == 0) {
                return "V";
            }
            var p = visiblePlots[0];
            switch (p.units) {
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

        bool active() { return plots.Count > 0 && plots[0].elm != null; }

        void initialize() {
            resetGraph();
            scale[UNITS_W] = scale[UNITS_OHMS] = scale[UNITS_V] = 5;
            scale[UNITS_A] = .1;
            scaleX = 5;
            scaleY = .1;
            speed = 64;
            showMax = true;
            showV = showI = false;
            showScale = showFreq = lockScale = showMin = false;
            _showFFT = false;
            plot2d = false;
            if (!loadDefaults()) {
                /* set showV and showI appropriately depending on what plots are present */
                int i;
                for (i = 0; i != plots.Count; i++) {
                    var plot = plots[i];
                    if (plot.units == UNITS_V) {
                        showV = true;
                    }
                    if (plot.units == UNITS_A) {
                        showI = true;
                    }
                }
            }
        }

        void calcVisiblePlots() {
            visiblePlots = new List<ScopePlot>();
            int i;
            int vc = 0, ac = 0, oc = 0;
            for (i = 0; i != plots.Count; i++) {
                var plot = plots[i];
                if (plot.units == UNITS_V) {
                    if (showV) {
                        visiblePlots.Add(plot);
                        plot.assignColor(vc++);
                    }
                } else if (plot.units == UNITS_A) {
                    if (showI) {
                        visiblePlots.Add(plot);
                        plot.assignColor(ac++);
                    }
                } else {
                    visiblePlots.Add(plot);
                    plot.assignColor(oc++);
                }
            }
        }

        public void setRect(Rectangle r) {
            int w = rect.Width;
            rect = r;
            if (rect.Width != w) {
                resetGraph();
            }
        }

        int getWidth() { return rect.Width; }

        public int rightEdge() { return rect.X + rect.Width; }

        public void setElm(CircuitElm ce) {
            plots = new List<ScopePlot>();
            if (null != ce && (ce is TransistorElm)) {
                setValue(VAL_VCE, ce);
            } else {
                setValue(0, ce);
            }
            initialize();
        }

        void setValue(int val) {
            if (plots.Count > 2 || plots.Count == 0) {
                return;
            }
            var ce = plots[0].elm;
            if (plots.Count == 2 && !plots[1].elm.Equals(ce)) {
                return;
            }
            plot2d = plotXY = false;
            setValue(val, ce);
        }

        void setValue(int val, CircuitElm ce) {
            plots = new List<ScopePlot>();
            if (val == 0) {
                plots.Add(new ScopePlot(ce, UNITS_V, 0));
                /* create plot for current if applicable */
                if (ce != null && !((ce is OutputElm) || (ce is LogicOutputElm) || (ce is _AudioOutputElm) || (ce is ProbeElm))) {
                    plots.Add(new ScopePlot(ce, UNITS_A, VAL_CURRENT));
                }
            } else {
                int u = ce.getScopeUnits(val);
                plots.Add(new ScopePlot(ce, u, val));
                if (u == UNITS_V) {
                    showV = true;
                }
                if (u == UNITS_A) {
                    showI = true;
                }
            }
            calcVisiblePlots();
            resetGraph();
        }

        void setValues(int val, int ival, CircuitElm ce, CircuitElm yelm) {
            if (ival > 0) {
                plots = new List<ScopePlot>();
                plots.Add(new ScopePlot(ce, ce.getScopeUnits(val), val));
                plots.Add(new ScopePlot(ce, ce.getScopeUnits(ival), ival));
                return;
            }
            if (yelm != null) {
                plots = new List<ScopePlot>();
                plots.Add(new ScopePlot(ce, ce.getScopeUnits(val), 0));
                plots.Add(new ScopePlot(yelm, ce.getScopeUnits(ival), 0));
                return;
            }
            setValue(val);
        }

        public void setText(string s) { text = s; }

        public string getText() { return text; }

        public bool showingValue(int v) {
            for (int i = 0; i != plots.Count; i++) {
                var sp = plots[i];
                if (sp.value != v) {
                    return false;
                }
            }
            return true;
        }

        /* returns true if we have a plot of voltage and nothing else (except current).
        /* The default case is a plot of voltage and current, so we're basically checking if that case is true. */
        bool showingVoltageAndMaybeCurrent() {
            bool gotv = false;
            for (int i = 0; i != plots.Count; i++) {
                var sp = plots[i];
                if (sp.value == 0) {
                    gotv = true;
                } else if (sp.value != VAL_CURRENT) {
                    return false;
                }
            }
            return gotv;
        }

        public void combine(Scope s) {
            plots = visiblePlots;
            plots.AddRange(s.visiblePlots);
            s.plots.Clear();
            calcVisiblePlots();
        }

        /* separate this scope's plots into separate scopes and return them in arr[pos], arr[pos+1], etc.
         * return new length of array. */
        public int separate(List<Scope> arr, int pos) {
            ScopePlot lastPlot = null;
            for (int i = 0; i != visiblePlots.Count; i++) {
                if (pos >= arr.Count) {
                    return pos;
                }
                var s = new Scope(sim);
                var sp = visiblePlots[i];
                if (lastPlot != null && lastPlot.elm == sp.elm && lastPlot.value == 0 && sp.value == VAL_CURRENT) {
                    continue;
                }
                s.setValue(sp.value, sp.elm);
                s.position = pos;
                arr[pos++] = s;
                lastPlot = sp;
                s.setFlags(getFlags());
                s.setSpeed(speed);
            }
            return pos;
        }

        public void removePlot(int plot) {
            if (plot < visiblePlots.Count) {
                var p = visiblePlots[plot];
                plots.Remove(p);
                calcVisiblePlots();
            }
        }

        /* called for each timestep */
        public void timeStep() {
            int i;
            for (i = 0; i != plots.Count; i++) {
                plots[i].timeStep();
            }

            if (plot2d && imageContext != null) {
                bool newscale = false;
                if (plots.Count < 2) {
                    return;
                }
                double v = plots[0].lastValue;
                while (v > scaleX || v < -scaleX) {
                    scaleX *= 2;
                    newscale = true;
                }
                double yval = plots[1].lastValue;
                while (yval > scaleY || yval < -scaleY) {
                    scaleY *= 2;
                    newscale = true;
                }
                if (newscale) {
                    clear2dView();
                }
                double xa = v / scaleX;
                double ya = yval / scaleY;
                int x = (int)(rect.Width * (1 + xa) * .499);
                int y = (int)(rect.Height * (1 - ya) * .499);
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
            if (plot2d) {
                double x = 1e-8;
                scale[UNITS_V] *= x;
                scale[UNITS_A] *= x;
                scale[UNITS_OHMS] *= x;
                scale[UNITS_W] *= x;
                scaleX *= x;
                scaleY *= x;
                return;
            }
            /* toggle max scale.  This isn't on by default because, for the examples, we sometimes want two plots
             * matched to the same scale so we can show one is larger.  Also, for some fast-moving scopes
             * (like for AM detector), the amplitude varies over time but you can't see that if the scale is
             * constantly adjusting.  It's also nice to set the default scale to hide noise and to avoid
             * having the scale moving around a lot when a circuit starts up. */
            _maxScale = !_maxScale;
            showNegative = false;
        }

        bool showSettingsWheel() {
            return rect.Height > 100 && rect.Width > 100;
        }

        public bool cursorInSettingsWheel() {
            return showSettingsWheel() &&
                sim.mouseCursorX >= rect.X &&
                sim.mouseCursorX <= rect.X + 36 &&
                sim.mouseCursorY >= rect.Y + rect.Height - 36 &&
                sim.mouseCursorY <= rect.Y + rect.Height;
        }

        public void draw(Graphics g) {
            if (plots.Count == 0) {
                return;
            }

            /* reset if timestep changed */
            if (scopeTimeStep != sim.timeStep) {
                scopeTimeStep = sim.timeStep;
                resetGraph();
            }

            g.TranslateTransform(rect.X, rect.Y);

            if (plot2d) {
                draw2d(g);
                return;
            }

            LINE_COLOR.Color = Color.Red;

            drawSettingsWheel(g);

            if (_showFFT) {
                drawFFTVerticalGridLines(g);
                drawFFT(g);
            }

            for (int i = 0; i != UNITS_COUNT; i++) {
                reduceRange[i] = false;
                if (_maxScale) {
                    scale[i] = 1e-4;
                }
            }

            somethingSelected = false;  /* is one of our plots selected? */

            for (int si = 0; si != visiblePlots.Count; si++) {
                var plot = visiblePlots[si];
                calcPlotScale(plot);
                if (sim.scopeSelected == -1 && plot.elm != null && plot.elm.isMouseElm()) {
                    somethingSelected = true;
                }
                reduceRange[plot.units] = true;
            }

            checkForSelection();
            if (selectedPlot >= 0) {
                somethingSelected = true;
            }

            drawGridLines = true;
            bool hGridLines = true;
            for (int i = 1; i < visiblePlots.Count; i++) {
                if (visiblePlots[i].units != visiblePlots[0].units) {
                    hGridLines = false;
                }
            }

            if ((hGridLines || showMax || showMin) && visiblePlots.Count > 0) {
                calcMaxAndMin(visiblePlots[0].units);
            }

            /* draw volts on top (last), then current underneath, then everything else */
            for (int i = 0; i != visiblePlots.Count; i++) {
                if (visiblePlots[i].units > UNITS_A && i != selectedPlot) {
                    drawPlot(g, visiblePlots[i], hGridLines, false);
                }
            }
            for (int i = 0; i != visiblePlots.Count; i++) {
                if (visiblePlots[i].units == UNITS_A && i != selectedPlot) {
                    drawPlot(g, visiblePlots[i], hGridLines, false);
                }
            }
            for (int i = 0; i != visiblePlots.Count; i++) {
                if (visiblePlots[i].units == UNITS_V && i != selectedPlot) {
                    drawPlot(g, visiblePlots[i], hGridLines, false);
                }
            }
            /* draw selection on top.  only works if selection chosen from scope */
            if (selectedPlot >= 0 && selectedPlot < visiblePlots.Count) {
                drawPlot(g, visiblePlots[selectedPlot], hGridLines, true);
            } 

            if (visiblePlots.Count > 0) {
                drawInfoTexts(g);
            }

            drawCrosshairs(g);

            g.Transform = new Matrix(1, 0, 0, 1, 0, 0);

            if (plots[0].ptr > 5 && !lockScale) {
                for (int i = 0; i != UNITS_COUNT; i++) {
                    if (scale[i] > 1e-4 && reduceRange[i]) {
                        scale[i] /= 2;
                    }
                }
            }
        }

        /* calculate maximum and minimum values for all plots of given units */
        void calcMaxAndMin(int units) {
            maxValue = -1e8;
            minValue = 1e8;
            int i;
            for (int si = 0; si != visiblePlots.Count; si++) {
                var plot = visiblePlots[si];
                if (plot.units != units) {
                    continue;
                }
                int ipa = plot.startIndex(rect.Width);
                var maxV = plot.maxValues;
                var minV = plot.minValues;
                for (i = 0; i != rect.Width; i++) {
                    int ip = (i + ipa) & (scopePointCount - 1);
                    if (maxV[ip] > maxValue) {
                        maxValue = maxV[ip];
                    }
                    if (minV[ip] < minValue) {
                        minValue = minV[ip];
                    }
                }
            }
        }

        /* adjust scale of a plot */
        void calcPlotScale(ScopePlot plot) {
            if (lockScale) {
                return;
            }
            int ipa = plot.startIndex(rect.Width);
            var maxV = plot.maxValues;
            var minV = plot.minValues;
            double max = 0;
            double gridMax = scale[plot.units];
            for (int i = 0; i != rect.Width; i++) {
                int ip = (i + ipa) & (scopePointCount - 1);
                if (maxV[ip] > max) {
                    max = maxV[ip];
                }
                if (minV[ip] < -max) {
                    max = -minV[ip];
                }
            }
            /* scale fixed at maximum? */
            if (_maxScale) {
                gridMax = Math.Max(max, gridMax);
            } else {
                /* adjust in powers of two */
                while (max > gridMax) {
                    gridMax *= 2;
                }
            }
            scale[plot.units] = gridMax;
        }

        public double calcGridStepX() {
            int multptr = 0;
            double gsx = 1e-15;

            double ts = sim.timeStep * speed;
            while (gsx < ts * 20) {
                gsx *= multa[(multptr++) % 3];
            }
            return gsx;
        }

        /* find selected plot */
        void checkForSelection() {
            if (sim.dialogIsShowing()) {
                return;
            }
            if (!rect.Contains(sim.mouseCursorX, sim.mouseCursorY)) {
                selectedPlot = -1;
                return;
            }
            int ipa = plots[0].startIndex(rect.Width);
            int ip = (sim.mouseCursorX - rect.X + ipa) & (scopePointCount - 1);
            int maxy = (rect.Height - 1) / 2;
            int y = maxy;
            int i;
            int bestdist = 10000;
            int best = -1;
            for (i = 0; i != visiblePlots.Count; i++) {
                var plot = visiblePlots[i];
                int maxvy = (int)((maxy / scale[plot.units]) * plot.maxValues[ip]);
                int dist = Math.Abs(sim.mouseCursorY - (rect.Y + y - maxvy));
                if (dist < bestdist) {
                    bestdist = dist;
                    best = i;
                }
            }
            selectedPlot = best;
        }

        public bool canShowRMS() {
            if (visiblePlots.Count == 0) {
                return false;
            }
            var plot = visiblePlots[0];
            return (plot.units == UNITS_V || plot.units == UNITS_A);
        }

        string getScopeText() {
            /* stacked scopes?  don't show text */
            if (stackCount != 1) {
                return null;
            }

            /* multiple elms?  don't show text (unless one is selected) */
            if (selectedPlot < 0 && getSingleElm() == null) {
                return null;
            }

            var plot = visiblePlots[0];
            if (selectedPlot >= 0 && visiblePlots.Count > selectedPlot) {
                plot = visiblePlots[selectedPlot];
            }
            if (plot.elm == null) {
                return "";
            } else {
                return plot.elm.getScopeText(plot.value);
            }
        }

        public void setSpeed(int sp) {
            if (sp < 1) {
                sp = 1;
            }
            if (sp > 1024) {
                sp = 1024;
            }
            speed = sp;
            resetGraph();
        }

        public void properties() {
            CirSim.dialogShowing = new ScopePropertiesDialog(sim, this);
        }

        public void speedUp() {
            if (speed > 1) {
                speed /= 2;
                resetGraph();
            }
        }

        public void slowDown() {
            if (speed < 1024) {
                speed *= 2;
            }
            resetGraph();
        }

        /* get scope element, returning null if there's more than one */
        public CircuitElm getSingleElm() {
            var elm = plots[0].elm;
            for (int i = 1; i < plots.Count; i++) {
                if (!plots[i].elm.Equals(elm)) {
                    return null;
                }
            }
            return elm;
        }

        public bool canMenu() {
            return plots[0].elm != null;
        }

        public bool canShowResistance() {
            var elm = getSingleElm();
            return elm != null && elm.canShowValueInScope(VAL_R);
        }

        public bool isShowingVceAndIc() {
            return plot2d && plots.Count == 2 && plots[0].value == VAL_VCE && plots[1].value == VAL_IC;
        }

        int getFlags() {
            int flags
                = (showI ? 1 : 0)
                | (showV ? 2 : 0)
                | (showMax ? 0 : 4)   /* showMax used to be always on */
                | (showFreq ? 8 : 0)
                | (lockScale ? 16 : 0)
                | (plot2d ? 64 : 0)
                | (plotXY ? 128 : 0)
                | (showMin ? 256 : 0)
                | (showScale ? 512 : 0)
                | (_showFFT ? 1024 : 0)
                | (_maxScale ? 8192 : 0)
                | (showRMS ? 16384 : 0)
                | (showDutyCycle ? 32768 : 0)
                | (logSpectrum ? 65536 : 0);
            flags |= FLAG_PLOTS;
            return flags;
        }

        public string dump() {
            var vPlot = plots[0];

            var elm = vPlot.elm;
            if (elm == null) {
                return null;
            }
            int flags = getFlags();
            int eno = sim.locateElm(elm);
            if (eno < 0) {
                return null;
            }
            string x = "o " + eno
                + " " + vPlot.speed
                + " " + vPlot.value
                + " " + flags
                + " " + scale[UNITS_V]
                + " " + scale[UNITS_A]
                + " " + position
                + " " + plots.Count;
            for (int i = 0; i < plots.Count; i++) {
                var p = plots[i];
                if (i > 0) {
                    x += " " + sim.locateElm(p.elm) + " " + p.value;
                }
                /* dump scale if units are not V or A */
                if (p.units > UNITS_A) {
                    x += " " + scale[p.units];
                }
            }
            if (text != null) {
                x += " " + CustomLogicModel.escape(text);
            }
            return x;
        }

        public void undump(StringTokenizer st) {
            initialize();

            int e = st.nextTokenInt();
            if (e == -1) {
                return;
            }

            var ce = sim.getElm(e);
            setElm(ce);
            speed = st.nextTokenInt();
            int value = st.nextTokenInt();

            /* fix old value for VAL_POWER which doesn't work for transistors (because it's the same as VAL_IB) */
            if (!((ce is TransistorElm) && value == VAL_POWER_OLD)) {
                value = VAL_POWER;
            }

            int flags = st.nextTokenInt();
            scale[UNITS_V] = st.nextTokenDouble();
            scale[UNITS_A] = st.nextTokenDouble();

            if (scale[UNITS_V] == 0) {
                scale[UNITS_V] = .5;
            }
            if (scale[UNITS_A] == 0) {
                scale[UNITS_A] = 1;
            }
            scaleX = scale[UNITS_V];
            scaleY = scale[UNITS_A];
            scale[UNITS_OHMS] = scale[UNITS_W] = scale[UNITS_V];
            text = null;
            bool plot2dFlag = (flags & 64) != 0;
            if ((flags & FLAG_PLOTS) != 0) {
                /* new-style dump */
                try {
                    position = st.nextTokenInt();
                    int sz = st.nextTokenInt();

                    int i;
                    setValue(value);
                    /* setValue(0) creates an extra plot for current, so remove that */
                    while (plots.Count > 1) {
                        plots.RemoveAt(1);
                    }

                    int u = plots[0].units;
                    if (u > UNITS_A) {
                        scale[u] = st.nextTokenDouble();
                    }

                    for (i = 0; i != sz; i++) {
                        int ne = st.nextTokenInt();
                        int val = st.nextTokenInt();

                        var elm = sim.getElm(ne);
                        u = elm.getScopeUnits(val);
                        if (u > UNITS_A) {
                            scale[u] = st.nextTokenDouble();
                        }
                        plots.Add(new ScopePlot(elm, u, val));
                    }
                    while (st.hasMoreTokens()) {
                        if (text == null) {
                            text = st.nextToken();
                        } else {
                            text += " " + st.nextToken();
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
                    position = st.nextTokenInt();
                    int ye = -1;
                    if ((flags & FLAG_YELM) != 0) {
                        ye = st.nextTokenInt();
                        if (ye != -1) {
                            yElm = sim.getElm(ye);
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
                        if (text == null) {
                            text = st.nextToken();
                        } else {
                            text += " " + st.nextToken();
                        }
                    }
                } catch (Exception ex) {
                    throw ex;
                }
                setValues(value, ivalue, sim.getElm(e), yElm);
            }
            if (text != null) {
                text = CustomLogicModel.unescape(text);
            }
            plot2d = plot2dFlag;
            setFlags(flags);
        }

        void setFlags(int flags) {
            showI = (flags & 1) != 0;
            showV = (flags & 2) != 0;
            showMax = (flags & 4) == 0;
            showFreq = (flags & 8) != 0;
            lockScale = (flags & 16) != 0;
            plotXY = (flags & 128) != 0;
            showMin = (flags & 256) != 0;
            showScale = (flags & 512) != 0;
            showFFT((flags & 1024) != 0);
            _maxScale = (flags & 8192) != 0;
            showRMS = (flags & 16384) != 0;
            showDutyCycle = (flags & 32768) != 0;
            logSpectrum = (flags & 65536) != 0;
        }

        public void saveAsDefault() {
            var stor = Storage.getLocalStorageIfSupported();
            if (stor == null) {
                return;
            }
            var vPlot = plots[0];
            int flags = getFlags();

            /* store current scope settings as default.  1 is a version code */
            stor.setItem("scopeDefaults", "1 " + flags + " " + vPlot.speed);
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
            speed = int.Parse(arr[2]);
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
                showScale = state;
            if (mi == SCOPE_MENU.showpeak)
                showMax = state;
            if (mi == SCOPE_MENU.shownegpeak)
                showMin = state;
            if (mi == SCOPE_MENU.showfreq)
                showFreq = state;
            if (mi == SCOPE_MENU.showfft)
                showFFT(state);
            if (mi == SCOPE_MENU.logspectrum)
                logSpectrum = state;
            if (mi == SCOPE_MENU.showrms)
                showRMS = state;
            if (mi == SCOPE_MENU.showduty)
                showDutyCycle = state;
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
                plot2d = true;
                plotXY = false;
                setValues(VAL_VCE, VAL_IC, getElm(), null);
                resetGraph();
            }

            if (mi == SCOPE_MENU.showvvsi) {
                plot2d = state;
                plotXY = false;
                resetGraph();
            }

            if (mi == SCOPE_MENU.manualscale) {
                setManualScale(state);
            }

            if (mi == SCOPE_MENU.plotxy) {
                plotXY = plot2d = state;
                if (plot2d) {
                    plots = visiblePlots;
                }
                if (plot2d && plots.Count == 1) {
                    selectY();
                }
                resetGraph();
            }

            if (mi == SCOPE_MENU.showresistance) {
                setValue(VAL_R);
            }
        }

        public void selectY() {
            var yElm = (plots.Count == 2) ? plots[1].elm : null;
            int e = (yElm == null) ? -1 : sim.locateElm(yElm);
            int firstE = e;
            while (true) {
                for (e++; e < sim.elmList.Count; e++) {
                    var ce = sim.getElm(e);
                    if (((ce is OutputElm) || (ce is ProbeElm)) && !ce.Equals(plots[0].elm)) {
                        yElm = ce;
                        if (plots.Count == 1) {
                            plots.Add(new ScopePlot(yElm, UNITS_V));
                        } else {
                            plots[1].elm = yElm;
                            plots[1].units = UNITS_V;
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
            wheelDeltaY += e.Delta;
            if (wheelDeltaY > 5) {
                slowDown();
                wheelDeltaY = 0;
            }
            if (wheelDeltaY < -5) {
                speedUp();
                wheelDeltaY = 0;
            }
        }

        public CircuitElm getElm() {
            if (selectedPlot >= 0 && visiblePlots.Count > selectedPlot) {
                return visiblePlots[selectedPlot].elm;
            }
            return visiblePlots.Count > 0 ? visiblePlots[0].elm : plots[0].elm;
        }

        public bool viewingWire() {
            for (int i = 0; i != plots.Count; i++) {
                if (plots[i].elm is WireElm) {
                    return true;
                }
            }
            return false;
        }

        public CircuitElm getXElm() {
            return getElm();
        }

        public CircuitElm getYElm() {
            if (plots.Count == 2) {
                return plots[1].elm;
            }
            return null;
        }

        public bool needToRemove() {
            bool ret = true;
            bool removed = false;
            int i;
            for (i = 0; i != plots.Count; i++) {
                var plot = plots[i];
                if (sim.locateElm(plot.elm) < 0) {
                    plots.RemoveAt(i--);
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
            if (imageCanvas == null) {
                imageCanvas = new Bitmap(rect.Width, rect.Height);
                imageContext = Graphics.FromImage(imageCanvas);
            }
            clear2dView();
        }

        void clear2dView() {
            if (imageContext != null) {
                imageContext.Clear(Color.Black);
            }
            draw_ox = draw_oy = -1;
        }

        void drawCrosshairs(Graphics g) {
            if (sim.dialogIsShowing()) {
                return;
            }
            if (!rect.Contains(sim.mouseCursorX, sim.mouseCursorY)) {
                return;
            }
            if (selectedPlot < 0 && !_showFFT) {
                return;
            }
            var info = new string[4];
            int ipa = plots[0].startIndex(rect.Width);
            int ip = (sim.mouseCursorX - rect.X + ipa) & (scopePointCount - 1);
            int ct = 0;
            int maxy = (rect.Height - 1) / 2;
            int y = maxy;
            if (selectedPlot >= 0) {
                var plot = visiblePlots[selectedPlot];
                info[ct++] = plot.getUnitText(plot.maxValues[ip]);
                int maxvy = (int)(mainGridMult * (plot.maxValues[ip] - mainGridMid));
                LINE_COLOR.Color = plot.color;
                g.FillPie(LINE_COLOR.Brush, sim.mouseCursorX - 2, rect.Y + y - maxvy - 2, 5, 5, 0, 360);
            }
            if (_showFFT) {
                double maxFrequency = 1 / (sim.timeStep * speed * 2);
                info[ct++] = CircuitElm.getUnitText(maxFrequency * (sim.mouseCursorX - rect.X) / rect.Width, "Hz");
            }
            if (visiblePlots.Count > 0) {
                double t = sim.t - sim.timeStep * speed * (rect.X + rect.Width - sim.mouseCursorX);
                info[ct++] = CircuitElm.getTimeText(t);
            }
            int szw = 0, szh = 15 * ct;
            int i;
            for (i = 0; i != ct; i++) {
                int w = (int)g.MeasureString(info[i], LABEL_FONT).Width;
                if (w > szw) {
                    szw = w;
                }
            }

            LINE_COLOR.Color = CircuitElm.whiteColor;

            g.DrawLine(LINE_COLOR, sim.mouseCursorX, rect.Y, sim.mouseCursorX, rect.Y + rect.Height);
            LINE_COLOR.Color = sim.chkPrintableCheckItem.Checked ? Color.White : Color.Black;
            int bx = sim.mouseCursorX;
            if (bx < szw / 2) {
                bx = szw / 2;
            }
            g.FillRectangle(LINE_COLOR.Brush, bx - szw / 2, rect.Y - szh, szw, szh);
            LABEL_COLOR.Color = CircuitElm.whiteColor;
            for (i = 0; i != ct; i++) {
                int w = (int)g.MeasureString(info[i], LABEL_FONT).Width;
                g.DrawString(info[i], LABEL_FONT, LABEL_COLOR.Brush, bx - w / 2, rect.Y - 2 - (ct - 1 - i) * 15);
            }
        }

        void drawPlot(Graphics g, ScopePlot plot, bool drawHGridLines, bool selected) {
            if (plot.elm == null) {
                return;
            }
            int i;
            int multptr = 0;
            int x = 0;
            int maxy = (rect.Height - 1) / 2;
            int y = maxy;

            var color = (somethingSelected) ? Color.FromArgb(0xA0, 0xA0, 0xA0) : plot.color;
            if (sim.scopeSelected == -1 && plot.elm.isMouseElm()) {
                color = Color.FromArgb(0x00, 0xFF, 0xFF);
            } else if (selected) {
                color = plot.color;
            }
            int ipa = plot.startIndex(rect.Width);
            var maxV = plot.maxValues;
            var minV = plot.minValues;
            double gridMax = scale[plot.units];
            double gridMid = 0;
            if (drawHGridLines) {
                /* if we don't have overlapping scopes of different units, we can move zero around.
                 * Put it at the bottom if the scope is never negative. */
                double mx = gridMax;
                double mn = 0;
                if (_maxScale) {
                    /* scale is maxed out, so fix boundaries of scope at maximum and minimum. */
                    mx = maxValue;
                    mn = minValue;
                } else if (showNegative || minValue < (mx + mn) * .5 - (mx - mn) * .55) {
                    mn = -gridMax;
                    showNegative = true;
                }
                gridMid = (mx + mn) * .5;
                gridMax = (mx - mn) * .55;  /* leave space at top and bottom */
            }
            double gridMult = maxy / gridMax;
            if (selected) {
                mainGridMult = gridMult;
                mainGridMid = gridMid;
            }
            int minRangeLo = -10 - (int)(gridMid * gridMult);
            int minRangeHi = 10 - (int)(gridMid * gridMult);

            gridStepY = 1e-8;
            while (gridStepY < 20 * gridMax / maxy) {
                gridStepY *= multa[(multptr++) % 3];
            }

            /* Horizontal gridlines */
            int ll;
            var minorDiv = Color.FromArgb(0x30, 0x30, 0x30);
            var majorDiv = Color.FromArgb(0xA0, 0xA0, 0xA0);
            if (sim.chkPrintableCheckItem.Checked) {
                minorDiv = Color.FromArgb(0xD0, 0xD0, 0xD0);
                majorDiv = Color.FromArgb(0x80, 0x80, 0x80);
                curColor = Color.FromArgb(0xA0, 0xA0, 0x00);
            }

            /* Vertical (T) gridlines */
            double ts = sim.timeStep * speed;
            gridStepX = calcGridStepX();

            if (drawGridLines) {
                /* horizontal gridlines */

                /* don't show gridlines if lines are too close together (except for center line) */
                bool showGridLines = (gridStepY != 0) && drawHGridLines;
                for (ll = -100; ll <= 100; ll++) {
                    if (ll != 0 && !showGridLines) {
                        continue;
                    }
                    int yl = maxy - (int)((ll * gridStepY - gridMid) * gridMult);
                    if (yl < 0 || yl >= rect.Height - 1) {
                        continue;
                    }
                    LINE_COLOR.Color = ll == 0 ? majorDiv : minorDiv;
                    g.DrawLine(LINE_COLOR, 0, yl, rect.Width - 1, yl);
                }

                /* vertical gridlines */
                double tstart = sim.t - sim.timeStep * speed * rect.Width;
                double tx = sim.t - (sim.t % gridStepX);

                for (ll = 0; ; ll++) {
                    double tl = tx - gridStepX * ll;
                    int gx = (int)((tl - tstart) / ts);
                    if (gx < 0) {
                        break;
                    }
                    if (gx >= rect.Width) {
                        continue;
                    }
                    if (tl < 0) {
                        continue;
                    }
                    LINE_COLOR.Color = minorDiv;
                    if (((tl + gridStepX / 4) % (gridStepX * 10)) < gridStepX) {
                        LINE_COLOR.Color = majorDiv;
                    }
                    g.DrawLine(LINE_COLOR, gx, 0, gx, rect.Height - 1);
                }
            }

            /* only need gridlines drawn once */
            drawGridLines = false;

            LINE_COLOR.Color = color;

            int ox = -1, oy = -1;
            for (i = 0; i != rect.Width; i++) {
                int ip = (i + ipa) & (scopePointCount - 1);
                int minvy = (int)(gridMult * (minV[ip] - gridMid));
                int maxvy = (int)(gridMult * (maxV[ip] - gridMid));
                if (minvy <= maxy) {
                    if (minvy < minRangeLo || maxvy > minRangeHi) {
                        /* we got a value outside min range, so we don't need to rescale later */
                        reduceRange[plot.units] = false;
                        minRangeLo = -1000;
                        minRangeHi = 1000; /* avoid triggering this test again */
                    }
                    if (ox != -1) {
                        if (minvy == oy && maxvy == oy) {
                            continue;
                        }
                        g.DrawLine(LINE_COLOR, ox, y - oy, x + i - 1, y - oy);
                        ox = oy = -1;
                    }
                    if (minvy == maxvy) {
                        ox = x + i;
                        oy = minvy;
                        continue;
                    }
                    g.DrawLine(LINE_COLOR, x + i, y - minvy, x + i, y - maxvy - 1);
                }
            } /* for (i=0...) */
            if (ox != -1) {
                g.DrawLine(LINE_COLOR, ox, y - oy, x + i - 1, y - oy); /* Horizontal */
            }
        }

        void drawFFTVerticalGridLines(Graphics g) {
            /* Draw x-grid lines and label the frequencies in the FFT that they point to. */
            int prevEnd = 0;
            int divs = 20;
            double maxFrequency = 1 / (sim.timeStep * speed * divs * 2);
            for (int i = 0; i < divs; i++) {
                int x = rect.Width * i / divs;
                if (x < prevEnd) {
                    continue;
                }
                string s = ((int)Math.Round(i * maxFrequency)) + "Hz";
                int sWidth = (int)Math.Ceiling(g.MeasureString(s, LABEL_FONT).Width);
                prevEnd = x + sWidth + 4;
                if (i > 0) {
                    LINE_COLOR.Color = Color.FromArgb(0x88, 0x00, 0x00);
                    g.DrawLine(LINE_COLOR, x, 0, x, rect.Height);
                }
                LABEL_COLOR.Color = Color.FromArgb(0xFF, 0x00, 0x00);
                g.DrawString(s, LABEL_FONT, LABEL_COLOR.Brush, x + 2, rect.Height);
            }
        }

        void drawFFT(Graphics g) {
            if (fft == null || fft.getSize() != scopePointCount) {
                fft = new FFT(scopePointCount);
            }
            var real = new double[scopePointCount];
            var imag = new double[scopePointCount];
            var plot = (visiblePlots.Count == 0) ? plots[0] : visiblePlots[0];
            var maxV = plot.maxValues;
            var minV = plot.minValues;
            int ptr = plot.ptr;
            for (int i = 0; i < scopePointCount; i++) {
                int ii = (ptr - i + scopePointCount) & (scopePointCount - 1);
                /* need to average max and min or else it could cause average of function to be > 0, which
                /* produces spike at 0 Hz that hides rest of spectrum */
                real[i] = .5 * (maxV[ii] + minV[ii]);
                imag[i] = 0;
            }
            fft.fft(real, imag);
            double maxM = 1e-8;
            for (int i = 0; i < scopePointCount / 2; i++) {
                double m = fft.magnitude(real[i], imag[i]);
                if (m > maxM) {
                    maxM = m;
                }
            }
            int prevX = 0;
            LINE_COLOR.Color = Color.Red;
            if (!logSpectrum) {
                int prevHeight = 0;
                int y = (rect.Height - 1) - 12;
                for (int i = 0; i < scopePointCount / 2; i++) {
                    int x = 2 * i * rect.Width / scopePointCount;
                    /* rect.width may be greater than or less than scopePointCount/2,
                     * so x may be greater than or equal to prevX. */
                    double magnitude = fft.magnitude(real[i], imag[i]);
                    int height = (int)((magnitude * y) / maxM);
                    if (x != prevX) {
                        g.DrawLine(LINE_COLOR, prevX, y - prevHeight, x, y - height);
                    }
                    prevHeight = height;
                    prevX = x;
                }
            } else {
                int y0 = 5;
                int prevY = 0;
                double ymult = rect.Height / 10;
                double val0 = Math.Log(scale[plot.units]) * ymult;
                for (int i = 0; i < scopePointCount / 2; i++) {
                    int x = 2 * i * rect.Width / scopePointCount;
                    /* rect.width may be greater than or less than scopePointCount/2,
                     * so x may be greater than or equal to prevX. */
                    var mag = fft.magnitude(real[i], imag[i]);
                    if (0 == mag) {
                        mag = 1;
                    }
                    double val = Math.Log(mag);
                    int y = y0 - (int)(val * ymult - val0);
                    if (x != prevX) {
                        g.DrawLine(LINE_COLOR, prevX, prevY, x, y);
                    }
                    prevY = y;
                    prevX = x;
                }
            }
        }

        void drawSettingsWheel(Graphics g) {
            const int outR = 8 * 12 / 8;
            const int inR = 5 * 12 / 8;
            const int inR45 = 4 * 12 / 8;
            const int outR45 = 6 * 12 / 8;
            if (showSettingsWheel()) {
                Pen pen;
                if (cursorInSettingsWheel()) {
                    pen = Pens.Cyan;
                } else {
                    pen = Pens.DarkGray;
                }
                g.TranslateTransform(rect.X + 18, rect.Y + rect.Height - 18);
                g.DrawArc(pen, -inR / 2, -inR / 2, inR, inR, 0, 360);
                g.DrawLine(pen,   -outR,       0,   -inR,      0);
                g.DrawLine(pen,    outR,       0,    inR,      0);
                g.DrawLine(pen,       0,   -outR,      0,   -inR);
                g.DrawLine(pen,       0,    outR,      0,    inR);
                g.DrawLine(pen, -outR45, -outR45, -inR45, -inR45);
                g.DrawLine(pen,  outR45, -outR45,  inR45, -inR45);
                g.DrawLine(pen, -outR45,  outR45, -inR45,  inR45);
                g.DrawLine(pen,  outR45,  outR45,  inR45,  inR45);
                g.TranslateTransform(0, 0);
            }
        }

        void draw2d(Graphics g) {
            if (imageContext == null) {
                return;
            }

            alphadiv++;
            if (alphadiv > 2) {
                alphadiv = 0;
                Pen pen;
                if (sim.chkPrintableCheckItem.Checked) {
                    pen = new Pen(Color.FromArgb(0x0F, 0xFF, 0xFF, 0xFF), 1.0f);
                } else {
                    pen = new Pen(Color.FromArgb(0x0F, 0, 0, 0), 1.0f);
                }
                g.FillRectangle(pen.Brush, 0, 0, rect.Width, rect.Height);
            }

            LINE_COLOR.Color = CircuitElm.whiteColor;
            g.FillPie(LINE_COLOR.Brush, draw_ox - 2, draw_oy - 2, 5, 5, 0, 360);
            int yt = 10;
            int x = 0;
            if (text != null && rect.Height > yt + 5) {
                LABEL_COLOR.Color = CircuitElm.whiteColor;
                g.DrawString(text, LABEL_FONT, LABEL_COLOR.Brush, x, yt);
                yt += 15;
            }

            LINE_COLOR.Color = Color.Green;
            g.DrawLine(LINE_COLOR, 0, rect.Height / 2, rect.Width - 1, rect.Height / 2);
            if (!plotXY) {
                LINE_COLOR.Color = Color.Yellow;
            }
            g.DrawLine(LINE_COLOR, rect.Width / 2, 0, rect.Width / 2, rect.Height - 1);

            g.TranslateTransform(0, 0);
            drawSettingsWheel(g);
        }

        void drawTo(int x2, int y2) {
            if (draw_ox == -1) {
                draw_ox = x2;
                draw_oy = y2;
            }
            LINE_COLOR.Color = Color.GreenYellow;
            imageContext.DrawLine(LINE_COLOR, draw_ox, draw_oy, x2, y2);
            draw_ox = x2;
            draw_oy = y2;
        }

        /* calc RMS and display it */
        void drawRMS(Graphics g) {
            if (!canShowRMS()) {
                drawAverage(g);
                return;
            }
            var plot = visiblePlots[0];
            int i;
            double avg = 0;
            int ipa = plot.ptr + scopePointCount - rect.Width;
            var maxV = plot.maxValues;
            var minV = plot.minValues;
            double mid = (maxValue + minValue) / 2;
            int state = -1;

            /* skip zeroes */
            for (i = 0; i != rect.Width; i++) {
                int ip = (i + ipa) & (scopePointCount - 1);
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
            for (; i != rect.Width; i++) {
                int ip = (i + ipa) & (scopePointCount - 1);
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
                drawInfoText(g, plot.getUnitText(rms) + "rms");
            }
        }

        void drawAverage(Graphics g) {
            var plot = visiblePlots[0];
            int i;
            double avg = 0;
            int ipa = plot.ptr + scopePointCount - rect.Width;
            var maxV = plot.maxValues;
            var minV = plot.minValues;
            double mid = (maxValue + minValue) / 2;
            int state = -1;

            /* skip zeroes */
            for (i = 0; i != rect.Width; i++) {
                int ip = (i + ipa) & (scopePointCount - 1);
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
            for (; i != rect.Width; i++) {
                int ip = (i + ipa) & (scopePointCount - 1);
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
                drawInfoText(g, plot.getUnitText(avg) + " average");
            }
        }

        void drawDutyCycle(Graphics g) {
            var plot = visiblePlots[0];
            int i;
            int ipa = plot.ptr + scopePointCount - rect.Width;
            var maxV = plot.maxValues;
            var minV = plot.minValues;
            double mid = (maxValue + minValue) / 2;
            int state = -1;

            /* skip zeroes */
            for (i = 0; i != rect.Width; i++) {
                int ip = (i + ipa) & (scopePointCount - 1);
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
            for (; i != rect.Width; i++) {
                int ip = (i + ipa) & (scopePointCount - 1);
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
        void drawFrequency(Graphics g) {
            /* try to get frequency
             * get average */
            double avg = 0;
            int i;
            var plot = visiblePlots[0];
            int ipa = plot.ptr + scopePointCount - rect.Width;
            var minV = plot.minValues;
            var maxV = plot.maxValues;
            for (i = 0; i != rect.Width; i++) {
                int ip = (i + ipa) & (scopePointCount - 1);
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
            for (i = 0; i != rect.Width; i++) {
                int ip = (i + ipa) & (scopePointCount - 1);
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
            double freq = 1 / (avperiod * sim.timeStep * speed);
            /* don't show freq if standard deviation is too great */
            if (periodct < 1 || periodstd > 2) {
                freq = 0;
            }
            /* Console.WriteLine(freq + " " + periodstd + " " + periodct); */
            if (freq != 0) {
                drawInfoText(g, CircuitElm.getUnitText(freq, "Hz"));
            }
        }

        void drawInfoText(Graphics g, string text) {
            if (rect.Y + rect.Height <= textY + 5) {
                return;
            }
            g.DrawString(text, LABEL_FONT, LABEL_COLOR.Brush, 0, textY);
            textY += 15;
        }

        void drawInfoTexts(Graphics g) {
            LABEL_COLOR.Color = CircuitElm.whiteColor;
            textY = 10;
            var plot = visiblePlots[0];
            if (showScale) {
                string vScaleText = "";
                if (gridStepY != 0 && (!(showV && showI))) {
                    vScaleText = " V=" + plot.getUnitText(gridStepY) + "/div";
                }
                drawInfoText(g, "H=" + CircuitElm.getUnitText(gridStepX, "s") + "/div" + vScaleText);
            }
            if (showMax) {
                drawInfoText(g, plot.getUnitText(maxValue));
            }
            if (showMin) {
                int ym = rect.Height - 5;
                g.DrawString(plot.getUnitText(minValue), LABEL_FONT, LABEL_COLOR.Brush, 0, ym);
            }
            if (showRMS) {
                drawRMS(g);
            }
            if (showDutyCycle) {
                drawDutyCycle(g);
            }
            string t = text;
            if (t == null) {
                t = getScopeText();
            }
            if (t != null) {
                drawInfoText(g, t);
            }
            if (showFreq) {
                drawFrequency(g);
            }
        }
        #endregion
    }
}
