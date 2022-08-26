using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

using Circuit.Elements;
using Circuit.Elements.Passive;
using Circuit.Elements.Active;
using Circuit.Forms;

namespace Circuit {
    public class Scope {
        #region CONST
        const int FLAG_PLOTS = 4096;
        const double Mindb = -100.0;

        readonly double[] MULTA = new double[] { 1.5, 2.0, 1.5 };
        #endregion

        #region dynamic variable
        FFT mFft;
        Rectangle mFFTBoundingBox;

        CustomGraphics mContext;

        List<ScopePlot> mPlots;
        List<ScopePlot> mVisiblePlots;

        double mScopeTimeStep;

        double mScale;
        bool mReduceRange;
        int mScopePointCount;

        int mSpeed;

        double mGridStepX;
        double mGridStepY;
        double mMaxValue;
        double mMinValue;
        double mMainGridMult;
        double mMainGridMid;

        bool mDrawGridLines;
        bool mSomethingSelected;
        bool mMaxScale;
        bool mShowNegative;
        bool mShowV;
        bool mShowFFT;
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
                return mScale;
            }
            set {
                if (mVisiblePlots.Count == 0) {
                    return;
                }
                mScale = Math.Max(1e-4, value);
            }
        }
        public string Text { get; set; }
        public int SelectedPlot { get; private set; }
        public bool ShowMax { get; set; }
        public bool ShowMin { get; set; }
        public bool ShowScale { get; set; }
        public bool ShowFreq { get; set; }
        public bool ManualScale { get; set; }
        public bool LogSpectrum { get; set; }
        public bool ShowRMS { get; set; }
        public bool ShowVoltage {
            get { return mShowV; }
            set {
                mShowV = value;
                if (mShowV) {
                    setValue();
                }
                calcVisiblePlots();
            }
        }
        public bool ShowFFT {
            get { return mShowFFT; }
            set {
                mShowFFT = value;
                if (!mShowFFT) {
                    mFft = null;
                }
            }
        }

        /* get scope element, returning null if there's more than one */
        public BaseUI SingleElm {
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
        public BaseUI Elm {
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
                    if (plot.Elm is WireUI) {
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
                    if (CirSimForm.Sim.LocateElm(plot.Elm) < 0) {
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
        }
        public bool CursorInSettingsWheel {
            get {
                return mShowSettingsWheel
                    && BoundingBox.X <= CirSimForm.Sim.MouseCursorX
                    && BoundingBox.Y + BoundingBox.Height - 24 <= CirSimForm.Sim.MouseCursorY
                    && CirSimForm.Sim.MouseCursorX <= BoundingBox.X + 24
                    && CirSimForm.Sim.MouseCursorY <= BoundingBox.Y + BoundingBox.Height;
            }
        }
        #endregion

        #region [private property]
        bool active {
            get {
                return 0 < mPlots.Count && mPlots[0].Elm != null;
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
                    return plot.Elm.GetScopeText();
                }
            }
        }
        int mFlags {
            set {
                mShowV = (value & 2) != 0;
                ShowMax = (value & 4) == 0;
                ShowFreq = (value & 8) != 0;
                ManualScale = (value & 16) != 0;
                ShowMin = (value & 256) != 0;
                ShowScale = (value & 512) != 0;
                ShowFFT = (value & 1024) != 0;
                mMaxScale = (value & 8192) != 0;
                ShowRMS = (value & 16384) != 0;
                LogSpectrum = (value & 65536) != 0;
            }
            get {
                int flags
                    = (mShowV ? 2 : 0)
                    | (ShowMax ? 0 : 4)   /* showMax used to be always on */
                    | (ShowFreq ? 8 : 0)
                    | (ManualScale ? 16 : 0)
                    | (ShowMin ? 256 : 0)
                    | (ShowScale ? 512 : 0)
                    | (mShowFFT ? 1024 : 0)
                    | (mMaxScale ? 8192 : 0)
                    | (ShowRMS ? 16384 : 0)
                    | (LogSpectrum ? 65536 : 0);
                return flags | FLAG_PLOTS;
            }
        }
        #endregion

        public Scope() {
            BoundingBox = new Rectangle(0, 0, 1, 1);
            allocImage();
            initialize();
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
            calcVisiblePlots();
            mScopeTimeStep = ControlPanel.TimeStep;
            allocImage();
        }

        public void SetRect(Rectangle r) {
            int w = BoundingBox.Width;
            BoundingBox = r;
            if (BoundingBox.Width != w) {
                ResetGraph();
            }
            mFFTBoundingBox = new Rectangle(40, 0, r.Width - 40, r.Height - 16);
        }

        public void SetElm(BaseUI ce) {
            mPlots = new List<ScopePlot>();
            setValue(ce);
            initialize();
        }

        public void Combine(Scope s) {
            mPlots = mVisiblePlots;
            mPlots.AddRange(s.mVisiblePlots);
            s.mPlots.Clear();
            calcVisiblePlots();
        }

        /* separate this scope's plots into separate scopes and return them in arr[pos], arr[pos+1], etc.
         * return new length of array. */
        public int Separate(List<Scope> arr, int pos) {
            for (int i = 0; i != mVisiblePlots.Count; i++) {
                if (pos >= arr.Count) {
                    return pos;
                }
                var s = new Scope();
                var sp = mVisiblePlots[i];
                s.setValue(sp.Elm);
                s.Position = pos;
                arr[pos++] = s;
                s.mFlags = mFlags;
                s.Speed = Speed;
            }
            return pos;
        }

        public void RemovePlot(int plot) {
            if (plot < mVisiblePlots.Count) {
                var p = mVisiblePlots[plot];
                mPlots.Remove(p);
                calcVisiblePlots();
            }
        }

        public void TimeStep() {
            for (int i = 0; i != mPlots.Count; i++) {
                mPlots[i].TimeStep();
            }
        }

        public void MaxScale() {
            mMaxScale = !mMaxScale;
            mShowNegative = false;
        }

        public void Properties(Form parent) {
            var fm = new ScopeProperties(this);
            fm.Show(parent);
            CirSimForm.DialogShowing = fm;
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
            var eno = CirSimForm.Sim.LocateElm(elm);
            if (eno < 0) {
                return null;
            }
            string x = "o " + eno
                + " " + vPlot.Speed
                + " "
                + " " + flags
                + " " + mScale
                + " "
                + " " + Position
                + " " + mPlots.Count;
            for (int i = 0; i < mPlots.Count; i++) {
                var p = mPlots[i];
                x += " " + CirSimForm.Sim.LocateElm(p.Elm) + " ";
            }
            if (Text != null) {
                x += " " + Utils.Escape(Text);
            }
            return x;
        }

        public void Undump(StringTokenizer st) {
            initialize();

            int e = st.nextTokenInt();
            if (e == -1) {
                return;
            }

            var ce = CirSimForm.Sim.GetElm(e);
            SetElm(ce);
            Speed = st.nextTokenInt();
            var value = st.nextTokenEnum<string>();

            var flags = st.nextTokenInt();
            mScale = st.nextTokenDouble();
            st.nextTokenDouble();

            if (mScale == 0) {
                mScale = 0.5;
            }
            Text = null;
            if ((flags & FLAG_PLOTS) != 0) {
                try {
                    Position = st.nextTokenInt();
                    int sz = st.nextTokenInt();

                    setValue();
                    /* setValue(0) creates an extra plot for current, so remove that */
                    while (1 < mPlots.Count) {
                        mPlots.RemoveAt(1);
                    }

                    for (int i = 0; i != sz; i++) {
                        var eleNum = st.nextTokenInt();
                        var val = st.nextTokenEnum<string>();
                        var elm = CirSimForm.Sim.GetElm(eleNum);
                        mPlots.Add(new ScopePlot(elm));
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
                Text = Utils.Unescape(Text);
            }
            mFlags = flags;
        }

        public double CalcGridStepX() {
            int multptr = 0;
            var gsx = 1e-15;
            var ts = ControlPanel.TimeStep * Speed;
            while (gsx < ts * 20) {
                gsx *= MULTA[(multptr++) % 3];
            }
            return gsx;
        }

        public void Draw(CustomGraphics g) {
            if (mPlots.Count == 0) {
                return;
            }

            /* reset if timestep changed */
            if (mScopeTimeStep != ControlPanel.TimeStep) {
                mScopeTimeStep = ControlPanel.TimeStep;
                ResetGraph();
            }

            drawSettingsWheel(g);

            if (mMaxScale) {
                mScale = 1e-4;
            }
            mReduceRange = false;
            mSomethingSelected = false;  /* is one of our plots selected? */

            for (int si = 0; si != mVisiblePlots.Count; si++) {
                var plot = mVisiblePlots[si];
                calcPlotScale(plot);
                if (CirSimForm.Sim.ScopeSelected == -1 && plot.Elm != null && plot.Elm.IsMouseElm) {
                    mSomethingSelected = true;
                }
                mReduceRange = true;
            }

            checkForSelection();
            if (SelectedPlot >= 0) {
                mSomethingSelected = true;
            }

            mDrawGridLines = true;
            if ((ShowMax || ShowMin) && mVisiblePlots.Count > 0) {
                calcMaxAndMin();
            }

            g.SetTransform(new Matrix(1, 0, 0, 1, BoundingBox.X, BoundingBox.Y));
            {
                if (mShowFFT) {
                    drawFFTGridLines(g);
                    drawFFT(g);
                }
                if (mShowV) {
                    /* draw volts on top (last), then current underneath, then everything else */
                    for (int i = 0; i != mVisiblePlots.Count; i++) {
                        if (i != SelectedPlot) {
                            drawPlot(g, mVisiblePlots[i], false);
                        }
                    }
                    /* draw selection on top.  only works if selection chosen from scope */
                    if (SelectedPlot >= 0 && SelectedPlot < mVisiblePlots.Count) {
                        drawPlot(g, mVisiblePlots[SelectedPlot], true);
                    }
                }
                if (mVisiblePlots.Count > 0) {
                    drawInfoTexts(g);
                }
            }
            g.ClearTransform();
            drawCrosshairs(g);

            g.SetTransform(new Matrix(
                CirSimForm.Sim.Transform[0], CirSimForm.Sim.Transform[1],
                CirSimForm.Sim.Transform[2], CirSimForm.Sim.Transform[3],
                CirSimForm.Sim.Transform[4], CirSimForm.Sim.Transform[5]
            ));

            if (5 < mPlots[0].Pointer && !ManualScale) {
                if (1e-4 < mScale && mReduceRange) {
                    mScale /= 2;
                }
            }
        }
        #endregion

        #region [private method]
        void initialize() {
            ResetGraph();
            mScale = 5;
            mScale = 0.1;
            Speed = 64;
            ShowMax = true;
            mShowV = false;
            ShowScale = ShowFreq = ManualScale = ShowMin = false;
            mShowFFT = false;
            mShowV = true;
        }

        void setValue() {
            if (mPlots.Count > 2 || mPlots.Count == 0) {
                return;
            }
            var ce = mPlots[0].Elm;
            if (mPlots.Count == 2 && !mPlots[1].Elm.Equals(ce)) {
                return;
            }
            setValue(ce);
        }

        void setValue(BaseUI ce) {
            mPlots = new List<ScopePlot>();
            mPlots.Add(new ScopePlot(ce));
            mShowV = true;
            calcVisiblePlots();
            ResetGraph();
        }

        void calcVisiblePlots() {
            mVisiblePlots = new List<ScopePlot>();
            int vc = 0;
            int oc = 0;
            for (int i = 0; i != mPlots.Count; i++) {
                var plot = mPlots[i];
                if (mShowV) {
                    mVisiblePlots.Add(plot);
                    plot.AssignColor(vc++);
                } else {
                    mVisiblePlots.Add(plot);
                    plot.AssignColor(oc++);
                }
            }
        }

        /* calculate maximum and minimum values for all plots of given units */
        void calcMaxAndMin() {
            mMaxValue = -1e8;
            mMinValue = 1e8;
            for (int si = 0; si != mVisiblePlots.Count; si++) {
                var plot = mVisiblePlots[si];
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
        void calcPlotScale(ScopePlot plot) {
            if (ManualScale) {
                return;
            }
            int ipa = plot.StartIndex(BoundingBox.Width);
            var maxV = plot.MaxValues;
            var minV = plot.MinValues;
            double max = 0;
            double gridMax = mScale;
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
            mScale = gridMax;
        }

        /* find selected plot */
        void checkForSelection() {
            if (CirSimForm.Sim.DialogIsShowing()) {
                return;
            }
            if (!BoundingBox.Contains(CirSimForm.Sim.MouseCursorX, CirSimForm.Sim.MouseCursorY)) {
                SelectedPlot = -1;
                return;
            }
            int ipa = mPlots[0].StartIndex(BoundingBox.Width);
            int ip = (CirSimForm.Sim.MouseCursorX - BoundingBox.X + ipa) & (mScopePointCount - 1);
            int maxy = (BoundingBox.Height - 1) / 2;
            int y = maxy;
            int i;
            int bestdist = 10000;
            int best = -1;
            
            for (i = 0; i != mVisiblePlots.Count; i++) {
                var plot = mVisiblePlots[i];
                var scale = mScale;
                int maxvy = (int)(maxy / scale * plot.MaxValues[ip]);
                int dist = Math.Abs(CirSimForm.Sim.MouseCursorY - (BoundingBox.Y + y - maxvy));
                if (dist < bestdist) {
                    bestdist = dist;
                    best = i;
                }
            }
            SelectedPlot = best;
        }
        #endregion

        #region Draw Utils
        void allocImage() {
            if (mContext == null) {
                mContext = CustomGraphics.FromImage(BoundingBox.Width, BoundingBox.Height);
            }
        }

        void drawSettingsWheel(CustomGraphics g) {
            const int outR = 6 * 18 / 16;
            const int inR = 4 * 18 / 16;
            const int inR45 = 3 * 18 / 16;
            const int outR45 = 4 * 18 / 16;
            if (mShowSettingsWheel) {
                if (CursorInSettingsWheel) {
                    g.LineColor = Color.Cyan;
                } else {
                    g.LineColor = Color.DarkGray;
                }
                g.SetTransform(new Matrix(1, 0, 0, 1, BoundingBox.X + 12, BoundingBox.Y + BoundingBox.Height - 16));
                g.DrawCircle(new Point(), inR);
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

        void drawCrosshairs(CustomGraphics g) {
            if (CirSimForm.Sim.DialogIsShowing()) {
                return;
            }
            if (!BoundingBox.Contains(CirSimForm.Sim.MouseCursorX, CirSimForm.Sim.MouseCursorY)) {
                return;
            }
            if (SelectedPlot < 0 && !mShowFFT) {
                return;
            }
            var info = new string[4];
            int ipa = mPlots[0].StartIndex(BoundingBox.Width);
            int ip = (CirSimForm.Sim.MouseCursorX - BoundingBox.X + ipa) & (mScopePointCount - 1);
            int ct = 0;
            int maxy = (BoundingBox.Height - 1) / 2;
            int y = maxy;
            if (mShowV && SelectedPlot >= 0) {
                var plot = mVisiblePlots[SelectedPlot];
                info[ct++] = plot.GetUnitText(plot.MaxValues[ip]);
                int maxvy = (int)(mMainGridMult * (plot.MaxValues[ip] - mMainGridMid));
                g.LineColor = plot.Color;
                g.FillCircle(CirSimForm.Sim.MouseCursorX, BoundingBox.Y + y - maxvy, 3);
            }
            if (mShowV && mVisiblePlots.Count > 0) {
                double t = CirSimForm.Sim.Time - ControlPanel.TimeStep * Speed * (BoundingBox.X + BoundingBox.Width - CirSimForm.Sim.MouseCursorX);
                info[ct++] = Utils.TimeText(t);
            }
            if (mShowFFT) {
                double maxFrequency = 1 / (ControlPanel.TimeStep * Speed * 2);
                var posX = CirSimForm.Sim.MouseCursorX - mFFTBoundingBox.X;
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

            g.LineColor = CustomGraphics.WhiteColor;
            g.DrawLine(CirSimForm.Sim.MouseCursorX, BoundingBox.Y, CirSimForm.Sim.MouseCursorX, BoundingBox.Y + BoundingBox.Height);

            int bx = CirSimForm.Sim.MouseCursorX;
            if (bx < szw / 2) {
                bx = szw / 2;
            }

            g.LineColor = ControlPanel.ChkPrintable.Checked ? Color.White : Color.Black;
            g.FillRectangle(bx - szw / 2, BoundingBox.Y - szh, szw, szh);

            for (int i = 0; i != ct; i++) {
                int w = (int)g.GetTextSize(info[i]).Width;
                g.DrawLeftText(info[i], bx - w / 2, BoundingBox.Y - 2 - (ct - 1 - i) * 15);
            }
        }

        void drawPlot(CustomGraphics g, ScopePlot plot, bool selected) {
            if (plot.Elm == null) {
                return;
            }
            int i;
            int multptr = 0;
            int x = 0;
            int maxy = (BoundingBox.Height - 1) / 2;
            int y = maxy;

            var color = (mSomethingSelected) ? Color.FromArgb(0xA0, 0xA0, 0xA0) : plot.Color;
            if (selected || (CirSimForm.Sim.ScopeSelected == -1 && plot.Elm.IsMouseElm)) {
                color = CustomGraphics.SelectColor;
            } else if (ControlPanel.ChkPrintable.Checked) {
                color = CustomGraphics.GrayColor;
            }

            var ipa = plot.StartIndex(BoundingBox.Width);
            var maxV = plot.MaxValues;
            var minV = plot.MinValues;
            var gridMax = mScale;

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
            var gridMid = (mx + mn) * .5;
            gridMax = (mx - mn) * .55;  /* leave space at top and bottom */

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
            if (ControlPanel.ChkPrintable.Checked) {
                minorDiv = Color.FromArgb(0xD0, 0xD0, 0xD0);
                majorDiv = Color.FromArgb(0x80, 0x80, 0x80);
            }

            /* Vertical (T) gridlines */
            double ts = ControlPanel.TimeStep * Speed;
            mGridStepX = CalcGridStepX();

            if (mDrawGridLines) {
                /* horizontal gridlines */

                /* don't show gridlines if lines are too close together (except for center line) */
                bool showGridLines = (mGridStepY != 0);
                for (ll = -100; ll <= 100; ll++) {
                    if (ll != 0 && !showGridLines) {
                        continue;
                    }
                    var yl = (float)(maxy - (ll * mGridStepY - gridMid) * gridMult);
                    if (yl < 0 || yl >= BoundingBox.Height - 1) {
                        continue;
                    }
                    g.LineColor = ll == 0 ? majorDiv : minorDiv;
                    g.DrawLine(0, yl, BoundingBox.Width - 1, yl);
                }

                /* vertical gridlines */
                double tstart = CirSimForm.Sim.Time - ControlPanel.TimeStep * Speed * BoundingBox.Width;
                double tx = CirSimForm.Sim.Time - (CirSimForm.Sim.Time % mGridStepX);

                for (ll = 0; ; ll++) {
                    var tl = tx - mGridStepX * ll;
                    var gx = (float)((tl - tstart) / ts);
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
                        mReduceRange = false;
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

        void drawFFTGridLines(CustomGraphics g) {
            const int xDivs = 20;
            const int yDivs = 5;
            int prevEnd = 0;
            double maxFrequency = 1 / (ControlPanel.TimeStep * Speed * xDivs * 2);
            var gridBottom = mFFTBoundingBox.Height - 1;
            g.LineColor = CustomGraphics.GrayColor;
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
            for (int i = 1; i < yDivs; i++) {
                int y = mFFTBoundingBox.Height * i / yDivs;
                string s;
                if (LogSpectrum) {
                    s = (Mindb * i / yDivs).ToString() + "db";
                } else {
                    s = (1.0 * (yDivs - i) / yDivs).ToString();
                }
                if (i > 0) {
                    g.DrawLine(0, y, BoundingBox.Width, y);
                }
                g.DrawLeftText(s, 0, y + 8);
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
                real[i] = 0.5 * (maxV[ii] + minV[ii]) * (0.5 - 0.5 * Math.Cos(2.0 * Math.PI * i / mScopePointCount));
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
            var bottom = mFFTBoundingBox.Height - 1;
            var scaleX = 2.0f * mFFTBoundingBox.Width / mScopePointCount;
            var x0 = 1.0f * mFFTBoundingBox.X;
            var x1 = x0;
            var y0 = 0.0f;
            g.LineColor = Color.Red;
            if (LogSpectrum) {
                var ymult = -bottom / Mindb;
                for (int i = 0; i < mScopePointCount / 2; i++) {
                    var mag = mFft.Magnitude(real[i], imag[i]);
                    if (0 == mag) {
                        mag = 1;
                    }
                    var db = 20 * Math.Log10(mag / maxM);
                    if (db < Mindb) {
                        db = Mindb;
                    }
                    var y1 = (float)(-db * ymult);
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
                    var mag = mFft.Magnitude(real[i], imag[i]);
                    var y1 = bottom - (float)(mag * bottom / maxM);
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

        string calcRMS() {
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
                    var m = (maxV[ip] + minV[ip]) * .5;
                    avg += m * m;
                }
            }
            if (1 < waveCount) {
                var rms = Math.Sqrt(endAvg / (end - start));
                return plot.GetUnitText(rms) + "rms";
            } else {
                return "";
            }
        }

        string calcFrequency() {
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
            double freq = 1 / (avperiod * ControlPanel.TimeStep * Speed);
            /* don't show freq if standard deviation is too great */
            if (periodct < 1 || periodstd > 2) {
                freq = 0;
            }
            /* Console.WriteLine(freq + " " + periodstd + " " + periodct); */
            if (0 == freq) {
                return "";
            } else {
                return Utils.UnitText(freq, "Hz");
            }
        }

        void drawInfoTexts(CustomGraphics g) {
            string t = Text;
            if (string.IsNullOrEmpty(t)) {
                t = mScopeText;
            }
            var textY = 8;
            if (!string.IsNullOrEmpty(t)) {
                g.DrawLeftText(t, 0, textY);
                textY += 12;
            }
            var plot = mVisiblePlots[0];
            if (mShowV && ShowScale) {
                string vScaleText = "";
                if (mGridStepY != 0) {
                    vScaleText = ", V=" + plot.GetUnitText(mGridStepY) + "/div";
                }
                g.DrawLeftText("H=" + Utils.UnitText(mGridStepX, "s") + "/div" + vScaleText, 0, textY);
                textY += 12;
            }
            if (mShowV && ShowMax) {
                g.DrawLeftText(plot.GetUnitText(mMaxValue), 0, textY);
                textY += 12;
            }
            if (mShowV && ShowMin) {
                int ym = BoundingBox.Height - 8;
                g.DrawLeftText(plot.GetUnitText(mMinValue), 0, ym);
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
