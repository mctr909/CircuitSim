using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Circuit.Elements;

namespace Circuit {
    partial class CirSim {
        public CircuitElm getElm(int n) {
            if (n >= elmList.Count) {
                return null;
            }
            return elmList[n];
        }

        public void updateCircuit() {
            long mystarttime = DateTime.Now.ToFileTimeUtc();
            bool didAnalyze = analyzeFlag;
            if (analyzeFlag || dcAnalysisFlag) {
                mCir.AnalyzeCircuit();
                analyzeFlag = false;
            }

            if (mCir.StopElm != null && mCir.StopElm != mouseElm) {
                mCir.StopElm.SetMouseElm(true);
            }
            setupScopes();

            var g = backcontext;

            CircuitElm.TextColor = Color.Red;
            g.TextColor = Color.Red;
            CircuitElm.SelectColor = Color.Cyan;
            if (chkPrintableCheckItem.Checked) {
                CircuitElm.WhiteColor = Color.Black;
                CircuitElm.LightGrayColor = Color.Black;
                g.LineColor = Color.White;
            } else {
                CircuitElm.WhiteColor = Color.White;
                CircuitElm.LightGrayColor = Color.Gray;
                g.LineColor = Color.Black;
            }

            g.FillRectangle(0, 0, backcv.Width, backcv.Height);

            long myrunstarttime = DateTime.Now.ToFileTimeUtc();
            if (simRunning) {
                try {
                    runCircuit(didAnalyze);
                } catch (Exception e) {
                    Console.WriteLine("exception in runCircuit " + e + "\r\n" + e.StackTrace);
                    return;
                }
                myruntime += DateTime.Now.ToFileTimeUtc() - myrunstarttime;
            }

            long sysTime = DateTime.Now.ToFileTimeUtc();
            if (simRunning) {
                if (lastTime != 0) {
                    int inc = (int)(sysTime - lastTime);
                    double c = trbCurrentBar.Value;
                    c = Math.Exp(c / 3.5 - 14.2);
                    CircuitElm.CurrentMult = 1.7 * inc * c;
                }
                lastTime = sysTime;
            } else {
                lastTime = 0;
            }

            if (sysTime - secTime >= 1000) {
                framerate = frames;
                steprate = steps;
                frames = 0;
                steps = 0;
                secTime = sysTime;
            }

            long mydrawstarttime = DateTime.Now.ToFileTimeUtc();

            /* draw elements */
            g.SetTransform(new Matrix(transform[0], transform[1], transform[2], transform[3], transform[4], transform[5]));
            for (int i = 0; i != elmList.Count; i++) {
                getElm(i).Draw(g);
            }
            mydrawtime += DateTime.Now.ToFileTimeUtc() - mydrawstarttime;

            /* draw posts normally */
            if (mouseMode != MOUSE_MODE.DRAG_ROW && mouseMode != MOUSE_MODE.DRAG_COLUMN) {
                for (int i = 0; i != mCir.PostDrawList.Count; i++) {
                    g.DrawPost(mCir.PostDrawList[i]);
                }
            }

            /* for some mouse modes, what matters is not the posts but the endpoints (which are only
            /* the same for 2-terminal elements).  We draw those now if needed */
            if (tempMouseMode == MOUSE_MODE.DRAG_ROW
                || tempMouseMode == MOUSE_MODE.DRAG_COLUMN
                || tempMouseMode == MOUSE_MODE.DRAG_POST
                || tempMouseMode == MOUSE_MODE.DRAG_SELECTED) {
                for (int i = 0; i != elmList.Count; i++) {
                    var ce = getElm(i);
                    g.DrawPost(ce.X1, ce.Y1);
                    g.DrawPost(ce.X2, ce.Y2);
                    if (ce != mouseElm || tempMouseMode != MOUSE_MODE.DRAG_POST) {
                        g.FillCircle(Color.Gray, ce.X1, ce.Y1, 3.5f);
                        g.FillCircle(Color.Gray, ce.X2, ce.Y2, 3.5f);
                    } else {
                        ce.DrawHandles(g);
                    }
                }
            }

            /* draw handles for elm we're creating */
            if (tempMouseMode == MOUSE_MODE.SELECT && mouseElm != null) {
                mouseElm.DrawHandles(g);
            }

            /* draw handles for elm we're dragging */
            if (dragElm != null && (dragElm.X1 != dragElm.X2 || dragElm.Y1 != dragElm.Y2)) {
                dragElm.Draw(g);
                dragElm.DrawHandles(g);
            }

            /* draw bad connections.  do this last so they will not be overdrawn. */
            for (int i = 0; i != mCir.BadConnectionList.Count; i++) {
                var cn = mCir.BadConnectionList[i];
                g.FillCircle(Color.Red, cn.X, cn.Y, 3.5f);
            }

            if (0 < selectedArea.Width) {
                g.LineColor = CircuitElm.SelectColor;
                g.DrawRectangle(selectedArea.X, selectedArea.Y, selectedArea.Width, selectedArea.Height);
            }

            if (chkCrossHairCheckItem.Checked && mouseCursorX >= 0
                && mouseCursorX <= circuitArea.Width && mouseCursorY <= circuitArea.Height) {
                int x = snapGrid(inverseTransformX(mouseCursorX));
                int y = snapGrid(inverseTransformY(mouseCursorY));
                g.LineColor = Color.Gray;
                g.DrawLine(x, inverseTransformY(0), x, inverseTransformY(circuitArea.Height));
                g.DrawLine(inverseTransformX(0), y, inverseTransformX(circuitArea.Width), y);
            }

            g.ClearTransform();

            Color bCircuitArea;
            if (chkPrintableCheckItem.Checked) {
                bCircuitArea = Color.White;
            } else {
                bCircuitArea = Color.Black;
            }
            g.FillRectangle(bCircuitArea, 0, circuitArea.Height, circuitArea.Width, backcv.Height - circuitArea.Height);

            int ct = scopeCount;
            if (mCir.StopMessage != null) {
                ct = 0;
            }
            for (int i = 0; i != ct; i++) {
                scopes[i].draw(g);
            }
            if (mouseWasOverSplitter) {
                g.LineColor = Color.Cyan;
                g.DrawLine(0, circuitArea.Height - 2, circuitArea.Width, circuitArea.Height - 2);
            }

            if (mCir.StopMessage != null) {
                g.DrawLeftText(mCir.StopMessage, 10, circuitArea.Height - 10);
            } else {
                var info = new string[10];
                if (mouseElm != null) {
                    if (mousePost == -1) {
                        mouseElm.GetInfo(info);
                    } else {
                        info[0] = "V = " + mouseElm.DispPostVoltage(mousePost);
                    }
                } else {
                    info[0] = "t = " + CircuitElm.getTimeText(t);
                    info[1] = "time step = " + CircuitElm.getTimeText(timeStep);
                }
                if (Hint.Type != -1) {
                    int infoIdx;
                    for (infoIdx = 0; info[infoIdx] != null; infoIdx++) ;
                    var s = Hint.getHint(elmList);
                    if (s == null) {
                        Hint.Type = -1;
                    } else {
                        info[infoIdx] = s;
                    }
                }
                int x = 0;
                if (ct != 0) {
                    x = scopes[ct - 1].rightEdge() + 20;
                }
                x = Math.Max(x, backcv.Width * 2 / 3);

                /* count lines of data */
                {
                    int infoIdx;
                    for (infoIdx = 0; infoIdx < info.Length - 1 && info[infoIdx] != null; infoIdx++) ;
                    int badnodes = mCir.BadConnectionList.Count;
                    if (badnodes > 0) {
                        info[infoIdx++] = badnodes + ((badnodes == 1) ? " bad connection" : " bad connections");
                    }
                }
                int ybase = circuitArea.Height;
                for (int i = 0; i < info.Length && info[i] != null; i++) {
                    g.DrawLeftText(info[i], x, ybase + 15 * (i + 1));
                }
            }

            if (mCir.StopElm != null && mCir.StopElm != mouseElm) {
                mCir.StopElm.SetMouseElm(false);
            }
            frames++;

            if (null != picCir.Image) {
                picCir.Image.Dispose();
                picCir.Image = null;
            }
            if (null != cv || null != cvcontext) {
                if (null == cvcontext) {
                    cv.Dispose();
                    cv = null;
                } else {
                    cvcontext.Dispose();
                    cvcontext = null;
                }
            }

            g.FillCircle(Color.White, mouseCursorX, mouseCursorY, 2);
            cv = new Bitmap(backcv.Width, backcv.Height);
            cvcontext = Graphics.FromImage(cv);
            cvcontext.DrawImage(backcv, 0, 0);
            picCir.Image = cv;

            /* if we did DC analysis, we need to re-analyze the circuit with that flag cleared. */
            if (dcAnalysisFlag) {
                dcAnalysisFlag = false;
                analyzeFlag = true;
            }

            lastFrameTime = lastTime;
            mytime = mytime + DateTime.Now.ToFileTimeUtc() - mystarttime;
            myframes++;
        }

        void runCircuit(bool didAnalyze) {
            if (mCir.Matrix == null || elmList.Count == 0) {
                mCir.Matrix = null;
                return;
            }

            bool debugprint = dumpMatrix;
            dumpMatrix = false;
            double steprate = getIterCount();
            long tm = DateTime.Now.ToFileTimeUtc();
            long lit = lastIterTime;
            if (lit == 0) {
                lastIterTime = tm;
                return;
            }

            /* Check if we don't need to run simulation (for very slow simulation speeds).
            /* If the circuit changed, do at least one iteration to make sure everything is consistent. */
            if (12500 >= steprate * (tm - lastIterTime) && !didAnalyze) {
                return;
            }

            bool delayWireProcessing = canDelayWireProcessing();

            int iter;
            for (iter = 1; ; iter++) {
                int i;

                for (i = 0; i != elmList.Count; i++) {
                    var ce = getElm(i);
                    ce.StartIteration();
                }
                steps++;

                if (!mCir.Run(debugprint)) {
                    break;
                }

                t += timeStep;
                for (i = 0; i != elmList.Count; i++) {
                    getElm(i).StepFinished();
                }
                if (!delayWireProcessing) {
                    mCir.CalcWireCurrents();
                }
                for (i = 0; i != scopeCount; i++) {
                    scopes[i].timeStep();
                }
                for (i = 0; i != elmList.Count; i++) {
                    if (getElm(i) is ScopeElm) {
                        ((ScopeElm)getElm(i)).stepScope();
                    }
                }

                tm = DateTime.Now.ToFileTimeUtc();
                lit = tm;
                /* Check whether enough time has elapsed to perform an *additional* iteration after
                /* those we have already completed. */
                if ((iter + 1) * 1000 >= steprate * (tm - lastIterTime) || (tm - lastFrameTime > 250000)) {
                    break;
                }
                if (!simRunning) {
                    break;
                }
            } /* for (iter = 1; ; iter++) */

            lastIterTime = lit;
            if (delayWireProcessing) {
                mCir.CalcWireCurrents();
            }
            /* Console.WriteLine((DateTime.Now.ToFileTimeUtc() - lastFrameTime) / (double)iter); */
        }

        void setupScopes() {
            /* check scopes to make sure the elements still exist, and remove
            /* unused scopes/columns */
            int pos = -1;
            for (int i = 0; i < scopeCount; i++) {
                if (scopes[i].needToRemove()) {
                    int j;
                    for (j = i; j != scopeCount; j++) {
                        scopes[j] = scopes[j + 1];
                    }
                    scopeCount--;
                    i--;
                    continue;
                }
                if (scopes[i].Position > pos + 1) {
                    scopes[i].Position = pos + 1;
                }
                pos = scopes[i].Position;
            }

            while (scopeCount > 0 && scopes[scopeCount - 1].getElm() == null) {
                scopeCount--;
            }

            int h = backcv.Height - circuitArea.Height;
            pos = 0;
            for (int i = 0; i != scopeCount; i++) {
                scopeColCount[i] = 0;
            }
            for (int i = 0; i != scopeCount; i++) {
                pos = Math.Max(scopes[i].Position, pos);
                scopeColCount[scopes[i].Position]++;
            }
            int colct = pos + 1;
            int iw = infoWidth;
            if (colct <= 2) {
                iw = iw * 3 / 2;
            }
            int w = (backcv.Width - iw) / colct;
            int marg = 10;
            if (w < marg * 2) {
                w = marg * 2;
            }

            pos = -1;
            int colh = 0;
            int row = 0;
            int speed = 0;
            for (int i = 0; i != scopeCount; i++) {
                var s = scopes[i];
                if (s.Position > pos) {
                    pos = s.Position;
                    colh = h / scopeColCount[pos];
                    row = 0;
                    speed = s.Speed;
                }
                s.StackCount = scopeColCount[pos];
                if (s.Speed != speed) {
                    s.Speed = speed;
                    s.resetGraph();
                }
                var r = new Rectangle(pos * w, backcv.Height - h + colh * row, w - marg, colh);
                row++;
                if (!r.Equals(s.BoundingBox)) {
                    s.setRect(r);
                }
            }
        }

        public double getIterCount() {
            /* IES - remove interaction */
            if (trbSpeedBar.Value == 0) {
                return 0;
            }
            return 1.0 * trbSpeedBar.Value / trbSpeedBar.Maximum;
        }

        /* we need to calculate wire currents for every iteration if someone is viewing a wire in the
        /* scope.  Otherwise we can do it only once per frame. */
        bool canDelayWireProcessing() {
            int i;
            for (i = 0; i != scopeCount; i++) {
                if (scopes[i].viewingWire()) {
                    return false;
                }
            }
            for (i = 0; i != elmList.Count; i++) {
                if ((getElm(i) is ScopeElm) && ((ScopeElm)getElm(i)).elmScope.viewingWire()) {
                    return false;
                }
            }
            return true;
        }
    }
}
