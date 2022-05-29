using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Circuit.Elements;
using Circuit.Elements.Passive;
using Circuit.Elements.Active;
using Circuit.Elements.Output;
using Circuit.Elements.Custom;

namespace Circuit {
    partial class CirSimForm {
        public CirSimForm(Form parent) {
            Sim = this;
            mParent = parent;
            mMenuItems = new MenuItems(this);
            ControlPanel.Init(this);

            mParent.KeyPreview = true;
            mParent.KeyDown += onKeyDown;
            mParent.KeyUp += onKeyUp;

            ElmList = new List<BaseUI>();
            mRedoItem = new MenuItem();
            mUndoItem = new MenuItem();
            mPasteItem = new MenuItem();
            Transform = new float[] { 1, 0, 0, 1, 0, 0 };

            mScopes = new Scope[20];
            mScopeColCount = new int[20];
            mScopeCount = 0;

            setTimer();

            mMenuBar = new MenuStrip();
            {
                mMenuItems.ComposeMainMenu(mMenuBar);
                mParent.Controls.Add(mMenuBar);
            }

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
                mParent.Controls.Add(mSplitContainer);
            }

            readCircuit("");
            readRecovery();

            enableUndoRedo();
            enablePaste();

            ControlPanel.SetSliderPanelHeight();

            mElementPopupMenu = new ElementPopupMenu(this);
            mScopePopupMenu = new ScopePopupMenu(this);

            SetSimRunning(true);
        }

        public void Performed(MENU_ITEM item) {
            switch (item) {
            case MENU_ITEM.OPEN_NEW:
                readCircuit("");
                writeRecoveryToStorage();
                readRecovery();
                break;
            case MENU_ITEM.OPEN_FILE:
                doOpenFile();
                writeRecoveryToStorage();
                readRecovery();
                break;
            case MENU_ITEM.SAVE_FILE:
                doSaveFile();
                break;
            case MENU_ITEM.CREATE_MODULE:
                doCreateSubcircuit();
                break;
            case MENU_ITEM.DC_ANALYSIS:
                doDCAnalysis();
                break;
            case MENU_ITEM.PRINT:
                BaseUI.Context.DoPrint = true;
                break;
            case MENU_ITEM.RECOVER:
                doRecover();
                break;
            }

            if (mMouseElm != null) {
                mMenuElm = mMouseElm;
                return;
            }

            switch (item) {
            case MENU_ITEM.UNDO:
                doUndo();
                break;
            case MENU_ITEM.REDO:
                doRedo();
                break;
            case MENU_ITEM.CUT:
                mMenuElm = null;
                doCut();
                break;
            case MENU_ITEM.COPY:
                mMenuElm = null;
                doCopy();
                break;
            case MENU_ITEM.PASTE:
                doPaste(null);
                break;
            case MENU_ITEM.DELETE:
                mMenuElm = null;
                PushUndo();
                doDelete(true);
                break;
            case MENU_ITEM.DUPLICATE:
                mMenuElm = null;
                doDuplicate();
                break;
            case MENU_ITEM.SELECT_ALL:
                doSelectAll();
                break;
            case MENU_ITEM.CENTER_CIRCUIT:
                PushUndo();
                centreCircuit();
                break;
            }

            switch (item) {
            case MENU_ITEM.STACK_ALL:
                stackAll();
                break;
            case MENU_ITEM.UNSTACK_ALL:
                unstackAll();
                break;
            case MENU_ITEM.COMBINE_ALL:
                combineAll();
                break;
            case MENU_ITEM.SEPARATE_ALL:
                separateAll();
                break;
            }

            Repaint();
        }

        public void Performed(ELEMENTS item) {
            if (mContextMenu != null) {
                mContextMenu.Close();
            }
            setMouseMode(MOUSE_MODE.ADD_ELM);
            mMouseMode = item;
            TempMouseMode = MouseMode;
            Repaint();
        }

        public void Performed(ELEMENT_MENU_ITEM item) {
            if (mContextMenu != null) {
                mContextMenu.Close();
            }

            if (item == ELEMENT_MENU_ITEM.EDIT) {
                doEdit(mMenuElm, mContextMenuLocation);
            }
            if (item == ELEMENT_MENU_ITEM.CUT) {
                doCut();
            }
            if (item == ELEMENT_MENU_ITEM.COPY) {
                doCopy();
            }
            if (item == ELEMENT_MENU_ITEM.DELETE) {
                PushUndo();
                doDelete(true);
            }
            if (item == ELEMENT_MENU_ITEM.DUPLICATE) {
                doDuplicate();
            }

            if (item == ELEMENT_MENU_ITEM.FLIP) {
                doFlip();
            }
            if (item == ELEMENT_MENU_ITEM.SPLIT) {
                doSplit(mMenuElm);
            }
            if (item == ELEMENT_MENU_ITEM.SLIDERS) {
                doSliders(mMenuElm, mContextMenuLocation);
            }

            if (item == ELEMENT_MENU_ITEM.VIEW_IN_SCOPE && mMenuElm != null) {
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
                    mScopes[i] = new Scope();
                    mScopes[i].Position = i;
                }
                mScopes[i].SetElm(mMenuElm);
                if (i > 0) {
                    mScopes[i].Speed = mScopes[i - 1].Speed;
                }
            }

            if (item == ELEMENT_MENU_ITEM.VIEW_IN_FLOAT_SCOPE && mMenuElm != null) {
                var newScope = new ScopeUI(SnapGrid(mMenuElm.P1.X + 50, mMenuElm.P1.Y + 50));
                ElmList.Add(newScope);
                newScope.setScopeElm(mMenuElm);
            }

            Repaint();
        }

        public void Performed(SCOPE_MENU_ITEM item) {
            if (mContextMenu != null) {
                mContextMenu.Close();
            }

            PushUndo();

            Scope s;
            if (mMenuScope != -1) {
                s = mScopes[mMenuScope];
            } else {
                if (mMouseElm is ScopeUI) {
                    s = ((ScopeUI)mMouseElm).elmScope;
                } else {
                    return;
                }
            }

            if (item == SCOPE_MENU_ITEM.DOCK) {
                if (mScopeCount == mScopes.Length) {
                    return;
                }
                mScopes[mScopeCount] = ((ScopeUI)mMouseElm).elmScope;
                ((ScopeUI)mMouseElm).clearElmScope();
                mScopes[mScopeCount].Position = mScopeCount;
                mScopeCount++;
                doDelete(false);
            }

            if (item == SCOPE_MENU_ITEM.UNDOCK && 0 <= mMenuScope) {
                var newScope = new ScopeUI(SnapGrid(mMenuElm.P1.X + 50, mMenuElm.P1.Y + 50));
                ElmList.Add(newScope);
                newScope.setElmScope(mScopes[mMenuScope]);
                /* remove scope from list.  setupScopes() will fix the positions */
                for (int i = mMenuScope; i < mScopeCount; i++) {
                    mScopes[i] = mScopes[i + 1];
                }
                mScopeCount--;
            }

            if (null == s) {
                deleteUnusedScopeElms();
                return;
            }

            if (item == SCOPE_MENU_ITEM.REMOVE_SCOPE) {
                s.SetElm(null);  /* setupScopes() will clean this up */
            }
            if (item == SCOPE_MENU_ITEM.REMOVE_PLOT) {
                s.RemovePlot(mMenuPlot);
            }
            if (item == SCOPE_MENU_ITEM.SPEED_UP) {
                s.SpeedUp();
            }
            if (item == SCOPE_MENU_ITEM.SPEED_DOWN) {
                s.SlowDown();
            }
            if (item == SCOPE_MENU_ITEM.MAX_SCALE) {
                s.MaxScale();
            }
            if (item == SCOPE_MENU_ITEM.STACK) {
                stackScope(mMenuScope);
            }
            if (item == SCOPE_MENU_ITEM.UNSTACK) {
                unstackScope(mMenuScope);
            }
            if (item == SCOPE_MENU_ITEM.COMBINE) {
                combineScope(mMenuScope);
            }
            if (item == SCOPE_MENU_ITEM.RESET) {
                s.ResetGraph(true);
            }
            if (item == SCOPE_MENU_ITEM.PROPERTIES) {
                s.Properties(mParent);
            }

            deleteUnusedScopeElms();
        }

        // Todo: GetCircuitAsComposite
        //public CustomCompositeModel GetCircuitAsComposite() {
        //    string nodeDump = "";
        //    string dump = "";
        //    CustomLogicModel.clearDumpedFlags();
        //    DiodeModel.ClearDumpedFlags();
        //    var extList = new List<ExtListEntry>();

        //    bool sel = isSelection();

        //    // mapping of node labels -> node numbers
        //    var nodeNameHash = new Dictionary<string, int>();

        //    // mapping of node numbers -> equivalent node numbers (if they both have the same label)
        //    var nodeNumberHash = new Dictionary<int, int>();

        //    var used = new bool[mCir.NodeList.Count];

        //    // find all the labeled nodes, get a list of them, and create a node number map
        //    for (int i = 0; i != ElmCount; i++) {
        //        var ce = getElm(i).Item1;
        //        if (sel && !ce.IsSelected) {
        //            continue;
        //        }
        //        if (ce is LabeledNodeElm) {
        //            var lne = (LabeledNodeElm)ce;
        //            var label = lne.Text;
        //            // this node name already seen?  map the new node number to the old one
        //            if (nodeNameHash.ContainsKey(label)) {
        //                var map = nodeNameHash[label];
        //                if (nodeNumberHash.ContainsKey(lne.CirNodes[0]) && nodeNumberHash[lne.CirNodes[0]] != map) {
        //                    MessageBox.Show("Can't have a node with two labels!");
        //                    return null;
        //                }
        //                nodeNumberHash.Add(lne.CirNodes[0], map);
        //                continue;
        //            }
        //            nodeNameHash.Add(label, lne.CirNodes[0]);
        //            // put an entry in nodeNumberHash so we can detect if we try to map it to something else later
        //            nodeNumberHash.Add(lne.CirNodes[0], lne.CirNodes[0]);
        //            if (lne.IsInternal) {
        //                continue;
        //            }
        //            // create ext list entry for external nodes
        //            var ent = new ExtListEntry(label, ce.CirNodes[0]);
        //            extList.Add(ent);
        //        }
        //    }

        //    // output all the elements
        //    for (int i = 0; i != ElmCount; i++) {
        //        var ce = getElm(i).Item1;
        //        if (sel && !ce.IsSelected) {
        //            continue;
        //        }
        //        // don't need these elements dumped
        //        if ((ce is WireElm) || (ce is ScopeElm)) {
        //            continue;
        //        }
        //        if (nodeDump.Length > 0) {
        //            nodeDump += "\r";
        //        }
        //        // TODO: GetCircuitAsComposite
        //        nodeDump += ELEMENTS.RAIL_AC;
        //        for (int j = 0; j != ce.CirPostCount; j++) {
        //            int n = ce.CirNodes[j];
        //            int n0 = nodeNumberHash.ContainsKey(n) ? nodeNumberHash[n] : n;
        //            used[n0] = true;
        //            nodeDump += " " + n0;
        //        }

        //        // save positions
        //        int x1 = ce.P1.X;
        //        int y1 = ce.P1.Y;
        //        int x2 = ce.P2.X;
        //        int y2 = ce.P2.Y;

        //        // set them to 0 so they're easy to remove
        //        ce.P1.X = ce.P1.Y = ce.P2.X = ce.P2.Y = 0;

        //        string tstring = ce.Dump;
        //        var rg = new Regex("[A-Za-z0-9]+ 0 0 0 0 ");
        //        tstring = rg.Replace(tstring, "", 1); // remove unused tint_x1 y1 x2 y2 coords for internal components

        //        // restore positions
        //        ce.P1.X = x1;
        //        ce.P1.Y = y1;
        //        ce.P2.X = x2;
        //        ce.P2.Y = y2;
        //        if (dump.Length > 0) {
        //            dump += " ";
        //        }
        //        dump += CustomLogicModel.escape(tstring);
        //    }

        //    for (int i = 0; i != extList.Count; i++) {
        //        var ent = extList[i];
        //        if (!used[ent.node]) {
        //            MessageBox.Show("Node \"" + ent.name + "\" is not used!");
        //            return null;
        //        }
        //    }

        //    var ccm = new CustomCompositeModel();
        //    ccm.NodeList = nodeDump;
        //    ccm.ElmDump = dump;
        //    ccm.ExtList = extList;
        //    return ccm;
        //}

        #region Public method
        public void SetSimRunning(bool s) {
            if (s) {
                if (Circuit.StopMessage != null) {
                    return;
                }
                IsRunning = true;
                ControlPanel.BtnRunStop.Text = "RUN";
            } else {
                IsRunning = false;
                mAnalyzeFlag = false;
                ControlPanel.BtnRunStop.Text = "STOP";
                Repaint();
            }
        }

        public void Repaint() {
            if (!mNeedsRepaint) {
                mNeedsRepaint = true;
                UpdateCircuit();
                mNeedsRepaint = false;
            }
        }

        public void NeedAnalyze() {
            mAnalyzeFlag = true;
            Repaint();
        }

        public Adjustable FindAdjustable(BaseUI elm, int item) {
            for (int i = 0; i != Adjustables.Count; i++) {
                var a = Adjustables[i];
                if (a.UI == elm && a.EditItem == item) {
                    return a;
                }
            }
            return null;
        }

        /* delete sliders for an element */
        public void DeleteSliders(BaseUI elm) {
            if (Adjustables == null) {
                return;
            }
            for (int i = Adjustables.Count - 1; i >= 0; i--) {
                var adj = Adjustables[i];
                if (adj.UI == elm) {
                    adj.DeleteSlider();
                    Adjustables.RemoveAt(i);
                }
            }
        }

        public int LocateElm(BaseUI elm) {
            for (int i = 0; i != ElmCount; i++) {
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
            if (mContextMenu != null && mContextMenu.Visible) {
                return true;
            }
            if (mScrollValuePopup != null && mScrollValuePopup.Visible) {
                return true;
            }
            return false;
        }

        public void UpdateModels() {
            for (int i = 0; i != ElmCount; i++) {
                ElmList[i].UpdateModels();
            }
        }

        public void ResetButton_onClick() {
            for (int i = 0; i != ElmCount; i++) {
                GetElm(i).Elm.Reset();
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

        public int SnapGrid(int x) {
            return (x + GRID_ROUND) & GRID_MASK;
        }

        public Point SnapGrid(int x, int y) {
            return new Point(
                (x + GRID_ROUND) & GRID_MASK,
                (y + GRID_ROUND) & GRID_MASK);
        }

        public Point SnapGrid(Point pos) {
            return new Point(
                (pos.X + GRID_ROUND) & GRID_MASK,
                (pos.Y + GRID_ROUND) & GRID_MASK);
        }

        /* convert grid coordinates to screen coordinates */
        public int TransformX(double x) {
            return (int)((x * Transform[0]) + Transform[4]);
        }
        public int TransformY(double y) {
            return (int)((y * Transform[3]) + Transform[5]);
        }
        #endregion

        #region Key event method
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
            keyUpPerformed(e);
        }

        void keyUpPerformed(KeyEventArgs e) {
            var code = e.KeyCode;

            if (DialogIsShowing()) {
                if (mScrollValuePopup != null && mScrollValuePopup.Visible) {
                    if (code == Keys.Escape || code == Keys.Space) {
                        mScrollValuePopup.Close(false);
                    }
                    if (code == Keys.Enter) {
                        mScrollValuePopup.Close(true);
                    }
                }
                if (EditDialog != null && EditDialog.Visible) {
                    if (code == Keys.Escape) {
                        EditDialog.Close();
                    }
                    if (code == Keys.Enter) {
                        EditDialog.EnterPressed();
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
                mMenuItems.AllUnchecked();
                setMouseMode(MOUSE_MODE.SELECT);
                mMouseMode = ELEMENTS.INVALID;
                TempMouseMode = MouseMode;
            }
            if (e.KeyValue == 32) {
                setMouseMode(MOUSE_MODE.SELECT);
                mMouseMode = ELEMENTS.INVALID;
                TempMouseMode = MouseMode;
            }
        }
        #endregion

        #region Mouse event method
        void onClick(Control s, MouseEventArgs e) {
            if (e.Button == MouseButtons.Middle) {
                scrollValues(0);
            }
            if (e.Button == MouseButtons.Right) {
                onContextMenu(s, e);
            }
        }

        void onDoubleClick(EventArgs e) {
            if (mMouseElm == null) {
                return;
            }
            // Todo: OptocouplerElm
            //if (mMouseElm is SwitchElm || mMouseElm is GroundElm || mMouseElm is OptocouplerElm) {
            if (mMouseElm is SwitchUI || mMouseElm is GroundUI) {
                return;
            }
            doEdit(mMouseElm, new Point(
                mParent.Location.X + mMenuClient.X,
                mParent.Location.Y + mMenuClient.Y));
        }

        void onMouseDown(MouseEventArgs e) {
            Circuit.StopElm = null; /* if stopped, allow user to select other elements to fix circuit */
            mMenuPos.X = mMenuClient.X = MouseCursorX = e.X;
            mMenuPos.Y = mMenuClient.Y = MouseCursorY = e.Y;
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

            if (MouseMode == MOUSE_MODE.SELECT && mMouseButton == MouseButtons.Left) {
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
                (ScopeSelected == -1 && mMouseElm != null && (mMouseElm is ScopeUI) && ((ScopeUI)mMouseElm).elmScope.CursorInSettingsWheel)) {
                Console.WriteLine("Doing something");
                Scope s;
                if (ScopeSelected != -1) {
                    s = mScopes[ScopeSelected];
                } else {
                    s = ((ScopeUI)mMouseElm).elmScope;
                }
                s.Properties(mParent);
                clearSelection();
                mouseDragging = false;
                return;
            }

            var gpos = new Point(
                inverseTransformX(e.X),
                inverseTransformY(e.Y));
            if (doSwitch(gpos)) {
                /* do this BEFORE we change the mouse mode to MODE_DRAG_POST!  Or else logic inputs */
                /* will add dots to the whole circuit when we click on them! */
                return;
            }

            /* IES - Grab resize handles in select mode if they are far enough apart and you are on top of them */
            if (TempMouseMode == MOUSE_MODE.SELECT && mMouseElm != null
                && mMouseElm.GetHandleGrabbedClose(gpos, POSTGRABSQ, MINPOSTGRABSIZE) >= 0
                && !anySelectedButMouse()) {
                TempMouseMode = MOUSE_MODE.DRAG_POST;
            }

            if (TempMouseMode != MOUSE_MODE.SELECT && TempMouseMode != MOUSE_MODE.DRAG_SELECTED) {
                clearSelection();
            }

            PushUndo();
            mInitDragGrid.X = gpos.X;
            mInitDragGrid.Y = gpos.Y;
            if (TempMouseMode != MOUSE_MODE.ADD_ELM) {
                return;
            }
            /* */
            gpos = SnapGrid(gpos);
            if (!mCircuitArea.Contains(MouseCursorX, MouseCursorY)) {
                return;
            }
            DragElm = MenuItems.ConstructElement(mMouseMode, gpos);
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
                mHeldSwitchElm.MouseUp();
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
        #endregion

        #region Private methond
        void setTimer() {
            mTimer = new Timer();
            mTimer.Tick += new EventHandler((s, e) => {
                if (IsRunning) {
                    UpdateCircuit();
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
            var isRunning = IsRunning;
            if (isRunning) {
                SetSimRunning(false);
            }

            mPixCir.Width = width;
            mPixCir.Height = height;
            if (BaseUI.Context != null) {
                BaseUI.Context.Dispose();
            }
            BaseUI.Context = CustomGraphics.FromImage(width, height);
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
            /* calculate transform so circuit fills most of screen */
            Transform[0] = Transform[3] = 1;
            Transform[1] = Transform[2] = Transform[4] = Transform[5] = 0;
            if (0 < bounds.Width) {
                Transform[4] = (mCircuitArea.Width - bounds.Width) / 2 - bounds.X;
                Transform[5] = (mCircuitArea.Height - bounds.Height) / 2 - bounds.Y;
            }
        }

        /* get circuit bounds.  remember this doesn't use setBbox().  That is calculated when we draw */
        /* the circuit, but this needs to be ready before we first draw it, so we use this crude method */
        Rectangle getCircuitBounds() {
            int i;
            int minx = 1000, maxx = 0, miny = 1000, maxy = 0;
            for (i = 0; i != ElmCount; i++) {
                var ce = GetElm(i);
                /* centered text causes problems when trying to center the circuit, */
                /* so we special-case it here */
                if (!ce.IsCenteredText) {
                    minx = Math.Min(ce.P1.X, Math.Min(ce.P2.X, minx));
                    maxx = Math.Max(ce.P1.X, Math.Max(ce.P2.X, maxx));
                }
                miny = Math.Min(ce.P1.Y, Math.Min(ce.P2.Y, miny));
                maxy = Math.Max(ce.P1.Y, Math.Max(ce.P2.Y, maxy));
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

        void doEdit(Editable eable, Point location) {
            clearSelection();
            PushUndo();
            if (EditDialog != null) {
                EditDialog.Close();
                EditDialog = null;
            }
            EditDialog = new ElementInfoDialog(eable);
            EditDialog.Show(location.X, location.Y);
        }

        void doSliders(BaseUI ce, Point location) {
            clearSelection();
            PushUndo();
            if (SliderDialog != null) {
                SliderDialog.closeDialog();
                SliderDialog = null;
            }
            SliderDialog = new SliderDialog(ce, this);
            SliderDialog.Show(location.X, location.Y);
        }

        void doCreateSubcircuit() {
            // Todo: doCreateSubcircuit
            //var dlg = new EditCompositeModelDialog();
            //if (!dlg.CreateModel()) {
            //    return;
            //}
            //dlg.CreateDialog();
            //DialogShowing = dlg;
            //DialogShowing.Show();
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
            // Todo: CustomLogicModel
            //CustomLogicModel.clearDumpedFlags();
            // Todo: CustomCompositeModel
            //CustomCompositeModel.ClearDumpedFlags();
            DiodeModel.ClearDumpedFlags();

            int f = ControlPanel.ChkShowDots.Checked ? 1 : 0;
            f |= ControlPanel.ChkShowValues.Checked ? 0 : 16;

            /* 32 = linear scale in afilter */
            string dump = "$ " + f
                + " " + ControlPanel.TimeStep
                + " " + ControlPanel.IterCount
                + " " + ControlPanel.TrbCurrent.Value + "\n";

            int i;
            for (i = 0; i != ElmCount; i++) {
                var ce = GetElm(i);
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

            return dump;
        }

        void readCircuit(string text, int flags) {
            readCircuit(Encoding.UTF8.GetBytes(text), flags);
        }

        void readCircuit(string text) {
            readCircuit(Encoding.UTF8.GetBytes(text), 0);
        }

        void readCircuit(byte[] b, int flags) {
            Console.WriteLine("readCircuit");
            int i;
            int len = b.Length;
            if ((flags & RC_RETAIN) == 0) {
                clearMouseElm();
                for (i = 0; i != ElmCount; i++) {
                    var ce = GetElm(i);
                    ce.Delete();
                }
                ElmList.Clear();
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
                string line = Encoding.UTF8.GetString(b, p, linelen);
                var st = new StringTokenizer(line, " +\t\n\r\f");
                while (st.hasMoreTokens()) {
                    string type = st.nextToken();
                    int tint = type.ElementAt(0);
                    try {
                        if (subs && tint != '.') {
                            continue;
                        }
                        if (tint == 'o') {
                            var sc = new Scope();
                            sc.Position = mScopeCount;
                            sc.Undump(st);
                            mScopes[mScopeCount++] = sc;
                            break;
                        }
                        if (tint == '$') {
                            readOptions(st);
                            break;
                        }
                        if (tint == '!') {
                            // Todo: CustomLogicModel
                            //CustomLogicModel.undumpModel(st);
                            break;
                        }
                        if (tint == '%' || tint == '?') {
                            /* ignore afilter-specific stuff */
                            break;
                        }
                        /* do not add new symbols here without testing export as link */

                        /* if first character is a digit then parse the type as a number */
                        if (tint >= '0' && tint <= '9') {
                            tint = int.Parse(type);
                        }
                        if (tint == 34) {
                            DiodeModel.UndumpModel(st);
                            break;
                        }
                        if (tint == 38) {
                            var adj = new Adjustable(st, this);
                            Adjustables.Add(adj);
                            break;
                        }
                        if (tint == '.') {
                            // Todo: CustomCompositeModel
                            //CustomCompositeModel.UndumpModel(st);
                            break;
                        }
                        var p1 = new Point(
                            st.nextTokenInt(),
                            st.nextTokenInt());
                        var p2 = new Point(
                            st.nextTokenInt(),
                            st.nextTokenInt());
                        int f = st.nextTokenInt();
                        var dumpId = MenuItems.GetDumpIdFromString(type);
                        var newce = MenuItems.CreateCe(dumpId, p1, p2, f, st);
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

        void readOptions(StringTokenizer st) {
            int flags = st.nextTokenInt();
            ControlPanel.ChkShowDots.Checked = (flags & 1) != 0;
            ControlPanel.ChkShowValues.Checked = (flags & 16) == 0;

            ControlPanel.TimeStep = st.nextTokenDouble();
            double sp = st.nextTokenDouble();
            int sp2 = (int)(Math.Log(10 * sp) * 24 + 61.5);
            ControlPanel.TrbSpeed.Value = sp2;
            ControlPanel.TrbCurrent.Value = st.nextTokenInt();
        }

        bool doSwitch(Point pos) {
            if (mMouseElm == null || !(mMouseElm is SwitchUI)) {
                return false;
            }
            var se = (SwitchUI)mMouseElm;
            if (!se.GetSwitchRect().Contains(pos)) {
                return false;
            }
            se.Toggle();
            if (((SwitchElm)se.Elm).Momentary) {
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
            var gpos = new Point(
                inverseTransformX(MouseCursorX),
                inverseTransformY(MouseCursorY));
            if (!mCircuitArea.Contains(MouseCursorX, MouseCursorY)) {
                return;
            }
            bool changed = false;
            if (DragElm != null) {
                DragElm.Drag(gpos);
            }
            bool success = true;
            switch (TempMouseMode) {
            case MOUSE_MODE.DRAG_ALL:
                dragAll(MouseCursorX, MouseCursorY);
                break;
            case MOUSE_MODE.DRAG_ROW:
                dragRow(SnapGrid(gpos));
                changed = true;
                break;
            case MOUSE_MODE.DRAG_COLUMN:
                dragColumn(SnapGrid(gpos));
                changed = true;
                break;
            case MOUSE_MODE.DRAG_POST:
                if (mMouseElm != null) {
                    dragPost(SnapGrid(gpos));
                    changed = true;
                }
                break;
            case MOUSE_MODE.SELECT:
                if (mMouseElm == null) {
                    selectArea(gpos);
                } else {
                    /* wait short delay before dragging.  This is to fix problem where switches were accidentally getting */
                    /* dragged when tapped on mobile devices */
                    if (DateTime.Now.ToFileTimeUtc() - mMouseDownTime < 150) {
                        return;
                    }
                    TempMouseMode = MOUSE_MODE.DRAG_SELECTED;
                    changed = success = dragSelected(gpos);
                }
                break;
            case MOUSE_MODE.DRAG_SELECTED:
                changed = success = dragSelected(gpos);
                break;
            }
            if (success) {
                mDragScreen.X = MouseCursorX;
                mDragScreen.Y = MouseCursorY;
                /* Console.WriteLine("setting dragGridx in mousedragged");*/
                mDragGrid = inverseTransform(mDragScreen);
                if (!(TempMouseMode == MOUSE_MODE.DRAG_SELECTED && onlyGraphicsElmsSelected())) {
                    mDragGrid = SnapGrid(mDragGrid);
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
            int dx = x - mDragScreen.X;
            int dy = y - mDragScreen.Y;
            if (dx == 0 && dy == 0) {
                return;
            }
            Transform[4] += dx;
            Transform[5] += dy;
            mDragScreen.X = x;
            mDragScreen.Y = y;
        }

        void dragRow(Point pos) {
            int dy = pos.Y - mDragGrid.Y;
            if (dy == 0) {
                return;
            }
            for (int i = 0; i != ElmCount; i++) {
                var ce = GetElm(i);
                if (ce.P1.Y == mDragGrid.Y) {
                    ce.MovePoint(0, 0, dy);
                }
                if (ce.P2.Y == mDragGrid.Y) {
                    ce.MovePoint(1, 0, dy);
                }
            }
            removeZeroLengthElements();
        }

        void dragColumn(Point pos) {
            int dx = pos.X - mDragGrid.X;
            if (dx == 0) {
                return;
            }
            for (int i = 0; i != ElmCount; i++) {
                var ce = GetElm(i);
                if (ce.P1.X == mDragGrid.X) {
                    ce.MovePoint(0, dx, 0);
                }
                if (ce.P1.X == mDragGrid.X) {
                    ce.MovePoint(1, dx, 0);
                }
            }
            removeZeroLengthElements();
        }

        bool dragSelected(Point pos) {
            bool me = false;
            int i;
            if (mMouseElm != null && !mMouseElm.IsSelected) {
                mMouseElm.IsSelected = me = true;
            }
            if (!onlyGraphicsElmsSelected()) {
                Console.WriteLine("Snapping x and y");
                pos = SnapGrid(pos);
            }
            int dx = pos.X - mDragGrid.X;
            int dy = pos.Y - mDragGrid.Y;
            if (dx == 0 && dy == 0) {
                /* don't leave mouseElm selected if we selected it above */
                if (me) {
                    mMouseElm.IsSelected = false;
                }
                return false;
            }
            /* check if moves are allowed */
            bool allowed = true;
            for (i = 0; allowed && i != ElmCount; i++) {
                var ce = GetElm(i);
                if (ce.IsSelected && !ce.AllowMove(dx, dy)) {
                    allowed = false;
                }
            }
            if (allowed) {
                for (i = 0; i != ElmCount; i++) {
                    var ce = GetElm(i);
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

        void dragPost(Point pos) {
            if (mDraggingPost == -1) {
                mDraggingPost
                    = (Utils.Distance(mMouseElm.P1.X, mMouseElm.P1.Y, pos.X, pos.Y)
                    > Utils.Distance(mMouseElm.P2.X, mMouseElm.P2.Y, pos.X, pos.Y))
                    ? 1 : 0;
            }
            int dx = pos.X - mDragGrid.X;
            int dy = pos.Y - mDragGrid.Y;
            if (dx == 0 && dy == 0) {
                return;
            }
            mMouseElm.MovePoint(mDraggingPost, dx, dy);
            NeedAnalyze();
        }

        bool onlyGraphicsElmsSelected() {
            if (mMouseElm != null) {
                return false;
            }
            for (int i = 0; i != ElmCount; i++) {
                var ce = GetElm(i);
                if (ce.IsSelected) {
                    return false;
                }
            }
            return true;
        }

        void doFlip() {
            mMenuElm.FlipPosts();
            NeedAnalyze();
        }

        void doSplit(BaseUI ce) {
            var pos = SnapGrid(inverseTransform(mMenuPos));
            if (ce == null || !(ce is WireUI)) {
                return;
            }
            if (ce.P1.X == ce.P2.X) {
                pos.X = ce.P1.X;
            } else {
                pos.Y = ce.P1.Y;
            }
            /* don't create zero-length wire */
            if (pos.X == ce.P1.X && pos.Y == ce.P1.Y || pos.X == ce.P2.X && pos.Y == ce.P2.Y) {
                return;
            }
            var newWire = new WireUI(pos);
            newWire.Drag(ce.P2);
            ce.Drag(pos);
            ElmList.Add(newWire);
            NeedAnalyze();
        }

        void selectArea(Point pos) {
            int x1 = Math.Min(pos.X, mInitDragGrid.X);
            int x2 = Math.Max(pos.X, mInitDragGrid.X);
            int y1 = Math.Min(pos.Y, mInitDragGrid.Y);
            int y2 = Math.Max(pos.Y, mInitDragGrid.Y);
            mSelectedArea = new Rectangle(x1, y1, x2 - x1, y2 - y1);
            for (int i = 0; i != ElmCount; i++) {
                var ce = GetElm(i);
                ce.SelectRect(mSelectedArea);
            }
        }

        void setMouseElm(BaseUI ce) {
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
            for (int i = ElmCount - 1; i >= 0; i--) {
                var ce = GetElm(i);
                if (ce.P1.X == ce.P2.X && ce.P1.Y == ce.P2.Y) {
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
        Point inverseTransform(Point pos) {
            return new Point(
                (int)((pos.X - Transform[4]) / Transform[0]),
                (int)((pos.Y - Transform[5]) / Transform[3]));
        }

        /* need to break this out into a separate routine to handle selection, */
        /* since we don't get mouse move events on mobile */
        void mouseSelect() {
            BaseUI newMouseElm = null;
            int mx = MouseCursorX;
            int my = MouseCursorY;
            int gx = inverseTransformX(mx);
            int gy = inverseTransformY(my);

            /*Console.WriteLine("Settingd draggridx in mouseEvent");*/
            mDragGrid.X = SnapGrid(gx);
            mDragGrid.Y = SnapGrid(gy);
            mDragScreen.X = mx;
            mDragScreen.Y = my;
            mDraggingPost = -1;

            mMousePost = -1;
            PlotXElm = PlotYElm = null;

            if (mouseIsOverSplitter(mx, my)) {
                setMouseElm(null);
                return;
            }

            double minDistance = 8;
            for (int i = 0; i != ElmCount; i++) {
                var ce = GetElm(i);
                var distance = ce.Distance(gx, gy);
                if (distance < minDistance) {
                    newMouseElm = ce;
                    minDistance = distance;
                }
            }

            ScopeSelected = -1;
            if (newMouseElm == null) {
                /* the mouse pointer was not in any of the bounding boxes, but we
                /* might still be close to a post */
                for (int i = 0; i != ElmCount; i++) {
                    var ce = GetElm(i);
                    if (MouseMode == MOUSE_MODE.DRAG_POST) {
                        if (ce.GetHandleGrabbedClose(gx, gy, POSTGRABSQ, 0) > 0) {
                            newMouseElm = ce;
                            break;
                        }
                    }
                    int jn = ce.Elm.PostCount;
                    if (2 == jn) {
                        var p1 = ce.GetPost(0);
                        var p2 = ce.GetPost(1);
                        if (Utils.DistanceOnLine(p1.X, p1.Y, p2.X, p2.Y, gx, gy) < 16) {
                            newMouseElm = ce;
                            break;
                        }
                    } else {
                        for (int j = 0; j != jn; j++) {
                            var pt = ce.GetPost(j);
                            if (Utils.Distance(pt, gx, gy) < 16) {
                                newMouseElm = ce;
                                mMousePost = j;
                                break;
                            }
                        }
                    }
                }
                for (int i = 0; i != mScopeCount; i++) {
                    var s = mScopes[i];
                    if (s.BoundingBox.Contains(mx, my)) {
                        newMouseElm = s.Elm;
                        ScopeSelected = i;
                        break;
                    }
                }
            } else {
                mMousePost = -1;
                /* look for post close to the mouse pointer */
                for (int i = 0; i != newMouseElm.Elm.PostCount; i++) {
                    var pt = newMouseElm.GetPost(i);
                    if (Utils.Distance(pt, gx, gy) < 16) {
                        mMousePost = i;
                    }
                }
            }
            Repaint();
            setMouseElm(newMouseElm);
        }

        void onContextMenu(Control ctrl, MouseEventArgs e) {
            mMenuClient.X = mParent.Location.X + e.X;
            mMenuClient.Y = mParent.Location.Y + e.Y;
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
                    var y = Math.Max(0, Math.Min(mMenuClient.Y, mBmp.Height - 160));
                    mContextMenu = mScopePopupMenu.Show(mMenuClient.X, y, false);
                    mContextMenuLocation = mContextMenu.Location;
                }
            } else if (mMouseElm != null) {
                if (!(mMouseElm is ScopeUI)) {
                    mContextMenu = mElementPopupMenu.Show(mMenuClient.X, mMenuClient.Y, mMouseElm);
                    mContextMenuLocation = mContextMenu.Location;
                } else {
                    var s = (ScopeUI)mMouseElm;
                    if (s.elmScope.CanMenu) {
                        mMenuPlot = s.elmScope.SelectedPlot;
                        mContextMenu = mScopePopupMenu.Show(mMenuClient.X, mMenuClient.Y, true);
                        mContextMenuLocation = mContextMenu.Location;
                    }
                }
            }
        }

        void clearMouseElm() {
            ScopeSelected = -1;
            setMouseElm(null);
            PlotXElm = PlotYElm = null;
        }

        void scrollValues(int deltay) {
            if (mMouseElm != null && !DialogIsShowing() && ScopeSelected == -1) {
                if ((mMouseElm is ResistorUI) || (mMouseElm is CapacitorUI) || (mMouseElm is InductorUI)) {
                    mScrollValuePopup = new ScrollValuePopup(deltay, mMouseElm, this);
                    mScrollValuePopup.Show(
                        mParent.Location.X + MouseCursorX,
                        mParent.Location.Y + MouseCursorY
                    );
                }
            }
        }

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
            for (i = ElmCount - 1; i >= 0; i--) {
                var ce = GetElm(i);
                /* ScopeElms don't cut-paste well because their reference to a parent
                /* elm by number get's messed up in the dump. For now we will just ignore them
                /* until I can be bothered to come up with something better */
                if (willDelete(ce) && !(ce is ScopeUI)) {
                    mClipboard += ce.Dump + "\n";
                }
            }
            writeClipboardToStorage();
            doDelete(true);
            enablePaste();
        }

        void writeClipboardToStorage() {
            Storage.GetInstance().SetItem("circuitClipboard", mClipboard);
        }

        void readClipboardFromStorage() {
            mClipboard = Storage.GetInstance().GetItem("circuitClipboard");
        }

        void writeRecoveryToStorage() {
            Console.WriteLine("write recovery");
            var s = dumpCircuit();
            Storage.GetInstance().SetItem("circuitRecovery", s);
        }

        void readRecovery() {
            mRecovery = Storage.GetInstance().GetItem("circuitRecovery");
        }

        void deleteUnusedScopeElms() {
            /* Remove any scopeElms for elements that no longer exist */
            for (int i = ElmCount - 1; 0 <= i; i--) {
                var ce = GetElm(i);
                if ((ce is ScopeUI) && ((ScopeUI)ce).elmScope.NeedToRemove) {
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

            for (i = ElmCount - 1; i >= 0; i--) {
                var ce = GetElm(i);
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

        bool willDelete(BaseUI ce) {
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
            // Todo: CustomLogicModel
            //CustomLogicModel.clearDumpedFlags();
            // Todo: CustomCompositeModel
            //CustomCompositeModel.ClearDumpedFlags();
            DiodeModel.ClearDumpedFlags();
            for (int i = ElmCount - 1; i >= 0; i--) {
                var ce = GetElm(i);
                string m = ce.DumpModel();
                if (!string.IsNullOrEmpty(m)) {
                    r += m + "\n";
                }
                /* See notes on do cut why we don't copy ScopeElms. */
                if (ce.IsSelected && !(ce is ScopeUI)) {
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
            var oldbb = new RectangleF();
            for (i = 0; i != ElmCount; i++) {
                var ce = GetElm(i);
                var bb = ce.BoundingBox;
                if (0 == i) {
                    oldbb = bb;
                } else {
                    oldbb = RectangleF.Union(oldbb, bb);
                }
            }

            /* add new items */
            int oldsz = ElmCount;
            if (dump != null) {
                readCircuit(dump, RC_RETAIN);
            } else {
                readClipboardFromStorage();
                readCircuit(mClipboard, RC_RETAIN);
            }

            /* select new items and get their bounding box */
            var newbb = new RectangleF();
            for (i = oldsz; i != ElmCount; i++) {
                var ce = GetElm(i);
                ce.IsSelected = true;
                var bb = ce.BoundingBox;
                if (0 == i) {
                    newbb = bb;
                } else {
                    newbb = RectangleF.Union(newbb, bb);
                }
            }

            if (oldbb != null && newbb != null && oldbb.Contains(newbb)) {
                /* find a place on the edge for new items */
                int dx = 0;
                int dy = 0;
                var spacew = (int)(mCircuitArea.Width - oldbb.Width - newbb.Width);
                var spaceh = (int)(mCircuitArea.Height - oldbb.Height - newbb.Height);
                if (spacew > spaceh) {
                    dx = SnapGrid((int)(oldbb.X + oldbb.Width - newbb.X + GRID_SIZE));
                } else {
                    dy = SnapGrid((int)(oldbb.Y + oldbb.Height - newbb.Y + GRID_SIZE));
                }

                /* move new items near the mouse if possible */
                if (MouseCursorX > 0 && mCircuitArea.Contains(MouseCursorX, MouseCursorY)) {
                    int gx = inverseTransformX(MouseCursorX);
                    int gy = inverseTransformY(MouseCursorY);
                    int mdx = SnapGrid((int)(gx - (newbb.X + newbb.Width / 2)));
                    int mdy = SnapGrid((int)(gy - (newbb.Y + newbb.Height / 2)));
                    for (i = oldsz; i != ElmCount; i++) {
                        if (!GetElm(i).AllowMove(mdx, mdy)) {
                            break;
                        }
                    }
                    if (i == ElmCount) {
                        dx = mdx;
                        dy = mdy;
                    }
                }

                /* move the new items */
                for (i = oldsz; i != ElmCount; i++) {
                    var ce = GetElm(i);
                    ce.Move(dx, dy);
                }
            }
            NeedAnalyze();
            writeRecoveryToStorage();
        }

        void clearSelection() {
            for (int i = 0; i != ElmCount; i++) {
                var ce = GetElm(i);
                ce.IsSelected = false;
            }
        }

        void doSelectAll() {
            for (int i = 0; i != ElmCount; i++) {
                var ce = GetElm(i);
                ce.IsSelected = true;
            }
        }

        bool anySelectedButMouse() {
            for (int i = 0; i != ElmCount; i++) {
                var ce = GetElm(i);
                if (ce != mMouseElm && ce.IsSelected) {
                    return true;
                }
            }
            return false;
        }

        /* For debugging */
        void dumpNodelist() {
            BaseUI e;
            int i, j;
            string s;
            string cs;
            Console.WriteLine("Elm list Dump");
            for (i = 0; i < ElmCount; i++) {
                e = ElmList[i];
                cs = e.ToString();
                int p = cs.LastIndexOf('.');
                cs = cs.Substring(p + 1);
                if (cs == "WireElm") {
                    continue;
                }
                if (cs == "TransistorElm") {
                    if (((TransistorElm)e.Elm).NPN == -1) {
                        cs = "PTransistorElm";
                    } else {
                        cs = "NTransistorElm";
                    }
                }
                s = cs;
                for (j = 0; j < e.Elm.PostCount; j++) {
                    s = s + " " + e.Elm.Nodes[j];
                }
                Console.WriteLine(s);
            }
        }

        void doDCAnalysis() {
            DcAnalysisFlag = true;
            ResetButton_onClick();
        }

        bool isSelection() {
            for (int i = 0; i != ElmCount; i++) {
                if (GetElm(i).IsSelected) {
                    return true;
                }
            }
            return false;
        }
        #endregion

        public BaseUI GetElm(int n) {
            if (n >= ElmList.Count) {
                return null;
            }
            return ElmList[n];
        }

        public void UpdateCircuit() {
            bool didAnalyze = mAnalyzeFlag;
            if (mAnalyzeFlag || DcAnalysisFlag) {
                Circuit.AnalyzeCircuit();
                mAnalyzeFlag = false;
            }

            if (null != mMouseElm && null != Circuit.StopElm && Circuit.StopElm != mMouseElm.Elm) {
                // Todo: SetMouseElm
                //mCir.StopElm.SetMouseElm(true);
            }
            setupScopes();

            var g = BaseUI.Context;
            PDF.Page pdfG = null;
            if (g.DoPrint) {
                g.DoPrint = false;
                pdfG = new PDF.Page(g.Width, g.Height);
                g = pdfG;
                BaseUI.Context = pdfG;
            }

            if (ControlPanel.ChkPrintable.Checked) {
                CustomGraphics.WhiteColor = Color.Gray;
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

            Circuit.SetSim(this);

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

            g.SetTransform(new Matrix(Transform[0], Transform[1], Transform[2], Transform[3], Transform[4], Transform[5]));
            {
                var pdfX0 = 0;
                var pdfX1 = (int)PDF.Width;
                var pdfY0 = 0;
                var pdfY1 = (int)PDF.Height;
                g.LineColor = Color.Yellow;
                g.DrawLine(pdfX0, pdfY0, pdfX1, pdfY0);
                g.DrawLine(pdfX1, pdfY0, pdfX1, pdfY1);
                g.DrawLine(pdfX1, pdfY1, pdfX0, pdfY1);
                g.DrawLine(pdfX0, pdfY1, pdfX0, pdfY0);

                /* draw elements */
                for (int i = 0; i != ElmCount; i++) {
                    ElmList[i].Draw(g);
                }

                /* draw posts normally */
                if (MouseMode != MOUSE_MODE.DRAG_ROW && MouseMode != MOUSE_MODE.DRAG_COLUMN) {
                    for (int i = 0; i != Circuit.PostDrawList.Count; i++) {
                        g.DrawPost(Circuit.PostDrawList[i]);
                    }
                }

                /* for some mouse modes, what matters is not the posts but the endpoints (which are only
                /* the same for 2-terminal elements).  We draw those now if needed */
                if (TempMouseMode == MOUSE_MODE.DRAG_ROW
                    || TempMouseMode == MOUSE_MODE.DRAG_COLUMN
                    || TempMouseMode == MOUSE_MODE.DRAG_POST
                    || TempMouseMode == MOUSE_MODE.DRAG_SELECTED) {
                    for (int i = 0; i != ElmCount; i++) {
                        var ce = GetElm(i);
                        g.DrawPost(ce.P1);
                        g.DrawPost(ce.P2);
                        if (ce != mMouseElm || TempMouseMode != MOUSE_MODE.DRAG_POST) {
                            g.DrawHandle(ce.P1);
                            g.DrawHandle(ce.P2);
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
                for (int i = 0; i != Circuit.BadConnectionList.Count; i++) {
                    var cn = Circuit.BadConnectionList[i];
                    g.DrawHandle(cn);
                }

                if (0 < mSelectedArea.Width) {
                    g.LineColor = CustomGraphics.SelectColor;
                    g.DrawRectangle(mSelectedArea);
                }

                /* draw cross hair */
                if (ControlPanel.ChkCrossHair.Checked && MouseCursorX >= 0
                    && MouseCursorX <= mCircuitArea.Width && MouseCursorY <= mCircuitArea.Height) {
                    int x = SnapGrid(inverseTransformX(MouseCursorX));
                    int y = SnapGrid(inverseTransformY(MouseCursorY));
                    g.LineColor = Color.Gray;
                    g.DrawLine(x, inverseTransformY(0), x, inverseTransformY(mCircuitArea.Height));
                    g.DrawLine(inverseTransformX(0), y, inverseTransformX(mCircuitArea.Width), y);
                }
            }
            g.ClearTransform();

            Brush bCircuitArea;
            if (ControlPanel.ChkPrintable.Checked) {
                bCircuitArea = Brushes.White;
            } else {
                bCircuitArea = Brushes.Black;
            }
            g.FillRectangle(bCircuitArea, 0, mCircuitArea.Height, mCircuitArea.Width, g.Height - mCircuitArea.Height);

            g.LineColor = mMouseWasOverSplitter ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            g.DrawLine(0, mCircuitArea.Height - 2, mCircuitArea.Width, mCircuitArea.Height - 2);

            int ct = mScopeCount;
            if (Circuit.StopMessage != null) {
                ct = 0;
            }
            for (int i = 0; i != ct; i++) {
                mScopes[i].Draw(g);
            }
            g.ClearTransform();

            if (Circuit.StopMessage != null) {
                g.DrawLeftText(Circuit.StopMessage, 10, mCircuitArea.Height - 10);
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

                /* count lines of data */
                {
                    int infoIdx;
                    for (infoIdx = 0; infoIdx < info.Length - 1 && info[infoIdx] != null; infoIdx++)
                        ;
                    int badnodes = Circuit.BadConnectionList.Count;
                    if (badnodes > 0) {
                        info[infoIdx++] = badnodes + ((badnodes == 1) ? " bad connection" : " bad connections");
                    }
                }

                int x = 0;
                if (ct != 0) {
                    x = mScopes[ct - 1].RightEdge + 20;
                }
                x = Math.Max(x, g.Width * 2 / 3);
                int ybase = mCircuitArea.Height;
                for (int i = 0; i < info.Length && info[i] != null; i++) {
                    g.DrawLeftText(info[i], x, ybase + 15 * (i + 1));
                }
            }

            if (null != mMouseElm && null != Circuit.StopElm && Circuit.StopElm != mMouseElm.Elm) {
                // Todo: SetMouseElm
                //mCir.StopElm.SetMouseElm(false);
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

            if (null == pdfG) {
                mBmp = new Bitmap(g.Width, g.Height);
                mContext = Graphics.FromImage(mBmp);
                BaseUI.Context.CopyTo(mContext);
                mPixCir.Image = mBmp;
            } else {
                var pdf = new PDF();
                pdf.AddPage(pdfG);
                var saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "PDFファイル(*.pdf)|*.pdf";
                saveFileDialog.ShowDialog();
                pdf.Save(saveFileDialog.FileName);
                BaseUI.Context = CustomGraphics.FromImage(g.Width, g.Height);
            }

            /* if we did DC analysis, we need to re-analyze the circuit with that flag cleared. */
            if (DcAnalysisFlag) {
                DcAnalysisFlag = false;
                mAnalyzeFlag = true;
            }

            mLastFrameTime = mLastTime;
        }

        void runCircuit(bool didAnalyze) {
            if (Circuit.Matrix == null || ElmCount == 0) {
                Circuit.Matrix = null;
                return;
            }

            bool debugprint = mDumpMatrix;
            mDumpMatrix = false;
            double steprate = ControlPanel.IterCount;
            long tm = DateTime.Now.ToFileTimeUtc();
            long lit = mLastIterTime;
            if (lit == 0) {
                mLastIterTime = tm;
                return;
            }

            /* Check if we don't need to run simulation (for very slow simulation speeds).
            /* If the circuit changed, do at least one iteration to make sure everything is consistent. */
            if (1000 >= steprate * (tm - mLastIterTime) && !didAnalyze) {
                return;
            }

            bool delayWireProcessing = canDelayWireProcessing();

            int iter;
            for (iter = 1; ; iter++) {
                if (!Circuit.Run(debugprint)) {
                    break;
                }

                Time += ControlPanel.TimeStep;

                if (!delayWireProcessing) {
                    Circuit.CalcWireCurrents();
                }
                for (int i = 0; i != mScopeCount; i++) {
                    mScopes[i].TimeStep();
                }
                for (int i = 0; i != ElmCount; i++) {
                    if (GetElm(i) is ScopeUI) {
                        ((ScopeUI)GetElm(i)).stepScope();
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
                Circuit.CalcWireCurrents();
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

            int h = BaseUI.Context.Height - mCircuitArea.Height;
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
            int w = (BaseUI.Context.Width - iw) / colct;
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
                var r = new Rectangle(pos * w, BaseUI.Context.Height - h + colh * row, w - marg, colh);
                row++;
                if (!r.Equals(s.BoundingBox)) {
                    s.SetRect(r);
                }
            }
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
            for (i = 0; i != ElmCount; i++) {
                if ((GetElm(i) is ScopeUI) && ((ScopeUI)GetElm(i)).elmScope.ViewingWire) {
                    return false;
                }
            }
            return true;
        }
    }
}
