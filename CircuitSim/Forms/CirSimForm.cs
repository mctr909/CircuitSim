using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Circuit.Elements.Active;
using Circuit.Elements.Passive;

using Circuit.UI;
using Circuit.UI.Passive;
using Circuit.UI.Output;
using Circuit.Forms;

namespace Circuit {
    public partial class CirSimForm : Form {
        #region CONST
        public static readonly Font FONT_TEXT = new Font("Meiryo UI", 9.0f);
        public static readonly Brush BRUSH_TEXT = Brushes.Red;

        public static readonly string OHM_TEXT = "Ω";

        public enum MOUSE_MODE {
            NONE = 0,
            ADD_ELM,
            SPLIT,
            SCROLL,
            SELECT,
            SELECT_AREA,
            DRAG_ITEM,
            DRAG_POST,
            DRAG_ROW,
            DRAG_COLUMN
        }

        public const int GRID_SIZE = 8;
        public const int CURRENT_DOT_SIZE = 6;
        public const int POSTGRABSQ = 25;
        public const int MINPOSTGRABSIZE = 256;

        public const int RC_RETAIN = 1;
        public const int RC_NO_CENTER = 2;
        public const int RC_SUBCIRCUITS = 4;

        const int GRID_MASK = ~(GRID_SIZE - 1);
        const int GRID_ROUND = GRID_SIZE / 2 - 1;
        #endregion

        #region Property
        public static ElementInfoDialog EditDialog { get; set; }
        public static ElementInfoDialog CustomLogicEditDialog { get; set; }
        public static ElementInfoDialog DiodeModelEditDialog { get; set; }
        public static SliderDialog SliderDialog { get; set; }
        public static Form DialogShowing { get; set; } = null;
        public static Random Random { get; set; } = new Random();
        public static double CurrentMult { get; set; } = 0;
        public static bool IsRunning { get; private set; }
        public static MOUSE_MODE MouseMode { get; private set; } = MOUSE_MODE.NONE;
        public static BaseUI DragElm { get; private set; }
        public static int MouseCursorX { get; private set; } = -1;
        public static int MouseCursorY { get; private set; } = -1;
        public static List<BaseUI> UIList { get; private set; }
        public static int UICount { get { return null == UIList ? 0 : UIList.Count; } }
        public static BaseUI PlotXElm { get; private set; }
        public static BaseUI PlotYElm { get; private set; }
        public static List<Adjustable> Adjustables { get; private set; } = new List<Adjustable>();
        #endregion

        #region Variable
        public static class Mouse {
            public static BaseUI GripElm = null;
            public static ELEMENTS EditElm = ELEMENTS.INVALID;
            public static MouseButtons Button = MouseButtons.None;
            public static DateTime LastMove = DateTime.Now;
            public static bool IsDragging = false;
            public static long DownTime;
            public static Point InitDragGrid;
            public static Point DragGrid;
            public static Point DragScreen;
            public static int DraggingPost;
            public static int Post = -1;
            public static Rectangle SelectedArea;
        }

        public static CirSimForm Instance = null;

        static ScopeForm mScopeForm = new ScopeForm();

        static string mFileName = "";
        static ScrollValuePopup mScrollValuePopup;
        static bool mNeedsRepaint;
        static bool mAnalyzeFlag;
        static bool mDumpMatrix;

        Timer mTimer;

        MenuStrip mMenuBar;
        MenuItems mMenuItems;
        SplitContainer mSplitContainer;

        static ContextMenuStrip mContextMenu = null;
        ElementPopupMenu mElementPopupMenu;
        ScopePopupMenu mScopePopupMenu;

        static PictureBox mPixCir;
        static Bitmap mBmp = null;
        static Graphics mContext;

        static MenuItem mUndoItem;
        static MenuItem mRedoItem;
        MenuItem mPasteItem;

        static BaseUI mMenuElm;
        Switch mHeldSwitchElm;

        static List<string> mUndoStack = new List<string>();
        static List<string> mRedoStack = new List<string>();

        string mRecovery;
        string mClipboard = "";

        static Point mScroll;
        static Rectangle mCircuitArea;

        bool mIsPressShift;
        bool mIsPressCtrl;
        bool mIsPressAlt;

        Point mMenuClient;
        Point mMenuPos;
        static int mMenuScope = -1;
        static int mMenuPlotWave = -1;

        static long mLastTime = 0;
        static long mLastFrameTime;
        static long mLastIterTime;
        static long mLastSysTime = 0;
        #endregion

        public CirSimForm() {
            InitializeComponent();
            Instance = this;

            mMenuItems = new MenuItems(this);
            ControlPanel.Init();
            CustomGraphics.SetColor(ControlPanel.ChkPrintable.Checked);

            KeyPreview = true;
            KeyDown += onKeyDown;
            KeyUp += onKeyUp;

            UIList = new List<BaseUI>();
            mRedoItem = new MenuItem();
            mUndoItem = new MenuItem();
            mPasteItem = new MenuItem();
            mScroll.X = 0;
            mScroll.Y = 0;

            setTimer();

            mMenuBar = new MenuStrip();
            {
                mMenuItems.ComposeMainMenu(mMenuBar);
                Controls.Add(mMenuBar);
            }

            mPixCir = new PictureBox() { Left = 0, Top = mMenuBar.Height };
            {
                mPixCir.MouseDown += new MouseEventHandler((s, e) => { onMouseDown(e); });
                mPixCir.MouseMove += new MouseEventHandler((s, e) => { onMouseMove(e); });
                mPixCir.MouseUp += new MouseEventHandler((s, e) => { onMouseUp(e); });
                mPixCir.MouseWheel += new MouseEventHandler((s, e) => { onMouseWheel((PictureBox)s, e); });
                mPixCir.MouseClick += new MouseEventHandler((s, e) => { onClick(e); });
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
                ControlPanel.VerticalPanel.Top = mMenuBar.Height;
                int width = ControlPanel.VerticalPanel.Width;
                mSplitContainer.SizeChanged += new EventHandler((s, e) => {
                    if (0 <= mSplitContainer.Width - width) {
                        mSplitContainer.SplitterDistance = mSplitContainer.Width - width;
                        setCanvasSize();
                    }
                });
                Controls.Add(mSplitContainer);
            }

            readCircuit("");
            readRecovery();

            enableUndoRedo();
            enablePaste();

            ControlPanel.SetSliderPanelHeight();

            mElementPopupMenu = new ElementPopupMenu(this);
            mScopePopupMenu = new ScopePopupMenu();

            SetSimRunning(true);
        }

        private void Form1_Load(object sender, EventArgs e) {
            Width = 800;
            Height = 600;
            mScopeForm.Show();
        }

        public void Performed(MENU_ITEM item) {
            switch (item) {
            case MENU_ITEM.OPEN_NEW:
                mFileName = "";
                Text = mFileName;
                readCircuit("");
                writeRecoveryToStorage();
                readRecovery();
                break;
            case MENU_ITEM.OPEN_FILE:
                doOpenFile();
                writeRecoveryToStorage();
                readRecovery();
                break;
            case MENU_ITEM.OVERWRITE:
                doSaveFile(true);
                break;
            case MENU_ITEM.SAVE_FILE:
                doSaveFile(false);
                break;
            case MENU_ITEM.CREATE_MODULE:
                doCreateSubcircuit();
                break;
            case MENU_ITEM.PRINT:
                BaseUI.Context.DoPrint = true;
                break;
            case MENU_ITEM.RECOVER:
                doRecover();
                break;
            }

            if (Mouse.GripElm != null) {
                mMenuElm = Mouse.GripElm;
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
            case MENU_ITEM.SELECT_ALL:
                doSelectAll();
                MouseMode = MOUSE_MODE.DRAG_ITEM;
                break;
            case MENU_ITEM.CENTER_CIRCUIT:
                PushUndo();
                centreCircuit();
                break;
            }

            switch (item) {
            case MENU_ITEM.STACK_ALL:
                Scope.Property.StackAll();
                break;
            case MENU_ITEM.UNSTACK_ALL:
                Scope.Property.UnstackAll();
                break;
            case MENU_ITEM.COMBINE_ALL:
                Scope.Property.CombineAll();
                break;
            case MENU_ITEM.SEPARATE_ALL:
                Scope.Property.SeparateAll();
                break;
            }

            Repaint();
        }

        public void Performed(ELEMENTS item) {
            if (mContextMenu != null) {
                mContextMenu.Close();
            }
            MouseMode = MOUSE_MODE.ADD_ELM;
            Cursor = Cursors.Arrow;
            Mouse.EditElm = item;
            Repaint();
        }

        public void Performed(ELEMENT_MENU_ITEM item) {
            if (mContextMenu != null) {
                mContextMenu.Close();
            }

            if (item == ELEMENT_MENU_ITEM.EDIT) {
                doEdit(mMenuElm, mMenuClient);
            }
            if (item == ELEMENT_MENU_ITEM.SPLIT) {
                doSplit(mMenuElm);
            }
            if (item == ELEMENT_MENU_ITEM.FLIP) {
                doFlip();
            }
            if (item == ELEMENT_MENU_ITEM.SLIDERS) {
                doSliders(mMenuElm, mMenuClient);
            }

            if (item == ELEMENT_MENU_ITEM.VIEW_IN_SCOPE && mMenuElm != null) {
                int i;
                for (i = 0; i != Scope.Property.Count; i++) {
                    if (Scope.Property.List[i].UI == null) {
                        break;
                    }
                }
                if (i == Scope.Property.Count) {
                    if (Scope.Property.Count == Scope.Property.List.Length) {
                        return;
                    }
                    Scope.Property.Count++;
                    Scope.Property.List[i] = new Scope.Property();
                    Scope.Property.List[i].Position = i;
                }
                Scope.Property.List[i].SetElm(mMenuElm);
                if (i > 0) {
                    Scope.Property.List[i].Speed = Scope.Property.List[i - 1].Speed;
                }
            }

            if (item == ELEMENT_MENU_ITEM.VIEW_IN_FLOAT_SCOPE && mMenuElm != null) {
                var newScope = new Scope(SnapGrid(mMenuElm.DumpInfo.P1.X + 50, mMenuElm.DumpInfo.P1.Y + 50));
                UIList.Add(newScope);
                newScope.SetScopeElm(mMenuElm);
            }

            Repaint();
        }

        public static void Performed(SCOPE_MENU_ITEM item) {
            if (mContextMenu != null) {
                mContextMenu.Close();
            }

            PushUndo();

            Scope.Property s;
            if (mMenuScope != -1) {
                s = Scope.Property.List[mMenuScope];
            } else {
                if (Mouse.GripElm is Scope) {
                    s = ((Scope)Mouse.GripElm).Properties;
                } else {
                    return;
                }
            }

            if (item == SCOPE_MENU_ITEM.DOCK) {
                if (Scope.Property.Count == Scope.Property.List.Length) {
                    return;
                }
                Scope.Property.List[Scope.Property.Count] = ((Scope)Mouse.GripElm).Properties;
                ((Scope)Mouse.GripElm).ClearElmScope();
                Scope.Property.List[Scope.Property.Count].Position = Scope.Property.Count;
                Scope.Property.Count++;
                doDelete(false);
            }

            if (item == SCOPE_MENU_ITEM.UNDOCK && 0 <= mMenuScope) {
                var newScope = new Scope(SnapGrid(mMenuElm.DumpInfo.P1.X + 50, mMenuElm.DumpInfo.P1.Y + 50));
                UIList.Add(newScope);
                newScope.SetElmScope(Scope.Property.List[mMenuScope]);
                /* remove scope from list.  setupScopes() will fix the positions */
                for (int i = mMenuScope; i < Scope.Property.Count; i++) {
                    Scope.Property.List[i] = Scope.Property.List[i + 1];
                }
                Scope.Property.Count--;
            }

            if (null == s) {
                deleteUnusedScopeElms();
                return;
            }

            if (item == SCOPE_MENU_ITEM.REMOVE_SCOPE) {
                s.SetElm(null);  /* setupScopes() will clean this up */
            }
            if (item == SCOPE_MENU_ITEM.REMOVE_WAVE) {
                s.RemoveWave(mMenuPlotWave);
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
                Scope.Property.Stack(mMenuScope);
            }
            if (item == SCOPE_MENU_ITEM.UNSTACK) {
                Scope.Property.Unstack(mMenuScope);
            }
            if (item == SCOPE_MENU_ITEM.COMBINE) {
                Scope.Property.Combine(mMenuScope);
            }
            if (item == SCOPE_MENU_ITEM.RESET) {
                s.ResetGraph(true);
            }
            if (item == SCOPE_MENU_ITEM.PROPERTIES) {
                s.Properties(mPixCir.Left + mPixCir.Width / 2, mPixCir.Bottom);
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
        public static BaseUI GetUI(int n) {
            if (n >= UIList.Count) {
                return null;
            }
            return UIList[n];
        }

        public static int GetUIIndex(BaseUI elm) {
            for (int i = 0; i != UICount; i++) {
                if (elm == UIList[i]) {
                    return i;
                }
            }
            return -1;
        }

        public static Adjustable FindAdjustable(BaseUI elm, int item) {
            for (int i = 0; i != Adjustables.Count; i++) {
                var a = Adjustables[i];
                if (a.UI == elm && a.EditItemR == item) {
                    return a;
                }
            }
            return null;
        }

        public static void DeleteSliders(BaseUI elm) {
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

        public static bool DialogIsShowing() {
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

        public static void UpdateModels() {
            for (int i = 0; i != UICount; i++) {
                UIList[i].UpdateModels();
            }
        }

        public static void SetSimRunning(bool s) {
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

        public static void Repaint() {
            if (!mNeedsRepaint) {
                mNeedsRepaint = true;
                updateCircuit();
                mNeedsRepaint = false;
            }
        }

        public static void NeedAnalyze() {
            mAnalyzeFlag = true;
            Repaint();
        }

        public static void ResetButton_onClick() {
            for (int i = 0; i != UIList.Count; i++) {
                UIList[i].Elm.Reset();
            }
            for (int i = 0; i != Scope.Property.Count; i++) {
                Scope.Property.List[i].ResetGraph(true);
            }
            mAnalyzeFlag = true;
            if (Circuit.Time == 0) {
                SetSimRunning(true);
            } else {
                Circuit.Time = 0;
            }
        }

        public static void PushUndo() {
            mRedoStack.Clear();
            string s = dumpCircuit();
            if (mUndoStack.Count > 0 && s == mUndoStack[mUndoStack.Count - 1]) {
                return;
            }
            mUndoStack.Add(s);
            enableUndoRedo();
        }

        public static int SnapGrid(int x) {
            return (x + GRID_ROUND) & GRID_MASK;
        }
        public static Point SnapGrid(int x, int y) {
            return new Point(
                (x + GRID_ROUND) & GRID_MASK,
                (y + GRID_ROUND) & GRID_MASK);
        }
        public static Point SnapGrid(Point pos) {
            return new Point(
                (pos.X + GRID_ROUND) & GRID_MASK,
                (pos.Y + GRID_ROUND) & GRID_MASK);
        }
        public static int TransformX(double x) {
            return (int)(x + mScroll.X);
        }
        public static int TransformY(double y) {
            return (int)(y + mScroll.Y);
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
            Cursor = Cursors.Arrow;
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
                if (ScopeForm.SelectedScope != -1 && null != Scope.Property.List[ScopeForm.SelectedScope]) {
                    /* Treat DELETE key with scope selected as "remove scope", not delete */
                    Scope.Property.List[ScopeForm.SelectedScope].SetElm(null);
                    ScopeForm.SelectedScope = -1;
                } else {
                    mMenuElm = null;
                    PushUndo();
                    doDelete(true);
                }
            }

            if (code == Keys.Escape || e.KeyValue == 32) {
                mMenuItems.AllUnchecked();
                MouseMode = MOUSE_MODE.NONE;
                Mouse.EditElm = ELEMENTS.INVALID;
            }
        }
        #endregion

        #region Mouse event method
        void onClick(MouseEventArgs e) {
            if (e.Button == MouseButtons.Middle) {
                scrollValues(0);
            }
            if (e.Button == MouseButtons.Right) {
                onContextMenu(e);
            }
        }

        void onDoubleClick(EventArgs e) {
            if (Mouse.GripElm == null) {
                return;
            }
            doEdit(Mouse.GripElm, new Point(
                Location.X + mMenuClient.X,
                Location.Y + mMenuClient.Y));
        }

        void onMouseDown(MouseEventArgs e) {
            Circuit.StopElm = null; /* if stopped, allow user to select other elements to fix circuit */
            mMenuPos.X = mMenuClient.X = MouseCursorX = e.X;
            mMenuPos.Y = mMenuClient.Y = MouseCursorY = e.Y;
            Mouse.Button = e.Button;
            Mouse.DownTime = DateTime.Now.ToFileTimeUtc();

            /* maybe someone did copy in another window?  should really do this when */
            /* window receives focus */
            enablePaste();

            if (Mouse.Button != MouseButtons.Left && Mouse.Button != MouseButtons.Middle) {
                return;
            }

            // set mouseElm in case we are on mobile
            mouseSelect();

            Mouse.IsDragging = true;

            if (MouseMode == MOUSE_MODE.NONE && Mouse.Button == MouseButtons.Left) {
                /* left mouse */
                if (mIsPressCtrl && mIsPressAlt) {
                    MouseMode = MOUSE_MODE.DRAG_COLUMN;
                    Cursor = Cursors.SizeWE;
                } else if (mIsPressCtrl && mIsPressShift) {
                    MouseMode = MOUSE_MODE.DRAG_ROW;
                    Cursor = Cursors.SizeNS;
                } else if (mIsPressCtrl) {
                    MouseMode = MOUSE_MODE.SCROLL;
                    Cursor = Cursors.NoMove2D;
                } else if (mIsPressAlt) {
                    MouseMode = MOUSE_MODE.SPLIT;
                    Cursor = Cursors.Cross;
                } else {
                    MouseMode = MOUSE_MODE.SELECT;
                    Cursor = Cursors.Hand;
                }
            }

            if ((ScopeForm.SelectedScope != -1 && Scope.Property.List[ScopeForm.SelectedScope].CursorInSettingsWheel) ||
                (ScopeForm.SelectedScope == -1 && Mouse.GripElm != null && (Mouse.GripElm is Scope) &&
                ((Scope)Mouse.GripElm).Properties.CursorInSettingsWheel)) {
                Console.WriteLine("Doing something");
                Scope.Property s;
                if (ScopeForm.SelectedScope != -1) {
                    s = Scope.Property.List[ScopeForm.SelectedScope];
                } else {
                    s = ((Scope)Mouse.GripElm).Properties;
                }
                s.Properties(mPixCir.Left + mPixCir.Width / 2, mPixCir.Bottom);
                clearSelection();
                Mouse.IsDragging = false;
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

            if (MouseMode != MOUSE_MODE.NONE && MouseMode != MOUSE_MODE.DRAG_ITEM) {
                clearSelection();
            }

            PushUndo();
            Mouse.InitDragGrid.X = gpos.X;
            Mouse.InitDragGrid.Y = gpos.Y;
            if (MouseMode != MOUSE_MODE.ADD_ELM) {
                return;
            }
            /* */
            gpos = SnapGrid(gpos);
            if (!mCircuitArea.Contains(MouseCursorX, MouseCursorY)) {
                return;
            }
            DragElm = MenuItems.ConstructElement(Mouse.EditElm, gpos);
        }

        void onMouseUp(MouseEventArgs e) {
            Mouse.IsDragging = false;
            Mouse.Button = MouseButtons.None;

            switch (MouseMode) {
            case MOUSE_MODE.ADD_ELM:
                break;
            case MOUSE_MODE.SELECT_AREA:
                if (Mouse.SelectedArea.Width == 0 || Mouse.SelectedArea.Height == 0) {
                    clearSelection();
                }
                if (hasSelection()) {
                    MouseMode = MOUSE_MODE.DRAG_ITEM;
                } else {
                    MouseMode = MOUSE_MODE.NONE;
                }
                break;
            case MOUSE_MODE.DRAG_ITEM:
                clearSelection();
                MouseMode = MOUSE_MODE.NONE;
                break;
            case MOUSE_MODE.SPLIT:
                doSplit(Mouse.GripElm);
                MouseMode = MOUSE_MODE.NONE;
                break;
            default:
                MouseMode = MOUSE_MODE.NONE;
                break;
            }

            Mouse.SelectedArea = new Rectangle();
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
                    if (MouseMode == MOUSE_MODE.SELECT || MouseMode == MOUSE_MODE.DRAG_ITEM) {
                        clearSelection();
                    }
                } else {
                    UIList.Add(DragElm);
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
            Cursor = Cursors.Default;
            Repaint();
        }

        void onMouseWheel(Control sender, MouseEventArgs e) {
        }

        void onMouseMove(MouseEventArgs e) {
            MouseCursorX = e.X;
            MouseCursorY = e.Y;
            if (33 < (DateTime.Now - Mouse.LastMove).Milliseconds) {
                Mouse.LastMove = DateTime.Now;
            } else {
                return;
            }
            if (Mouse.IsDragging) {
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
            mCircuitArea = new Rectangle(0, 0, width, height);
            SetSimRunning(isRunning);
        }

        void centreCircuit() {
            var bounds = getCircuitBounds();
            if (0 < bounds.Width) {
                mScroll.X = (mCircuitArea.Width - bounds.Width) / 2 - bounds.X;
            } else {
                mScroll.X = 0;
            }
            if (0 < bounds.Height) {
                mScroll.Y = (mCircuitArea.Height - bounds.Height) / 2 - bounds.Y;
            } else {
                mScroll.Y = 0;
            }
        }

        Rectangle getCircuitBounds() {
            if (0 == UICount) {
                return new Rectangle();
            }
            int minx = int.MaxValue, miny = int.MaxValue;
            int maxx = 0, maxy = 0;
            for (int i = 0; i < UICount; i++) {
                var ce = GetUI(i);
                minx = Math.Min(ce.DumpInfo.P1.X, Math.Min(ce.DumpInfo.P2.X, minx));
                miny = Math.Min(ce.DumpInfo.P1.Y, Math.Min(ce.DumpInfo.P2.Y, miny));
                maxx = Math.Max(ce.DumpInfo.P1.X, Math.Max(ce.DumpInfo.P2.X, maxx));
                maxy = Math.Max(ce.DumpInfo.P1.Y, Math.Max(ce.DumpInfo.P2.Y, maxy));
            }
            return new Rectangle(minx, miny, maxx - minx, maxy - miny);
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
            SliderDialog = new SliderDialog(ce);
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
            string data = "";
            try {
                var fs = new StreamReader(open.FileName);
                data = fs.ReadToEnd();
                fs.Close();
                fs.Dispose();
                mFileName = open.FileName;
            } catch (Exception ex) {
                MessageBox.Show(ex.ToString());
            }
            Text = mFileName;
            readCircuit(data);
        }

        void doSaveFile(bool overWrite) {
            var filePath = "";
            if (overWrite) {
                filePath = mFileName;
            }

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) {
                var save = new SaveFileDialog();
                save.Filter = "テキストファイル(*.txt)|*.txt";
                save.ShowDialog();
                if (string.IsNullOrEmpty(save.FileName) || !Directory.Exists(Path.GetDirectoryName(save.FileName))) {
                    return;
                }
                filePath = save.FileName;
                mFileName = filePath;
                Text = mFileName;
            }

            string dump = dumpCircuit();
            try {
                var fs = new FileStream(filePath, FileMode.Create);
                var sw = new StreamWriter(fs);
                sw.Write(dump);
                sw.Close();
                sw.Dispose();
            } catch (Exception ex) {
                MessageBox.Show(ex.ToString());
            }
        }

        static string dumpCircuit() {
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
                + " " + ControlPanel.StepRate
                + " " + ControlPanel.TrbCurrent.Value + "\n";

            int i;
            for (i = 0; i != UICount; i++) {
                var ce = GetUI(i);
                string m = ce.DumpModel();
                if (!string.IsNullOrEmpty(m)) {
                    dump += m + "\n";
                }
                dump += ce.Dump + "\n";
            }
            for (i = 0; i != Scope.Property.Count; i++) {
                string d = Scope.Property.List[i].Dump();
                if (d != null) {
                    dump += d + "\n";
                }
            }
            for (i = 0; i != Adjustables.Count; i++) {
                var adj = Adjustables[i];
                dump += adj.Dump() + "\n";
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
            int i;
            int len = b.Length;
            if ((flags & RC_RETAIN) == 0) {
                clearMouseElm();
                for (i = 0; i != UICount; i++) {
                    var ce = GetUI(i);
                    ce.Delete();
                }
                UIList.Clear();
                ControlPanel.Reset();
                Scope.Property.Count = 0;
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
                while (st.HasMoreTokens) {
                    string type;
                    st.nextToken(out type);
                    int tint = type.ElementAt(0);
                    try {
                        if (subs && tint != '.') {
                            continue;
                        }
                        if (tint == 'o') {
                            var sc = new Scope.Property();
                            sc.Position = Scope.Property.Count;
                            sc.Undump(st);
                            Scope.Property.List[Scope.Property.Count++] = sc;
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
                        if (tint == (int)DUMP_ID.ADJUSTABLE) {
                            var adj = new Adjustable(st);
                            Adjustables.Add(adj);
                            break;
                        }
                        if (tint == '.') {
                            // Todo: CustomCompositeModel
                            //CustomCompositeModel.UndumpModel(st);
                            break;
                        }
                        int x, y;
                        st.nextTokenInt(out x);
                        st.nextTokenInt(out y);
                        var p1 = new Point(x, y);
                        st.nextTokenInt(out x);
                        st.nextTokenInt(out y);
                        var p2 = new Point(x, y);
                        int f;
                        st.nextTokenInt(out f);
                        var dumpId = MenuItems.GetDumpIdFromString(type);
                        var newce = MenuItems.CreateCe(dumpId, p1, p2, f, st);
                        try {
                            if (st.HasMoreTokens) {
                                string v;
                                st.nextToken(out v);
                                newce.DumpInfo.ReferenceName = Utils.Unescape(v);
                            } else {
                                newce.DumpInfo.ReferenceName = "";
                            }
                        } catch { }
                        if (newce == null) {
                            Console.WriteLine("unrecognized dump type: " + type);
                            break;
                        }
                        newce.SetPoints();
                        UIList.Add(newce);
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
            int flags;
            st.nextTokenInt(out flags);
            ControlPanel.ChkShowDots.Checked = (flags & 1) != 0;
            ControlPanel.ChkShowValues.Checked = (flags & 16) == 0;

            ControlPanel.TimeStep = st.nextTokenDouble();
            double sp = st.nextTokenDouble();
            int sp2 = (int)(Math.Log(10 * sp) * 24 + 61.5);
            ControlPanel.TrbSpeed.Value = sp2;
            int v;
            st.nextTokenInt(out v);
            ControlPanel.TrbCurrent.Value = v;
        }

        bool doSwitch(Point pos) {
            if (Mouse.GripElm == null || !(Mouse.GripElm is Switch)) {
                return false;
            }
            var se = (Switch)Mouse.GripElm;
            if (!se.GetSwitchRect().Contains(pos)) {
                return false;
            }
            se.Toggle();
            if (((ElmSwitch)se.Elm).Momentary) {
                mHeldSwitchElm = se;
            }
            NeedAnalyze();
            return true;
        }

        void mouseDragged() {
            /* ignore right mouse button with no modifiers (needed on PC) */
            if (Mouse.Button == MouseButtons.Right) {
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
            switch (MouseMode) {
            case MOUSE_MODE.SCROLL:
                scroll(MouseCursorX, MouseCursorY);
                break;
            case MOUSE_MODE.DRAG_ROW:
                dragRow(SnapGrid(gpos));
                changed = true;
                break;
            case MOUSE_MODE.DRAG_COLUMN:
                dragColumn(SnapGrid(gpos));
                changed = true;
                break;
            case MOUSE_MODE.SELECT:
                if (Mouse.GripElm == null) {
                    MouseMode = MOUSE_MODE.SELECT_AREA;
                } else {
                    var spos = SnapGrid(gpos);
                    var dumpInfo = Mouse.GripElm.DumpInfo;
                    var d1 = Utils.Distance(dumpInfo.P1, spos);
                    var d2 = Utils.Distance(dumpInfo.P2, spos);
                    var d1_d2 = Utils.Distance(dumpInfo.P1, dumpInfo.P2);
                    var dl = Math.Max(d1_d2 / 4, Utils.DistanceOnLine(dumpInfo.P1, dumpInfo.P2, spos));
                    if (dl < Math.Min(d1, d2)) {
                        MouseMode = MOUSE_MODE.DRAG_ITEM;
                    } else {
                        Mouse.DraggingPost = (d1 < d2) ? 0 : 1;
                        MouseMode = MOUSE_MODE.DRAG_POST;
                    }
                }
                break;
            case MOUSE_MODE.SELECT_AREA:
                selectArea(gpos);
                break;
            case MOUSE_MODE.DRAG_ITEM:
                changed = success = dragSelected(gpos);
                break;
            case MOUSE_MODE.DRAG_POST:
                dragPost(SnapGrid(gpos));
                changed = true;
                break;
            }
            if (success) {
                Mouse.DragScreen.X = MouseCursorX;
                Mouse.DragScreen.Y = MouseCursorY;
                /* Console.WriteLine("setting dragGridx in mousedragged");*/
                Mouse.DragGrid = inverseTransform(Mouse.DragScreen);
                if (!(MouseMode == MOUSE_MODE.DRAG_ITEM && onlyGraphicsElmsSelected())) {
                    Mouse.DragGrid = SnapGrid(Mouse.DragGrid);
                }
            }
            if (changed) {
                writeRecoveryToStorage();
            }
            Repaint();
        }

        void scroll(int x, int y) {
            int dx = x - Mouse.DragScreen.X;
            int dy = y - Mouse.DragScreen.Y;
            if (dx == 0 && dy == 0) {
                return;
            }
            mScroll.X += dx;
            mScroll.Y += dy;
            Mouse.DragScreen.X = x;
            Mouse.DragScreen.Y = y;
        }

        void dragRow(Point pos) {
            int dy = pos.Y - Mouse.DragGrid.Y;
            if (dy == 0) {
                return;
            }
            for (int i = 0; i != UICount; i++) {
                var ce = GetUI(i);
                if (ce.DumpInfo.P1.Y == Mouse.DragGrid.Y) {
                    ce.MovePoint(0, 0, dy);
                }
                if (ce.DumpInfo.P2.Y == Mouse.DragGrid.Y) {
                    ce.MovePoint(1, 0, dy);
                }
            }
            removeZeroLengthElements();
        }

        void dragColumn(Point pos) {
            int dx = pos.X - Mouse.DragGrid.X;
            if (dx == 0) {
                return;
            }
            for (int i = 0; i != UICount; i++) {
                var ce = GetUI(i);
                if (ce.DumpInfo.P1.X == Mouse.DragGrid.X) {
                    ce.MovePoint(0, dx, 0);
                }
                if (ce.DumpInfo.P1.X == Mouse.DragGrid.X) {
                    ce.MovePoint(1, dx, 0);
                }
            }
            removeZeroLengthElements();
        }

        bool dragSelected(Point pos) {
            bool me = false;
            int i;
            if (Mouse.GripElm != null && !Mouse.GripElm.IsSelected) {
                Mouse.GripElm.IsSelected = me = true;
            }
            if (!onlyGraphicsElmsSelected()) {
                pos = SnapGrid(pos);
            }
            int dx = pos.X - Mouse.DragGrid.X;
            int dy = pos.Y - Mouse.DragGrid.Y;
            if (dx == 0 && dy == 0) {
                /* don't leave mouseElm selected if we selected it above */
                if (me) {
                    Mouse.GripElm.IsSelected = false;
                }
                return false;
            }
            /* check if moves are allowed */
            bool allowed = true;
            for (i = 0; allowed && i != UICount; i++) {
                var ce = GetUI(i);
                if (ce.IsSelected && !ce.AllowMove(dx, dy)) {
                    allowed = false;
                }
            }
            if (allowed) {
                for (i = 0; i != UICount; i++) {
                    var ce = GetUI(i);
                    if (ce.IsSelected) {
                        ce.Move(dx, dy);
                    }
                }
                NeedAnalyze();
            }
            /* don't leave mouseElm selected if we selected it above */
            if (me) {
                Mouse.GripElm.IsSelected = false;
            }

            return allowed;
        }

        void dragPost(Point pos) {
            int dx = pos.X - Mouse.DragGrid.X;
            int dy = pos.Y - Mouse.DragGrid.Y;
            if (dx == 0 && dy == 0) {
                return;
            }
            Mouse.GripElm.MovePoint(Mouse.DraggingPost, dx, dy);
            NeedAnalyze();
        }

        bool onlyGraphicsElmsSelected() {
            if (Mouse.GripElm != null) {
                return false;
            }
            for (int i = 0; i != UICount; i++) {
                var ce = GetUI(i);
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
            if (ce == null || !(ce is Wire)) {
                return;
            }
            if (ce.DumpInfo.P1.X == ce.DumpInfo.P2.X) {
                pos.X = ce.DumpInfo.P1.X;
            } else {
                pos.Y = ce.DumpInfo.P1.Y;
            }
            /* don't create zero-length wire */
            if (pos.X == ce.DumpInfo.P1.X && pos.Y == ce.DumpInfo.P1.Y || pos.X == ce.DumpInfo.P2.X && pos.Y == ce.DumpInfo.P2.Y) {
                return;
            }
            var newWire = new Wire(pos);
            newWire.Drag(new Point(ce.DumpInfo.P2.X, ce.DumpInfo.P2.Y));
            ce.Drag(pos);
            UIList.Add(newWire);
            NeedAnalyze();
        }

        void selectArea(Point pos) {
            int x1 = Math.Min(pos.X, Mouse.InitDragGrid.X);
            int x2 = Math.Max(pos.X, Mouse.InitDragGrid.X);
            int y1 = Math.Min(pos.Y, Mouse.InitDragGrid.Y);
            int y2 = Math.Max(pos.Y, Mouse.InitDragGrid.Y);
            Mouse.SelectedArea = new Rectangle(x1, y1, x2 - x1, y2 - y1);
            for (int i = 0; i != UICount; i++) {
                var ce = GetUI(i);
                ce.SelectRect(Mouse.SelectedArea);
            }
        }

        static void setMouseElm(BaseUI ce) {
            if (ce != Mouse.GripElm) {
                if (Mouse.GripElm != null) {
                    Mouse.GripElm.SetMouseElm(false);
                }
                if (ce != null) {
                    ce.SetMouseElm(true);
                }
                Mouse.GripElm = ce;
            }
        }

        void removeZeroLengthElements() {
            for (int i = UICount - 1; i >= 0; i--) {
                var ce = GetUI(i);
                if (ce.DumpInfo.P1.X == ce.DumpInfo.P2.X && ce.DumpInfo.P1.Y == ce.DumpInfo.P2.Y) {
                    UIList.RemoveAt(i);
                    /*Console.WriteLine("delete element: {0} {1}\t{2} {3}\t{4}", ce.GetType(), ce.x1, ce.y1, ce.x2, ce.y2); */
                    ce.Delete();
                }
            }
            NeedAnalyze();
        }

        /* convert screen coordinates to grid coordinates by inverting circuit transform */
        static int inverseTransformX(double x) {
            return (int)(x - mScroll.X);
        }
        static int inverseTransformY(double y) {
            return (int)(y - mScroll.Y);
        }
        Point inverseTransform(Point pos) {
            return new Point(pos.X - mScroll.X, pos.Y - mScroll.Y);
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
            Mouse.DragGrid.X = SnapGrid(gx);
            Mouse.DragGrid.Y = SnapGrid(gy);
            Mouse.DragScreen.X = mx;
            Mouse.DragScreen.Y = my;
            Mouse.DraggingPost = -1;

            Mouse.Post = -1;
            PlotXElm = PlotYElm = null;

            double minDistance = 8;
            for (int i = 0; i != UICount; i++) {
                var ce = GetUI(i);
                var distance = ce.Distance(gx, gy);
                if (distance < minDistance) {
                    newMouseElm = ce;
                    minDistance = distance;
                }
            }

            ScopeForm.SelectedScope = -1;
            if (newMouseElm == null) {
                /* the mouse pointer was not in any of the bounding boxes, but we
                /* might still be close to a post */
                for (int i = 0; i != UICount; i++) {
                    var ce = GetUI(i);
                    var elm = ce.Elm;
                    if (2 == elm.PostCount) {
                        var p1 = elm.GetPost(0);
                        var p2 = elm.GetPost(1);
                        if (Utils.Distance(p1, gx, gy) < 5) {
                            /// TODO: select post
                            newMouseElm = ce;
                            Mouse.Post = 0;
                            break;
                        }
                        if (Utils.Distance(p2, gx, gy) < 5) {
                            /// TODO: select post
                            newMouseElm = ce;
                            Mouse.Post = 1;
                            break;
                        }
                        if (Utils.DistanceOnLine(p1, p2, gx, gy) < 8) {
                            newMouseElm = ce;
                            break;
                        }
                    } else {
                        for (int j = elm.PostCount - 1; 0 <= j; j--) {
                            var pt = elm.GetPost(j);
                            if (Utils.Distance(pt, gx, gy) < 5) {
                                /// TODO: select post
                                newMouseElm = ce;
                                Mouse.Post = j;
                                break;
                            }
                        }
                        if (ce.DumpInfo.BoundingBox.Contains(gx, gy)) {
                            newMouseElm = ce;
                            break;
                        }
                    }
                }
            } else {
                Mouse.Post = -1;
                /* look for post close to the mouse pointer */
                for (int i = 0; i != newMouseElm.Elm.PostCount; i++) {
                    var pt = newMouseElm.Elm.GetPost(i);
                    if (Utils.Distance(pt, gx, gy) < 16) {
                        Mouse.Post = i;
                    }
                }
            }
            Repaint();
            setMouseElm(newMouseElm);
        }

        void onContextMenu(MouseEventArgs e) {
            mMenuClient.X = Location.X + e.X;
            mMenuClient.Y = Location.Y + e.Y;
            doPopupMenu();
        }

        public void doPopupMenu(int x, int y) {
            mMenuClient.X = x;
            mMenuClient.Y = y;
            doPopupMenu();
        }

        void doPopupMenu() {
            mMenuElm = Mouse.GripElm;
            mMenuScope = -1;
            mMenuPlotWave = -1;
            if (ScopeForm.SelectedScope != -1) {
                if (Scope.Property.List[ScopeForm.SelectedScope].CanMenu) {
                    mMenuScope = ScopeForm.SelectedScope;
                    mMenuPlotWave = Scope.Property.List[ScopeForm.SelectedScope].SelectedPlot;
                    mContextMenu = mScopePopupMenu.Show(mMenuClient.X, mMenuClient.Y, Scope.Property.List, ScopeForm.SelectedScope, false);
                }
            } else if (Mouse.GripElm != null) {
                if (!(Mouse.GripElm is Scope)) {
                    mContextMenu = mElementPopupMenu.Show(mMenuClient.X, mMenuClient.Y, Mouse.GripElm);
                } else {
                    var s = (Scope)Mouse.GripElm;
                    if (s.Properties.CanMenu) {
                        mMenuPlotWave = s.Properties.SelectedPlot;
                        mContextMenu = mScopePopupMenu.Show(mMenuClient.X, mMenuClient.Y, new Scope.Property[] { s.Properties }, 0, true);
                    }
                }
            }
        }

        void clearMouseElm() {
            ScopeForm.SelectedScope = -1;
            setMouseElm(null);
            PlotXElm = PlotYElm = null;
        }

        void scrollValues(int deltay) {
            if (Mouse.GripElm != null && !DialogIsShowing() && ScopeForm.SelectedScope == -1) {
                if ((Mouse.GripElm is Resistor) || (Mouse.GripElm is Capacitor) || (Mouse.GripElm is Inductor)) {
                    mScrollValuePopup = new ScrollValuePopup(deltay, Mouse.GripElm);
                    mScrollValuePopup.Show(
                        Location.X + MouseCursorX,
                        Location.Y + MouseCursorY
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

        static void enableUndoRedo() {
            mRedoItem.Enabled = mRedoStack.Count > 0;
            mUndoItem.Enabled = mUndoStack.Count > 0;
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
            for (i = UICount - 1; i >= 0; i--) {
                var ce = GetUI(i);
                /* ScopeElms don't cut-paste well because their reference to a parent
                /* elm by number get's messed up in the dump. For now we will just ignore them
                /* until I can be bothered to come up with something better */
                if (willDelete(ce) && !(ce is Scope)) {
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

        static void writeRecoveryToStorage() {
            var s = dumpCircuit();
            Storage.GetInstance().SetItem("circuitRecovery", s);
        }

        void readRecovery() {
            mRecovery = Storage.GetInstance().GetItem("circuitRecovery");
        }

        static void deleteUnusedScopeElms() {
            /* Remove any scopeElms for elements that no longer exist */
            for (int i = UICount - 1; 0 <= i; i--) {
                var ce = GetUI(i);
                if ((ce is Scope) && ((Scope)ce).Properties.NeedToRemove) {
                    ce.Delete();
                    UIList.RemoveAt(i);
                }
            }
        }

        static void doDelete(bool pushUndoFlag) {
            int i;
            if (pushUndoFlag) {
                PushUndo();
            }
            bool hasDeleted = false;

            for (i = UICount - 1; i >= 0; i--) {
                var ce = GetUI(i);
                if (willDelete(ce)) {
                    if (ce.IsMouseElm) {
                        setMouseElm(null);
                    }
                    ce.Delete();
                    UIList.RemoveAt(i);
                    hasDeleted = true;
                }
            }
            if (hasDeleted) {
                deleteUnusedScopeElms();
                NeedAnalyze();
                writeRecoveryToStorage();
            }
        }

        static bool willDelete(BaseUI ce) {
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
            for (int i = UICount - 1; i >= 0; i--) {
                var ce = GetUI(i);
                string m = ce.DumpModel();
                if (!string.IsNullOrEmpty(m)) {
                    r += m + "\n";
                }
                /* See notes on do cut why we don't copy ScopeElms. */
                if (ce.IsSelected && !(ce is Scope)) {
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

        void doPaste(string dump) {
            PushUndo();
            clearSelection();
            int i;

            /* get old bounding box */
            var oldbb = new RectangleF();
            for (i = 0; i != UICount; i++) {
                var ce = GetUI(i);
                var bb = ce.DumpInfo.BoundingBox;
                if (0 == i) {
                    oldbb = bb;
                } else {
                    oldbb = RectangleF.Union(oldbb, bb);
                }
            }

            /* add new items */
            int oldsz = UICount;
            if (dump != null) {
                readCircuit(dump, RC_RETAIN);
            } else {
                readClipboardFromStorage();
                readCircuit(mClipboard, RC_RETAIN);
            }

            /* select new items and get their bounding box */
            var newbb = new RectangleF();
            for (i = oldsz; i != UICount; i++) {
                var ce = GetUI(i);
                ce.IsSelected = true;
                var bb = ce.DumpInfo.BoundingBox;
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
                    for (i = oldsz; i != UICount; i++) {
                        if (!GetUI(i).AllowMove(mdx, mdy)) {
                            break;
                        }
                    }
                    if (i == UICount) {
                        dx = mdx;
                        dy = mdy;
                    }
                }

                /* move the new items */
                for (i = oldsz; i != UICount; i++) {
                    var ce = GetUI(i);
                    ce.Move(dx, dy);
                }
            }
            NeedAnalyze();
            writeRecoveryToStorage();
        }

        void clearSelection() {
            for (int i = 0; i != UICount; i++) {
                var ce = GetUI(i);
                ce.IsSelected = false;
            }
        }

        void doSelectAll() {
            for (int i = 0; i != UICount; i++) {
                var ce = GetUI(i);
                ce.IsSelected = true;
            }
        }

        bool anySelectedButMouse() {
            for (int i = 0; i != UICount; i++) {
                var ce = GetUI(i);
                if (ce != Mouse.GripElm && ce.IsSelected) {
                    return true;
                }
            }
            return false;
        }

        /* For debugging */
        void dumpNodelist() {
            string s;
            string cs;
            Console.WriteLine("Elm list Dump");
            for (int i = 0; i < UIList.Count; i++) {
                var e = UIList[i].Elm;
                cs = e.ToString();
                int p = cs.LastIndexOf('.');
                cs = cs.Substring(p + 1);
                if (cs == "WireElm") {
                    continue;
                }
                if (cs == "TransistorElm") {
                    if (((ElmTransistor)e).NPN == -1) {
                        cs = "PTransistorElm";
                    } else {
                        cs = "NTransistorElm";
                    }
                }
                s = cs;
                for (int j = 0; j < e.PostCount; j++) {
                    s = s + " " + e.Nodes[j];
                }
                Console.WriteLine(s);
            }
        }

        bool hasSelection() {
            for (int i = 0; i != UICount; i++) {
                if (GetUI(i).IsSelected) {
                    return true;
                }
            }
            return false;
        }

        static void updateCircuit() {
            bool didAnalyze = mAnalyzeFlag;
            if (mAnalyzeFlag) {
                Circuit.ClearElm();
                foreach (var ui in UIList) {
                    Circuit.AddElm(ui.Elm);
                }
                Circuit.AnalyzeCircuit();
                mAnalyzeFlag = false;
            }

            var g = BaseUI.Context;
            PDF.Page pdfG = null;
            PDF.Page scopeG = null;
            var bkIsRun = IsRunning;
            var bkPrint = ControlPanel.ChkPrintable.Checked;
            if (g.DoPrint) {
                g.DoPrint = false;
                if (bkIsRun) {
                    IsRunning = false;
                }
                if (bkPrint) {
                    ControlPanel.ChkPrintable.Checked = false;
                }
                pdfG = new PDF.Page(g.Width, g.Height);
                scopeG = new PDF.Page(mScopeForm.Width, mScopeForm.Height);
                g = pdfG;
                BaseUI.Context = pdfG;
            }

            if (ControlPanel.ChkPrintable.Checked) {
                g.Clear(Color.White);
            } else {
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

            g.ScrollBoard(mScroll);
            {
                if (null == pdfG) {
                    var pdfX0 = 0;
                    var pdfX1 = (int)PDF.Page.Width * 2;
                    var pdfY0 = 0;
                    var pdfY1 = (int)PDF.Page.Height * 2;
                    g.DrawColor = Color.Yellow;
                    g.DrawLine(pdfX0, pdfY0, pdfX1, pdfY0);
                    g.DrawLine(pdfX1, pdfY0, pdfX1, pdfY1);
                    g.DrawLine(pdfX1, pdfY1, pdfX0, pdfY1);
                    g.DrawLine(pdfX0, pdfY1, pdfX0, pdfY0);
                }

                /* draw elements */
                for (int i = 0; i != UICount; i++) {
                    var ui = UIList[i];
                    ui.Draw(g);
                    if (ui is Scope) {
                        g.ScrollBoard(mScroll);
                    }
                }

                /* draw posts normally */
                if (MouseMode != MOUSE_MODE.DRAG_ROW && MouseMode != MOUSE_MODE.DRAG_COLUMN) {
                    for (int i = 0; i != Circuit.PostDrawList.Count; i++) {
                        g.DrawPost(Circuit.PostDrawList[i]);
                    }
                }

                /* for some mouse modes, what matters is not the posts but the endpoints (which are only
                /* the same for 2-terminal elements).  We draw those now if needed */
                if (MouseMode == MOUSE_MODE.DRAG_ROW
                    || MouseMode == MOUSE_MODE.DRAG_COLUMN
                    || MouseMode == MOUSE_MODE.SPLIT
                    || MouseMode == MOUSE_MODE.DRAG_ITEM) {
                    for (int i = 0; i != UICount; i++) {
                        var ce = GetUI(i);
                        g.DrawPost(ce.DumpInfo.P1);
                        g.DrawPost(ce.DumpInfo.P2);
                        if (ce != Mouse.GripElm || MouseMode != MOUSE_MODE.SPLIT) {
                            g.DrawHandle(ce.DumpInfo.P1);
                            g.DrawHandle(ce.DumpInfo.P2);
                        } else {
                            ce.DrawHandles(g);
                        }
                    }
                }

                /* draw handles for elm we're creating */
                if ((MouseMode == MOUSE_MODE.SELECT) && Mouse.GripElm != null) {
                    Mouse.GripElm.DrawHandles(g);
                }

                /* draw handles for elm we're dragging */
                if (DragElm != null && (DragElm.DumpInfo.P1.X != DragElm.DumpInfo.P2.X || DragElm.DumpInfo.P1.Y != DragElm.DumpInfo.P2.Y)) {
                    DragElm.Draw(g);
                    DragElm.DrawHandles(g);
                }

                /* draw bad connections.  do this last so they will not be overdrawn. */
                for (int i = 0; i != Circuit.BadConnectionList.Count; i++) {
                    var cn = Circuit.BadConnectionList[i];
                    g.DrawHandle(cn);
                }

                if (0 < Mouse.SelectedArea.Width) {
                    g.DrawColor = CustomGraphics.SelectColor;
                    g.DrawRectangle(Mouse.SelectedArea);
                }

                /* draw cross hair */
                if (ControlPanel.ChkCrossHair.Checked && MouseCursorX >= 0
                    && MouseCursorX <= mCircuitArea.Width && MouseCursorY <= mCircuitArea.Height) {
                    int x = SnapGrid(inverseTransformX(MouseCursorX));
                    int y = SnapGrid(inverseTransformY(MouseCursorY));
                    g.DrawColor = Color.Gray;
                    g.DrawLine(x, inverseTransformY(0), x, inverseTransformY(mCircuitArea.Height));
                    g.DrawLine(inverseTransformX(0), y, inverseTransformX(mCircuitArea.Width), y);
                }
            }
            g.ClearTransform();

            mScopeForm.Draw(scopeG);

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
                pdf.AddPage(scopeG);
                var saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "PDFファイル(*.pdf)|*.pdf";
                saveFileDialog.FileName = Path.GetFileNameWithoutExtension(mFileName);
                saveFileDialog.ShowDialog();
                try {
                    pdf.Save(saveFileDialog.FileName);
                } catch(Exception ex) {
                    MessageBox.Show(ex.ToString());
                }
                IsRunning = bkIsRun;
                ControlPanel.ChkPrintable.Checked = bkPrint;
                BaseUI.Context = CustomGraphics.FromImage(g.Width, g.Height);
            }
            mLastFrameTime = mLastTime;
        }

        static void runCircuit(bool didAnalyze) {
            if (UICount == 0) {
                return;
            }

            mDumpMatrix = false;
            double steprate = ControlPanel.StepRate;
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

            int iter;
            for (iter = 1; ; iter++) {
                if (!Circuit.DoIteration()) {
                    break;
                }
                Circuit.Time += ControlPanel.TimeStep;

                for (int i = 0; i < Scope.Property.Count; i++) {
                    Scope.Property.List[i].TimeStep();
                }
                for (int i = 0; i < UICount; i++) {
                    if (UIList[i] is Scope) {
                        ((Scope)UIList[i]).StepScope();
                    }
                }

                /* Check whether enough time has elapsed to perform an *additional* iteration after
                /* those we have already completed. */
                tm = DateTime.Now.ToFileTimeUtc();
                lit = tm;
                if ((iter + 1) * 1000 >= steprate * (tm - mLastIterTime) || (tm - mLastFrameTime > 250000)) {
                    break;
                }
                if (!IsRunning) {
                    break;
                }
            }
            mLastIterTime = lit;
        }
        #endregion
    }
}
