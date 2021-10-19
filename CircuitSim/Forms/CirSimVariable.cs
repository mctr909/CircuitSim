using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Circuit.Elements;
using Circuit.Elements.Passive;

namespace Circuit {
    partial class CirSim {
        #region CONST
        public static readonly Font FONT_TEXT = new Font("Meiryo UI", 9.0f);
        public static readonly Brush BRUSH_TEXT = Brushes.Red;

        public static readonly string MU_TEXT = "u";
        public static readonly string OHM_TEXT = "Ω";

        public enum MOUSE_MODE {
            ADD_ELM,
            DRAG_ALL,
            DRAG_ROW,
            DRAG_COLUMN,
            DRAG_SELECTED,
            DRAG_POST,
            SELECT,
            DRAG_SPLITTER
        }

        public const int GRID_SIZE = 8;
        public const int POSTGRABSQ = 25;
        public const int MINPOSTGRABSIZE = 256;

        public const int RC_RETAIN = 1;
        public const int RC_NO_CENTER = 2;
        public const int RC_SUBCIRCUITS = 4;

        const int INFO_WIDTH = 120;
        const int GRID_MASK = ~(GRID_SIZE - 1);
        const int GRID_ROUND = GRID_SIZE / 2 - 1;
        #endregion

        #region Static Property
        public static CirSim Sim { get; private set; } = null;
        public static ElementInfoDialog EditDialog { get; set; }
        public static ElementInfoDialog CustomLogicEditDialog { get; set; }
        public static ElementInfoDialog DiodeModelEditDialog { get; set; }
        public static SliderDialog SliderDialog { get; set; }
        public static Form DialogShowing { get; set; } = null;
        public static Random Random { get; set; } = new Random();
        public static double CurrentMult { get; set; } = 0;
        #endregion

        #region Property
        public bool IsRunning { get; private set; }
        public MOUSE_MODE MouseMode { get; private set; } = MOUSE_MODE.SELECT;
        public MOUSE_MODE TempMouseMode { get; private set; } = MOUSE_MODE.SELECT;
        public Point DisplayLocation { get { return mParent.Location; } }
        public int ScopeSelected { get; private set; } = -1;
        public int MouseCursorX { get; private set; } = -1;
        public int MouseCursorY { get; private set; } = -1;
        public float[] Transform { get; private set; }
        public bool DcAnalysisFlag { get; private set; }
        public double Time { get; private set; }
        
        public List<CircuitElm> ElmList { get; private set; }
        public CircuitElm PlotXElm { get; private set; }
        public CircuitElm PlotYElm { get; private set; }
        public CircuitElm DragElm { get; private set; }
        public List<Adjustable> Adjustables { get; private set; } = new List<Adjustable>();
        #endregion

        #region Variable
        static ScrollValuePopup mScrollValuePopup;

        Form mParent;
        Timer mTimer;

        MenuStrip mMenuBar;
        MenuItems mMenuItems;
        SplitContainer mSplitContainer;

        ContextMenuStrip mContextMenu = null;
        Point mContextMenuLocation;
        ElementPopupMenu mElementPopupMenu;
        ScopePopupMenu mScopePopupMenu;

        PictureBox mPixCir;
        Bitmap mBmp = null;
        Graphics mContext;

        MenuItem mUndoItem;
        MenuItem mRedoItem;
        MenuItem mPasteItem;

        Circuit mCir;
        CircuitElm mMenuElm;
        CircuitElm mMouseElm = null;
        SwitchElm mHeldSwitchElm;

        List<string> mUndoStack = new List<string>();
        List<string> mRedoStack = new List<string>();

        string mStartCircuit = null;
        string mStartLabel = null;

        string mRecovery;
        string mClipboard = "";

        double mScopeHeightFraction = 0.2;
        public int mScopeCount { get; set; }
        public Scope[] mScopes { get; set; }
        int[] mScopeColCount;

        ELEMENTS mMouseMode = ELEMENTS.INVALID;
        MouseButtons mMouseButton = MouseButtons.None;
        DateTime mLastMouseMove = DateTime.Now;
        bool mouseDragging = false;
        bool mMouseWasOverSplitter = false;
        int mMousePost = -1;

        int mDraggingPost;
        Point mDragGrid;
        Point mDragScreen;
        Point mInitDragGrid;

        bool mIsPressShift;
        bool mIsPressCtrl;
        bool mIsPressAlt;

        Point mMenuClient;
        Point mMenuPos;
        int mMenuScope = -1;
        int mMenuPlot = -1;

        long mMouseDownTime;
        long mZoomTime;

        Rectangle mSelectedArea;
        Rectangle mCircuitArea;

        long mLastTime = 0;
        long mLastFrameTime;
        long mLastIterTime;
        long mLastSysTime = 0;

        bool mNeedsRepaint;
        bool mAnalyzeFlag;
        bool mDumpMatrix;
        #endregion
    }
}
