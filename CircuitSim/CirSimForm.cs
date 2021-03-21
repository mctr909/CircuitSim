using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Circuit.Elements;

namespace Circuit {
    partial class CirSim {
        public CirSim() {
            Sim = this;
            mCir = new Circuit(this);
            mMenuItems = new MenuItems(this);
            ControlPanel.Init(this);
        }

        public void init(Form parent) {
            mParent = parent;
            mParent.KeyPreview = true;
            mParent.KeyDown += onKeyDown;
            mParent.KeyUp += onKeyUp;

            ElmList = new List<CircuitElm>();
            mRedoItem = new MenuItem();
            mUndoItem = new MenuItem();
            mPasteItem = new MenuItem();
            Transform = new float[] { 1, 0, 0, 1, 0, 0 };

            mScopes = new Scope[20];
            mScopeColCount = new int[20];
            mScopeCount = 0;

            setTimer();

            CircuitElm.InitClass(this, mCir);
            readRecovery();

            mMenuBar = new MenuStrip();
            {
                mMenuItems.composeMainMenu(mMenuBar);
                parent.Controls.Add(mMenuBar);
            }

            mElementMenu = new ElementMenu(this);

            mPixCir = new PictureBox() { Left = 0, Top = mMenuBar.Height };
            {
                mPixCir.MouseDown += new MouseEventHandler((s, e) => { onMouseDown(e); });
                mPixCir.MouseMove += new MouseEventHandler((s, e) => { onMouseMove(e); });
                mPixCir.MouseUp += new MouseEventHandler((s, e) => { onMouseUp(e); });
                mPixCir.MouseWheel += new MouseEventHandler((s, e) => { onMouseWheel((PictureBox)s, e); });
                mPixCir.MouseClick += new MouseEventHandler((s, e) => { onClick((PictureBox)s, e); });
                mPixCir.MouseLeave += new EventHandler((s, e) => { onMouseLeave(); });
                mPixCir.DoubleClick += new EventHandler((s, e) => { onDoubleClick(e); });
            }

            mSplitContainer = new SplitContainer();
            {
                mSplitContainer.Dock = DockStyle.Fill;
                mSplitContainer.BorderStyle = BorderStyle.FixedSingle;
                mSplitContainer.IsSplitterFixed = true;
                mSplitContainer.Panel1.Controls.Add(mPixCir);
                mSplitContainer.Panel2.Controls.Add(ControlPanel.VerticalPanel);
                ControlPanel.VerticalPanel.Top = Sim.mMenuBar.Height;
                int width = ControlPanel.VerticalPanel.Width;
                mSplitContainer.SizeChanged += new EventHandler((s, e) => {
                    if (0 <= mSplitContainer.Width - width) {
                        mSplitContainer.SplitterDistance = mSplitContainer.Width - width;
                        setCanvasSize();
                    }
                });
                parent.Controls.Add(mSplitContainer);
            }

            CircuitElm.SetColorScale(64);

            readCircuit("");

            enableUndoRedo();
            enablePaste();
            ControlPanel.SetiFrameHeight();

            mScopePopupMenu = new ScopePopupMenu(this);

            SetSimRunning(true);
        }

        #region Public method
        public void SetSimRunning(bool s) {
            if (s) {
                if (mCir.StopMessage != null) {
                    return;
                }
                mSimRunning = true;
                ControlPanel.BtnRunStop.Text = "RUN";
            } else {
                mSimRunning = false;
                mAnalyzeFlag = false;
                ControlPanel.BtnRunStop.Text = "STOP";
                Repaint();
            }
        }

        public bool SimIsRunning() {
            return mSimRunning;
        }

        public void Repaint() {
            if (!mNeedsRepaint) {
                mNeedsRepaint = true;
                updateCircuit();
                mNeedsRepaint = false;
            }
        }

        public Color getBackgroundColor() {
            if (ControlPanel.ChkPrintable.Checked) {
                return Color.White;
            }
            return Color.Black;
        }

        public void NeedAnalyze() {
            mAnalyzeFlag = true;
            Repaint();
        }

        public Adjustable FindAdjustable(CircuitElm elm, int item) {
            for (int i = 0; i != Adjustables.Count; i++) {
                var a = Adjustables[i];
                if (a.Elm == elm && a.EditItem == item) {
                    return a;
                }
            }
            return null;
        }

        public void MenuPerformed(MENU_CATEGORY cat, MENU_ITEM item, string option = "") {
            if (item == MENU_ITEM.OPEN_FILE) {
                doOpenFile();
            }
            if (item == MENU_ITEM.SAVE_FILE) {
                doSaveFile();
            }
            if (item == MENU_ITEM.createsubcircuit) {
                doCreateSubcircuit();
            }
            if (item == MENU_ITEM.dcanalysis) {
                doDCAnalysis();
            }
            if (item == MENU_ITEM.print) {
                mBackContext.Print();
            }
            if (item == MENU_ITEM.recover) {
                doRecover();
            }

            if ((cat == MENU_CATEGORY.ELEMENTS || cat == MENU_CATEGORY.SCOPE_POP) && mPopupMenu != null) {
                mPopupMenu.Close();
            }

            if (cat == MENU_CATEGORY.KEY && mMouseElm != null) {
                mMenuElm = mMouseElm;
                cat = MENU_CATEGORY.ELEMENTS;
            }

            if (item == MENU_ITEM.UNDO) {
                doUndo();
            }
            if (item == MENU_ITEM.REDO) {
                doRedo();
            }
            if (item == MENU_ITEM.CUT) {
                if (cat != MENU_CATEGORY.ELEMENTS) {
                    mMenuElm = null;
                }
                doCut();
            }
            if (item == MENU_ITEM.COPY) {
                if (cat != MENU_CATEGORY.ELEMENTS) {
                    mMenuElm = null;
                }
                doCopy();
            }
            if (item == MENU_ITEM.PASTE) {
                doPaste(null);
            }
            if (item == MENU_ITEM.DELETE) {
                if (cat != MENU_CATEGORY.ELEMENTS) {
                    mMenuElm = null;
                }
                PushUndo();
                doDelete(true);
            }
            if (item == MENU_ITEM.DUPLICATE) {
                if (cat != MENU_CATEGORY.ELEMENTS) {
                    mMenuElm = null;
                }
                doDuplicate();
            }
            if (item == MENU_ITEM.SELECT_ALL) {
                doSelectAll();
            }

            if (item == MENU_ITEM.ZOOM_IN) {
                zoomCircuit(20);
            }
            if (item == MENU_ITEM.ZOOM_OUT) {
                zoomCircuit(-20);
            }
            if (item == MENU_ITEM.ZOOM_100) {
                setCircuitScale(1);
            }
            if (item == MENU_ITEM.CENTER_CIRCUIT) {
                PushUndo();
                centreCircuit();
            }

            if (item == MENU_ITEM.flip) {
                doFlip();
            }
            if (item == MENU_ITEM.split) {
                doSplit(mMenuElm);
            }

            if (item == MENU_ITEM.STACK_ALL) {
                stackAll();
            }
            if (item == MENU_ITEM.UNSTACK_ALL) {
                unstackAll();
            }
            if (item == MENU_ITEM.COMBINE_ALL) {
                combineAll();
            }
            if (item == MENU_ITEM.SEPARATE_ALL) {
                separateAll();
            }

            if (cat == MENU_CATEGORY.ELEMENTS && item == MENU_ITEM.edit) {
                doEdit(mMenuElm);
            }
            if (item == MENU_ITEM.sliders) {
                doSliders(mMenuElm);
            }

            if (item == MENU_ITEM.VIEW_IN_SCOPE && mMenuElm != null) {
                int i;
                for (i = 0; i != mScopeCount; i++) {
                    if (mScopes[i].Elm == null) {
                        break;
                    }
                }
                if (i == mScopeCount) {
                    if (mScopeCount == mScopes.Length) {
                        return;
                    }
                    mScopeCount++;
                    mScopes[i] = new Scope(this);
                    mScopes[i].Position = i;
                }
                mScopes[i].SetElm(mMenuElm);
                if (i > 0) {
                    mScopes[i].Speed = mScopes[i - 1].Speed;
                }
            }

            if (item == MENU_ITEM.VIEW_IN_FLOAT_SCOPE && mMenuElm != null) {
                var newScope = new ScopeElm(snapGrid(mMenuElm.X1 + 50), snapGrid(mMenuElm.Y1 + 50));
                ElmList.Add(newScope);
                newScope.setScopeElm(mMenuElm);
            }

            if (cat == MENU_CATEGORY.SCOPE_POP) {
                PushUndo();
                Scope s;
                if (mMenuScope != -1) {
                    s = mScopes[mMenuScope];
                } else {
                    if (mMouseElm is ScopeElm) {
                        s = ((ScopeElm)mMouseElm).elmScope;
                    } else {
                        return;
                    }
                }

                if (item == MENU_ITEM.DOCK) {
                    if (mScopeCount == mScopes.Length) {
                        return;
                    }
                    mScopes[mScopeCount] = ((ScopeElm)mMouseElm).elmScope;
                    ((ScopeElm)mMouseElm).clearElmScope();
                    mScopes[mScopeCount].Position = mScopeCount;
                    mScopeCount++;
                    doDelete(false);
                }
                if (item == MENU_ITEM.UNDOCK && 0 <= mMenuScope) {
                    var newScope = new ScopeElm(snapGrid(mMenuElm.X1 + 50), snapGrid(mMenuElm.Y1 + 50));
                    ElmList.Add(newScope);
                    newScope.setElmScope(mScopes[mMenuScope]);
                    /* remove scope from list.  setupScopes() will fix the positions */
                    for (int i = mMenuScope; i < mScopeCount; i++) {
                        mScopes[i] = mScopes[i + 1];
                    }
                    mScopeCount--;
                }
                if (null != s) {
                    if (item == MENU_ITEM.REMOVE_SCOPE) {
                        s.SetElm(null);  /* setupScopes() will clean this up */
                    }
                    if (item == MENU_ITEM.REMOVE_PLOT) {
                        s.RemovePlot(mMenuPlot);
                    }
                    if (item == MENU_ITEM.speed2) {
                        s.SpeedUp();
                    }
                    if (item == MENU_ITEM.speed1_2) {
                        s.SlowDown();
                    }
                    if (item == MENU_ITEM.MAX_SCALE) {
                        s.MaxScale();
                    }
                    if (item == MENU_ITEM.STACK) {
                        stackScope(mMenuScope);
                    }
                    if (item == MENU_ITEM.UNSTACK) {
                        unstackScope(mMenuScope);
                    }
                    if (item == MENU_ITEM.COMBINE) {
                        combineScope(mMenuScope);
                    }
                    if (item == MENU_ITEM.RESET) {
                        s.ResetGraph(true);
                    }
                    if (item == MENU_ITEM.PROPERTIES) {
                        s.Properties(mParent);
                    }
                }
                deleteUnusedScopeElms();
            }

            /* IES: Moved from itemStateChanged() */
            if (cat == MENU_CATEGORY.MAIN) {
                if (mPopupMenu != null) {
                    mPopupMenu.Close();
                }
                setMouseMode(MOUSE_MODE.ADD_ELM);
                if (item != MENU_ITEM.INVALID) {
                    mMouseMode = item;
                }
                if (item == MENU_ITEM.SELECT) {
                    setMouseMode(MOUSE_MODE.SELECT);
                }
                TempMouseMode = MouseMode;
            }
            Repaint();
        }

        /* delete sliders for an element */
        public void DeleteSliders(CircuitElm elm) {
            if (Adjustables == null) {
                return;
            }
            for (int i = Adjustables.Count - 1; i >= 0; i--) {
                var adj = Adjustables[i];
                if (adj.Elm == elm) {
                    adj.DeleteSlider();
                    Adjustables.RemoveAt(i);
                }
            }
        }

        public int LocateElm(CircuitElm elm) {
            for (int i = 0; i != ElmList.Count; i++) {
                if (elm == ElmList[i]) {
                    return i;
                }
            }
            return -1;
        }


        public bool DialogIsShowing() {
            if (EditDialog != null && EditDialog.Visible) {
                return true;
            }
            if (SliderDialog != null && SliderDialog.Visible) {
                return true;
            }
            if (CustomLogicEditDialog != null && CustomLogicEditDialog.Visible) {
                return true;
            }
            if (DiodeModelEditDialog != null && DiodeModelEditDialog.Visible) {
                return true;
            }
            if (DialogShowing != null && DialogShowing.Visible) {
                return true;
            }
            if (mPopupMenu != null && mPopupMenu.Visible) {
                return true;
            }
            if (mScrollValuePopup != null && mScrollValuePopup.Visible) {
                return true;
            }
            // TODO: dialogIsShowing
            //if (aboutBox != null && aboutBox.isShowing())
            //    return true;
            return false;
        }

        public void UpdateModels() {
            for (int i = 0; i != ElmList.Count; i++) {
                ElmList[i].UpdateModels();
            }
        }

        public void ResetButton_onClick() {
            for (int i = 0; i != ElmList.Count; i++) {
                getElm(i).Reset();
            }
            for (int i = 0; i != mScopeCount; i++) {
                mScopes[i].ResetGraph(true);
            }
            mAnalyzeFlag = true;
            if (Time == 0) {
                SetSimRunning(true);
            } else {
                Time = 0;
            }
        }

        public void PushUndo() {
            mRedoStack.Clear();
            string s = dumpCircuit();
            if (mUndoStack.Count > 0 && s == mUndoStack[mUndoStack.Count - 1]) {
                return;
            }
            mUndoStack.Add(s);
            enableUndoRedo();
        }

        /* convert grid coordinates to screen coordinates */
        public int TransformX(double x) {
            return (int)((x * Transform[0]) + Transform[4]);
        }
        public int TransformY(double y) {
            return (int)((y * Transform[3]) + Transform[5]);
        }
        #endregion

        #region Private methond
        void onKeyDown(object sender, KeyEventArgs e) {
            mIsPressShift = e.Shift;
            mIsPressCtrl = e.Control;
            mIsPressAlt = e.Alt;
        }

        void onKeyUp(object sender, KeyEventArgs e) {
            mIsPressShift = false;
            mIsPressCtrl = false;
            mIsPressAlt = false;
            mParent.Cursor = Cursors.Arrow;
            onPreviewNativeEvent(e);
        }

        void onClick(Control s, MouseEventArgs e) {
            if (e.Button == MouseButtons.Middle) {
                scrollValues(0);
            }
            if (e.Button == MouseButtons.Right) {
                onContextMenu(s, e);
            }
        }

        void onDoubleClick(EventArgs e) {
            if (mMouseElm != null && !(mMouseElm is SwitchElm)) {
                doEdit(mMouseElm);
            }
        }

        void onMouseDown(MouseEventArgs e) {
            mCir.StopElm = null; /* if stopped, allow user to select other elements to fix circuit */
            mMenuX = mMenuClientX = MouseCursorX = e.X;
            mMenuY = mMenuClientY = MouseCursorY = e.Y;
            mMouseButton = e.Button;
            mMouseDownTime = DateTime.Now.ToFileTimeUtc();

            /* maybe someone did copy in another window?  should really do this when */
            /* window receives focus */
            enablePaste();

            if (mMouseButton != MouseButtons.Left && mMouseButton != MouseButtons.Middle) {
                return;
            }

            // set mouseElm in case we are on mobile
            mouseSelect();

            mouseDragging = true;

            if (mMouseWasOverSplitter) {
                TempMouseMode = MOUSE_MODE.DRAG_SPLITTER;
                return;
            }
            if (mMouseButton == MouseButtons.Left) {
                /* left mouse */
                TempMouseMode = MouseMode;
                if (mIsPressCtrl && mIsPressShift) {
                    TempMouseMode = MOUSE_MODE.DRAG_COLUMN;
                    mParent.Cursor = Cursors.SizeWE;
                } else if (mIsPressCtrl && mIsPressAlt) {
                    TempMouseMode = MOUSE_MODE.DRAG_ROW;
                    mParent.Cursor = Cursors.SizeNS;
                } else if (mIsPressCtrl) {
                    TempMouseMode = MOUSE_MODE.DRAG_POST;
                    mParent.Cursor = Cursors.Arrow;
                } else if (mIsPressAlt) {
                    TempMouseMode = MOUSE_MODE.DRAG_ALL;
                    mParent.Cursor = Cursors.NoMove2D;
                } else {
                    TempMouseMode = MOUSE_MODE.SELECT;
                    mParent.Cursor = Cursors.SizeAll;
                }
            }

            if ((ScopeSelected != -1 && mScopes[ScopeSelected].CursorInSettingsWheel) ||
                (ScopeSelected == -1 && mMouseElm != null && (mMouseElm is ScopeElm) && ((ScopeElm)mMouseElm).elmScope.CursorInSettingsWheel)) {
                Console.WriteLine("Doing something");
                Scope s;
                if (ScopeSelected != -1) {
                    s = mScopes[ScopeSelected];
                } else {
                    s = ((ScopeElm)mMouseElm).elmScope;
                }
                s.Properties(mParent);
                clearSelection();
                mouseDragging = false;
                return;
            }

            int gx = inverseTransformX(e.X);
            int gy = inverseTransformY(e.Y);
            if (doSwitch(gx, gy)) {
                /* do this BEFORE we change the mouse mode to MODE_DRAG_POST!  Or else logic inputs */
                /* will add dots to the whole circuit when we click on them! */
                return;
            }

            /* IES - Grab resize handles in select mode if they are far enough apart and you are on top of them */
            if (TempMouseMode == MOUSE_MODE.SELECT && mMouseElm != null
                && mMouseElm.GetHandleGrabbedClose(gx, gy, POSTGRABSQ, MINPOSTGRABSIZE) >= 0
                && !anySelectedButMouse()) {
                TempMouseMode = MOUSE_MODE.DRAG_POST;
            }

            if (TempMouseMode != MOUSE_MODE.SELECT && TempMouseMode != MOUSE_MODE.DRAG_SELECTED) {
                clearSelection();
            }

            PushUndo();
            mInitDragGridX = gx;
            mInitDragGridY = gy;
            if (TempMouseMode != MOUSE_MODE.ADD_ELM) {
                return;
            }
            /* */
            int x0 = snapGrid(gx);
            int y0 = snapGrid(gy);
            if (!mCircuitArea.Contains(MouseCursorX, MouseCursorY)) {
                return;
            }

            DragElm = MenuItems.constructElement(mMouseMode, x0, y0);
        }

        void onMouseUp(MouseEventArgs e) {
            mouseDragging = false;
            mMouseButton = MouseButtons.None;

            /* click to clear selection */
            if (TempMouseMode == MOUSE_MODE.SELECT && mSelectedArea.Width == 0) {
                clearSelection();
            }

            /* cmd-click = split wire */
            if (TempMouseMode == MOUSE_MODE.DRAG_POST && mDraggingPost == -1) {
                doSplit(mMouseElm);
            }

            TempMouseMode = MouseMode;
            mSelectedArea = new Rectangle();
            bool circuitChanged = false;
            if (mHeldSwitchElm != null) {
                mHeldSwitchElm.mouseUp();
                mHeldSwitchElm = null;
                circuitChanged = true;
            }
            if (DragElm != null) {
                /* if the element is zero size then don't create it */
                /* IES - and disable any previous selection */
                if (DragElm.IsCreationFailed) {
                    DragElm.Delete();
                    if (MouseMode == MOUSE_MODE.SELECT || MouseMode == MOUSE_MODE.DRAG_SELECTED) {
                        clearSelection();
                    }
                } else {
                    ElmList.Add(DragElm);
                    DragElm.DraggingDone();
                    circuitChanged = true;
                    writeRecoveryToStorage();
                }
                DragElm = null;
            }
            if (circuitChanged) {
                NeedAnalyze();
            }
            if (DragElm != null) {
                DragElm.Delete();
            }
            DragElm = null;
            Repaint();
        }

        void onMouseWheel(Control sender, MouseEventArgs e) {
            /* once we start zooming, don't allow other uses of mouse wheel for a while */
            /* so we don't accidentally edit a resistor value while zooming */
            bool zoomOnly = DateTime.Now.ToFileTimeUtc() < mZoomTime + 1000;

            if (!zoomOnly) {
                scrollValues(e.Delta);
            }
            // TODO: onMouseWheel
            //if ((mouseElm is MouseWheelHandler) && !zoomOnly) {
            //    ((MouseWheelHandler)mouseElm).onMouseWheel(e);
            //}
            if (!DialogIsShowing()) {
                zoomCircuit(-e.Delta);
                mZoomTime = DateTime.Now.ToFileTimeUtc();
            }
            Repaint();
        }

        void onMouseMove(MouseEventArgs e) {
            MouseCursorX = e.X;
            MouseCursorY = e.Y;
            if (33 < (DateTime.Now - mLastMouseMove).Milliseconds) {
                mLastMouseMove = DateTime.Now;
            } else {
                return;
            }
            if (mouseDragging) {
                mouseDragged();
                return;
            }
            mouseSelect();
        }

        void onMouseLeave() {
            MouseCursorX = -1;
            MouseCursorY = -1;
        }

        void setTimer() {
            mTimer = new Timer();
            mTimer.Tick += new EventHandler((s, e) => {
                if (mSimRunning) {
                    updateCircuit();
                    mNeedsRepaint = false;
                }
            });
            mTimer.Interval = 1;
            mTimer.Enabled = true;
            mTimer.Start();
        }

        void setCanvasSize() {
            int width = mSplitContainer.Panel1.Width;
            int height = mSplitContainer.Panel1.Height - mMenuBar.Height;
            if (width < 1) {
                width = 1;
            }
            if (height < 1) {
                height = 1;
            }
            var isRunning = SimIsRunning();
            if (isRunning) {
                SetSimRunning(false);
            }

            mPixCir.Width = width;
            mPixCir.Height = height;
            if (mBackContext != null) {
                mBackContext.Dispose();
            }
            mBackContext = CustomGraphics.FromImage(width, height);
            setCircuitArea();
            SetSimRunning(isRunning);
        }

        void setCircuitArea() {
            int height = mPixCir.Height;
            int width = mPixCir.Width;
            int h = (int)(height * mScopeHeightFraction);
            mCircuitArea = new Rectangle(0, 0, width, height - h);
        }

        void centreCircuit() {
            var bounds = getCircuitBounds();

            double scale = 1;

            if (0 < bounds.Width) {
                /* add some space on edges because bounds calculation is not perfect */
                scale = Math.Min(
                    mCircuitArea.Width / (double)(bounds.Width + 140),
                    mCircuitArea.Height / (double)(bounds.Height + 100));
            }
            scale = Math.Min(scale, 1.5); // Limit scale so we don't create enormous circuits in big windows

            /* calculate transform so circuit fills most of screen */
            Transform[0] = Transform[3] = (float)scale;
            Transform[1] = Transform[2] = Transform[4] = Transform[5] = 0;
            if (0 < bounds.Width) {
                Transform[4] = (float)((mCircuitArea.Width - bounds.Width * scale) / 2 - bounds.X * scale);
                Transform[5] = (float)((mCircuitArea.Height - bounds.Height * scale) / 2 - bounds.Y * scale);
            }
        }

        /* get circuit bounds.  remember this doesn't use setBbox().  That is calculated when we draw */
        /* the circuit, but this needs to be ready before we first draw it, so we use this crude method */
        Rectangle getCircuitBounds() {
            int i;
            int minx = 1000, maxx = 0, miny = 1000, maxy = 0;
            for (i = 0; i != ElmList.Count; i++) {
                var ce = getElm(i);
                /* centered text causes problems when trying to center the circuit, */
                /* so we special-case it here */
                if (!ce.IsCenteredText) {
                    minx = Math.Min(ce.X1, Math.Min(ce.X2, minx));
                    maxx = Math.Max(ce.X1, Math.Max(ce.X2, maxx));
                }
                miny = Math.Min(ce.Y1, Math.Min(ce.Y2, miny));
                maxy = Math.Max(ce.Y1, Math.Max(ce.Y2, maxy));
            }
            if (minx > maxx) {
                return new Rectangle();
            }
            return new Rectangle(minx, miny, maxx - minx, maxy - miny);
        }

        void stackScope(int s) {
            if (s == 0) {
                if (mScopeCount < 2) {
                    return;
                }
                s = 1;
            }
            if (mScopes[s].Position == mScopes[s - 1].Position) {
                return;
            }
            mScopes[s].Position = mScopes[s - 1].Position;
            for (s++; s < mScopeCount; s++) {
                mScopes[s].Position--;
            }
        }

        void unstackScope(int s) {
            if (s == 0) {
                if (mScopeCount < 2) {
                    return;
                }
                s = 1;
            }
            if (mScopes[s].Position != mScopes[s - 1].Position) {
                return;
            }
            for (; s < mScopeCount; s++) {
                mScopes[s].Position++;
            }
        }

        void combineScope(int s) {
            if (s == 0) {
                if (mScopeCount < 2) {
                    return;
                }
                s = 1;
            }
            mScopes[s - 1].Combine(mScopes[s]);
            mScopes[s].SetElm(null);
        }

        void stackAll() {
            for (int i = 0; i != mScopeCount; i++) {
                mScopes[i].Position = 0;
                mScopes[i].ShowMax = false;
                mScopes[i].ShowMin = false;
            }
        }

        void unstackAll() {
            for (int i = 0; i != mScopeCount; i++) {
                mScopes[i].Position = i;
                mScopes[i].ShowMax = true;
            }
        }

        void combineAll() {
            for (int i = mScopeCount - 2; i >= 0; i--) {
                mScopes[i].Combine(mScopes[i + 1]);
                mScopes[i + 1].SetElm(null);
            }
        }

        void separateAll() {
            var newscopes = new List<Scope>();
            int ct = 0;
            for (int i = 0; i < mScopeCount; i++) {
                ct = mScopes[i].Separate(newscopes, ct);
            }
            mScopes = newscopes.ToArray();
            mScopeCount = ct;
        }

        void doEdit(Editable eable) {
            clearSelection();
            PushUndo();
            if (EditDialog != null) {
                EditDialog.closeDialog();
                EditDialog = null;
            }
            EditDialog = new ElementInfoDialog(eable, this);
            EditDialog.Show(
                MouseCursorX + mParent.Location.X,
                MouseCursorY + mParent.Location.Y
            );
        }

        void doSliders(CircuitElm ce) {
            clearSelection();
            PushUndo();
            if (SliderDialog != null) {
                SliderDialog.closeDialog();
                SliderDialog = null;
            }
            SliderDialog = new SliderDialog(ce, this);
            SliderDialog.Show(mParent.Left + MouseCursorX, mParent.Top + MouseCursorY);
        }

        void doCreateSubcircuit() {
            var dlg = new EditCompositeModelDialog();
            if (!dlg.createModel()) {
                return;
            }
            dlg.createDialog();
            DialogShowing = dlg;
            DialogShowing.Show();
        }

        void doOpenFile() {
            var open = new OpenFileDialog();
            open.Filter = "テキストファイル(*.txt)|*.txt";
            open.ShowDialog();
            if (string.IsNullOrEmpty(open.FileName) || !Directory.Exists(Path.GetDirectoryName(open.FileName))) {
                return;
            }
            PushUndo();
            var fs = new StreamReader(open.FileName);
            var data = fs.ReadToEnd();
            fs.Close();
            fs.Dispose();
            readCircuit(data);
        }

        void doSaveFile() {
            var save = new SaveFileDialog();
            save.Filter = "テキストファイル(*.txt)|*.txt";
            save.ShowDialog();
            if (string.IsNullOrEmpty(save.FileName) || !Directory.Exists(Path.GetDirectoryName(save.FileName))) {
                return;
            }
            string dump = dumpCircuit();
            var fs = new StreamWriter(save.FileName);
            fs.Write(dump);
            fs.Close();
            fs.Dispose();
        }

        string dumpCircuit() {
            CustomLogicModel.clearDumpedFlags();
            CustomCompositeModel.clearDumpedFlags();
            DiodeModel.clearDumpedFlags();

            int f = ControlPanel.ChkShowDots.Checked ? 1 : 0;
            f |= ControlPanel.ChkShowVolts.Checked ? 0 : 4;
            f |= ControlPanel.ChkShowValues.Checked ? 0 : 16;

            /* 32 = linear scale in afilter */
            string dump = "$ " + f
                + " " + ControlPanel.TimeStep
                + " " + getIterCount()
                + " " + ControlPanel.TrbCurrent.Value
                + " " + ControlPanel.VoltageRange + "\n";

            int i;
            for (i = 0; i != ElmList.Count; i++) {
                var ce = getElm(i);
                string m = ce.DumpModel();
                if (!string.IsNullOrEmpty(m)) {
                    dump += m + "\n";
                }
                dump += ce.Dump + "\n";
            }
            for (i = 0; i != mScopeCount; i++) {
                string d = mScopes[i].Dump();
                if (d != null) {
                    dump += d + "\n";
                }
            }
            for (i = 0; i != Adjustables.Count; i++) {
                var adj = Adjustables[i];
                dump += "38 " + adj.Dump() + "\n";
            }
            if (Hint.Type != -1) {
                dump += "h " + Hint.Type + " " + Hint.Item1 + " " +
                Hint.Item2 + "\n";
            }

            return dump;
        }

        void readCircuit(string text, int flags) {
            readCircuit(Encoding.ASCII.GetBytes(text), flags);
        }

        void readCircuit(string text) {
            readCircuit(Encoding.ASCII.GetBytes(text), 0);
        }

        void readCircuit(byte[] b, int flags) {
            Console.WriteLine("readCircuit");
            int i;
            int len = b.Length;
            if ((flags & RC_RETAIN) == 0) {
                clearMouseElm();
                for (i = 0; i != ElmList.Count; i++) {
                    var ce = getElm(i);
                    ce.Delete();
                }
                ElmList.Clear();
                Hint.Type = -1;
                ControlPanel.Reset();
                mScopeCount = 0;
                mLastIterTime = 0;
            }

            bool subs = (flags & RC_SUBCIRCUITS) != 0;

            int p;
            for (p = 0; p < len;) {
                int l;
                int linelen = len - p; /* IES - changed to allow the last line to not end with a delim. */
                for (l = 0; l != len - p; l++) {
                    if (b[l + p] == '\n' || b[l + p] == '\r') {
                        linelen = l++;
                        if (l + p < b.Length && b[l + p] == '\n') {
                            l++;
                        }
                        break;
                    }
                }
                string line = Encoding.ASCII.GetString(b, p, linelen);
                var st = new StringTokenizer(line, " +\t\n\r\f");
                while (st.hasMoreTokens()) {
                    string type = st.nextToken();
                    int tint = type.ElementAt(0);
                    try {
                        if (subs && tint != '.') {
                            continue;
                        }
                        if (tint == 'o') {
                            var sc = new Scope(this);
                            sc.Position = mScopeCount;
                            sc.Undump(st);
                            mScopes[mScopeCount++] = sc;
                            break;
                        }
                        if (tint == 'h') {
                            readHint(st);
                            break;
                        }
                        if (tint == '$') {
                            readOptions(st);
                            break;
                        }
                        if (tint == '!') {
                            CustomLogicModel.undumpModel(st);
                            break;
                        }
                        if (tint == '%' || tint == '?' || tint == 'B') {
                            /* ignore afilter-specific stuff */
                            break;
                        }
                        /* do not add new symbols here without testing export as link */

                        /* if first character is a digit then parse the type as a number */
                        if (tint >= '0' && tint <= '9') {
                            tint = int.Parse(type);
                        }
                        if (tint == 34) {
                            DiodeModel.undumpModel(st);
                            break;
                        }
                        if (tint == 38) {
                            var adj = new Adjustable(st, this);
                            Adjustables.Add(adj);
                            break;
                        }
                        if (tint == '.') {
                            CustomCompositeModel.undumpModel(st);
                            break;
                        }
                        int x1 = st.nextTokenInt();
                        int y1 = st.nextTokenInt();
                        int x2 = st.nextTokenInt();
                        int y2 = st.nextTokenInt();
                        int f = st.nextTokenInt();
                        var dumpId = MenuItems.getDumpIdFromString(type);
                        var newce = MenuItems.createCe(dumpId, x1, y1, x2, y2, f, st);
                        if (newce == null) {
                            Console.WriteLine("unrecognized dump type: " + type);
                            break;
                        }
                        newce.SetPoints();
                        ElmList.Add(newce);
                    } catch (Exception ee) {
                        Console.WriteLine("exception while undumping " + ee);
                        Console.WriteLine(ee.StackTrace);
                        break;
                    }
                    break;
                }
                p += l;
            }

            enableItems();
            if ((flags & RC_RETAIN) == 0) {
                /* create sliders as needed */
                for (i = 0; i != Adjustables.Count; i++) {
                    Adjustables[i].CreateSlider();
                }
            }
            NeedAnalyze();
            if ((flags & RC_NO_CENTER) == 0) {
                centreCircuit();
            }
            if ((flags & RC_SUBCIRCUITS) != 0) {
                UpdateModels();
            }
            // TODO: readCircuit
            //AudioInputElm.clearCache();  /* to save memory */
        }

        void readHint(StringTokenizer st) {
            Hint.Type = st.nextTokenInt();
            Hint.Item1 = st.nextTokenInt();
            Hint.Item2 = st.nextTokenInt();
        }

        void readOptions(StringTokenizer st) {
            int flags = st.nextTokenInt();
            ControlPanel.ChkShowDots.Checked = (flags & 1) != 0;
            ControlPanel.ChkShowVolts.Checked = (flags & 4) == 0;
            ControlPanel.ChkShowValues.Checked = (flags & 16) == 0;

            ControlPanel.TimeStep = st.nextTokenDouble();
            double sp = st.nextTokenDouble();
            int sp2 = (int)(Math.Log(10 * sp) * 24 + 61.5);
            ControlPanel.TrbSpeed.Value = sp2;
            ControlPanel.TrbCurrent.Value = st.nextTokenInt();
            ControlPanel.VoltageRange = st.nextTokenDouble();
        }

        public int snapGrid(int x) {
            return (x + GRID_ROUND) & GRID_MASK;
        }

        bool doSwitch(int x, int y) {
            if (mMouseElm == null || !(mMouseElm is SwitchElm)) {
                return false;
            }
            var se = (SwitchElm)mMouseElm;
            if (!se.getSwitchRect().Contains(x, y)) {
                return false;
            }
            se.toggle();
            if (se.momentary) {
                mHeldSwitchElm = se;
            }
            NeedAnalyze();
            return true;
        }

        void mouseDragged() {
            /* ignore right mouse button with no modifiers (needed on PC) */
            if (mMouseButton == MouseButtons.Right) {
                return;
            }

            if (TempMouseMode == MOUSE_MODE.DRAG_SPLITTER) {
                dragSplitter(MouseCursorX, MouseCursorY);
                return;
            }
            int gx = inverseTransformX(MouseCursorX);
            int gy = inverseTransformY(MouseCursorY);
            if (!mCircuitArea.Contains(MouseCursorX, MouseCursorY)) {
                return;
            }
            bool changed = false;
            if (DragElm != null) {
                DragElm.Drag(gx, gy);
            }
            bool success = true;
            switch (TempMouseMode) {
            case MOUSE_MODE.DRAG_ALL:
                dragAll(MouseCursorX, MouseCursorY);
                break;
            case MOUSE_MODE.DRAG_ROW:
                dragRow(snapGrid(gx), snapGrid(gy));
                changed = true;
                break;
            case MOUSE_MODE.DRAG_COLUMN:
                dragColumn(snapGrid(gx), snapGrid(gy));
                changed = true;
                break;
            case MOUSE_MODE.DRAG_POST:
                if (mMouseElm != null) {
                    dragPost(snapGrid(gx), snapGrid(gy));
                    changed = true;
                }
                break;
            case MOUSE_MODE.SELECT:
                if (mMouseElm == null) {
                    selectArea(gx, gy);
                } else {
                    /* wait short delay before dragging.  This is to fix problem where switches were accidentally getting */
                    /* dragged when tapped on mobile devices */
                    if (DateTime.Now.ToFileTimeUtc() - mMouseDownTime < 150) {
                        return;
                    }
                    TempMouseMode = MOUSE_MODE.DRAG_SELECTED;
                    changed = success = dragSelected(gx, gy);
                }
                break;
            case MOUSE_MODE.DRAG_SELECTED:
                changed = success = dragSelected(gx, gy);
                break;
            }
            if (success) {
                mDragScreenX = MouseCursorX;
                mDragScreenY = MouseCursorY;
                /* Console.WriteLine("setting dragGridx in mousedragged");*/
                mDragGridX = inverseTransformX(mDragScreenX);
                mDragGridY = inverseTransformY(mDragScreenY);
                if (!(TempMouseMode == MOUSE_MODE.DRAG_SELECTED && onlyGraphicsElmsSelected())) {
                    mDragGridX = snapGrid(mDragGridX);
                    mDragGridY = snapGrid(mDragGridY);
                }
            }
            if (changed) {
                writeRecoveryToStorage();
            }
            Repaint();
        }

        void dragSplitter(int x, int y) {
            double h = mPixCir.Height;
            if (h < 1) {
                h = 1;
            }
            mScopeHeightFraction = 1.0 - (y / h);
            if (mScopeHeightFraction < 0.1) {
                mScopeHeightFraction = 0.1;
            }
            if (mScopeHeightFraction > 0.9) {
                mScopeHeightFraction = 0.9;
            }
            setCircuitArea();
            Repaint();
        }

        void dragAll(int x, int y) {
            int dx = x - mDragScreenX;
            int dy = y - mDragScreenY;
            if (dx == 0 && dy == 0) {
                return;
            }
            Transform[4] += dx;
            Transform[5] += dy;
            mDragScreenX = x;
            mDragScreenY = y;
        }

        void dragRow(int x, int y) {
            int dy = y - mDragGridY;
            if (dy == 0) {
                return;
            }
            for (int i = 0; i != ElmList.Count; i++) {
                var ce = getElm(i);
                if (ce.Y1 == mDragGridY) {
                    ce.MovePoint(0, 0, dy);
                }
                if (ce.Y2 == mDragGridY) {
                    ce.MovePoint(1, 0, dy);
                }
            }
            removeZeroLengthElements();
        }

        void dragColumn(int x, int y) {
            int dx = x - mDragGridX;
            if (dx == 0) {
                return;
            }
            for (int i = 0; i != ElmList.Count; i++) {
                var ce = getElm(i);
                if (ce.X1 == mDragGridX) {
                    ce.MovePoint(0, dx, 0);
                }
                if (ce.X2 == mDragGridX) {
                    ce.MovePoint(1, dx, 0);
                }
            }
            removeZeroLengthElements();
        }

        bool onlyGraphicsElmsSelected() {
            if (mMouseElm != null && !(mMouseElm is GraphicElm)) {
                return false;
            }
            for (int i = 0; i != ElmList.Count; i++) {
                var ce = getElm(i);
                if (ce.IsSelected && !(ce is GraphicElm)) {
                    return false;
                }
            }
            return true;
        }

        bool dragSelected(int x, int y) {
            bool me = false;
            int i;
            if (mMouseElm != null && !mMouseElm.IsSelected) {
                mMouseElm.IsSelected = me = true;
            }
            if (!onlyGraphicsElmsSelected()) {
                Console.WriteLine("Snapping x and y");
                x = snapGrid(x);
                y = snapGrid(y);
            }
            int dx = x - mDragGridX;
            int dy = y - mDragGridY;
            if (dx == 0 && dy == 0) {
                /* don't leave mouseElm selected if we selected it above */
                if (me) {
                    mMouseElm.IsSelected = false;
                }
                return false;
            }
            /* check if moves are allowed */
            bool allowed = true;
            for (i = 0; allowed && i != ElmList.Count; i++) {
                var ce = getElm(i);
                if (ce.IsSelected && !ce.AllowMove(dx, dy)) {
                    allowed = false;
                }
            }
            if (allowed) {
                for (i = 0; i != ElmList.Count; i++) {
                    var ce = getElm(i);
                    if (ce.IsSelected) {
                        ce.Move(dx, dy);
                    }
                }
                NeedAnalyze();
            }
            /* don't leave mouseElm selected if we selected it above */
            if (me) {
                mMouseElm.IsSelected = false;
            }

            return allowed;
        }

        void dragPost(int x, int y) {
            if (mDraggingPost == -1) {
                mDraggingPost
                    = (Utils.Distance(mMouseElm.X1, mMouseElm.Y1, x, y)
                    > Utils.Distance(mMouseElm.X2, mMouseElm.Y2, x, y))
                    ? 1 : 0;
            }
            int dx = x - mDragGridX;
            int dy = y - mDragGridY;
            if (dx == 0 && dy == 0) {
                return;
            }
            mMouseElm.MovePoint(mDraggingPost, dx, dy);
            NeedAnalyze();
        }

        void doFlip() {
            mMenuElm.FlipPosts();
            NeedAnalyze();
        }

        void doSplit(CircuitElm ce) {
            int x = snapGrid(inverseTransformX(mMenuX));
            int y = snapGrid(inverseTransformY(mMenuY));
            if (ce == null || !(ce is WireElm)) {
                return;
            }
            if (ce.X1 == ce.X2) {
                x = ce.X1;
            } else {
                y = ce.Y1;
            }
            /* don't create zero-length wire */
            if (x == ce.X1 && y == ce.Y1 || x == ce.X2 && y == ce.Y2) {
                return;
            }
            var newWire = new WireElm(x, y);
            newWire.Drag(ce.X2, ce.Y2);
            ce.Drag(x, y);
            ElmList.Add(newWire);
            NeedAnalyze();
        }

        void selectArea(int x, int y) {
            int x1 = Math.Min(x, mInitDragGridX);
            int x2 = Math.Max(x, mInitDragGridX);
            int y1 = Math.Min(y, mInitDragGridY);
            int y2 = Math.Max(y, mInitDragGridY);
            mSelectedArea = new Rectangle(x1, y1, x2 - x1, y2 - y1);
            for (int i = 0; i != ElmList.Count; i++) {
                var ce = getElm(i);
                ce.SelectRect(mSelectedArea);
            }
        }

        void setMouseElm(CircuitElm ce) {
            if (ce != mMouseElm) {
                if (mMouseElm != null) {
                    mMouseElm.SetMouseElm(false);
                }
                if (ce != null) {
                    ce.SetMouseElm(true);
                }
                mMouseElm = ce;
            }
        }

        void removeZeroLengthElements() {
            for (int i = ElmList.Count - 1; i >= 0; i--) {
                var ce = getElm(i);
                if (ce.X1 == ce.X2 && ce.Y1 == ce.Y2) {
                    ElmList.RemoveAt(i);
                    /*Console.WriteLine("delete element: {0} {1}\t{2} {3}\t{4}", ce.GetType(), ce.x1, ce.y1, ce.x2, ce.y2); */
                    ce.Delete();
                }
            }
            NeedAnalyze();
        }

        bool mouseIsOverSplitter(int x, int y) {
            bool isOverSplitter = (x >= 0)
                && (x < mCircuitArea.Width)
                && (y >= mCircuitArea.Height - 10)
                && (y <= mCircuitArea.Height + 5);
            if (isOverSplitter != mMouseWasOverSplitter) {
                if (isOverSplitter) {
                    mParent.Cursor = Cursors.HSplit;
                } else {
                    setMouseMode(MouseMode);
                }
            }
            mMouseWasOverSplitter = isOverSplitter;
            return isOverSplitter;
        }

        /* convert screen coordinates to grid coordinates by inverting circuit transform */
        int inverseTransformX(double x) {
            return (int)((x - Transform[4]) / Transform[0]);
        }
        int inverseTransformY(double y) {
            return (int)((y - Transform[5]) / Transform[3]);
        }

        /* need to break this out into a separate routine to handle selection, */
        /* since we don't get mouse move events on mobile */
        void mouseSelect() {
            CircuitElm newMouseElm = null;
            int mx = MouseCursorX;
            int my = MouseCursorY;
            int gx = inverseTransformX(mx);
            int gy = inverseTransformY(my);

            /*Console.WriteLine("Settingd draggridx in mouseEvent");*/
            mDragGridX = snapGrid(gx);
            mDragGridY = snapGrid(gy);
            mDragScreenX = mx;
            mDragScreenY = my;
            mDraggingPost = -1;

            mMousePost = -1;
            PlotXElm = PlotYElm = null;

            if (mouseIsOverSplitter(mx, my)) {
                setMouseElm(null);
                return;
            }

            double minDistance = 8;
            for (int i = 0; i != ElmList.Count; i++) {
                var ce = getElm(i);
                var distance = ce.Distance(gx, gy);
                if (distance < minDistance || ce.BoundingBox.IntersectsWith(new Rectangle(gx - 1, gy - 1, 2, 2))) {
                    newMouseElm = ce;
                    minDistance = distance;
                }
            }

            ScopeSelected = -1;
            if (newMouseElm == null) {
                for (int i = 0; i != mScopeCount; i++) {
                    var s = mScopes[i];
                    if (s.BoundingBox.Contains(mx, my)) {
                        newMouseElm = s.Elm;
                        ScopeSelected = i;
                    }
                }
                /* the mouse pointer was not in any of the bounding boxes, but we
                /* might still be close to a post */
                for (int i = 0; i != ElmList.Count; i++) {
                    var ce = getElm(i);
                    if (MouseMode == MOUSE_MODE.DRAG_POST) {
                        if (ce.GetHandleGrabbedClose(gx, gy, POSTGRABSQ, 0) > 0) {
                            newMouseElm = ce;
                            break;
                        }
                    }
                    int jn = ce.PostCount;
                    for (int j = 0; j != jn; j++) {
                        var pt = ce.GetPost(j);
                        if (Utils.Distance(pt, gx, gy) < 26) {
                            newMouseElm = ce;
                            mMousePost = j;
                            break;
                        }
                    }
                }
            } else {
                mMousePost = -1;
                /* look for post close to the mouse pointer */
                for (int i = 0; i != newMouseElm.PostCount; i++) {
                    var pt = newMouseElm.GetPost(i);
                    if (Utils.Distance(pt, gx, gy) < 26) {
                        mMousePost = i;
                    }
                }
            }
            Repaint();
            setMouseElm(newMouseElm);
        }

        void onContextMenu(Control ctrl, MouseEventArgs e) {
            mMenuClientX = mParent.Location.X + e.X;
            mMenuClientY = mParent.Location.Y + e.Y;
            doPopupMenu();
        }

        void doPopupMenu() {
            mMenuElm = mMouseElm;
            mMenuScope = -1;
            mMenuPlot = -1;
            if (ScopeSelected != -1) {
                if (mScopes[ScopeSelected].CanMenu) {
                    mMenuScope = ScopeSelected;
                    mMenuPlot = mScopes[ScopeSelected].SelectedPlot;
                    mScopePopupMenu.DoScopePopupChecks(false);
                    mPopupMenu = new ContextMenuStrip();
                    mPopupMenu.Items.AddRange(mScopePopupMenu.MenuBar);
                    var y = Math.Max(0, Math.Min(mMenuClientY, mBmp.Height - 160));
                    mPopupMenu.Show();
                    mPopupMenu.Location = new Point(mMenuClientX, y);
                }
            } else if (mMouseElm != null) {
                if (!(mMouseElm is ScopeElm)) {
                    mElementMenu.Scope.Enabled = mMouseElm.CanViewInScope;
                    mElementMenu.FloatScope.Enabled = mMouseElm.CanViewInScope;
                    mElementMenu.Edit.Enabled = mMouseElm.GetElementInfo(0) != null;
                    mElementMenu.Flip.Enabled = 2 == mMouseElm.PostCount;
                    mElementMenu.Split.Enabled = canSplit(mMouseElm);
                    mElementMenu.Slider.Enabled = sliderItemEnabled(mMouseElm);
                    mPopupMenu = new ContextMenuStrip();
                    mPopupMenu.Items.AddRange(mElementMenu.MenuBar);
                    mPopupMenu.Show();
                    mPopupMenu.Location = new Point(mMenuClientX, mMenuClientY);
                } else {
                    var s = (ScopeElm)mMouseElm;
                    if (s.elmScope.CanMenu) {
                        mMenuPlot = s.elmScope.SelectedPlot;
                        mScopePopupMenu.DoScopePopupChecks(true);
                        mPopupMenu = new ContextMenuStrip();
                        mPopupMenu.Items.AddRange(mScopePopupMenu.MenuBar);
                        mPopupMenu.Show();
                        mPopupMenu.Location = new Point(mMenuClientX, mMenuClientY);
                    }
                }
            }
        }

        bool canSplit(CircuitElm ce) {
            if (!(ce is WireElm)) {
                return false;
            }
            var we = (WireElm)ce;
            if (we.X1 == we.X2 || we.Y1 == we.Y2) {
                return true;
            }
            return false;
        }

        /* check if the user can create sliders for this element */
        bool sliderItemEnabled(CircuitElm elm) {
            /* prevent confusion */
            if (elm is PotElm) {
                return false;
            }
            for (int i = 0; ; i++) {
                var ei = elm.GetElementInfo(i);
                if (ei == null) {
                    return false;
                }
                if (ei.CanCreateAdjustable()) {
                    return true;
                }
            }
        }

        void clearMouseElm() {
            ScopeSelected = -1;
            setMouseElm(null);
            PlotXElm = PlotYElm = null;
        }

        void zoomCircuit(int dy) {
            double newScale;
            double oldScale = Transform[0];
            double val = dy * .01;
            newScale = Math.Max(oldScale + val, .2);
            newScale = Math.Min(newScale, 2.5);
            setCircuitScale(newScale);
        }

        void setCircuitScale(double newScale) {
            int cx = inverseTransformX(mCircuitArea.Width / 2);
            int cy = inverseTransformY(mCircuitArea.Height / 2);
            Transform[0] = Transform[3] = (float)newScale;
            /* adjust translation to keep center of screen constant
            /* inverse transform = (x-t4)/t0 */
            Transform[4] = (float)(mCircuitArea.Width / 2 - cx * newScale);
            Transform[5] = (float)(mCircuitArea.Height / 2 - cy * newScale);
        }

        void scrollValues(int deltay) {
            if (mMouseElm != null && !DialogIsShowing() && ScopeSelected == -1) {
                if ((mMouseElm is ResistorElm) || (mMouseElm is CapacitorElm) || (mMouseElm is InductorElm)) {
                    mScrollValuePopup = new ScrollValuePopup(deltay, mMouseElm, this);
                    mScrollValuePopup.Show(
                        mParent.Location.X + MouseCursorX,
                        mParent.Location.Y + MouseCursorY
                    );
                }
            }
        }

        void enableItems() { }

        void doUndo() {
            if (mUndoStack.Count == 0) {
                return;
            }
            mRedoStack.Add(dumpCircuit());
            string tmp = mUndoStack[mUndoStack.Count - 1];
            mUndoStack.RemoveAt(mUndoStack.Count - 1);
            readCircuit(tmp, RC_NO_CENTER);
            enableUndoRedo();
        }

        void doRedo() {
            if (mRedoStack.Count == 0) {
                return;
            }
            mUndoStack.Add(dumpCircuit());
            string tmp = mRedoStack[mRedoStack.Count - 1];
            mRedoStack.RemoveAt(mRedoStack.Count - 1);
            readCircuit(tmp, RC_NO_CENTER);
            enableUndoRedo();
        }

        void doRecover() {
            PushUndo();
            readCircuit(mRecovery);
        }

        void enableUndoRedo() {
            mRedoItem.Enabled = mRedoStack.Count > 0;
            mUndoItem.Enabled = mUndoStack.Count > 0;
        }

        void setMouseMode(MOUSE_MODE mode) {
            MouseMode = mode;
            if (mode == MOUSE_MODE.ADD_ELM) {
                mParent.Cursor = Cursors.Cross;
            } else {
                mParent.Cursor = Cursors.Arrow;
            }
        }

        void setMenuSelection() {
            if (mMenuElm != null) {
                if (mMenuElm.IsSelected) {
                    return;
                }
                clearSelection();
                mMenuElm.IsSelected = true;
            }
        }

        void doCut() {
            int i;
            PushUndo();
            setMenuSelection();
            mClipboard = "";
            for (i = ElmList.Count - 1; i >= 0; i--) {
                var ce = getElm(i);
                /* ScopeElms don't cut-paste well because their reference to a parent
                /* elm by number get's messed up in the dump. For now we will just ignore them
                /* until I can be bothered to come up with something better */
                if (willDelete(ce) && !(ce is ScopeElm)) {
                    mClipboard += ce.Dump + "\n";
                }
            }
            writeClipboardToStorage();
            doDelete(true);
            enablePaste();
        }

        void writeClipboardToStorage() {
            var stor = Storage.getLocalStorageIfSupported();
            if (stor == null) {
                return;
            }
            stor.setItem("circuitClipboard", mClipboard);
        }

        void readClipboardFromStorage() {
            var stor = Storage.getLocalStorageIfSupported();
            if (stor == null) {
                return;
            }
            mClipboard = stor.getItem("circuitClipboard");
        }

        void writeRecoveryToStorage() {
            Console.WriteLine("write recovery");
            var stor = Storage.getLocalStorageIfSupported();
            if (stor == null) {
                return;
            }
            string s = dumpCircuit();
            stor.setItem("circuitRecovery", s);
        }

        void readRecovery() {
            var stor = Storage.getLocalStorageIfSupported();
            if (stor == null) {
                return;
            }
            mRecovery = stor.getItem("circuitRecovery");
        }

        void deleteUnusedScopeElms() {
            /* Remove any scopeElms for elements that no longer exist */
            for (int i = ElmList.Count - 1; i >= 0; i--) {
                var ce = getElm(i);
                if ((ce is ScopeElm) && ((ScopeElm)ce).elmScope.NeedToRemove) {
                    ce.Delete();
                    ElmList.RemoveAt(i);
                }
            }
        }

        void doDelete(bool pushUndoFlag) {
            int i;
            if (pushUndoFlag) {
                PushUndo();
            }
            bool hasDeleted = false;

            for (i = ElmList.Count - 1; i >= 0; i--) {
                var ce = getElm(i);
                if (willDelete(ce)) {
                    if (ce.IsMouseElm) {
                        setMouseElm(null);
                    }
                    ce.Delete();
                    ElmList.RemoveAt(i);
                    hasDeleted = true;
                }
            }
            if (hasDeleted) {
                deleteUnusedScopeElms();
                NeedAnalyze();
                writeRecoveryToStorage();
            }
        }

        bool willDelete(CircuitElm ce) {
            /* Is this element in the list to be deleted.
            /* This changes the logic from the previous version which would initially only
            /* delete selected elements (which could include the mouseElm) and then delete the
            /* mouseElm if there were no selected elements. Not really sure this added anything useful
            /* to the user experience.
            /*
            /* BTW, the old logic could also leave mouseElm pointing to a deleted element. */
            return ce.IsSelected || ce.IsMouseElm;
        }

        string copyOfSelectedElms() {
            string r = "";
            CustomLogicModel.clearDumpedFlags();
            CustomCompositeModel.clearDumpedFlags();
            DiodeModel.clearDumpedFlags();
            for (int i = ElmList.Count - 1; i >= 0; i--) {
                var ce = getElm(i);
                string m = ce.DumpModel();
                if (!string.IsNullOrEmpty(m)) {
                    r += m + "\n";
                }
                /* See notes on do cut why we don't copy ScopeElms. */
                if (ce.IsSelected && !(ce is ScopeElm)) {
                    r += ce.Dump + "\n";
                }
            }
            return r;
        }

        void doCopy() {
            /* clear selection when we're done if we're copying a single element using the context menu */
            bool clearSel = (mMenuElm != null && !mMenuElm.IsSelected);

            setMenuSelection();
            mClipboard = copyOfSelectedElms();

            if (clearSel) {
                clearSelection();
            }
            writeClipboardToStorage();
            enablePaste();
        }

        void enablePaste() {
            if (string.IsNullOrEmpty(mClipboard)) {
                readClipboardFromStorage();
            }
            mPasteItem.Enabled = !string.IsNullOrEmpty(mClipboard);
        }

        void doDuplicate() {
            setMenuSelection();
            string s = copyOfSelectedElms();
            doPaste(s);
        }

        void doPaste(string dump) {
            PushUndo();
            clearSelection();
            int i;

            /* get old bounding box */
            var oldbb = new Rectangle();
            for (i = 0; i != ElmList.Count; i++) {
                var ce = getElm(i);
                var bb = ce.BoundingBox;
                if (0 == i) {
                    oldbb = bb;
                } else {
                    oldbb = Rectangle.Union(oldbb, bb);
                }
            }

            /* add new items */
            int oldsz = ElmList.Count;
            if (dump != null) {
                readCircuit(dump, RC_RETAIN);
            } else {
                readClipboardFromStorage();
                readCircuit(mClipboard, RC_RETAIN);
            }

            /* select new items and get their bounding box */
            var newbb = new Rectangle();
            for (i = oldsz; i != ElmList.Count; i++) {
                var ce = getElm(i);
                ce.IsSelected = true;
                var bb = ce.BoundingBox;
                if (0 == i) {
                    newbb = bb;
                } else {
                    newbb = Rectangle.Union(newbb, bb);
                }
            }

            if (oldbb != null && newbb != null && oldbb.Contains(newbb)) {
                /* find a place on the edge for new items */
                int dx = 0, dy = 0;
                int spacew = mCircuitArea.Width - oldbb.Width - newbb.Width;
                int spaceh = mCircuitArea.Height - oldbb.Height - newbb.Height;
                if (spacew > spaceh) {
                    dx = snapGrid(oldbb.X + oldbb.Width - newbb.X + GRID_SIZE);
                } else {
                    dy = snapGrid(oldbb.Y + oldbb.Height - newbb.Y + GRID_SIZE);
                }

                /* move new items near the mouse if possible */
                if (MouseCursorX > 0 && mCircuitArea.Contains(MouseCursorX, MouseCursorY)) {
                    int gx = inverseTransformX(MouseCursorX);
                    int gy = inverseTransformY(MouseCursorY);
                    int mdx = snapGrid(gx - (newbb.X + newbb.Width / 2));
                    int mdy = snapGrid(gy - (newbb.Y + newbb.Height / 2));
                    for (i = oldsz; i != ElmList.Count; i++) {
                        if (!getElm(i).AllowMove(mdx, mdy)) {
                            break;
                        }
                    }
                    if (i == ElmList.Count) {
                        dx = mdx;
                        dy = mdy;
                    }
                }

                /* move the new items */
                for (i = oldsz; i != ElmList.Count; i++) {
                    var ce = getElm(i);
                    ce.Move(dx, dy);
                }
            }
            NeedAnalyze();
            writeRecoveryToStorage();
        }

        void clearSelection() {
            for (int i = 0; i != ElmList.Count; i++) {
                var ce = getElm(i);
                ce.IsSelected = false;
            }
        }

        void doSelectAll() {
            for (int i = 0; i != ElmList.Count; i++) {
                var ce = getElm(i);
                ce.IsSelected = true;
            }
        }

        bool anySelectedButMouse() {
            for (int i = 0; i != ElmList.Count; i++) {
                if (getElm(i) != mMouseElm && getElm(i).IsSelected) {
                    return true;
                }
            }
            return false;
        }

        void onPreviewNativeEvent(KeyEventArgs e) {
            var code = e.KeyCode;

            if (DialogIsShowing()) {
                if (mScrollValuePopup != null && mScrollValuePopup.Visible) {
                    if (code == Keys.Escape || code == Keys.Space) {
                        mScrollValuePopup.close(false);
                    }
                    if (code == Keys.Enter) {
                        mScrollValuePopup.close(true);
                    }
                }
                if (EditDialog != null && EditDialog.Visible) {
                    if (code == Keys.Escape) {
                        EditDialog.closeDialog();
                    }
                    if (code == Keys.Enter) {
                        EditDialog.enterPressed();
                    }
                }
                return;
            }

            if (code == Keys.Back || code == Keys.Delete) {
                if (ScopeSelected != -1 && null != mScopes[ScopeSelected]) {
                    /* Treat DELETE key with scope selected as "remove scope", not delete */
                    mScopes[ScopeSelected].SetElm(null);
                    ScopeSelected = -1;
                } else {
                    mMenuElm = null;
                    PushUndo();
                    doDelete(true);
                }
            }

            if (code == Keys.Escape) {
                setMouseMode(MOUSE_MODE.SELECT);
                mMouseMode = MENU_ITEM.SELECT;
                TempMouseMode = MouseMode;
            }

            if (e.KeyValue > 32 && e.KeyValue < 127) {
                var c = mMenuItems.shortcuts[e.KeyValue];
                if (c == MENU_ITEM.INVALID) {
                    return;
                }
                setMouseMode(MOUSE_MODE.ADD_ELM);
                mMouseMode = c;
                TempMouseMode = MouseMode;
            }
            if (e.KeyValue == 32) {
                setMouseMode(MOUSE_MODE.SELECT);
                mMouseMode = MENU_ITEM.SELECT;
                TempMouseMode = MouseMode;
            }
        }

        /* For debugging */
        void dumpNodelist() {
            CircuitElm e;
            int i, j;
            string s;
            string cs;
            Console.WriteLine("Elm list Dump");
            for (i = 0; i < ElmList.Count; i++) {
                e = ElmList[i];
                cs = e.ToString();
                int p = cs.LastIndexOf('.');
                cs = cs.Substring(p + 1);
                if (cs == "WireElm") {
                    continue;
                }
                if (cs == "TransistorElm") {
                    if (((TransistorElm)e).pnp == -1) {
                        cs = "PTransistorElm";
                    } else {
                        cs = "NTransistorElm";
                    }
                }
                s = cs;
                for (j = 0; j < e.PostCount; j++) {
                    s = s + " " + e.Nodes[j];
                }
                Console.WriteLine(s);
            }
        }

        void doDCAnalysis() {
            DcAnalysisFlag = true;
            ResetButton_onClick();
        }

        bool isSelection() {
            for (int i = 0; i != ElmList.Count; i++) {
                if (getElm(i).IsSelected) {
                    return true;
                }
            }
            return false;
        }

        public CustomCompositeModel getCircuitAsComposite() {
            int i;
            string nodeDump = "";
            string dump = "";
            CustomLogicModel.clearDumpedFlags();
            DiodeModel.clearDumpedFlags();
            var extList = new List<ExtListEntry>();

            bool sel = isSelection();

            // mapping of node labels -> node numbers
            var nodeNameHash = new Dictionary<string, int>();

            // mapping of node numbers -> equivalent node numbers (if they both have the same label)
            var nodeNumberHash = new Dictionary<int, int>();

            var used = new bool[mCir.NodeList.Count];

            // output all the elements
            for (i = 0; i != ElmList.Count; i++) {
                var ce = getElm(i);
                if (sel && !ce.IsSelected) {
                    continue;
                }
                // don't need these elements dumped
                if ((ce is WireElm) || (ce is ScopeElm)) {
                    continue;
                }
                if (ce is GraphicElm) {
                    continue;
                }
                int j;
                if (nodeDump.Length > 0) {
                    nodeDump += "\r";
                }
                nodeDump += ce.GetType().ToString();
                for (j = 0; j != ce.PostCount; j++) {
                    int n = ce.Nodes[j];
                    int n0 = nodeNumberHash.ContainsKey(n) ? nodeNumberHash[n] : n;
                    used[n0] = true;
                    nodeDump += " " + n0;
                }

                // save positions
                int x1 = ce.X1;
                int y1 = ce.Y1;
                int x2 = ce.X2;
                int y2 = ce.Y2;

                // set them to 0 so they're easy to remove
                ce.X1 = ce.Y1 = ce.X2 = ce.Y2 = 0;

                string tstring = ce.Dump;
                var rg = new Regex("[A-Za-z0-9]+ 0 0 0 0 ");
                tstring = rg.Replace(tstring, "", 1); // remove unused tint_x1 y1 x2 y2 coords for internal components

                // restore positions
                ce.X1 = x1;
                ce.Y1 = y1;
                ce.X2 = x2;
                ce.Y2 = y2;
                if (dump.Length > 0) {
                    dump += " ";
                }
                dump += CustomLogicModel.escape(tstring);
            }

            for (i = 0; i != extList.Count; i++) {
                var ent = extList[i];
                if (!used[ent.node]) {
                    MessageBox.Show("Node \"" + ent.name + "\" is not used!");
                    return null;
                }
            }

            var ccm = new CustomCompositeModel();
            ccm.nodeList = nodeDump;
            ccm.elmDump = dump;
            ccm.extList = extList;
            return ccm;
        }
    }
    #endregion
}
