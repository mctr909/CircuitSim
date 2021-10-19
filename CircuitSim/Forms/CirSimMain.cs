using System;
using System.Drawing;
using System.Drawing.Drawing2D;

using Circuit.Elements;
using Circuit.Elements.Output;

namespace Circuit {
    partial class CirSim {
        public CircuitElm getElm(int n) {
            if (n >= ElmList.Count) {
                return null;
            }
            return ElmList[n];
        }

        public void updateCircuit() {
            bool didAnalyze = mAnalyzeFlag;
            if (mAnalyzeFlag || DcAnalysisFlag) {
                mCir.AnalyzeCircuit();
                mAnalyzeFlag = false;
            }

            if (mCir.StopElm != null && mCir.StopElm != mMouseElm) {
                mCir.StopElm.SetMouseElm(true);
            }
            setupScopes();

            var g = CircuitElm.Context;

            if (ControlPanel.ChkPrintable.Checked) {
                CustomGraphics.WhiteColor = Color.Black;
                CustomGraphics.GrayColor = Color.Black;
                CustomGraphics.TextColor = Color.Black;
                CustomGraphics.SelectColor = Color.Red;
                CustomGraphics.PenHandle = Pens.Red.Brush;
                g.PostColor = Color.Black;
                g.Clear(Color.White);
            } else {
                CustomGraphics.WhiteColor = Color.White;
                CustomGraphics.GrayColor = Color.Gray;
                CustomGraphics.TextColor = Color.LightGray;
                CustomGraphics.SelectColor = Color.Cyan;
                CustomGraphics.PenHandle = Pens.Cyan.Brush;
                g.PostColor = Color.Red;
                g.Clear(Color.Black);
            }

            if (IsRunning) {
                try {
                    runCircuit(didAnalyze);
                } catch (Exception e) {
                    Console.WriteLine("exception in runCircuit " + e + "\r\n" + e.StackTrace);
                    return;
                }
            }

            long sysTime = DateTime.Now.ToFileTimeUtc();
            if (IsRunning) {
                if (mLastTime != 0) {
                    int inc = (int)(sysTime - mLastTime);
                    double c = ControlPanel.TrbCurrent.Value;
                    c = Math.Exp(c / 3.5 - 14.2);
                    CurrentMult = 1.7 * inc * c;
                }
                mLastTime = sysTime;
            } else {
                mLastTime = 0;
            }

            if (sysTime - mLastSysTime >= 1000) {
                mLastSysTime = sysTime;
            }

            /* draw elements */
            g.SetTransform(new Matrix(Transform[0], Transform[1], Transform[2], Transform[3], Transform[4], Transform[5]));
            for (int i = 0; i != ElmList.Count; i++) {
                ElmList[i].Draw(g);
            }

            /* draw posts normally */
            if (MouseMode != MOUSE_MODE.DRAG_ROW && MouseMode != MOUSE_MODE.DRAG_COLUMN) {
                for (int i = 0; i != mCir.PostDrawList.Count; i++) {
                    g.DrawPost(mCir.PostDrawList[i]);
                }
            }

            /* for some mouse modes, what matters is not the posts but the endpoints (which are only
            /* the same for 2-terminal elements).  We draw those now if needed */
            if (TempMouseMode == MOUSE_MODE.DRAG_ROW
                || TempMouseMode == MOUSE_MODE.DRAG_COLUMN
                || TempMouseMode == MOUSE_MODE.DRAG_POST
                || TempMouseMode == MOUSE_MODE.DRAG_SELECTED) {
                for (int i = 0; i != ElmList.Count; i++) {
                    var ce = getElm(i);
                    g.DrawPost(ce.P1);
                    g.DrawPost(ce.P2);
                    if (ce != mMouseElm || TempMouseMode != MOUSE_MODE.DRAG_POST) {
                        g.FillCircle(Brushes.Gray, ce.P1, 3.5f);
                        g.FillCircle(Brushes.Gray, ce.P2, 3.5f);
                    } else {
                        ce.DrawHandles(g);
                    }
                }
            }

            /* draw handles for elm we're creating */
            if (TempMouseMode == MOUSE_MODE.SELECT && mMouseElm != null) {
                mMouseElm.DrawHandles(g);
            }

            /* draw handles for elm we're dragging */
            if (DragElm != null && (DragElm.P1.X != DragElm.P2.X || DragElm.P1.Y != DragElm.P2.Y)) {
                DragElm.Draw(g);
                DragElm.DrawHandles(g);
            }

            /* draw bad connections.  do this last so they will not be overdrawn. */
            for (int i = 0; i != mCir.BadConnectionList.Count; i++) {
                var cn = mCir.BadConnectionList[i];
                g.FillCircle(Brushes.Red, cn, 3.5f);
            }

            if (0 < mSelectedArea.Width) {
                g.LineColor = CustomGraphics.SelectColor;
                g.DrawRectangle(mSelectedArea);
            }

            if (ControlPanel.ChkCrossHair.Checked && MouseCursorX >= 0
                && MouseCursorX <= mCircuitArea.Width && MouseCursorY <= mCircuitArea.Height) {
                int x = SnapGrid(inverseTransformX(MouseCursorX));
                int y = SnapGrid(inverseTransformY(MouseCursorY));
                g.LineColor = Color.Gray;
                g.DrawLine(x, inverseTransformY(0), x, inverseTransformY(mCircuitArea.Height));
                g.DrawLine(inverseTransformX(0), y, inverseTransformX(mCircuitArea.Width), y);
            }

            g.ClearTransform();

            Brush bCircuitArea;
            if (ControlPanel.ChkPrintable.Checked) {
                bCircuitArea = Brushes.White;
            } else {
                bCircuitArea = Brushes.Black;
            }
            g.FillRectangle(bCircuitArea, 0, mCircuitArea.Height, mCircuitArea.Width, g.Height - mCircuitArea.Height);

            int ct = mScopeCount;
            if (mCir.StopMessage != null) {
                ct = 0;
            }
            for (int i = 0; i != ct; i++) {
                mScopes[i].Draw(g);
            }
            if (mMouseWasOverSplitter) {
                g.LineColor = Color.Cyan;
                g.DrawLine(0, mCircuitArea.Height - 2, mCircuitArea.Width, mCircuitArea.Height - 2);
            }

            if (mCir.StopMessage != null) {
                g.DrawLeftText(mCir.StopMessage, 10, mCircuitArea.Height - 10);
            } else {
                var info = new string[10];
                if (mMouseElm != null) {
                    if (mMousePost == -1) {
                        mMouseElm.GetInfo(info);
                    } else {
                        info[0] = "V = " + mMouseElm.DispPostVoltage(mMousePost);
                    }
                } else {
                    info[0] = "t = " + Utils.TimeText(Time);
                    info[1] = "time step = " + Utils.TimeText(ControlPanel.TimeStep);
                }
                if (Hint.Type != -1) {
                    int infoIdx;
                    for (infoIdx = 0; info[infoIdx] != null; infoIdx++) ;
                    var s = Hint.getHint(ElmList);
                    if (s == null) {
                        Hint.Type = -1;
                    } else {
                        info[infoIdx] = s;
                    }
                }
                int x = 0;
                if (ct != 0) {
                    x = mScopes[ct - 1].RightEdge + 20;
                }
                x = Math.Max(x, g.Width * 2 / 3);

                /* count lines of data */
                {
                    int infoIdx;
                    for (infoIdx = 0; infoIdx < info.Length - 1 && info[infoIdx] != null; infoIdx++) ;
                    int badnodes = mCir.BadConnectionList.Count;
                    if (badnodes > 0) {
                        info[infoIdx++] = badnodes + ((badnodes == 1) ? " bad connection" : " bad connections");
                    }
                }
                int ybase = mCircuitArea.Height;
                for (int i = 0; i < info.Length && info[i] != null; i++) {
                    g.DrawLeftText(info[i], x, ybase + 15 * (i + 1));
                }
            }

            if (mCir.StopElm != null && mCir.StopElm != mMouseElm) {
                mCir.StopElm.SetMouseElm(false);
            }

            if (null != mPixCir.Image) {
                mPixCir.Image.Dispose();
                mPixCir.Image = null;
            }
            if (null != mBmp || null != mContext) {
                if (null == mContext) {
                    mBmp.Dispose();
                    mBmp = null;
                } else {
                    mContext.Dispose();
                    mContext = null;
                }
            }

            mBmp = new Bitmap(g.Width, g.Height);
            mContext = Graphics.FromImage(mBmp);
            CircuitElm.Context.CopyTo(mContext);
            mPixCir.Image = mBmp;

            /* if we did DC analysis, we need to re-analyze the circuit with that flag cleared. */
            if (DcAnalysisFlag) {
                DcAnalysisFlag = false;
                mAnalyzeFlag = true;
            }

            mLastFrameTime = mLastTime;
        }

        void runCircuit(bool didAnalyze) {
            if (mCir.Matrix == null || ElmList.Count == 0) {
                mCir.Matrix = null;
                return;
            }

            bool debugprint = mDumpMatrix;
            mDumpMatrix = false;
            double steprate = getIterCount();
            long tm = DateTime.Now.ToFileTimeUtc();
            long lit = mLastIterTime;
            if (lit == 0) {
                mLastIterTime = tm;
                return;
            }

            /* Check if we don't need to run simulation (for very slow simulation speeds).
            /* If the circuit changed, do at least one iteration to make sure everything is consistent. */
            if (12500 >= steprate * (tm - mLastIterTime) && !didAnalyze) {
                return;
            }

            bool delayWireProcessing = canDelayWireProcessing();

            int iter;
            for (iter = 1; ; iter++) {
                for (int i = 0; i != ElmList.Count; i++) {
                    var ce = getElm(i);
                    ce.StartIteration();
                }

                if (!mCir.Run(debugprint)) {
                    break;
                }

                Time += ControlPanel.TimeStep;
                for (int i = 0; i != ElmList.Count; i++) {
                    getElm(i).StepFinished();
                }
                if (!delayWireProcessing) {
                    mCir.CalcWireCurrents();
                }
                for (int i = 0; i != mScopeCount; i++) {
                    mScopes[i].TimeStep();
                }
                for (int i = 0; i != ElmList.Count; i++) {
                    if (getElm(i) is ScopeElm) {
                        ((ScopeElm)getElm(i)).stepScope();
                    }
                }

                tm = DateTime.Now.ToFileTimeUtc();
                lit = tm;
                /* Check whether enough time has elapsed to perform an *additional* iteration after
                /* those we have already completed. */
                if ((iter + 1) * 1000 >= steprate * (tm - mLastIterTime) || (tm - mLastFrameTime > 250000)) {
                    break;
                }
                if (!IsRunning) {
                    break;
                }
            } /* for (iter = 1; ; iter++) */

            mLastIterTime = lit;
            if (delayWireProcessing) {
                mCir.CalcWireCurrents();
            }
            /* Console.WriteLine((DateTime.Now.ToFileTimeUtc() - lastFrameTime) / (double)iter); */
        }

        void setupScopes() {
            /* check scopes to make sure the elements still exist, and remove
            /* unused scopes/columns */
            int pos = -1;
            for (int i = 0; i < mScopeCount; i++) {
                if (mScopes[i].NeedToRemove) {
                    int j;
                    for (j = i; j != mScopeCount; j++) {
                        mScopes[j] = mScopes[j + 1];
                    }
                    mScopeCount--;
                    i--;
                    continue;
                }
                if (mScopes[i].Position > pos + 1) {
                    mScopes[i].Position = pos + 1;
                }
                pos = mScopes[i].Position;
            }

            while (mScopeCount > 0 && mScopes[mScopeCount - 1].Elm == null) {
                mScopeCount--;
            }

            int h = CircuitElm.Context.Height - mCircuitArea.Height;
            pos = 0;
            for (int i = 0; i != mScopeCount; i++) {
                mScopeColCount[i] = 0;
            }
            for (int i = 0; i != mScopeCount; i++) {
                pos = Math.Max(mScopes[i].Position, pos);
                mScopeColCount[mScopes[i].Position]++;
            }
            int colct = pos + 1;
            int iw = INFO_WIDTH;
            if (colct <= 2) {
                iw = iw * 3 / 2;
            }
            int w = (CircuitElm.Context.Width - iw) / colct;
            int marg = 10;
            if (w < marg * 2) {
                w = marg * 2;
            }

            pos = -1;
            int colh = 0;
            int row = 0;
            int speed = 0;
            for (int i = 0; i != mScopeCount; i++) {
                var s = mScopes[i];
                if (s.Position > pos) {
                    pos = s.Position;
                    colh = h / mScopeColCount[pos];
                    row = 0;
                    speed = s.Speed;
                }
                s.StackCount = mScopeColCount[pos];
                if (s.Speed != speed) {
                    s.Speed = speed;
                    s.ResetGraph();
                }
                var r = new Rectangle(pos * w, CircuitElm.Context.Height - h + colh * row, w - marg, colh);
                row++;
                if (!r.Equals(s.BoundingBox)) {
                    s.SetRect(r);
                }
            }
        }

        public double getIterCount() {
            /* IES - remove interaction */
            if (ControlPanel.TrbSpeed.Value == 0) {
                return 0;
            }
            return 1.0 * ControlPanel.TrbSpeed.Value / ControlPanel.TrbSpeed.Maximum;
        }

        /* we need to calculate wire currents for every iteration if someone is viewing a wire in the
        /* scope.  Otherwise we can do it only once per frame. */
        bool canDelayWireProcessing() {
            int i;
            for (i = 0; i != mScopeCount; i++) {
                if (mScopes[i].ViewingWire) {
                    return false;
                }
            }
            for (i = 0; i != ElmList.Count; i++) {
                if ((getElm(i) is ScopeElm) && ((ScopeElm)getElm(i)).elmScope.ViewingWire) {
                    return false;
                }
            }
            return true;
        }
    }
}
