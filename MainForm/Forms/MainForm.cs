using Circuit;
using Circuit.Elements.Active;
using Circuit.Forms;
using Circuit.Symbol.Passive;
using Circuit.Symbol.Measure;

using System.Text;
using Circuit.Symbol;
using MainForm.Forms;
using Circuit.Elements;

namespace MainForm {
	public partial class MainForm : Form {
		#region CONST
		public static readonly Font FONT_TEXT = new("Meiryo UI", 9.0f);
		public static readonly Brush BRUSH_TEXT = Brushes.Red;

		public const int POSTGRABSQ = 25;
		public const int MINPOSTGRABSIZE = 256;

		public const int RC_RETAIN = 1;
		public const int RC_NO_CENTER = 2;
		public const int RC_SUBCIRCUITS = 4;
		#endregion

		#region Property
		public static ElementInfoDialog? EditDialog { get; set; } = null;
		public static SliderDialog? SliderDialog { get; set; } = null;
		public static BaseSymbol? PlotXElm { get; private set; } = null;
		public static BaseSymbol? PlotYElm { get; private set; } = null;
		public static List<BaseSymbol> SymbolList { get; private set; } = [];
		public static int SymbolCount { get { return SymbolList.Count; } }
		#endregion

		#region Variable
		public static MainForm? Instance = null;
		public static bool IsRunning;
		public static bool NeedAnalyze;

		static ScopeForm mScopeForm = new();

		static string mFileName = "";
		static bool mNeedsRepaint;
		static ScrollValuePopup? mScrollValuePopup = null;

		System.Windows.Forms.Timer mTimer;

		MenuStrip mMenuBar;
		MenuItems mMenuItems;
		SplitContainer mSplitContainer;

		ToolStripMenuItem mUndoItem = new();
		ToolStripMenuItem mRedoItem = new();
		ToolStripMenuItem mPasteItem = new();

		static ContextMenuStrip? mContextMenu = null;

		static PictureBox? mPixCir = null;
		static Bitmap? mBmp = null;
		static Graphics? mContext = null;

		static DUMP_ID mAddElm = DUMP_ID.INVALID;

		static BaseSymbol? mMenuElm = null;
		static Switch? mHeldSwitchElm = null;
		Point mMenuClient;
		Point mMenuPos;

		static List<string> mUndoStack = new();
		static List<string> mRedoStack = new();

		string mRecovery;
		string mClipboard = "";
		static Rectangle mCircuitArea;

		bool mIsPressShift;
		bool mIsPressCtrl;
		bool mIsPressAlt;

		static long mLastTime = 0;
		#endregion

		public MainForm() {
			InitializeComponent();
			Instance = this;

			mMenuItems = new MenuItems(this);
			ControlPanel.Init(
				(s, e) => { ResetButton_onClick(); },
				(s, e) => { Reload(); }
			);
			CustomGraphics.SetColor(ControlPanel.ChkPrintable.Checked);

			KeyPreview = true;
			KeyDown += OnKeyDown;
			KeyUp += OnKeyUp;

			SetTimer();

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
				mPixCir.MouseWheel += new MouseEventHandler((s, e) => { ShowScrollValues(); });
				mPixCir.MouseClick += new MouseEventHandler((s, e) => { OnClick(e); });
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
						SetCanvasSize();
					}
				});
				Controls.Add(mSplitContainer);
			}

			ReadCircuit("");
			ReadRecovery();

			EnableUndoRedo();
			EnablePaste();

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
			mContextMenu?.Close();
			MouseInfo.Mode = MouseInfo.MODE.ADD_ELM;
			Cursor = Cursors.Arrow;
			mAddElm = item;
			Repaint();
		}

		public void Performed(MenuItems.ID item) {
			switch (item) {
			case MenuItems.ID.OPEN_NEW:
				mFileName = "";
				Text = mFileName;
				ReadCircuit("");
				WriteRecoveryToStorage();
				ReadRecovery();
				break;
			case MenuItems.ID.OPEN_FILE:
				DoOpenFile();
				WriteRecoveryToStorage();
				ReadRecovery();
				break;
			case MenuItems.ID.OVERWRITE:
				DoSaveFile(true);
				break;
			case MenuItems.ID.SAVE_FILE:
				DoSaveFile(false);
				break;
			case MenuItems.ID.PDF:
				CustomGraphics.Instance.DrawPDF = true;
				break;
			}

			if (MouseInfo.GrippedElm != null) {
				mMenuElm = MouseInfo.GrippedElm;
				return;
			}

			switch (item) {
			case MenuItems.ID.UNDO:
				DoUndo();
				break;
			case MenuItems.ID.REDO:
				DoRedo();
				break;
			case MenuItems.ID.CUT:
				mMenuElm = null;
				DoCut();
				break;
			case MenuItems.ID.COPY:
				mMenuElm = null;
				DoCopy();
				break;
			case MenuItems.ID.PASTE:
				DoPaste(null);
				break;
			case MenuItems.ID.DELETE:
				mMenuElm = null;
				PushUndo();
				DoDelete(true);
				break;
			case MenuItems.ID.SELECT_ALL:
				DoSelectAll();
				MouseInfo.Mode = MouseInfo.MODE.DRAG_ITEM;
				break;
			case MenuItems.ID.CENTER_CIRCUIT:
				PushUndo();
				MouseInfo.Centering(mCircuitArea.Width, mCircuitArea.Height, GetCircuitBounds());
				break;
			}

			Repaint();
		}

		public void Reload() {
			PushUndo();
			ReadCircuit(mRecovery);
		}

		static BaseSymbol? GetSymbol(int n) {
			if (n >= SymbolCount) {
				return null;
			}
			return SymbolList[n];
		}

		public static void SetSimRunning(bool s) {
			IsRunning = s;
			if (s) {
				CircuitState.Stopped = false;
				ControlPanel.BtnRunStop.Text = "停止";
			} else {
				NeedAnalyze = false;
				ControlPanel.BtnRunStop.Text = "実行";
			}
		}

		public static bool DialogIsShowing() {
			if (EditDialog != null && EditDialog.Visible) {
				return true;
			}
			if (SliderDialog != null && SliderDialog.Visible) {
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

		public static void Repaint() {
			if (!mNeedsRepaint) {
				mNeedsRepaint = true;
				UpdateDisplay();
				mNeedsRepaint = false;
			}
		}

		public static void ResetButton_onClick() {
			for (int i = 0; i != SymbolCount; i++) {
				SymbolList[i].Reset();
			}
			NeedAnalyze = true;
			CircuitState.Time = 0;
			ScopeForm.ResetGraph();
		}

		public void PushUndo() {
			mRedoStack.Clear();
			string s = DumpCircuit();
			if (mUndoStack.Count > 0 && s == mUndoStack[mUndoStack.Count - 1]) {
				return;
			}
			mUndoStack.Add(s);
			EnableUndoRedo();
		}
		#endregion

		#region Key event method
		void OnKeyDown(object sender, KeyEventArgs e) {
			mIsPressShift = e.Shift;
			mIsPressCtrl = e.Control;
			mIsPressAlt = e.Alt;
		}

		void OnKeyUp(object sender, KeyEventArgs e) {
			mIsPressShift = false;
			mIsPressCtrl = false;
			mIsPressAlt = false;
			Cursor = Cursors.Arrow;
			KeyUpPerformed(e);
		}

		void KeyUpPerformed(KeyEventArgs e) {
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
				DoDelete(true);
			}

			if (code == Keys.Escape || e.KeyValue == 32) {
				mMenuItems.AllUnchecked();
				MouseInfo.Mode = MouseInfo.MODE.NONE;
				mAddElm = DUMP_ID.INVALID;
			}
		}
		#endregion

		#region Mouse event method
		void OnClick(MouseEventArgs e) {
			if (e.Button == MouseButtons.Right) {
				OnContextMenu(e);
			}
		}

		void onDoubleClick(EventArgs e) {
			if (MouseInfo.GrippedElm == null) {
				return;
			}
			DoEdit(MouseInfo.GrippedElm, new Point(
				Location.X + mMenuClient.X,
				Location.Y + mMenuClient.Y));
		}

		void onMouseDown(MouseEventArgs e) {
			mMenuPos = mMenuClient = e.Location;
			MouseInfo.SetCursor(e.Location);
			MouseInfo.Button = e.Button;

			/* maybe someone did copy in another window?  should really do this when */
			/* window receives focus */
			EnablePaste();

			if (MouseInfo.Button != MouseButtons.Left && MouseInfo.Button != MouseButtons.Middle) {
				return;
			}

			// set mouseElm in case we are on mobile
			MouseSelect();

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
			if (DoSwitch(gpos)) {
				/* do this BEFORE we change the mouse mode to MODE_DRAG_POST!  Or else logic inputs */
				/* will add dots to the whole circuit when we click on them! */
				return;
			}

			if (MouseInfo.Mode != MouseInfo.MODE.NONE && MouseInfo.Mode != MouseInfo.MODE.DRAG_ITEM) {
				ClearSelection();
			}

			PushUndo();
			MouseInfo.DragBegin = gpos;
			if (MouseInfo.Mode != MouseInfo.MODE.ADD_ELM) {
				return;
			}
			/* */
			gpos = BaseSymbol.SnapGrid(gpos);
			if (!mCircuitArea.Contains(MouseInfo.Cursor)) {
				return;
			}
			BaseSymbol.ConstructItem = SymbolMenu.Construct(mAddElm, gpos);
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
					ClearSelection();
				}
				if (HasSelection()) {
					MouseInfo.Mode = MouseInfo.MODE.DRAG_ITEM;
				} else {
					MouseInfo.Mode = MouseInfo.MODE.NONE;
				}
				break;
			case MouseInfo.MODE.DRAG_ITEM:
				ClearSelection();
				MouseInfo.Mode = MouseInfo.MODE.NONE;
				break;
			case MouseInfo.MODE.SPLIT:
				DoSplit(MouseInfo.GrippedElm);
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
			if (BaseSymbol.ConstructItem != null) {
				/* if the element is zero size then don't create it */
				if (BaseSymbol.ConstructItem.IsCreationFailed) {
					BaseSymbol.ConstructItem.Delete();
					if (MouseInfo.Mode == MouseInfo.MODE.SELECT || MouseInfo.Mode == MouseInfo.MODE.DRAG_ITEM) {
						ClearSelection();
					}
				} else {
					SymbolList.Add(BaseSymbol.ConstructItem);
					circuitChanged = true;
					WriteRecoveryToStorage();
				}
				BaseSymbol.ConstructItem = null;
			}
			if (circuitChanged) {
				NeedAnalyze = true;
			}
			BaseSymbol.ConstructItem?.Delete();
			BaseSymbol.ConstructItem = null;
			Cursor = Cursors.Default;
			Repaint();
		}

		void onMouseMove(MouseEventArgs e) {
			if (MouseInfo.Delay()) {
				return;
			}
			MouseInfo.SetCursor(e.Location);
			if (MouseInfo.IsDragging) {
				MouseDrag();
			} else {
				MouseSelect();
			}
		}

		void OnContextMenu(MouseEventArgs e) {
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
					mContextMenu?.Close();
					switch (item) {
					case ElementPopupMenu.Item.EDIT:
						DoEdit(mMenuElm, mMenuClient);
						break;
					case ElementPopupMenu.Item.SPLIT_WIRE:
						DoSplit(mMenuElm);
						break;
					case ElementPopupMenu.Item.FLIP_POST:
						DoFlip();
						break;
					case ElementPopupMenu.Item.SLIDERS:
						DoSliders(mMenuElm, mMenuClient);
						break;
					case ElementPopupMenu.Item.SCOPE_WINDOW:
						ScopeForm.AddPlot(mMenuElm);
						break;
					case ElementPopupMenu.Item.SCOPE_FLOAT:
						if (mMenuElm != null) {
							var newScope = new Scope(BaseSymbol.SnapGrid(mMenuElm.Post.A.X + 50, mMenuElm.Post.A.Y + 50));
							SymbolList.Add(newScope);
							newScope.Plot.Setup(mMenuElm);
						}
						break;
					}
					Repaint();
				}));
				mContextMenu = menu.Show(mMenuClient, MouseInfo.GrippedElm);
			}
		}

		void MouseDrag() {
			/* ignore right mouse button with no modifiers (needed on PC) */
			if (MouseInfo.Button == MouseButtons.Right) {
				return;
			}
			var gpos = BaseSymbol.SnapGrid(MouseInfo.GetAbsPos());
			if (!mCircuitArea.Contains(MouseInfo.Cursor)) {
				return;
			}
			bool changed = false;
			BaseSymbol.ConstructItem?.Drag(gpos);
			bool success = true;
			switch (MouseInfo.Mode) {
			case MouseInfo.MODE.SCROLL: {
				MouseInfo.Scroll();
				break;
			}
			case MouseInfo.MODE.DRAG_ROW:
				DragRow(gpos);
				changed = true;
				break;
			case MouseInfo.MODE.DRAG_COLUMN:
				DragColumn(gpos);
				changed = true;
				break;
			case MouseInfo.MODE.SELECT:
				if (MouseInfo.GrippedElm == null) {
					MouseInfo.Mode = MouseInfo.MODE.SELECT_AREA;
				} else {
					MouseInfo.DraggingPost = MouseInfo.HoveringPost;
					MouseInfo.HoveringPost = Post.Selection.NONE;
					if (MouseInfo.DraggingPost == Post.Selection.BOTH) {
						MouseInfo.Mode = MouseInfo.MODE.DRAG_ITEM;
					} else {
						MouseInfo.Mode = MouseInfo.MODE.DRAG_POST;
					}
				}
				break;
			case MouseInfo.MODE.SELECT_AREA:
				SelectArea(gpos);
				break;
			case MouseInfo.MODE.DRAG_ITEM:
				changed = success = DragSelected(gpos);
				break;
			case MouseInfo.MODE.DRAG_POST:
				MouseInfo.MoveGrippedElm(gpos);
				NeedAnalyze = true;
				changed = true;
				break;
			}
			if (success) {
				/* Console.WriteLine("setting dragGridx in mousedragged");*/
				MouseInfo.DragEnd = MouseInfo.ToAbsPos(MouseInfo.CommitCursor());
				if (!(MouseInfo.Mode == MouseInfo.MODE.DRAG_ITEM && OnlyGraphicsElmsSelected())) {
					MouseInfo.DragEnd = BaseSymbol.SnapGrid(MouseInfo.DragEnd);
				}
			}
			if (changed) {
				WriteRecoveryToStorage();
			}
			Repaint();
		}

		void MouseSelect() {
			MouseInfo.CommitCursor();
			var gpos = MouseInfo.GetAbsPos();
			MouseInfo.DragEnd = BaseSymbol.SnapGrid(gpos);
			MouseInfo.DraggingPost = Post.Selection.NONE;
			MouseInfo.HoveringPost = Post.Selection.NONE;

			PlotXElm = PlotYElm = null;

			BaseSymbol mostNearUI = null;
			var mostNear = double.MaxValue;
			for (int i = 0; i != SymbolCount; i++) {
				var ce = GetSymbol(i);
				var lineD = ce.Distance(gpos);
				if (lineD <= CustomGraphics.HANDLE_RADIUS && lineD < mostNear) {
					MouseInfo.HoveringPost = Post.Selection.BOTH;
					mostNearUI = ce;
					mostNear = lineD;
				}
			}
			if (mostNearUI == null) {
				for (int i = 0; i != SymbolCount; i++) {
					var ce = GetSymbol(i);
					var postDa = ce.DistancePostA(gpos);
					var postDb = ce.DistancePostB(gpos);
					if (postDa <= CustomGraphics.HANDLE_RADIUS && postDa < mostNear) {
						MouseInfo.HoveringPost = Post.Selection.A;
						mostNearUI = ce;
						mostNear = postDa;
					}
					if (postDb <= CustomGraphics.HANDLE_RADIUS && postDb < mostNear) {
						MouseInfo.HoveringPost = Post.Selection.B;
						mostNearUI = ce;
						mostNear = postDb;
					}
				}
			}
			if (mostNearUI == null) {
				ClearMouseElm();
			} else {
				var postDa = mostNearUI.DistancePostA(gpos);
				var postDb = mostNearUI.DistancePostB(gpos);
				if (postDa <= CustomGraphics.HANDLE_RADIUS) {
					MouseInfo.HoveringPost = Post.Selection.A;
				}
				if (postDb <= CustomGraphics.HANDLE_RADIUS) {
					MouseInfo.HoveringPost = Post.Selection.B;
				}
				MouseInfo.GripElm(mostNearUI);
			}
			Repaint();
		}

		void SelectArea(Point pos) {
			MouseInfo.SelectArea(pos);
			for (int i = 0; i != SymbolCount; i++) {
				var ce = GetSymbol(i);
				ce.SelectRect(MouseInfo.SelectedArea);
			}
		}

		void DragRow(Point pos) {
			int dy = (pos.Y - MouseInfo.DragEnd.Y) / BaseSymbol.GRID_SIZE;
			dy *= BaseSymbol.GRID_SIZE;
			if (0 == dy) {
				return;
			}
			for (int i = 0; i != SymbolCount; i++) {
				var ce = GetSymbol(i);
				var p = Post.Selection.NONE;
				if (pos.Y <= ce.Post.A.Y) {
					p = Post.Selection.A;
				}
				if (pos.Y <= ce.Post.B.Y) {
					p = Post.Selection.B;
				}
				if (pos.Y <= ce.Post.A.Y && pos.Y <= ce.Post.B.Y) {
					p = Post.Selection.BOTH;
				}
				ce.Move(0, dy, p);
			}
			RemoveZeroLengthElements();
		}

		void DragColumn(Point pos) {
			int dx = (pos.X - MouseInfo.DragEnd.X) / BaseSymbol.GRID_SIZE;
			dx *= BaseSymbol.GRID_SIZE;
			if (0 == dx) {
				return;
			}
			for (int i = 0; i != SymbolCount; i++) {
				var ce = GetSymbol(i);
				var p = Post.Selection.NONE;
				if (pos.X <= ce.Post.A.X) {
					p = Post.Selection.A;
				}
				if (pos.X <= ce.Post.B.X) {
					p = Post.Selection.B;
				}
				if (pos.X <= ce.Post.A.X && pos.X <= ce.Post.B.X) {
					p = Post.Selection.BOTH;
				}
				ce.Move(dx, 0, p);
			}
			RemoveZeroLengthElements();
		}

		bool DragSelected(Point pos) {
			bool me = false;
			int i;
			if (MouseInfo.GrippedElm != null && !MouseInfo.GrippedElm.IsSelected) {
				MouseInfo.GrippedElm.IsSelected = me = true;
			}
			if (!OnlyGraphicsElmsSelected()) {
				pos = BaseSymbol.SnapGrid(pos);
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
			for (i = 0; allowed && i != SymbolCount; i++) {
				var ce = GetSymbol(i);
				if (ce.IsSelected && !ce.AllowMove(dx, dy)) {
					allowed = false;
				}
			}
			if (allowed) {
				for (i = 0; i != SymbolCount; i++) {
					var ce = GetSymbol(i);
					if (ce.IsSelected) {
						ce.Move(dx, dy);
					}
				}
				NeedAnalyze = true;
			}
			/* don't leave mouseElm selected if we selected it above */
			if (me) {
				MouseInfo.GrippedElm.IsSelected = false;
			}

			return allowed;
		}

		bool HasSelection() {
			for (int i = 0; i != SymbolCount; i++) {
				if (GetSymbol(i).IsSelected) {
					return true;
				}
			}
			return false;
		}

		bool DoSwitch(Point pos) {
			if (MouseInfo.GrippedElm == null || !(MouseInfo.GrippedElm is Switch)) {
				return false;
			}
			var se = (Switch)MouseInfo.GrippedElm;
			if (!se.GetSwitchRect().Contains(pos)) {
				return false;
			}
			se.Toggle();
			if (se.Momentary) {
				mHeldSwitchElm = se;
			}
			NeedAnalyze = true;
			return true;
		}
		#endregion

		#region Private methond
		void SetTimer() {
			mTimer = new System.Windows.Forms.Timer();
			mTimer.Tick += new EventHandler((s, e) => {
				if (IsRunning) {
					UpdateDisplay();
					mNeedsRepaint = false;
				}
			});
			mTimer.Interval = 1;
			mTimer.Enabled = true;
			mTimer.Start();
		}

		void SetCanvasSize() {
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
			CustomGraphics.Instance?.Dispose();
			CustomGraphics.Instance = CustomGraphics.FromImage(width, height);
			mCircuitArea = new Rectangle(0, 0, width, height);
			SetSimRunning(isRunning);
		}

		Rectangle GetCircuitBounds() {
			if (0 == SymbolCount) {
				return new Rectangle();
			}
			int minx = int.MaxValue, miny = int.MaxValue;
			int maxx = 0, maxy = 0;
			for (int i = 0; i < SymbolCount; i++) {
				var ce = GetSymbol(i);
				minx = Math.Min(ce.Post.A.X, Math.Min(ce.Post.B.X, minx));
				miny = Math.Min(ce.Post.A.Y, Math.Min(ce.Post.B.Y, miny));
				maxx = Math.Max(ce.Post.A.X, Math.Max(ce.Post.B.X, maxx));
				maxy = Math.Max(ce.Post.A.Y, Math.Max(ce.Post.B.Y, maxy));
			}
			return new Rectangle(minx, miny, maxx - minx, maxy - miny);
		}

		void DoEdit(BaseSymbol eable, Point location) {
			ClearSelection();
			PushUndo();
			if (EditDialog != null) {
				EditDialog.Close();
				EditDialog = null;
			}
			EditDialog = new ElementInfoDialog(eable);
			EditDialog.Show(location.X, location.Y);
		}

		void DoSliders(BaseSymbol ce, Point location) {
			ClearSelection();
			PushUndo();
			if (SliderDialog != null) {
				SliderDialog.Close();
				SliderDialog = null;
			}
			SliderDialog = new SliderDialog(ce);
			SliderDialog.Show(location.X, location.Y);
		}

		void DoOpenFile() {
			var open = new OpenFileDialog {
				Filter = "回路データ(*.txt)|*.txt"
			};
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
			ReadCircuit(data);
		}

		void DoSaveFile(bool overWrite) {
			var filePath = "";
			if (overWrite) {
				filePath = mFileName;
			}

			if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) {
				var save = new SaveFileDialog {
					Filter = "回路データ(*.txt)|*.txt"
				};
				save.ShowDialog();
				if (string.IsNullOrEmpty(save.FileName) || !Directory.Exists(Path.GetDirectoryName(save.FileName))) {
					return;
				}
				filePath = save.FileName;
				mFileName = filePath;
				Text = mFileName;
			}

			string dump = DumpCircuit();
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

		static string DumpCircuit() {
			// Todo: CustomLogicModel
			//CustomLogicModel.clearDumpedFlags();

			int f = ControlPanel.ChkShowCurrent.Checked ? 1 : 0;
			f |= ControlPanel.ChkShowValues.Checked ? 0 : 16;

			string dump = "$ " + f
				+ " " + ControlPanel.TimeStep
				+ " " + ControlPanel.StepRate
				+ " " + ControlPanel.TrbCurrent.Value + "\n";

			int i;
			for (i = 0; i != SymbolCount; i++) {
				var ce = GetSymbol(i);
				dump += ce.Dump() + "\n";
			}
			dump += ScopeForm.Dump();
			for (i = 0; i != BaseSymbol.Adjustables.Count; i++) {
				var adj = BaseSymbol.Adjustables[i];
				dump += adj.Dump() + "\n";
			}

			return dump;
		}

		void ReadCircuit(string text, int flags) {
			ReadCircuit(Encoding.UTF8.GetBytes(text), flags);
		}

		void ReadCircuit(string text) {
			ReadCircuit(Encoding.UTF8.GetBytes(text), 0);
		}

		void ReadCircuit(byte[] b, int flags) {
			int i;
			int len = b.Length;
			if ((flags & RC_RETAIN) == 0) {
				ClearMouseElm();
				for (i = 0; i != SymbolCount; i++) {
					var ce = GetSymbol(i);
					ce.Delete();
				}
				SymbolList.Clear();
				ControlPanel.Reset();
				ScopeForm.PlotCount = 0;
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
							ReadOptions(st);
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
						if (tint == '&') {
							var adj = new Slider(st);
							BaseSymbol.Adjustables.Add(adj);
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
						var newce = SymbolMenu.Construct(dumpId, p1, p2, f, st);
						if (st.HasMoreTokens) {
							string v;
							st.nextToken(out v);
							newce.ReferenceName = TextUtils.UnEscape(v);
						} else {
							newce.ReferenceName = "";
						}
						if (newce == null) {
							Console.WriteLine("unrecognized dump type: " + type);
							break;
						}
						newce.SetPoints();
						SymbolList.Add(newce);
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
				for (i = 0; i != BaseSymbol.Adjustables.Count; i++) {
					BaseSymbol.Adjustables[i].CreateSlider();
				}
			}
			NeedAnalyze = true;
			if ((flags & RC_NO_CENTER) == 0) {
				MouseInfo.Centering(mCircuitArea.Width, mCircuitArea.Height, GetCircuitBounds());
			}
		}

		void ReadOptions(StringTokenizer st) {
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

		bool OnlyGraphicsElmsSelected() {
			if (MouseInfo.GrippedElm != null) {
				return false;
			}
			for (int i = 0; i != SymbolCount; i++) {
				var ce = GetSymbol(i);
				if (ce.IsSelected) {
					return false;
				}
			}
			return true;
		}

		void DoFlip() {
			mMenuElm.FlipPosts();
			NeedAnalyze = true;
		}

		void DoSplit(BaseSymbol ce) {
			var pos = BaseSymbol.SnapGrid(MouseInfo.ToAbsPos(mMenuPos));
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
			SymbolList.Add(newWire);
			NeedAnalyze = true;
		}

		void RemoveZeroLengthElements() {
			for (int i = SymbolCount - 1; i >= 0; i--) {
				var ce = GetSymbol(i);
				if (ce.Post.A.X == ce.Post.B.X && ce.Post.A.Y == ce.Post.B.Y) {
					SymbolList.RemoveAt(i);
					/*Console.WriteLine("delete element: {0} {1}\t{2} {3}\t{4}", ce.GetType(), ce.x1, ce.y1, ce.x2, ce.y2); */
					ce.Delete();
				}
			}
			NeedAnalyze = true;
		}

		void ClearMouseElm() {
			MouseInfo.GripElm(null);
			PlotXElm = PlotYElm = null;
		}

		void ShowScrollValues() {
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

		void DoUndo() {
			if (mUndoStack.Count == 0) {
				return;
			}
			mRedoStack.Add(DumpCircuit());
			string tmp = mUndoStack[mUndoStack.Count - 1];
			mUndoStack.RemoveAt(mUndoStack.Count - 1);
			ReadCircuit(tmp, RC_NO_CENTER);
			EnableUndoRedo();
		}

		void DoRedo() {
			if (mRedoStack.Count == 0) {
				return;
			}
			mUndoStack.Add(DumpCircuit());
			string tmp = mRedoStack[mRedoStack.Count - 1];
			mRedoStack.RemoveAt(mRedoStack.Count - 1);
			ReadCircuit(tmp, RC_NO_CENTER);
			EnableUndoRedo();
		}

		void EnableUndoRedo() {
			mRedoItem.Enabled = mRedoStack.Count > 0;
			mUndoItem.Enabled = mUndoStack.Count > 0;
		}

		void SetMenuSelection() {
			if (mMenuElm != null) {
				if (mMenuElm.IsSelected) {
					return;
				}
				ClearSelection();
				mMenuElm.IsSelected = true;
			}
		}

		void DoCut() {
			int i;
			PushUndo();
			SetMenuSelection();
			mClipboard = "";
			for (i = SymbolCount - 1; i >= 0; i--) {
				var ce = GetSymbol(i);
				/* ScopeElms don't cut-paste well because their reference to a parent
                /* elm by number get's messed up in the dump. For now we will just ignore them
                /* until I can be bothered to come up with something better */
				if (WillDelete(ce) && !(ce is Scope)) {
					mClipboard += ce.Dump() + "\n";
				}
			}
			WriteClipboardToStorage();
			DoDelete(true);
			EnablePaste();
		}

		void WriteClipboardToStorage() {
			Storage.GetInstance().SetItem("circuitClipboard", mClipboard);
		}

		void ReadClipboardFromStorage() {
			mClipboard = Storage.GetInstance().GetItem("circuitClipboard");
		}

		static void WriteRecoveryToStorage() {
			var s = DumpCircuit();
			Storage.GetInstance().SetItem("circuitRecovery", s);
		}

		void ReadRecovery() {
			mRecovery = Storage.GetInstance().GetItem("circuitRecovery");
		}

		public static void DeleteUnusedScopeElms() {
			/* Remove any scopeElms for elements that no longer exist */
			for (int i = SymbolCount - 1; 0 <= i; i--) {
				var ce = GetSymbol(i);
				if ((ce is Scope) && ((Scope)ce).Plot.NeedToRemove) {
					ce.Delete();
					SymbolList.RemoveAt(i);
				}
			}
		}

		void DoDelete(bool pushUndoFlag) {
			int i;
			if (pushUndoFlag) {
				PushUndo();
			}
			bool hasDeleted = false;

			for (i = SymbolCount - 1; i >= 0; i--) {
				var ce = GetSymbol(i);
				if (WillDelete(ce)) {
					if (ce.IsMouseElm) {
						MouseInfo.GripElm(null);
					}
					ce.Delete();
					SymbolList.RemoveAt(i);
					hasDeleted = true;
				}
			}
			if (hasDeleted) {
				DeleteUnusedScopeElms();
				NeedAnalyze = true;
				WriteRecoveryToStorage();
			}
		}

		static bool WillDelete(BaseSymbol ce) {
			/* Is this element in the list to be deleted.
            /* This changes the logic from the previous version which would initially only
            /* delete selected elements (which could include the mouseElm) and then delete the
            /* mouseElm if there were no selected elements. Not really sure this added anything useful
            /* to the user experience.
            /*
            /* BTW, the old logic could also leave mouseElm pointing to a deleted element. */
			return ce.IsSelected || ce.IsMouseElm;
		}

		string CopyOfSelectedElms() {
			string r = "";
			// Todo: CustomLogicModel
			//CustomLogicModel.clearDumpedFlags();
			for (int i = SymbolCount - 1; i >= 0; i--) {
				var ce = GetSymbol(i);
				/* See notes on do cut why we don't copy ScopeElms. */
				if (ce.IsSelected && !(ce is Scope)) {
					r += ce.Dump() + "\n";
				}
			}
			return r;
		}

		void DoCopy() {
			/* clear selection when we're done if we're copying a single element using the context menu */
			bool clearSel = (mMenuElm != null && !mMenuElm.IsSelected);

			SetMenuSelection();
			mClipboard = CopyOfSelectedElms();

			if (clearSel) {
				ClearSelection();
			}
			WriteClipboardToStorage();
			EnablePaste();
		}

		void EnablePaste() {
			if (string.IsNullOrEmpty(mClipboard)) {
				ReadClipboardFromStorage();
			}
			mPasteItem.Enabled = !string.IsNullOrEmpty(mClipboard);
		}

		void DoPaste(string? dump) {
			PushUndo();
			ClearSelection();
			int i;

			/* get old bounding box */
			var oldbb = new RectangleF();
			for (i = 0; i != SymbolCount; i++) {
				var ce = GetSymbol(i);
				var bb = ce.Post.GetRect();
				if (0 == i) {
					oldbb = bb;
				} else {
					oldbb = RectangleF.Union(oldbb, bb);
				}
			}

			/* add new items */
			int oldsz = SymbolCount;
			if (dump != null) {
				ReadCircuit(dump, RC_RETAIN);
			} else {
				ReadClipboardFromStorage();
				ReadCircuit(mClipboard, RC_RETAIN);
			}

			/* select new items and get their bounding box */
			var newbb = new RectangleF();
			for (i = oldsz; i != SymbolCount; i++) {
				var ce = GetSymbol(i);
				ce.IsSelected = true;
				var bb = ce.Post.GetRect();
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
					dx = BaseSymbol.SnapGrid((int)(oldbb.X + oldbb.Width - newbb.X + BaseSymbol.GRID_SIZE));
				} else {
					dy = BaseSymbol.SnapGrid((int)(oldbb.Y + oldbb.Height - newbb.Y + BaseSymbol.GRID_SIZE));
				}

				/* move new items near the mouse if possible */
				if (MouseInfo.Cursor.X > 0 && mCircuitArea.Contains(MouseInfo.Cursor)) {
					var g = MouseInfo.GetAbsPos();
					int mdx = BaseSymbol.SnapGrid((int)(g.X - (newbb.X + newbb.Width / 2)));
					int mdy = BaseSymbol.SnapGrid((int)(g.Y - (newbb.Y + newbb.Height / 2)));
					for (i = oldsz; i != SymbolCount; i++) {
						if (!GetSymbol(i).AllowMove(mdx, mdy)) {
							break;
						}
					}
					if (i == SymbolCount) {
						dx = mdx;
						dy = mdy;
					}
				}

				/* move the new items */
				for (i = oldsz; i != SymbolCount; i++) {
					var ce = GetSymbol(i);
					ce.Move(dx, dy);
				}
			}
			NeedAnalyze = true;
			WriteRecoveryToStorage();
		}

		void ClearSelection() {
			for (int i = 0; i != SymbolCount; i++) {
				var ce = GetSymbol(i);
				ce.IsSelected = false;
			}
		}

		void DoSelectAll() {
			for (int i = 0; i != SymbolCount; i++) {
				var ce = GetSymbol(i);
				ce.IsSelected = true;
			}
		}

		bool anySelectedButMouse() {
			for (int i = 0; i != SymbolCount; i++) {
				var ce = GetSymbol(i);
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
			for (int i = 0; i < SymbolCount; i++) {
				var e = SymbolList[i].Element;
				cs = e.ToString();
				int p = cs.LastIndexOf('.');
				cs = cs.Substring(p + 1);
				if (cs == "WireElm") {
					continue;
				}
				if (cs == "TransistorElm") {
					if (e.Para[ElmBJT.NPN] < 0) {
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

		static void UpdateDisplay() {
			if (NeedAnalyze) {
				CircuitAnalizer.Analyze(SymbolList);
				Repaint();
				NeedAnalyze = false;
			}

			if (IsRunning) {
				BaseElement.DoFrame(ref IsRunning, ref NeedAnalyze, ControlPanel.StepRate);
			}

			long sysTime = DateTime.Now.ToFileTimeUtc();
			if (IsRunning) {
				if (mLastTime != 0) {
					int inc = (int)(sysTime - mLastTime);
					var c = ControlPanel.TrbCurrent.Value * 50.0 / ControlPanel.TrbCurrent.Maximum;
					c = Math.Exp(c / 3.5 - 14.2);
					BaseSymbol.CurrentMult = 1.7 * inc * c;
				}
				mLastTime = sysTime;
			} else {
				mLastTime = 0;
			}

			var g = CustomGraphics.Instance;
			PDF.Page pdfCircuit = null;
			PDF.Page pdfScope = null;
			var bkIsRun = IsRunning;
			var bkPrintable = ControlPanel.ChkPrintable.Checked;
			if (g.DrawPDF) {
				g.DrawPDF = false;
				if (bkIsRun) {
					IsRunning = false;
				}
				ControlPanel.ChkPrintable.Checked = true;
				pdfCircuit = new PDF.Page(g.Width, g.Height);
				pdfScope = new PDF.Page(mScopeForm.Width, mScopeForm.Height);
				g = pdfCircuit;
				CustomGraphics.Instance = pdfCircuit;
			}

			DrawCircuit(g);
			mScopeForm.Draw(pdfScope);

			var info = new string[10];
			if (MouseInfo.GrippedElm != null) {
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
				var saveFileDialog = new SaveFileDialog {
					Filter = "PDFファイル(*.pdf)|*.pdf",
					FileName = Path.GetFileNameWithoutExtension(mFileName)
				};
				saveFileDialog.ShowDialog();
				try {
					pdf.Save(saveFileDialog.FileName);
				} catch (Exception ex) {
					MessageBox.Show(ex.ToString());
				}
				IsRunning = bkIsRun;
				ControlPanel.ChkPrintable.Checked = bkPrintable;
				CustomGraphics.Instance = CustomGraphics.FromImage(g.Width, g.Height);
			} else {
				mBmp = new Bitmap(g.Width, g.Height);
				mContext = Graphics.FromImage(mBmp);
				CustomGraphics.Instance.CopyTo(mContext);
				mPixCir.Image = mBmp;
			}
		}

		static void DrawCircuit(CustomGraphics g) {
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
			foreach (var ui in SymbolList) {
				if (ui.NeedsHighlight) {
					g.DrawColor = CustomGraphics.SelectColor;
					g.FillColor = CustomGraphics.SelectColor;
				} else {
					g.DrawColor = CustomGraphics.LineColor;
					g.FillColor = CustomGraphics.LineColor;
				}
				ui.Draw(g);
				if (ui is Scope) {
					g.ScrollCircuit(MouseInfo.Offset);
				}
			}

			/* draw posts */
			foreach (var p in CircuitAnalizer.DrawPostList) {
				g.DrawPost(p);
			}
			if (BaseSymbol.ConstructItem != null && (
				BaseSymbol.ConstructItem.Post.A.X != BaseSymbol.ConstructItem.Post.B.X ||
				BaseSymbol.ConstructItem.Post.A.Y != BaseSymbol.ConstructItem.Post.B.Y
			)) {
				g.DrawColor = CustomGraphics.LineColor;
				g.FillColor = CustomGraphics.LineColor;
				BaseSymbol.ConstructItem.Draw(g);
				var ce = BaseSymbol.ConstructItem;
				for (int i = ce.Element.TermCount - 1; 0 <= i; i--) {
					var p = ce.NodePos[i];
					g.DrawPost(p);
				}
				g.DrawHandle(BaseSymbol.ConstructItem.Post.B);
			}
			if (MouseInfo.GrippedElm != null) {
				var ce = MouseInfo.GrippedElm;
				switch (MouseInfo.HoveringPost) {
				case Post.Selection.A:
					g.DrawHandle(ce.Post.A);
					g.DrawPost(ce.Post.B);
					break;
				case Post.Selection.B:
					g.DrawPost(ce.Post.A);
					g.DrawHandle(ce.Post.B);
					break;
				default:
					switch (MouseInfo.DraggingPost) {
					case Post.Selection.A:
						g.DrawHandle(ce.Post.A);
						g.DrawPost(ce.Post.B);
						break;
					case Post.Selection.B:
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
			foreach (var p in CircuitAnalizer.BadConnectionList) {
				g.DrawHandle(p);
			}

			if (0 < MouseInfo.SelectedArea.Width) {
				g.DrawColor = CustomGraphics.SelectColor;
				g.DrawRectangle(MouseInfo.SelectedArea);
			}

			/* draw cross hair */
			if (ControlPanel.ChkCrossHair.Checked && MouseInfo.Cursor.X >= 0
				&& MouseInfo.Cursor.X <= mCircuitArea.Width && MouseInfo.Cursor.Y <= mCircuitArea.Height) {
				var gr = BaseSymbol.SnapGrid(MouseInfo.GetAbsPos());
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
