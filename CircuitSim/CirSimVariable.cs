using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Circuit.Elements;

namespace Circuit {
    partial class CirSim {
        #region CONST
        public static readonly Font FONT_TEXT = new Font("Meiryo UI", 9.0f);
        public static readonly Brush BRUSH_TEXT = Brushes.Red;

        public static readonly string muString = "μ";
        public static readonly string ohmString = "Ω";

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

        public const int POSTGRABSQ = 25;
        public const int MINPOSTGRABSIZE = 256;

        public const int RC_RETAIN = 1;
        public const int RC_NO_CENTER = 2;
        public const int RC_SUBCIRCUITS = 4;

        const int infoWidth = 120;
        #endregion

        #region Form
        public MenuItems Menu;

        Form mParent;
        Timer timer;

        MenuStrip menuBar;
        Panel iFrame;
        SplitContainer layoutPanel;
        Panel verticalPanel;

        Button resetButton;
        Button runStopButton;

        TrackBar trbSpeedBar;
        TrackBar trbCurrentBar;

        public CheckBox chkShowVolts;
        public CheckBox chkShowDots;
        public CheckBox chkShowValues;
        public CheckBox chkSmallGrid;
        public CheckBox chkUseAnsiSymbols;
        public CheckBox chkPrintable;
        public CheckBox chkCrossHair;

        ContextMenuStrip contextPanel = null;
        ScopePopupMenu scopePopupMenu;

        ToolStripMenuItem[] elmMenuBar;
        ToolStripMenuItem elmScopeMenuItem;
        ToolStripMenuItem elmFloatScopeMenuItem;
        ToolStripMenuItem elmEditMenuItem;
        ToolStripMenuItem elmFlipMenuItem;
        ToolStripMenuItem elmSplitMenuItem;
        ToolStripMenuItem elmSliderMenuItem;
        ToolStripMenuItem elmCutMenuItem;
        ToolStripMenuItem elmCopyMenuItem;
        ToolStripMenuItem elmDeleteMenuItem;

        PictureBox picCir;
        Bitmap cv = null;
        Graphics context;
        CustomGraphics backContext;

        MenuItem recoverItem;
        MenuItem undoItem;
        MenuItem redoItem;
        MenuItem pasteItem;
        #endregion

        #region static variable
        public static CirSim theSim = null;

        public static EditDialog editDialog;
        static EditDialog customLogicEditDialog;
        public static EditDialog diodeModelEditDialog;

        static ScrollValuePopup scrollValuePopup;
        public static SliderDialog sliderDialog;

        public static Form dialogShowing = null;
        public static Random random = new Random();
        #endregion

        #region dynamic variable
        public List<CircuitElm> elmList;
        public CircuitElm plotXElm;
        public CircuitElm plotYElm;
        public CircuitElm dragElm;
        public CircuitElm menuElm;

        public List<Adjustable> adjustables = new List<Adjustable>();

        Circuit mCir;
        SwitchElm heldSwitchElm;

        List<string> undoStack = new List<string>();
        List<string> redoStack = new List<string>();

        string startCircuit = null;
        string startLabel = null;
        string startCircuitText = null;
        string startCircuitLink = null;

        bool unsavedChanges;
        bool didSwitch;

        string recovery;
        string clipboard = "";

        public int scopeSelected { get; private set; } = -1;
        double scopeHeightFraction = 0.2;
        int scopeCount;
        Scope[] scopes;
        int[] scopeColCount;


        public MOUSE_MODE mouseMode = MOUSE_MODE.SELECT;
        public MOUSE_MODE tempMouseMode = MOUSE_MODE.SELECT;
        MENU_ITEM mouseModeStr = MENU_ITEM.SELECT;

        public int mouseCursorX { get; private set; } = -1;
        public int mouseCursorY { get; private set; } = -1;
        MouseButtons mouseButton = MouseButtons.None;
        bool mouseDragging = false;
        Cursor lastCursorStyle;
        DateTime mLastMouseMove = DateTime.Now;

        CircuitElm mouseElm = null;
        bool mouseWasOverSplitter = false;
        int mousePost = -1;

        bool dragging;
        int draggingPost;
        int dragGridX;
        int dragGridY;
        int dragScreenX;
        int dragScreenY;
        int initDragGridX;
        int initDragGridY;

        bool isPressShift;
        bool isPressCtrl;
        bool isPressAlt;

        int menuClientX;
        int menuClientY;
        int menuX;
        int menuY;
        int menuScope = -1;
        int menuPlot = -1;

        long mouseDownTime;
        long zoomTime;

        float[] transform;
        Rectangle selectedArea;
        Rectangle circuitArea;

        public bool analyzeFlag;
        bool dumpMatrix;

        public double t { get; private set; }
        public double timeStep;
        int frames = 0;
        int steps = 0;
        int framerate = 0;
        int steprate = 0;
        long lastTime = 0;
        long lastFrameTime;
        long lastIterTime;
        long secTime = 0;

        long myruntime = 0;
        long mydrawtime = 0;
        long myframes = 1;
        long mytime = 0;

        public bool dcAnalysisFlag;
        bool simRunning;
        bool needsRepaint;

        public int gridSize { get; private set; }
        int gridMask;
        int gridRound;
        #endregion
    }
}
