using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

using Circuit.Elements;

namespace Circuit {
    class Scope {
        #region CONST
        /* bunch of other flags go here, see dump() */
        const int FLAG_IVALUE = 2048; /* Flag to indicate if IVALUE is included in dump */
        const int FLAG_PLOTS = 4096;  /* new-style dump with multiple plots */
        /* other flags go here too, see dump() */

        readonly double[] MULTA = new double[] { 1.75, 2.0, 2.25, 2.0, 1.75 };

        public enum VAL {
            INVALID,
            IB, IC, IE,
            VBE, VBC, VCE,
            CURRENT
        }

        public enum UNITS {
            V, A
        }
        #endregion

        #region dynamic variable
        CirSim mSim;
        FFT mFft;

        CustomGraphics mContext;

        List<ScopePlot> mPlots;
        List<ScopePlot> mVisiblePlots;

        double mScopeTimeStep;

        double mScaleV;
        double mScaleA;
        bool mReduceRangeV;
        bool mReduceRangeA;
        int mScopePointCount;

        int mWheelDeltaY;

        int mSpeed;

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
        #endregion

        #region [public property]
        public int Position { get; set; }
        public Rectangle BoundingBox { get; private set; }
        public int RightEdge { get { return BoundingBox.X + BoundingBox.Width; } }
        public int Speed {
            get { return mSpeed; }
            set {
                if (value < 1) { value = 1; }
                if (1024 < value) { value = 1024; }
                mSpeed = value;
                ResetGraph();
            }
        }
        public int StackCount { get; set; } /* number of scopes in this column */
        public double ScaleValue {
            get {
                if (mVisiblePlots.Count == 0) {
                    return 0;
                }
                if (mVisiblePlots[0].Units == UNITS.V) {
                    return mScaleV;
                } else {
                    return mScaleA;
                }
            }
            set {
                if (mVisiblePlots.Count == 0) {
                    return;
                }
                if (mVisiblePlots[0].Units == UNITS.V) {
                    mScaleV = value;
                } else {
                    mScaleA = value;
                }
            }
        }

        public string Text { get; set; }
        public string ScaleUnitsText {
            get {
                if (mVisiblePlots.Count == 0) {
                    return "V";
                }
                switch (mVisiblePlots[0].Units) {
                case UNITS.A:
                    return "A";
                default:
                    return "V";
                }
            }
        }
        public int SelectedPlot { get; private set; }

        public bool ShowMax { get; set; }
        public bool ShowMin { get; set; }
        public bool ShowI { get; private set; }
        public bool ShowV { get; private set; }
        public bool ShowScale { get; private set; }
        public bool ShowFreq { get; private set; }
        public bool LockScale { get; private set; }
        public bool ShowFFT { get; private set; }
        public bool LogSpectrum { get; private set; }
        public bool ShowRMS { get; private set; }

        /* get scope element, returning null if there's more than one */
        public CircuitElm SingleElm {
            get {
                var elm = mPlots[0].Elm;
                for (int i = 1; i < mPlots.Count; i++) {
                    if (!mPlots[i].Elm.Equals(elm)) {
                        return null;
                    }
                }
                return elm;
            }
        }
        public CircuitElm Elm {
            get {
                if (0 <= SelectedPlot && SelectedPlot < mVisiblePlots.Count) {
                    return mVisiblePlots[SelectedPlot].Elm;
                }
                return 0 < mVisiblePlots.Count ? mVisiblePlots[0].Elm : mPlots[0].Elm;
            }
        }

        public bool CanMenu {
            get { return mPlots[0].Elm != null; }
        }
        public bool ViewingWire {
            get {
                foreach (var plot in mPlots) {
                    if (plot.Elm is WireElm) {
                        return true;
                    }
                }
                return false;
            }
        }
        public bool NeedToRemove {
            get {
                bool ret = true;
                bool removed = false;
                for (int i = 0; i != mPlots.Count; i++) {
                    var plot = mPlots[i];
                    if (mSim.LocateElm(plot.Elm) < 0) {
                        mPlots.RemoveAt(i--);
                        removed = true;
                    } else {
                        ret = false;
                    }
                }
                if (removed) {
                    _calcVisiblePlots();
                }
                return ret;
            }
        }
        public bool CursorInSettingsWheel {
            get {
                return mShowSettingsWheel
                    && BoundingBox.X <= mSim.mouseCursorX
                    && BoundingBox.Y + BoundingBox.Height - 24 <= mSim.mouseCursorY
                    && mSim.mouseCursorX <= BoundingBox.X + 24
                    && mSim.mouseCursorY <= BoundingBox.Y + BoundingBox.Height;
            }
        }
        #endregion

        #region [private property]
        bool active {
            get {
                return 0 < mPlots.Count && mPlots[0].Elm != null;
            }
        }
        bool mShowCurrent {
            set {
                ShowI = value;
                if (ShowI && !mShowingVoltageAndMaybeCurrent) {
                    _setValue(0);
                }
                _calcVisiblePlots();
            }
        }
        bool mShowVoltage {
            set {
                ShowV = value;
                if (ShowV && !mShowingVoltageAndMaybeCurrent) {
                    _setValue(0);
                }
                _calcVisiblePlots();
            }
        }
        bool mShowFFT {
            set {
                ShowFFT = value;
                if (!ShowFFT) {
                    mFft = null;
                }
            }
        }

        /* returns true if we have a plot of voltage and nothing else (except current).
        /* The default case is a plot of voltage and current, so we're basically checking if that case is true. */
        bool mShowingVoltageAndMaybeCurrent {
            get {
                bool gotv = false;
                foreach (var plot in mPlots) {
                    if (plot.Value == 0) {
                        gotv = true;
                    } else if (plot.Value != VAL.CURRENT) {
                        return false;
                    }
                }
                return gotv;
            }
        }
        bool mShowSettingsWheel {
            get {
                return 50 < BoundingBox.Height && 50 < BoundingBox.Width;
            }
        }

        string mScopeText {
            get {
                /* stacked scopes?  don't show text */
                if (StackCount != 1) {
                    return null;
                }
                /* multiple elms?  don't show text (unless one is selected) */
                if (SelectedPlot < 0 && SingleElm == null) {
                    return null;
                }
                var plot = mVisiblePlots[0];
                if (0 <= SelectedPlot && SelectedPlot < mVisiblePlots.Count) {
                    plot = mVisiblePlots[SelectedPlot];
                }
                if (plot.Elm == null) {
                    return "";
                } else {
                    return plot.Elm.GetScopeText(plot.Value);
                }
            }
        }
        int mFlags {
            set {
                ShowI = (value & 1) != 0;
                ShowV = (value & 2) != 0;
                ShowMax = (value & 4) == 0;
                ShowFreq = (value & 8) != 0;
                LockScale = (value & 16) != 0;
                ShowMin = (value & 256) != 0;
                ShowScale = (value & 512) != 0;
                mShowFFT = (value & 1024) != 0;
                mMaxScale = (value & 8192) != 0;
                ShowRMS = (value & 16384) != 0;
                LogSpectrum = (value & 65536) != 0;
            }
            get {
                int flags
                    = (ShowI ? 1 : 0)
                    | (ShowV ? 2 : 0)
                    | (ShowMax ? 0 : 4)   /* showMax used to be always on */
                    | (ShowFreq ? 8 : 0)
                    | (LockScale ? 16 : 0)
                    | (ShowMin ? 256 : 0)
                    | (ShowScale ? 512 : 0)
                    | (ShowFFT ? 1024 : 0)
                    | (mMaxScale ? 8192 : 0)
                    | (ShowRMS ? 16384 : 0)
                    | (LogSpectrum ? 65536 : 0);
                return flags | FLAG_PLOTS;
            }
        }
        #endregion

        public Scope(CirSim s) {
            mSim = s;

            BoundingBox = new Rectangle(0, 0, 1, 1);

            _allocImage();
            _initialize();
        }

        public void onMouseWheel(MouseEventArgs e) {
            mWheelDeltaY += e.Delta;
            if (mWheelDeltaY > 5) {
                SlowDown();
                mWheelDeltaY = 0;
            }
            if (mWheelDeltaY < -5) {
                SpeedUp();
                mWheelDeltaY = 0;
            }
        }

        #region [public method]
        public void ResetGraph() { ResetGraph(false); }

        public void ResetGraph(bool full) {
            mScopePointCount = 1;
            while (mScopePointCount <= BoundingBox.Width) {
                mScopePointCount *= 2;
            }
            if (mPlots == null) {
                mPlots = new List<ScopePlot>();
            }
            mShowNegative = false;
            for (int i = 0; i != mPlots.Count; i++) {
                mPlots[i].Reset(mScopePointCount, Speed, full);
            }
            _calcVisiblePlots();
            mScopeTimeStep = mSim.timeStep;
            _allocImage();
        }

        public void SetRect(Rectangle r) {
            int w = BoundingBox.Width;
            BoundingBox = r;
            if (BoundingBox.Width != w) {
                ResetGraph();
            }
        }

        public void SetElm(CircuitElm ce) {
            mPlots = new List<ScopePlot>();
            if (null != ce && (ce is TransistorElm)) {
                _setValue(VAL.VCE, ce);
            } else {
                _setValue(0, ce);
            }
            _initialize();
        }

        public bool ShowingValue(VAL v) {
            foreach (var sp in mPlots) {
                if (sp.Value != v) {
                    return false;
                }
            }
            return true;
        }

        public void Combine(Scope s) {
            mPlots = mVisiblePlots;
            mPlots.AddRange(s.mVisiblePlots);
            s.mPlots.Clear();
            _calcVisiblePlots();
        }

        /* separate this scope's plots into separate scopes and return them in arr[pos], arr[pos+1], etc.
         * return new length of array. */
        public int Separate(List<Scope> arr, int pos) {
            ScopePlot lastPlot = null;
            for (int i = 0; i != mVisiblePlots.Count; i++) {
                if (pos >= arr.Count) {
                    return pos;
                }
                var s = new Scope(mSim);
                var sp = mVisiblePlots[i];
                if (lastPlot != null && lastPlot.Elm == sp.Elm && lastPlot.Value == 0 && sp.Value == VAL.CURRENT) {
                    continue;
                }
                s._setValue(sp.Value, sp.Elm);
                s.Position = pos;
                arr[pos++] = s;
                lastPlot = sp;
                s.mFlags = mFlags;
                s.Speed = Speed;
            }
            return pos;
        }

        public void RemovePlot(int plot) {
            if (plot < mVisiblePlots.Count) {
                var p = mVisiblePlots[plot];
                mPlots.Remove(p);
                _calcVisiblePlots();
            }
        }

        /* called for each timestep */
        public void TimeStep() {
            for (int i = 0; i != mPlots.Count; i++) {
                mPlots[i].TimeStep();
            }
        }

        public void MaxScale() {
            /* toggle max scale.  This isn't on by default because, for the examples, we sometimes want two plots
             * matched to the same scale so we can show one is larger.  Also, for some fast-moving scopes
             * (like for AM detector), the amplitude varies over time but you can't see that if the scale is
             * constantly adjusting.  It's also nice to set the default scale to hide noise and to avoid
             * having the scale moving around a lot when a circuit starts up. */
            mMaxScale = !mMaxScale;
            mShowNegative = false;
        }

        public void Properties(Form parent) {
            var fm = new ScopePropertiesDialog(this);
            fm.Show(parent);
            CirSim.dialogShowing = fm;
        }

        public void SpeedUp() {
            if (1 < Speed) {
                Speed /= 2;
                ResetGraph();
            }
        }

        public void SlowDown() {
            if (Speed < 1024) {
                Speed *= 2;
            }
            ResetGraph();
        }

        public string Dump() {
            var vPlot = mPlots[0];

            var elm = vPlot.Elm;
            if (elm == null) {
                return null;
            }
            var flags = mFlags;
            var eno = mSim.LocateElm(elm);
            if (eno < 0) {
                return null;
            }
            string x = "o " + eno
                + " " + vPlot.Speed
                + " " + vPlot.Value
                + " " + flags
                + " " + mScaleV
                + " " + mScaleA
                + " " + Position
                + " " + mPlots.Count;
            for (int i = 0; i < mPlots.Count; i++) {
                var p = mPlots[i];
                x += " " + mSim.LocateElm(p.Elm) + " " + p.Value;
            }
            if (Text != null) {
                x += " " + CustomLogicModel.escape(Text);
            }
            return x;
        }

        public void Undump(StringTokenizer st) {
            _initialize();

            int e = st.nextTokenInt();
            if (e == -1) {
                return;
            }

            var ce = mSim.getElm(e);
            SetElm(ce);
            Speed = st.nextTokenInt();
            var value = st.nextTokenEnum<VAL>();

            var flags = st.nextTokenInt();
            mScaleV = st.nextTokenDouble();
            mScaleA = st.nextTokenDouble();

            if (mScaleV == 0) {
                mScaleV = 0.5;
            }
            if (mScaleA == 0) {
                mScaleA = 1;
            }
            Text = null;
            if ((flags & FLAG_PLOTS) != 0) {
                try {
                    Position = st.nextTokenInt();
                    int sz = st.nextTokenInt();

                    _setValue(value);
                    /* setValue(0) creates an extra plot for current, so remove that */
                    while (1 < mPlots.Count) {
                        mPlots.RemoveAt(1);
                    }

                    for (int i = 0; i != sz; i++) {
                        var eleNum = st.nextTokenInt();
                        var val = st.nextTokenEnum<VAL>();
                        var elm = mSim.getElm(eleNum);
                        var u = elm.GetScopeUnits(val);
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
            }
            if (Text != null) {
                Text = CustomLogicModel.unescape(Text);
            }
            mFlags = flags;
        }

        public double CalcGridStepX() {
            int multptr = 0;
            var gsx = 1e-15;
            var ts = mSim.timeStep * Speed;
            while (gsx < ts * 20) {
                gsx *= MULTA[(multptr++) % 5];
            }
            return gsx;
        }

        public void SaveAsDefault() {
            var stor = Storage.getLocalStorageIfSupported();
            if (stor == null) {
                return;
            }
            var vPlot = mPlots[0];
            var flags = mFlags;
            /* store current scope settings as default.  1 is a version code */
            stor.setItem("scopeDefaults", "1 " + flags + " " + vPlot.Speed);
            Console.WriteLine("saved defaults " + flags);
        }

        public void HandleMenu(SCOPE_MENU mi, bool state) {
            switch (mi) {

            case SCOPE_MENU.MAX_SCALE:
                MaxScale();
                break;
            case SCOPE_MENU.MANUAL_SCALE:
                LockScale = state;
                break;

            case SCOPE_MENU.SHOW_VOLTAGE:
                mShowVoltage = state;
                break;
            case SCOPE_MENU.SHOW_CURRENT:
                mShowCurrent = state;
                break;
            case SCOPE_MENU.SHOW_SCALE:
                ShowScale = state;
                break;
            case SCOPE_MENU.SHOW_PEAK:
                ShowMax = state;
                break;
            case SCOPE_MENU.SHOW_NEG_PEAK:
                ShowMin = state;
                break;
            case SCOPE_MENU.SHOW_FREQ:
                ShowFreq = state;
                break;
            case SCOPE_MENU.SHOW_FFT:
                mShowFFT = state;
                break;
            case SCOPE_MENU.LOG_SPECTRUM:
                LogSpectrum = state;
                break;
            case SCOPE_MENU.SHOW_RMS:
                ShowRMS = state;
                break;

            case SCOPE_MENU.SHOW_IB:
                _setValue(VAL.IB);
                break;
            case SCOPE_MENU.SHOW_IC:
                _setValue(VAL.IC);
                break;
            case SCOPE_MENU.SHOW_IE:
                _setValue(VAL.IE);
                break;
            case SCOPE_MENU.SHOW_VBE:
                _setValue(VAL.VBE);
                break;
            case SCOPE_MENU.SHOW_VBC:
                _setValue(VAL.VBC);
                break;
            case SCOPE_MENU.SHOW_VCE:
                _setValue(VAL.VCE);
                break;
            }
        }

        public void Draw(CustomGraphics g) {
            if (mPlots.Count == 0) {
                return;
            }

            /* reset if timestep changed */
            if (mScopeTimeStep != mSim.timeStep) {
                mScopeTimeStep = mSim.timeStep;
                ResetGraph();
            }

            _drawSettingsWheel(g);

            g.LineColor = Color.Red;

            g.SetTransform(new Matrix(1, 0, 0, 1, BoundingBox.X, BoundingBox.Y));

            if (ShowFFT) {
                _drawFFTVerticalGridLines(g);
                _drawFFT(g);
            }

            if (mMaxScale) {
                mScaleV = 1e-4;
                mScaleA = 1e-4;
            }
            mReduceRangeV = false;
            mReduceRangeA = false;

            mSomethingSelected = false;  /* is one of our plots selected? */

            for (int si = 0; si != mVisiblePlots.Count; si++) {
                var plot = mVisiblePlots[si];
                _calcPlotScale(plot);
                if (mSim.scopeSelected == -1 && plot.Elm != null && plot.Elm.IsMouseElm) {
                    mSomethingSelected = true;
                }
                if (plot.Units == UNITS.V) {
                    mReduceRangeV = true;
                } else {
                    mReduceRangeA = true;
                }
            }

            _checkForSelection();
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
                _calcMaxAndMin(mVisiblePlots[0].Units);
            }

            /* draw volts on top (last), then current underneath, then everything else */
            for (int i = 0; i != mVisiblePlots.Count; i++) {
                if (mVisiblePlots[i].Units > UNITS.A && i != SelectedPlot) {
                    _drawPlot(g, mVisiblePlots[i], hGridLines, false);
                }
            }
            for (int i = 0; i != mVisiblePlots.Count; i++) {
                if (mVisiblePlots[i].Units == UNITS.A && i != SelectedPlot) {
                    _drawPlot(g, mVisiblePlots[i], hGridLines, false);
                }
            }
            for (int i = 0; i != mVisiblePlots.Count; i++) {
                if (mVisiblePlots[i].Units == UNITS.V && i != SelectedPlot) {
                    _drawPlot(g, mVisiblePlots[i], hGridLines, false);
                }
            }
            /* draw selection on top.  only works if selection chosen from scope */
            if (SelectedPlot >= 0 && SelectedPlot < mVisiblePlots.Count) {
                _drawPlot(g, mVisiblePlots[SelectedPlot], hGridLines, true);
            }

            if (mVisiblePlots.Count > 0) {
                _drawInfoTexts(g);
            }

            g.ClearTransform();
            _drawCrosshairs(g);

            g.SetTransform(new Matrix(
                mSim.transform[0], mSim.transform[1],
                mSim.transform[2], mSim.transform[3],
                mSim.transform[4], mSim.transform[5]
            ));

            if (5 < mPlots[0].Pointer && !LockScale) {
                if (1e-4 < mScaleV && mReduceRangeV) {
                    mScaleV /= 2;
                }
                if (1e-4 < mScaleV && mReduceRangeA) {
                    mScaleA /= 2;
                }
            }
        }
        #endregion

        #region [private method]
        void _initialize() {
            ResetGraph();
            mScaleV = 5;
            mScaleV = 0.1;
            Speed = 64;
            ShowMax = true;
            ShowV = ShowI = false;
            ShowScale = ShowFreq = LockScale = ShowMin = false;
            ShowFFT = false;
            if (!_loadDefaults()) {
                /* set showV and showI appropriately depending on what plots are present */
                int i;
                for (i = 0; i != mPlots.Count; i++) {
                    var plot = mPlots[i];
                    if (plot.Units == UNITS.V) {
                        ShowV = true;
                    }
                    if (plot.Units == UNITS.A) {
                        ShowI = true;
                    }
                }
            }
        }

        void _setValue(VAL val) {
            if (mPlots.Count > 2 || mPlots.Count == 0) {
                return;
            }
            var ce = mPlots[0].Elm;
            if (mPlots.Count == 2 && !mPlots[1].Elm.Equals(ce)) {
                return;
            }
            _setValue(val, ce);
        }

        void _setValue(VAL val, CircuitElm ce) {
            mPlots = new List<ScopePlot>();
            if (val == VAL.INVALID) {
                mPlots.Add(new ScopePlot(ce, UNITS.V, 0));
                /* create plot for current if applicable */
                if (ce != null && !((ce is OutputElm) || (ce is LogicOutputElm) || (ce is AudioOutputElm) || (ce is ProbeElm))) {
                    mPlots.Add(new ScopePlot(ce, UNITS.A, VAL.CURRENT));
                }
            } else {
                var u = ce.GetScopeUnits(val);
                mPlots.Add(new ScopePlot(ce, u, val));
                if (u == UNITS.V) {
                    ShowV = true;
                }
                if (u == UNITS.A) {
                    ShowI = true;
                }
            }
            _calcVisiblePlots();
            ResetGraph();
        }

        void _calcVisiblePlots() {
            mVisiblePlots = new List<ScopePlot>();
            int vc = 0;
            int ac = 0;
            int oc = 0;
            for (int i = 0; i != mPlots.Count; i++) {
                var plot = mPlots[i];
                if (plot.Units == UNITS.V) {
                    if (ShowV) {
                        mVisiblePlots.Add(plot);
                        plot.AssignColor(vc++);
                    }
                } else if (plot.Units == UNITS.A) {
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

        /* calculate maximum and minimum values for all plots of given units */
        void _calcMaxAndMin(UNITS units) {
            mMaxValue = -1e8;
            mMinValue = 1e8;
            for (int si = 0; si != mVisiblePlots.Count; si++) {
                var plot = mVisiblePlots[si];
                if (plot.Units != units) {
                    continue;
                }
                int ipa = plot.StartIndex(BoundingBox.Width);
                var maxV = plot.MaxValues;
                var minV = plot.MinValues;
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

        /* adjust scale of a plot */
        void _calcPlotScale(ScopePlot plot) {
            if (LockScale) {
                return;
            }
            int ipa = plot.StartIndex(BoundingBox.Width);
            var maxV = plot.MaxValues;
            var minV = plot.MinValues;
            double max = 0;
            double gridMax = (plot.Units == UNITS.V ? mScaleV : mScaleA);
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
            if (mMaxScale) {
                gridMax = Math.Max(max, gridMax);
            } else {
                /* adjust in powers of two */
                while (gridMax < max) {
                    gridMax *= 2;
                }
            }
            if (plot.Units == UNITS.V) {
                mScaleV = gridMax;
            } else {
                mScaleA = gridMax;
            }
        }

        /* find selected plot */
        void _checkForSelection() {
            if (mSim.DialogIsShowing()) {
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
                var scale = plot.Units == UNITS.V ? mScaleV : mScaleA;
                int maxvy = (int)(maxy / scale * plot.MaxValues[ip]);
                int dist = Math.Abs(mSim.mouseCursorY - (BoundingBox.Y + y - maxvy));
                if (dist < bestdist) {
                    bestdist = dist;
                    best = i;
                }
            }
            SelectedPlot = best;
        }

        bool _loadDefaults() {
            var stor = Storage.getLocalStorageIfSupported();
            if (stor == null) {
                return false;
            }
            string str = stor.getItem("scopeDefaults");
            if (string.IsNullOrEmpty(str)) {
                return false;
            }
            var arr = str.Split(' ');
            mFlags = int.Parse(arr[1]);
            Speed = int.Parse(arr[2]);
            return true;
        }
        #endregion

        #region Draw Utils
        void _allocImage() {
            if (mContext == null) {
                mContext = CustomGraphics.FromImage(BoundingBox.Width, BoundingBox.Height);
            }
        }

        void _drawCrosshairs(CustomGraphics g) {
            if (mSim.DialogIsShowing()) {
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

            g.LineColor = mSim.chkPrintable.Checked ? Color.White : Color.Black;
            g.FillRectangle(bx - szw / 2, BoundingBox.Y - szh, szw, szh);

            g.TextColor = CircuitElm.TextColor;
            for (int i = 0; i != ct; i++) {
                int w = (int)g.GetTextSize(info[i]).Width;
                g.DrawLeftText(info[i], bx - w / 2, BoundingBox.Y - 2 - (ct - 1 - i) * 15);
            }
        }

        void _drawPlot(CustomGraphics g, ScopePlot plot, bool drawHGridLines, bool selected) {
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
            var ipa = plot.StartIndex(BoundingBox.Width);
            var maxV = plot.MaxValues;
            var minV = plot.MinValues;
            var gridMax = (plot.Units == UNITS.V ? mScaleV : mScaleA);
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
            if (mSim.chkPrintable.Checked) {
                minorDiv = Color.FromArgb(0xD0, 0xD0, 0xD0);
                majorDiv = Color.FromArgb(0x80, 0x80, 0x80);
            }

            /* Vertical (T) gridlines */
            double ts = mSim.timeStep * Speed;
            mGridStepX = CalcGridStepX();

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
                        if (plot.Units == UNITS.V) {
                            mReduceRangeV = false;
                        } else {
                            mReduceRangeA = false;
                        }
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

        void _drawFFTVerticalGridLines(CustomGraphics g) {
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
                g.DrawLeftText(s, x + 2, BoundingBox.Height - 12);
            }
        }

        void _drawFFT(CustomGraphics g) {
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
                double val0 = Math.Log(plot.Units == UNITS.V ? mScaleV : mScaleA) * ymult;
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

        void _drawSettingsWheel(CustomGraphics g) {
            const int outR = 8 * 18 / 16;
            const int inR = 6 * 18 / 16;
            const int inR45 = 4 * 18 / 16;
            const int outR45 = 6 * 18 / 16;
            if (mShowSettingsWheel) {
                if (CursorInSettingsWheel) {
                    g.LineColor = Color.Cyan;
                } else {
                    g.LineColor = Color.DarkGray;
                }
                g.SetTransform(new Matrix(1, 0, 0, 1, BoundingBox.X + 12, BoundingBox.Y + BoundingBox.Height - 16));
                g.DrawCircle(0, 0, inR);
                g.DrawLine(-outR, 0, -inR, 0);
                g.DrawLine(outR, 0, inR, 0);
                g.DrawLine(0, -outR, 0, -inR);
                g.DrawLine(0, outR, 0, inR);
                g.DrawLine(-outR45, -outR45, -inR45, -inR45);
                g.DrawLine(outR45, -outR45, inR45, -inR45);
                g.DrawLine(-outR45, outR45, -inR45, inR45);
                g.DrawLine(outR45, outR45, inR45, inR45);
                g.ClearTransform();
            }
        }

        /* calc RMS and display it */
        void _drawRMS(CustomGraphics g) {
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
                _drawInfoText(g, plot.GetUnitText(rms) + "rms");
            }
        }

        /* calc frequency if possible and display it */
        void _drawFrequency(CustomGraphics g) {
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
                _drawInfoText(g, Utils.UnitText(freq, "Hz"));
            }
        }

        void _drawInfoText(CustomGraphics g, string text) {
            if (BoundingBox.Y + BoundingBox.Height <= mTextY + 5) {
                return;
            }
            g.DrawLeftText(text, 0, mTextY);
            mTextY += 15;
        }

        void _drawInfoTexts(CustomGraphics g) {
            g.TextColor = CircuitElm.TextColor;
            mTextY = 10;
            var plot = mVisiblePlots[0];
            if (ShowScale) {
                string vScaleText = "";
                if (mGridStepY != 0 && (!(ShowV && ShowI))) {
                    vScaleText = " V=" + plot.GetUnitText(mGridStepY) + "/div";
                }
                _drawInfoText(g, "H=" + Utils.UnitText(mGridStepX, "s") + "/div" + vScaleText);
            }
            if (ShowMax) {
                _drawInfoText(g, plot.GetUnitText(mMaxValue));
            }
            if (ShowMin) {
                int ym = BoundingBox.Height - 5;
                g.DrawLeftText(plot.GetUnitText(mMinValue), 0, ym);
            }
            if (ShowRMS) {
                _drawRMS(g);
            }
            string t = Text;
            if (t == null) {
                t = mScopeText;
            }
            if (t != null) {
                _drawInfoText(g, t);
            }
            if (ShowFreq) {
                _drawFrequency(g);
            }
        }
        #endregion
    }
}
