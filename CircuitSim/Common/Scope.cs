﻿using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Forms;

using Circuit.UI;
using Circuit.UI.Passive;

namespace Circuit {
    public class Scope {
        #region CONST
        const int INFO_WIDTH = 120;
        const int FLAG_PLOTS = 4096;
        const double Mindb = -100.0;

        readonly double[] MULTA = new double[] { 1.5, 2.0, 1.5 };
        #endregion

        public static int Count { get; set; }
        public static Scope[] List { get; set; } = new Scope[20];

        #region dynamic variable
        FFT mFft;
        Rectangle mFFTBoundingBox;

        CustomGraphics mContext;

        public List<ScopePlot> Plots { get; private set; }

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

        bool mDrewGridlines;
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
                if (Plots.Count == 0) {
                    return 0;
                }
                return mScale;
            }
            set {
                if (Plots.Count == 0) {
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
                var elm = Plots[0].Elm;
                for (int i = 1; i < Plots.Count; i++) {
                    if (!Plots[i].Elm.Equals(elm)) {
                        return null;
                    }
                }
                return elm;
            }
        }
        public BaseUI Elm {
            get {
                if (0 <= SelectedPlot && SelectedPlot < Plots.Count) {
                    return Plots[SelectedPlot].Elm;
                }
                return 0 < Plots.Count ? Plots[0].Elm : null;
            }
        }

        public bool CanMenu {
            get { return Plots[0].Elm != null; }
        }
        public bool ViewingWire {
            get {
                foreach (var plot in Plots) {
                    if (plot.Elm is Wire) {
                        return true;
                    }
                }
                return false;
            }
        }
        public bool NeedToRemove {
            get {
                bool ret = true;
                for (int i = 0; i != Plots.Count; i++) {
                    var plot = Plots[i];
                    if (CirSimForm.GetUIIndex(plot.Elm) < 0) {
                        Plots.RemoveAt(i--);
                    } else {
                        ret = false;
                    }
                }
                return ret;
            }
        }
        public bool CursorInSettingsWheel {
            get {
                return mShowSettingsWheel
                    && BoundingBox.X <= CirSimForm.MouseCursorX
                    && BoundingBox.Y + BoundingBox.Height - 24 <= CirSimForm.MouseCursorY
                    && CirSimForm.MouseCursorX <= BoundingBox.X + 24
                    && CirSimForm.MouseCursorY <= BoundingBox.Y + BoundingBox.Height;
            }
        }
        #endregion

        #region [private property]
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
                var plot = Plots[0];
                if (0 <= SelectedPlot && SelectedPlot < Plots.Count) {
                    plot = Plots[SelectedPlot];
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
        public static void Setup(int height) {
            /* check scopes to make sure the elements still exist, and remove
            /* unused scopes/columns */
            int pos = -1;
            for (int i = 0; i < Count; i++) {
                if (List[i].NeedToRemove) {
                    int j;
                    for (j = i; j != Count; j++) {
                        List[j] = List[j + 1];
                    }
                    Count--;
                    i--;
                    continue;
                }
                if (List[i].Position > pos + 1) {
                    List[i].Position = pos + 1;
                }
                pos = List[i].Position;
            }

            while (Count > 0 && List[Count - 1].Elm == null) {
                Count--;
            }

            if (Count <= 0) {
                return;
            }

            pos = 0;
            var scopeColCount = new int[Count];
            for (int i = 0; i != Count; i++) {
                pos = Math.Max(List[i].Position, pos);
                scopeColCount[List[i].Position]++;
            }
            int colct = pos + 1;
            int iw = INFO_WIDTH;
            if (colct <= 2) {
                iw = iw * 3 / 2;
            }
            int w = (BaseUI.Context.Width - iw) / colct;
            int marg = 10;
            if (w < marg * 2) {
                w = marg * 2;
            }

            pos = -1;
            int colh = 0;
            int row = 0;
            int speed = 0;
            foreach (var s in List) {
                if (s == null || scopeColCount.Length <= s.Position) {
                    break;
                }
                if (s.Position > pos) {
                    pos = s.Position;
                    colh = height / scopeColCount[pos];
                    row = 0;
                    speed = s.Speed;
                }
                s.StackCount = scopeColCount[pos];
                if (s.Speed != speed) {
                    s.Speed = speed;
                    s.ResetGraph();
                }
                var r = new Rectangle(pos * w, BaseUI.Context.Height - height + colh * row, w - marg, colh);
                row++;
                if (!r.Equals(s.BoundingBox)) {
                    s.SetRect(r);
                }
            }
        }

        public static void Stack(int s) {
            if (s == 0) {
                if (Count < 2) {
                    return;
                }
                s = 1;
            }
            if (List[s].Position == List[s - 1].Position) {
                return;
            }
            List[s].Position = List[s - 1].Position;
            for (s++; s < Count; s++) {
                List[s].Position--;
            }
        }

        public static void Unstack(int s) {
            if (s == 0) {
                if (Count < 2) {
                    return;
                }
                s = 1;
            }
            if (List[s].Position != List[s - 1].Position) {
                return;
            }
            for (; s < Count; s++) {
                List[s].Position++;
            }
        }

        public static void Combine(int s) {
            if (s == 0) {
                if (Count < 2) {
                    return;
                }
                s = 1;
            }
            List[s - 1].Combine(List[s]);
            List[s].SetElm(null);
        }

        public static void StackAll() {
            for (int i = 0; i != Count; i++) {
                List[i].Position = 0;
                List[i].ShowMax = false;
                List[i].ShowMin = false;
            }
        }

        public static void UnstackAll() {
            for (int i = 0; i != Count; i++) {
                List[i].Position = i;
                List[i].ShowMax = true;
            }
        }

        public static void CombineAll() {
            for (int i = Count - 2; i >= 0; i--) {
                List[i].Combine(List[i + 1]);
                List[i + 1].SetElm(null);
            }
        }

        public static void SeparateAll() {
            var newscopes = new List<Scope>();
            int ct = 0;
            for (int i = 0; i < Count; i++) {
                ct = List[i].Separate(newscopes, ct);
            }
            List = newscopes.ToArray();
            Count = ct;
        }

        public void ResetGraph(bool full = false) {
            mScopePointCount = 1;
            while (mScopePointCount <= BoundingBox.Width) {
                mScopePointCount *= 2;
            }
            if (Plots == null) {
                Plots = new List<ScopePlot>();
            }
            mShowNegative = false;
            for (int i = 0; i != Plots.Count; i++) {
                var p = Plots[i];
                p.Reset(mScopePointCount, Speed, full);
                if (p.ColorIndex == ScopePlot.E_COLOR.INVALID) {
                    p.SetColor(i);
                }
            }
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
            Plots = new List<ScopePlot>();
            setValue(ce);
            initialize();
        }

        public void Combine(Scope s) {
            var oc = Plots.Count;
            foreach (var p in s.Plots) {
                p.SetColor(oc++);
                Plots.Add(p);
            }
            s.Plots.Clear();
        }

        public int Separate(List<Scope> arr, int pos) {
            foreach (var sp in Plots) {
                if (arr.Count <= pos) {
                    return pos;
                }
                var s = new Scope();
                s.setValue(sp.Elm);
                s.Position = pos;
                s.mFlags = mFlags;
                s.Speed = Speed;
                arr[pos++] = s;
            }
            return pos;
        }

        public void RemoveWave(int plot) {
            if (plot < Plots.Count) {
                var p = Plots[plot];
                Plots.Remove(p);
            }
        }

        public void TimeStep() {
            foreach (var p in Plots) {
                p.TimeStep();
            }
        }

        public void MaxScale() {
            mMaxScale = !mMaxScale;
            mShowNegative = false;
        }

        public void Properties(int x, int y) {
            var fm = new ScopeProperties(this);
            fm.Show(x, y);
            CirSimForm.DialogShowing = fm;
        }

        public void SpeedUp() {
            if (1 < Speed) {
                Speed >>= 1;
                ResetGraph();
            }
        }

        public void SlowDown() {
            if (Speed < 1024) {
                Speed <<= 1;
            }
            ResetGraph();
        }

        public string Dump() {
            var vPlot = Plots[0];
            var elm = vPlot.Elm;
            if (elm == null) {
                return null;
            }
            var dumpList = new List<object>();
            dumpList.Add("o");
            dumpList.Add(vPlot.Speed);
            dumpList.Add(mFlags);
            dumpList.Add(mScale.ToString("0.000000"));
            dumpList.Add(Position);
            dumpList.Add(Plots.Count);
            foreach (var p in Plots) {
                dumpList.Add(CirSimForm.GetUIIndex(p.Elm) + "_" + p.ColorIndex);
            }
            if (!string.IsNullOrWhiteSpace(Text)) {
                dumpList.Add(Utils.Escape(Text));
            }
            return string.Join(" ", dumpList.ToArray());
        }

        public void Undump(StringTokenizer st) {
            initialize();
            Plots = new List<ScopePlot>();

            Speed = st.nextTokenInt();
            mFlags = st.nextTokenInt();
            mScale = st.nextTokenDouble();
            Position = st.nextTokenInt();

            try {
                int plotCount = st.nextTokenInt();
                for (int i = 0; i != plotCount; i++) {
                    var subElmCol = st.nextToken().Split('_');
                    var subElmIdx = int.Parse(subElmCol[0]);
                    var subElm = CirSimForm.UIList[subElmIdx];
                    var color = (int)Enum.Parse(typeof(ScopePlot.E_COLOR), subElmCol[1]);
                    var p = new ScopePlot(subElm);
                    p.SetColor(color);
                    Plots.Add(p);
                }
            } catch (Exception ex) {
                throw ex;
            }

            if (st.HasMoreTokens) {
                Text = Utils.Unescape(st.nextToken());
            } else {
                Text = "";
            }
        }

        public double CalcGridStepX() {
            int multIdx = 0;
            var step = 1e-9;
            var baseT = ControlPanel.TimeStep * Speed;
            while (step < baseT * 20) {
                step *= MULTA[(multIdx++) % 3];
            }
            return step;
        }

        public void Draw(CustomGraphics g, bool isFloat = false) {
            if (Plots.Count == 0) {
                return;
            }

            /* reset if timestep changed */
            if (mScopeTimeStep != ControlPanel.TimeStep) {
                mScopeTimeStep = ControlPanel.TimeStep;
                ResetGraph();
            }

            drawSettingsWheel(g, isFloat);

            if (mMaxScale) {
                mScale = 1e-4;
            }
            mReduceRange = false;
            mSomethingSelected = false;  /* is one of our plots selected? */

            foreach (var p in Plots) {
                calcPlotScale(p);
                if (CirSimForm.SelectedScope == -1 && p.Elm != null && p.Elm.IsMouseElm) {
                    mSomethingSelected = true;
                }
                mReduceRange = true;
            }

            checkForSelection();
            if (SelectedPlot >= 0) {
                mSomethingSelected = true;
            }

            mDrewGridlines = false;
            if ((ShowMax || ShowMin) && Plots.Count > 0) {
                calcMaxAndMin();
            }

            if (isFloat) {
                g.SetPlotFloat(BoundingBox.X, BoundingBox.Y);
                g.DrawRectangle(new Rectangle(0, 0, BoundingBox.Width, BoundingBox.Height));
            } else {
                g.SetPlotBottom(BoundingBox.X, BoundingBox.Y);
            }
            {
                if (mShowFFT) {
                    drawFFTGridLines(g);
                    drawFFT(g);
                }
                if (mShowV) {
                    /* Vertical (T) gridlines */
                    mGridStepX = CalcGridStepX();

                    /* draw volts on top (last), then current underneath, then everything else */
                    for (int i = 0; i != Plots.Count; i++) {
                        if (i != SelectedPlot) {
                            drawPlot(g, Plots[i], false);
                        }
                    }
                    /* draw selection on top.  only works if selection chosen from scope */
                    if (SelectedPlot >= 0 && SelectedPlot < Plots.Count) {
                        drawPlot(g, Plots[SelectedPlot], true);
                    }
                }
                if (Plots.Count > 0) {
                    drawInfoTexts(g);
                }
            }
            g.ClearTransform();

            drawCrosshairs(g);

            if (5 < Plots[0].Pointer && !ManualScale) {
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
            if (Plots.Count > 2 || Plots.Count == 0) {
                return;
            }
            var ce = Plots[0].Elm;
            if (Plots.Count == 2 && !Plots[1].Elm.Equals(ce)) {
                return;
            }
            setValue(ce);
        }

        void setValue(BaseUI ce) {
            Plots = new List<ScopePlot>();
            Plots.Add(new ScopePlot(ce));
            mShowV = true;
            ResetGraph();
        }

        /* calculate maximum and minimum values for all plots of given units */
        void calcMaxAndMin() {
            mMaxValue = -1e8;
            mMinValue = 1e8;
            for (int si = 0; si != Plots.Count; si++) {
                var plot = Plots[si];
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
            if (CirSimForm.DialogIsShowing()) {
                return;
            }
            if (!BoundingBox.Contains(CirSimForm.MouseCursorX, CirSimForm.MouseCursorY)) {
                SelectedPlot = -1;
                return;
            }
            int ipa = Plots[0].StartIndex(BoundingBox.Width);
            int ip = (CirSimForm.MouseCursorX - BoundingBox.X + ipa) & (mScopePointCount - 1);
            int maxy = (BoundingBox.Height - 1) / 2;
            int y = maxy;
            int i;
            int bestdist = 10000;
            int best = -1;
            
            for (i = 0; i != Plots.Count; i++) {
                var plot = Plots[i];
                var scale = mScale;
                int maxvy = (int)(maxy / scale * plot.MaxValues[ip]);
                int dist = Math.Abs(CirSimForm.MouseCursorY - (BoundingBox.Y + y - maxvy));
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

        void drawSettingsWheel(CustomGraphics g, bool isFloat) {
            const int outR = 6 * 18 / 16;
            const int inR = 4 * 18 / 16;
            const int inR45 = 3 * 18 / 16;
            const int outR45 = 4 * 18 / 16;
            if (mShowSettingsWheel) {
                if (CursorInSettingsWheel) {
                    g.DrawColor = Color.Cyan;
                } else {
                    g.DrawColor = Color.DarkGray;
                }
                if (isFloat) {
                    g.SetPlotFloat(BoundingBox.X + 12, BoundingBox.Y + BoundingBox.Height - 16);
                } else {
                    g.SetPlotBottom(BoundingBox.X + 12, BoundingBox.Y + BoundingBox.Height - 16);
                }
                {
                    g.DrawCircle(new Point(), inR);
                    g.DrawLine(-outR, 0, -inR, 0);
                    g.DrawLine(outR, 0, inR, 0);
                    g.DrawLine(0, -outR, 0, -inR);
                    g.DrawLine(0, outR, 0, inR);
                    g.DrawLine(-outR45, -outR45, -inR45, -inR45);
                    g.DrawLine(outR45, -outR45, inR45, -inR45);
                    g.DrawLine(-outR45, outR45, -inR45, inR45);
                    g.DrawLine(outR45, outR45, inR45, inR45);
                }
                g.ClearTransform();
            }
        }

        void drawCrosshairs(CustomGraphics g) {
            if (CirSimForm.DialogIsShowing()) {
                return;
            }
            if (!BoundingBox.Contains(CirSimForm.MouseCursorX, CirSimForm.MouseCursorY)) {
                return;
            }
            if (SelectedPlot < 0 && !mShowFFT) {
                return;
            }
            var info = new string[4];
            int ipa = Plots[0].StartIndex(BoundingBox.Width);
            int ip = (CirSimForm.MouseCursorX - BoundingBox.X + ipa) & (mScopePointCount - 1);
            int ct = 0;
            int maxy = (BoundingBox.Height - 1) / 2;
            int y = maxy;
            if (mShowV && SelectedPlot >= 0) {
                var plot = Plots[SelectedPlot];
                info[ct++] = plot.GetUnitText(plot.MaxValues[ip]);
                int maxvy = (int)(mMainGridMult * (plot.MaxValues[ip] - mMainGridMid));
                g.FillColor = plot.Color;
                g.FillCircle(CirSimForm.MouseCursorX, BoundingBox.Y + y - maxvy, 3);
            }
            if (mShowV && Plots.Count > 0) {
                double t = Circuit.Time - ControlPanel.TimeStep * Speed * (BoundingBox.X + BoundingBox.Width - CirSimForm.MouseCursorX);
                info[ct++] = Utils.TimeText(t);
            }
            if (mShowFFT) {
                double maxFrequency = 1 / (ControlPanel.TimeStep * Speed * 2);
                var posX = CirSimForm.MouseCursorX - mFFTBoundingBox.X;
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
            g.DrawLine(CirSimForm.MouseCursorX, BoundingBox.Y, CirSimForm.MouseCursorX, BoundingBox.Y + BoundingBox.Height);

            int bx = CirSimForm.MouseCursorX;
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

        void drawPlot(CustomGraphics g, ScopePlot plot, bool selected) {
            if (plot.Elm == null) {
                return;
            }

            int maxY = (BoundingBox.Height - 1) / 2;
            int minRangeLo;
            int minRangeHi;
            double gridMid;
            double gridMult;
            {
                /* if we don't have overlapping scopes of different units, we can move zero around.
                 * Put it at the bottom if the scope is never negative. */
                var mx = mScale;
                var mn = 0.0;
                if (mMaxScale) {
                    /* scale is maxed out, so fix boundaries of scope at maximum and minimum. */
                    mx = mMaxValue;
                    mn = mMinValue;
                } else if (mShowNegative || mMinValue < (mx + mn) * .5 - (mx - mn) * .55) {
                    mn = -mScale;
                    mShowNegative = true;
                }
                var gridMax = (mx - mn) * 0.55;  /* leave space at top and bottom */
                if (gridMax * gridMax < 0.01) {
                    gridMax = 0.1;
                }
                gridMid = (mx + mn) * 0.5;
                gridMult = maxY / gridMax;
                if (selected) {
                    mMainGridMult = gridMult;
                    mMainGridMid = gridMid;
                }
                minRangeLo = -10 - (int)(gridMid * gridMult);
                minRangeHi = 10 - (int)(gridMid * gridMult);

                int multIdx = 0;
                mGridStepY = 1e-8;
                while (mGridStepY < 20 * gridMax / maxY) {
                    mGridStepY *= MULTA[(multIdx++) % 3];
                }
            }

            if (!mDrewGridlines) {
                mDrewGridlines = true;
                var minorDiv = Color.FromArgb(0x30, 0x30, 0x30);
                var majorDiv = Color.FromArgb(0xA0, 0xA0, 0xA0);
                if (ControlPanel.ChkPrintable.Checked) {
                    minorDiv = Color.FromArgb(0xDF, 0xDF, 0xDF);
                    majorDiv = Color.FromArgb(0x7F, 0x7F, 0x7F);
                }

                /* horizontal gridlines */
                var showGridlines = mGridStepY != 0;
                for (int ll = -100; ll <= 100; ll++) {
                    if (ll != 0 && !showGridlines) {
                        continue;
                    }
                    var ly = (float)(maxY - (ll * mGridStepY - gridMid) * gridMult);
                    if (ly < 0 || BoundingBox.Height <= ly) {
                        continue;
                    }
                    g.DrawColor = ll == 0 ? majorDiv : minorDiv;
                    g.DrawLine(0, ly, BoundingBox.Width - 1, ly);
                }

                /* vertical gridlines */
                var baseT = ControlPanel.TimeStep * Speed;
                var beginT = Circuit.Time - BoundingBox.Width * baseT;
                var endT = Circuit.Time - (Circuit.Time % mGridStepX);
                for (int ll = 0; ; ll++) {
                    var t = endT - mGridStepX * ll;
                    var lx = (float)((t - beginT) / baseT);
                    if (lx < 0) {
                        break;
                    }
                    if (t < 0 || BoundingBox.Width <= lx) {
                        continue;
                    }
                    if (((t + mGridStepX / 4) % (mGridStepX * 10)) < mGridStepX) {
                        g.DrawColor = majorDiv;
                    } else {
                        g.DrawColor = minorDiv;
                    }
                    g.DrawLine(lx, 0, lx, BoundingBox.Height - 1);
                }
            }

            if (ControlPanel.ChkPrintable.Checked) {
                g.DrawColor = plot.Color;
            } else {
                if (selected || (CirSimForm.SelectedScope == -1 && plot.Elm.IsMouseElm)) {
                    g.DrawColor = CustomGraphics.SelectColor;
                } else {
                    g.DrawColor = mSomethingSelected ? ScopePlot.GRAY : plot.Color;
                }
            }

            var idxBegin = plot.StartIndex(BoundingBox.Width);
            var idx0 = idxBegin & (mScopePointCount - 1);
            var arrMaxV = plot.MaxValues;
            var arrMinV = plot.MinValues;
            var max_o = (int)(gridMult * (arrMaxV[idx0] - gridMid));
            var min_o = (int)(gridMult * (arrMinV[idx0] - gridMid));
            int ox = 0;
            for (int px = 0; px != BoundingBox.Width; px++) {
                int idx = (px + idxBegin) & (mScopePointCount - 1);
                var max_n = (int)(gridMult * (arrMaxV[idx] - gridMid));
                var min_n = (int)(gridMult * (arrMinV[idx] - gridMid));
                if (min_n < minRangeLo || max_n > minRangeHi) {
                    mReduceRange = false;
                    minRangeLo = -1000;
                    minRangeHi = 1000;
                }
                if (maxY < min_n) {
                    continue;
                }
                var dmax = max_n - max_o;
                var dmin = min_n - min_o;
                if (0 < dmax) {
                    g.DrawLine(ox, maxY - max_o, px, maxY - max_o);
                } else if (dmin < 0) {
                    g.DrawLine(ox, maxY - min_o, px, maxY - min_o);
                } else {
                    if (dmax * dmax < dmin * dmin) {
                        g.DrawLine(ox, maxY - max_o, px, maxY - max_n);
                    } else {
                        g.DrawLine(ox, maxY - min_o, px, maxY - min_n);
                    }
                }
                if (min_n != max_n) {
                    g.DrawLine(px, maxY - min_n, px, maxY - max_n);
                }
                max_o = max_n;
                min_o = min_n;
                ox = px;
            }
        }

        void drawFFTGridLines(CustomGraphics g) {
            const int xDivs = 20;
            const int yDivs = 5;
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
            var plot = (Plots.Count == 0) ? Plots[0] : Plots[0];
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
            g.DrawColor = Color.Red;
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
            var plot = Plots[0];
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
            var plot = Plots[0];
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
            var plot = Plots[0];
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
