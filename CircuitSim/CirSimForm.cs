using Circuit.Common;
using Circuit.Elements.Active;
using Circuit.Elements.Passive;
using Circuit.Forms;
using Circuit.UI;
using Circuit.UI.Output;
using Circuit.UI.Passive;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Circuit {
    public partial class CirSimForm : Form {
        #region CONST
        public static readonly Font FONT_TEXT = new Font("Meiryo UI", 9.0f);
        public static readonly Brush BRUSH_TEXT = Brushes.Red;

        public static readonly string OHM_TEXT = "Ω";

        public const int GRID_SIZE = 8;
        public const int CURRENT_DOT_SIZE = 8;
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
        public static BaseUI ConstructElm { get; private set; }
        public static List<BaseUI> UIList { get; private set; }
        public static int UICount { get { return null == UIList ? 0 : UIList.Count; } }
        public static BaseUI PlotXElm { get; private set; }
        public static BaseUI PlotYElm { get; private set; }
        public static List<Adjustable> Adjustables { get; private set; } = new List<Adjustable>();
        #endregion

        #region Variable
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

        static PictureBox mPixCir;
        static Bitmap mBmp = null;
        static Graphics mContext;

        static MenuItem mUndoItem;
        static MenuItem mRedoItem;
        MenuItem mPasteItem;

        static DUMP_ID mAddElm = DUMP_ID.INVALID;

        static BaseUI mMenuElm;
        static Switch mHeldSwitchElm;
        Point mMenuClient;
        Point mMenuPos;

        static List<string> mUndoStack = new List<string>();
        static List<string> mRedoStack = new List<string>();

        string mRecovery;
        string mClipboard = "";
        static Rectangle mCircuitArea;

        bool mIsPressShift;
        bool mIsPressCtrl;
        bool mIsPressAlt;

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
                mPixCir.MouseWheel += new MouseEventHandler((s, e) => { showScrollValues(); });
                mPixCir.MouseClick += new MouseEventHandler((s, e) => { onClick(e); });
                mPixCir.MouseLeave += new EventHandler((s, e) => { MouseInfo.SetCursor(new Point(-1, -1)); });
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

            SetSimRunning(true);
        }

        private void Form1_Load(object sender, EventArgs e) {
            Width = 800;
            Height = 600;
            mScopeForm.Show();
        }

        #region Public method
        public void AddElement(DUMP_ID item) {
            if (mContextMenu != null) {
                mContextMenu.Close();
            }
            MouseInfo.Mode = MouseInfo.MODE.ADD_ELM;
            Cursor = Cursors.Arrow;
            mAddElm = item;
            Repaint();
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
            case MENU_ITEM.PRINT:
                BaseUI.Context.DoPrint = true;
                break;
            }

            if (MouseInfo.GrippedElm != null) {
                mMenuElm = MouseInfo.GrippedElm;
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
                MouseInfo.Mode = MouseInfo.MODE.DRAG_ITEM;
                break;
            case MENU_ITEM.CENTER_CIRCUIT:
                PushUndo();
                MouseInfo.Centering(mCircuitArea.Width, mCircuitArea.Height, getCircuitBounds());
                break;
            }

            Repaint();
        }

        public void Reload() {
            PushUndo();
            readCircuit(mRecovery);
        }

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

        public static void SetSimRunning(bool s) {
            Console.WriteLine(Circuit.StopMessage);
            if (s) {
                if (Circuit.StopMessage != null) {
                    return;
                }
                IsRunning = true;
                ControlPanel.BtnRunStop.Text = "停止";
            } else {
                IsRunning = false;
                mAnalyzeFlag = false;
                ControlPanel.BtnRunStop.Text = "実行";
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
            ScopeForm.ResetGraph();
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
                mMenuElm = null;
                PushUndo();
                doDelete(true);
            }

            if (code == Keys.Escape || e.KeyValue == 32) {
                mMenuItems.AllUnchecked();
                MouseInfo.Mode = MouseInfo.MODE.NONE;
                mAddElm = DUMP_ID.INVALID;
            }
        }
        #endregion

        #region Mouse event method
        void onClick(MouseEventArgs e) {
            if (e.Button == MouseButtons.Right) {
                onContextMenu(e);
            }
        }

        void onDoubleClick(EventArgs e) {
            if (MouseInfo.GrippedElm == null) {
                return;
            }
            doEdit(MouseInfo.GrippedElm, new Point(
                Location.X + mMenuClient.X,
                Location.Y + mMenuClient.Y));
        }

        void onMouseDown(MouseEventArgs e) {
            Circuit.StopElm = null; /* if stopped, allow user to select other elements to fix circuit */
            mMenuPos = mMenuClient = e.Location;
            MouseInfo.SetCursor(e.Location);
            MouseInfo.Button = e.Button;

            /* maybe someone did copy in another window?  should really do this when */
            /* window receives focus */
            enablePaste();

            if (MouseInfo.Button != MouseButtons.Left && MouseInfo.Button != MouseButtons.Middle) {
                return;
            }

            // set mouseElm in case we are on mobile
            mouseSelect();

            MouseInfo.IsDragging = true;

            if (MouseInfo.Mode == MouseInfo.MODE.NONE && MouseInfo.Button == MouseButtons.Left) {
                /* left mouse */
                if (mIsPressCtrl && mIsPressAlt) {
                    MouseInfo.Mode = MouseInfo.MODE.DRAG_COLUMN;
                    Cursor = Cursors.SizeWE;
                } else if (mIsPressCtrl && mIsPressShift) {
                    MouseInfo.Mode = MouseInfo.MODE.DRAG_ROW;
                    Cursor = Cursors.SizeNS;
                } else if (mIsPressCtrl) {
                    MouseInfo.Mode = MouseInfo.MODE.SCROLL;
                    Cursor = Cursors.NoMove2D;
                } else if (mIsPressAlt) {
                    MouseInfo.Mode = MouseInfo.MODE.SPLIT;
                    Cursor = Cursors.Cross;
                } else {
                    MouseInfo.Mode = MouseInfo.MODE.SELECT;
                    Cursor = Cursors.Hand;
                }
            }

            var gpos = MouseInfo.ToAbsPos(e.Location);
            if (doSwitch(gpos)) {
                /* do this BEFORE we change the mouse mode to MODE_DRAG_POST!  Or else logic inputs */
                /* will add dots to the whole circuit when we click on them! */
                return;
            }

            if (MouseInfo.Mode != MouseInfo.MODE.NONE && MouseInfo.Mode != MouseInfo.MODE.DRAG_ITEM) {
                clearSelection();
            }

            PushUndo();
            MouseInfo.DragBegin = gpos;
            if (MouseInfo.Mode != MouseInfo.MODE.ADD_ELM) {
                return;
            }
            /* */
            gpos = SnapGrid(gpos);
            if (!mCircuitArea.Contains(MouseInfo.Cursor)) {
                return;
            }
            ConstructElm = MenuItems.ConstructElement(mAddElm, gpos);
        }

        void onMouseUp(MouseEventArgs e) {
            MouseInfo.IsDragging = false;
            MouseInfo.Button = MouseButtons.None;

            switch (MouseInfo.Mode) {
            case MouseInfo.MODE.ADD_ELM:
                if (!ControlPanel.ChkContinuousArrangement.Checked) {
                    mMenuItems.AllUnchecked();
                    MouseInfo.Mode = MouseInfo.MODE.NONE;
                    mAddElm = DUMP_ID.INVALID;
                }
                break;
            case MouseInfo.MODE.SELECT_AREA:
                if (MouseInfo.SelectedArea.Width == 0 || MouseInfo.SelectedArea.Height == 0) {
                    clearSelection();
                }
                if (hasSelection()) {
                    MouseInfo.Mode = MouseInfo.MODE.DRAG_ITEM;
                } else {
                    MouseInfo.Mode = MouseInfo.MODE.NONE;
                }
                break;
            case MouseInfo.MODE.DRAG_ITEM:
                clearSelection();
                MouseInfo.Mode = MouseInfo.MODE.NONE;
                break;
            case MouseInfo.MODE.SPLIT:
                doSplit(MouseInfo.GrippedElm);
                MouseInfo.Mode = MouseInfo.MODE.NONE;
                break;
            default:
                MouseInfo.Mode = MouseInfo.MODE.NONE;
                break;
            }

            MouseInfo.SelectedArea = new Rectangle();

            bool circuitChanged = false;
            if (mHeldSwitchElm != null) {
                mHeldSwitchElm.MouseUp();
                mHeldSwitchElm = null;
                circuitChanged = true;
            }
            if (ConstructElm != null) {
                /* if the element is zero size then don't create it */
                if (ConstructElm.IsCreationFailed) {
                    ConstructElm.Delete();
                    if (MouseInfo.Mode == MouseInfo.MODE.SELECT || MouseInfo.Mode == MouseInfo.MODE.DRAG_ITEM) {
                        clearSelection();
                    }
                } else {
                    UIList.Add(ConstructElm);
                    circuitChanged = true;
                    writeRecoveryToStorage();
                }
                ConstructElm = null;
            }
            if (circuitChanged) {
                NeedAnalyze();
            }
            if (ConstructElm != null) {
                ConstructElm.Delete();
            }
            ConstructElm = null;
            Cursor = Cursors.Default;
            Repaint();
        }

        void onMouseMove(MouseEventArgs e) {
            if (MouseInfo.Delay()) {
                return;
            }
            MouseInfo.SetCursor(e.Location);
            if (MouseInfo.IsDragging) {
                mouseDrag();
            } else {
                mouseSelect();
            }
        }

        void onContextMenu(MouseEventArgs e) {
            if (MouseInfo.GrippedElm == null) {
                return;
            }
            mMenuClient.X = Location.X + e.X;
            mMenuClient.Y = Location.Y + e.Y;
            mMenuElm = MouseInfo.GrippedElm;
            if (MouseInfo.GrippedElm is Scope) {
                var s = (Scope)MouseInfo.GrippedElm;
                if (s.Plot.CanMenu) {
                    var fm = new ScopePopupMenu();
                    mContextMenu = fm.Show(mMenuClient.X, mMenuClient.Y, new ScopePlot[] { s.Plot }, 0, true);
                }
            } else {
                var menu = new ElementPopupMenu(new ElementPopupMenu.Callback((item) => {
                    if (mContextMenu != null) {
                        mContextMenu.Close();
                    }
                    switch (item) {
                    case ElementPopupMenu.Item.EDIT:
                        doEdit(mMenuElm, mMenuClient);
                        break;
                    case ElementPopupMenu.Item.SPLIT_WIRE:
                        doSplit(mMenuElm);
                        break;
                    case ElementPopupMenu.Item.FLIP_POST:
                        doFlip();
                        break;
                    case ElementPopupMenu.Item.SLIDERS:
                        doSliders(mMenuElm, mMenuClient);
                        break;
                    case ElementPopupMenu.Item.SCOPE_WINDOW:
                        ScopeForm.AddPlot(mMenuElm);
                        break;
                    case ElementPopupMenu.Item.SCOPE_FLOAT:
                        if (mMenuElm != null) {
                            var newScope = new Scope(SnapGrid(mMenuElm.Post.A.X + 50, mMenuElm.Post.A.Y + 50));
                            UIList.Add(newScope);
                            newScope.Plot.Setup(mMenuElm);
                        }
                        break;
                    }
                    Repaint();
                }));
                mContextMenu = menu.Show(mMenuClient, MouseInfo.GrippedElm);
            }
        }

        void mouseDrag() {
            /* ignore right mouse button with no modifiers (needed on PC) */
            if (MouseInfo.Button == MouseButtons.Right) {
                return;
            }
            var gpos = SnapGrid(MouseInfo.GetAbsPos());
            if (!mCircuitArea.Contains(MouseInfo.Cursor)) {
                return;
            }
            bool changed = false;
            if (ConstructElm != null) {
                ConstructElm.Drag(gpos);
            }
            bool success = true;
            switch (MouseInfo.Mode) {
            case MouseInfo.MODE.SCROLL: {
                MouseInfo.Scroll();
                break;
            }   
            case MouseInfo.MODE.DRAG_ROW:
                dragRow(gpos);
                changed = true;
                break;
            case MouseInfo.MODE.DRAG_COLUMN:
                dragColumn(gpos);
                changed = true;
                break;
            case MouseInfo.MODE.SELECT:
                if (MouseInfo.GrippedElm == null) {
                    MouseInfo.Mode = MouseInfo.MODE.SELECT_AREA;
                } else {
                    MouseInfo.DraggingPost = MouseInfo.HoveringPost;
                    MouseInfo.HoveringPost = EPOST.INVALID;
                    if (MouseInfo.DraggingPost == EPOST.BOTH) {
                        MouseInfo.Mode = MouseInfo.MODE.DRAG_ITEM;
                    } else {
                        MouseInfo.Mode = MouseInfo.MODE.DRAG_POST;
                    }
                }
                break;
            case MouseInfo.MODE.SELECT_AREA:
                selectArea(gpos);
                break;
            case MouseInfo.MODE.DRAG_ITEM:
                changed = success = dragSelected(gpos);
                break;
            case MouseInfo.MODE.DRAG_POST:
                MouseInfo.MoveGrippedElm(gpos);
                NeedAnalyze();
                changed = true;
                break;
            }
            if (success) {
                /* Console.WriteLine("setting dragGridx in mousedragged");*/
                MouseInfo.DragEnd = MouseInfo.ToAbsPos(MouseInfo.CommitCursor());
                if (!(MouseInfo.Mode == MouseInfo.MODE.DRAG_ITEM && onlyGraphicsElmsSelected())) {
                    MouseInfo.DragEnd = SnapGrid(MouseInfo.DragEnd);
                }
            }
            if (changed) {
                writeRecoveryToStorage();
            }
            Repaint();
        }

        void mouseSelect() {
            MouseInfo.CommitCursor();
            var gpos = MouseInfo.GetAbsPos();
            MouseInfo.DragEnd = SnapGrid(gpos);
            MouseInfo.DraggingPost = EPOST.INVALID;
            MouseInfo.HoveringPost = EPOST.INVALID;

            PlotXElm = PlotYElm = null;

            BaseUI mostNearUI = null;
            var mostNear = double.MaxValue;
            for (int i = 0; i != UICount; i++) {
                var ce = GetUI(i);
                var lineD = ce.Distance(gpos);
                if (lineD <= CustomGraphics.HANDLE_RADIUS && lineD < mostNear) {
                    MouseInfo.HoveringPost = EPOST.BOTH;
                    mostNearUI = ce;
                    mostNear = lineD;
                }
            }
            if (mostNearUI == null) {
                for (int i = 0; i != UICount; i++) {
                    var ce = GetUI(i);
                    var postDa = ce.DistancePostA(gpos);
                    var postDb = ce.DistancePostB(gpos);
                    if (postDa <= CustomGraphics.HANDLE_RADIUS && postDa < mostNear) {
                        MouseInfo.HoveringPost = EPOST.A;
                        mostNearUI = ce;
                        mostNear = postDa;
                    }
                    if (postDb <= CustomGraphics.HANDLE_RADIUS && postDb < mostNear) {
                        MouseInfo.HoveringPost = EPOST.B;
                        mostNearUI = ce;
                        mostNear = postDb;
                    }
                }
            }
            if (mostNearUI == null) {
                clearMouseElm();
            } else {
                var postDa = mostNearUI.DistancePostA(gpos);
                var postDb = mostNearUI.DistancePostB(gpos);
                if (postDa <= CustomGraphics.HANDLE_RADIUS) {
                    MouseInfo.HoveringPost = EPOST.A;
                }
                if (postDb <= CustomGraphics.HANDLE_RADIUS) {
                    MouseInfo.HoveringPost = EPOST.B;
                }
                MouseInfo.GripElm(mostNearUI);
            }
            Repaint();
        }

        void selectArea(Point pos) {
            MouseInfo.SelectArea(pos);
            for (int i = 0; i != UICount; i++) {
                var ce = GetUI(i);
                ce.SelectRect(MouseInfo.SelectedArea);
            }
        }

        void dragRow(Point pos) {
            int dy = (pos.Y - MouseInfo.DragEnd.Y) / GRID_SIZE;
            dy *= GRID_SIZE;
            if (0 == dy) {
                return;
            }
            for (int i = 0; i != UICount; i++) {
                var ce = GetUI(i);
                var p = EPOST.INVALID;
                if (pos.Y <= ce.Post.A.Y) {
                    p = EPOST.A;
                }
                if (pos.Y <= ce.Post.B.Y) {
                    p = EPOST.B;
                }
                if (pos.Y <= ce.Post.A.Y && pos.Y <= ce.Post.B.Y) {
                    p = EPOST.BOTH;
                }
                ce.Move(0, dy, p);
            }
            removeZeroLengthElements();
        }

        void dragColumn(Point pos) {
            int dx = (pos.X - MouseInfo.DragEnd.X) / GRID_SIZE;
            dx *= GRID_SIZE;
            if (0 == dx) {
                return;
            }
            for (int i = 0; i != UICount; i++) {
                var ce = GetUI(i);
                var p = EPOST.INVALID;
                if (pos.X <= ce.Post.A.X) {
                    p = EPOST.A;
                }
                if (pos.X <= ce.Post.B.X) {
                    p = EPOST.B;
                }
                if (pos.X <= ce.Post.A.X && pos.X <= ce.Post.B.X) {
                    p = EPOST.BOTH;
                }
                ce.Move(dx, 0, p);
            }
            removeZeroLengthElements();
        }

        bool dragSelected(Point pos) {
            bool me = false;
            int i;
            if (MouseInfo.GrippedElm != null && !MouseInfo.GrippedElm.IsSelected) {
                MouseInfo.GrippedElm.IsSelected = me = true;
            }
            if (!onlyGraphicsElmsSelected()) {
                pos = SnapGrid(pos);
            }
            int dx = pos.X - MouseInfo.DragEnd.X;
            int dy = pos.Y - MouseInfo.DragEnd.Y;
            if (dx == 0 && dy == 0) {
                /* don't leave mouseElm selected if we selected it above */
                if (me) {
                    MouseInfo.GrippedElm.IsSelected = false;
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
                MouseInfo.GrippedElm.IsSelected = false;
            }

            return allowed;
        }

        bool hasSelection() {
            for (int i = 0; i != UICount; i++) {
                if (GetUI(i).IsSelected) {
                    return true;
                }
            }
            return false;
        }

        bool doSwitch(Point pos) {
            if (MouseInfo.GrippedElm == null || !(MouseInfo.GrippedElm is Switch)) {
                return false;
            }
            var se = (Switch)MouseInfo.GrippedElm;
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

        Rectangle getCircuitBounds() {
            if (0 == UICount) {
                return new Rectangle();
            }
            int minx = int.MaxValue, miny = int.MaxValue;
            int maxx = 0, maxy = 0;
            for (int i = 0; i < UICount; i++) {
                var ce = GetUI(i);
                minx = Math.Min(ce.Post.A.X, Math.Min(ce.Post.B.X, minx));
                miny = Math.Min(ce.Post.A.Y, Math.Min(ce.Post.B.Y, miny));
                maxx = Math.Max(ce.Post.A.X, Math.Max(ce.Post.B.X, maxx));
                maxy = Math.Max(ce.Post.A.Y, Math.Max(ce.Post.B.Y, maxy));
            }
            return new Rectangle(minx, miny, maxx - minx, maxy - miny);
        }

        void doEdit(BaseUI eable, Point location) {
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
            DiodeModel.ClearDumpedFlags();

            int f = ControlPanel.ChkShowCurrent.Checked ? 1 : 0;
            f |= ControlPanel.ChkShowValues.Checked ? 0 : 16;

            string dump = "$ " + f
                + " " + ControlPanel.TimeStep
                + " " + ControlPanel.StepRate
                + " " + ControlPanel.TrbCurrent.Value + "\n";

            int i;
            for (i = 0; i != UICount; i++) {
                var ce = GetUI(i);
                dump += ce.Dump() + "\n";
            }
            dump += ScopeForm.Dump();
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
                ScopeForm.PlotCount = 0;
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
                            ScopeForm.Undump(st);
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
                        if (tint == '&') {
                            var adj = new Adjustable(st);
                            Adjustables.Add(adj);
                            break;
                        }
                        var x = st.nextTokenInt();
                        var y = st.nextTokenInt();
                        var p1 = new Point(x, y);
                        x = st.nextTokenInt();
                        y = st.nextTokenInt();
                        var p2 = new Point(x, y);
                        var f = st.nextTokenInt();
                        var dumpId = MenuItems.GetDumpIdFromString(type);
                        var newce = MenuItems.CreateCe(dumpId, p1, p2, f, st);
                        if (st.HasMoreTokens) {
                            string v;
                            st.nextToken(out v);
                            newce.ReferenceName = Utils.Unescape(v);
                        } else {
                            newce.ReferenceName = "";
                        }
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
                MouseInfo.Centering(mCircuitArea.Width, mCircuitArea.Height, getCircuitBounds());
            }
        }

        void readOptions(StringTokenizer st) {
            var flags = st.nextTokenInt();
            ControlPanel.ChkShowCurrent.Checked = (flags & 1) != 0;
            ControlPanel.ChkShowValues.Checked = (flags & 16) == 0;

            ControlPanel.TimeStep = st.nextTokenDouble();
            double sp = st.nextTokenDouble();
            int sp2 = (int)(Math.Log(10 * sp) * 24 + 61.5);
            ControlPanel.TrbSpeed.Value = sp2;
            var v = st.nextTokenInt();
            ControlPanel.TrbCurrent.Value = v * ControlPanel.TrbCurrent.Maximum / 50;
        }

        bool onlyGraphicsElmsSelected() {
            if (MouseInfo.GrippedElm != null) {
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
            var pos = SnapGrid(MouseInfo.ToAbsPos(mMenuPos));
            if (ce == null || !(ce is Wire)) {
                return;
            }
            if (ce.Post.A.X == ce.Post.B.X) {
                pos.X = ce.Post.A.X;
            } else {
                pos.Y = ce.Post.A.Y;
            }
            /* don't create zero-length wire */
            if (pos.X == ce.Post.A.X && pos.Y == ce.Post.A.Y || pos.X == ce.Post.B.X && pos.Y == ce.Post.B.Y) {
                return;
            }
            var newWire = new Wire(pos);
            newWire.Drag(ce.Post.B);
            ce.Drag(pos);
            UIList.Add(newWire);
            NeedAnalyze();
        }

        void removeZeroLengthElements() {
            for (int i = UICount - 1; i >= 0; i--) {
                var ce = GetUI(i);
                if (ce.Post.A.X == ce.Post.B.X && ce.Post.A.Y == ce.Post.B.Y) {
                    UIList.RemoveAt(i);
                    /*Console.WriteLine("delete element: {0} {1}\t{2} {3}\t{4}", ce.GetType(), ce.x1, ce.y1, ce.x2, ce.y2); */
                    ce.Delete();
                }
            }
            NeedAnalyze();
        }

        void clearMouseElm() {
            MouseInfo.GripElm(null);
            PlotXElm = PlotYElm = null;
        }

        void showScrollValues() {
            if (MouseInfo.GrippedElm == null || DialogIsShowing()) {
                return;
            }
            if (MouseInfo.GrippedElm is Resistor || MouseInfo.GrippedElm is Pot || MouseInfo.GrippedElm is Capacitor || MouseInfo.GrippedElm is Inductor) {
                mScrollValuePopup = new ScrollValuePopup(MouseInfo.GrippedElm);
                mScrollValuePopup.Show(
                    Location.X + MouseInfo.Cursor.X,
                    Location.Y + MouseInfo.Cursor.Y
                );
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
                    mClipboard += ce.Dump() + "\n";
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

        public static void DeleteUnusedScopeElms() {
            /* Remove any scopeElms for elements that no longer exist */
            for (int i = UICount - 1; 0 <= i; i--) {
                var ce = GetUI(i);
                if ((ce is Scope) && ((Scope)ce).Plot.NeedToRemove) {
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
                        MouseInfo.GripElm(null);
                    }
                    ce.Delete();
                    UIList.RemoveAt(i);
                    hasDeleted = true;
                }
            }
            if (hasDeleted) {
                DeleteUnusedScopeElms();
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
            DiodeModel.ClearDumpedFlags();
            for (int i = UICount - 1; i >= 0; i--) {
                var ce = GetUI(i);
                /* See notes on do cut why we don't copy ScopeElms. */
                if (ce.IsSelected && !(ce is Scope)) {
                    r += ce.Dump() + "\n";
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
                var bb = ce.Post.BoundingBox;
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
                var bb = ce.Post.BoundingBox;
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
                if (MouseInfo.Cursor.X > 0 && mCircuitArea.Contains(MouseInfo.Cursor)) {
                    var g = MouseInfo.GetAbsPos();
                    int mdx = SnapGrid((int)(g.X - (newbb.X + newbb.Width / 2)));
                    int mdy = SnapGrid((int)(g.Y - (newbb.Y + newbb.Height / 2)));
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
                if (ce != MouseInfo.GrippedElm && ce.IsSelected) {
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
                for (int j = 0; j < e.TermCount; j++) {
                    s = s + " " + e.Nodes[j];
                }
                Console.WriteLine(s);
            }
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
                    var c = ControlPanel.TrbCurrent.Value * 50.0 / ControlPanel.TrbCurrent.Maximum;
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

            var g = BaseUI.Context;
            PDF.Page pdfCircuit = null;
            PDF.Page pdfScope = null;
            var bkIsRun = IsRunning;
            var bkPrint = ControlPanel.ChkPrintable.Checked;
            if (g.DoPrint) {
                g.DoPrint = false;
                if (bkIsRun) {
                    IsRunning = false;
                }
                ControlPanel.ChkPrintable.Checked = true;
                pdfCircuit = new PDF.Page(g.Width, g.Height);
                pdfScope = new PDF.Page(mScopeForm.Width, mScopeForm.Height);
                g = pdfCircuit;
                BaseUI.Context = pdfCircuit;
            }

            drawCircuit(g);
            mScopeForm.Draw(pdfScope);

            var info = new string[10];
            if (MouseInfo.GrippedElm == null) {
                ControlPanel.LblSelectInfo.Text = "";
            } else {
                MouseInfo.GrippedElm.GetInfo(info);
                ControlPanel.LblSelectInfo.Text = "";
                foreach (var str in info) {
                    if (string.IsNullOrEmpty(str)) {
                        break;
                    }
                    ControlPanel.LblSelectInfo.Text += str + "\n";
                }
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

            if (g is PDF.Page) {
                var pdf = new PDF();
                pdf.AddPage(pdfCircuit);
                pdf.AddPage(pdfScope);
                var saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "PDFファイル(*.pdf)|*.pdf";
                saveFileDialog.FileName = Path.GetFileNameWithoutExtension(mFileName);
                saveFileDialog.ShowDialog();
                try {
                    pdf.Save(saveFileDialog.FileName);
                } catch (Exception ex) {
                    MessageBox.Show(ex.ToString());
                }
                IsRunning = bkIsRun;
                ControlPanel.ChkPrintable.Checked = bkPrint;
                BaseUI.Context = CustomGraphics.FromImage(g.Width, g.Height);
            } else {
                mBmp = new Bitmap(g.Width, g.Height);
                mContext = Graphics.FromImage(mBmp);
                BaseUI.Context.CopyTo(mContext);
                mPixCir.Image = mBmp;
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
                ScopeForm.TimeStep();
                for (int i = 0; i < UICount; i++) {
                    if (UIList[i] is Scope) {
                        ((Scope)UIList[i]).Plot.TimeStep();
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

        static void drawCircuit(CustomGraphics g) {
            g.Clear(ControlPanel.ChkPrintable.Checked ? Color.White : Color.Black);
            g.ScrollCircuit(MouseInfo.Offset);

            if (!(g is PDF.Page)) {
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
            foreach (var ui in UIList) {
                ui.Draw(g);
                if (ui is Scope) {
                    g.ScrollCircuit(MouseInfo.Offset);
                }
            }

            /* draw posts */
            foreach (var p in Circuit.DrawPostList) {
                g.DrawPost(p);
            }
            if (MouseInfo.Mode == MouseInfo.MODE.DRAG_ITEM || MouseInfo.Mode == MouseInfo.MODE.DRAG_POST) {
                foreach(var p in Circuit.UndrawPostList) {
                    g.DrawPost(p);
                }
            }
            if (ConstructElm != null && (ConstructElm.Post.A.X != ConstructElm.Post.B.X || ConstructElm.Post.A.Y != ConstructElm.Post.B.Y)) {
                ConstructElm.Draw(g);
                var ce = ConstructElm.Elm;
                for (int i = ce.TermCount - 1; 0 <= i; i--) {
                    var p = ce.GetNodePos(i);
                    g.DrawPost(p);
                }
                g.DrawHandle(ConstructElm.Post.B);
            }
            if (MouseInfo.GrippedElm != null) {
                var ce = MouseInfo.GrippedElm;
                switch (MouseInfo.HoveringPost) {
                case EPOST.A:
                    g.DrawHandle(ce.Post.A);
                    g.DrawPost(ce.Post.B);
                    break;
                case EPOST.B:
                    g.DrawPost(ce.Post.A);
                    g.DrawHandle(ce.Post.B);
                    break;
                default:
                    switch (MouseInfo.DraggingPost) {
                    case EPOST.A:
                        g.DrawHandle(ce.Post.A);
                        g.DrawPost(ce.Post.B);
                        break;
                    case EPOST.B:
                        g.DrawPost(ce.Post.A);
                        g.DrawHandle(ce.Post.B);
                        break;
                    default:
                        g.DrawPost(ce.Post.A);
                        g.DrawPost(ce.Post.B);
                        break;
                    }
                    break;
                }
            }
            foreach (var p in Circuit.BadConnectionList) {
                g.DrawPost(p);
            }

            if (0 < MouseInfo.SelectedArea.Width) {
                g.DrawColor = CustomGraphics.SelectColor;
                g.DrawRectangle(MouseInfo.SelectedArea);
            }

            /* draw cross hair */
            if (ControlPanel.ChkCrossHair.Checked && MouseInfo.Cursor.X >= 0
                && MouseInfo.Cursor.X <= mCircuitArea.Width && MouseInfo.Cursor.Y <= mCircuitArea.Height) {
                var gr = SnapGrid(MouseInfo.GetAbsPos());
                var g1 = MouseInfo.ToAbsPos(0, 0);
                var g2 = MouseInfo.ToAbsPos(mCircuitArea.Width, mCircuitArea.Height);
                g.DrawColor = Color.Gray;
                g.DrawLine(gr.X, g1.Y, gr.X, g2.Y);
                g.DrawLine(g1.X, gr.Y, g2.X, gr.Y);
            }
            g.ClearTransform();
        }
        #endregion
    }
}
