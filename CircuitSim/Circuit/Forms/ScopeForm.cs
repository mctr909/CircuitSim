using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Forms {
	public partial class ScopeForm : Form {
		public static int PlotCount { get; set; } = 0;

		static ScopePlot[] mPlots = new ScopePlot[20];
		static int mSelectedPlot = -1;
		static int mSelectedWave = -1;

		ContextMenuStrip mScopePopupMenu = null;
		BaseSymbol mMouseElm = null;
		CustomGraphics mG;
		Bitmap mBmp;
		Graphics mContext;
		int mWidth;
		int mHeight;
		int mMouseCursorX = -1;
		int mMouseCursorY = -1;

		public ScopeForm() {
			InitializeComponent();
			mWidth = Width - 16;
			mHeight = Height - 39;
			SetGraphics();
		}

		private void ScopeForm_SizeChanged(object sender, EventArgs e) {
			mWidth = Width - 16;
			mHeight = Height - 39;
		}

		private void picScope_MouseMove(object sender, MouseEventArgs e) {
			mMouseCursorX = e.X;
			mMouseCursorY = e.Y;
		}

		private void picScope_MouseLeave(object sender, EventArgs e) {
			if (null == mScopePopupMenu || !mScopePopupMenu.Visible) {
				ClearUI();
			}
		}

		private void picScope_Click(object sender, EventArgs e) {
			var ev = (MouseEventArgs)e;
			switch (ev.Button) {
			case MouseButtons.Right:
				mSelectedWave = -1;
				if (0 <= mSelectedPlot) {
					if (mPlots[mSelectedPlot].CanMenu) {
						mSelectedWave = mPlots[mSelectedPlot].SelectedWave;
						var fm = new ScopePopupMenu();
						mScopePopupMenu = fm.Show(Left + mMouseCursorX, Top + mMouseCursorY, mPlots, mSelectedPlot, false);
					}
				}
				break;
			}
		}

		private void picScope_DoubleClick(object sender, EventArgs e) {
			mSelectedWave = -1;
			if (mSelectedPlot != -1) {
				if (mPlots[mSelectedPlot].CanMenu) {
					var ev = (MouseEventArgs)e;
					var plot = mPlots[mSelectedPlot];
					mSelectedWave = plot.SelectedWave;
					var fm = new ScopeProperties(plot);
					fm.Show(ev.X + Left, ev.Y + Top);
				}
			}
		}

		void SelectUI() {
			BaseSymbol selectElm = null;
			for (int i = 0; i != PlotCount; i++) {
				var plot = mPlots[i];
				if (plot.BoundingBox.Contains(mMouseCursorX, mMouseCursorY)) {
					selectElm = plot.GetUI();
					mSelectedPlot = i;
					break;
				}
			}
			if (null == selectElm) {
				if (null != mMouseElm) {
					mMouseElm.Select(false);
					mMouseElm = null;
				}
			} else {
				if (selectElm != MouseInfo.GrippedElm) {
					if (null != MouseInfo.GrippedElm) {
						MouseInfo.GrippedElm.Select(false);
					}
					if (null != mMouseElm) {
						mMouseElm.Select(false);
					}
					MouseInfo.GripElm(selectElm);
					mMouseElm = selectElm;
				}
			}
		}

		void ClearUI() {
			mSelectedPlot = -1;
			mSelectedWave = -1;
			mMouseCursorX = -1;
			mMouseCursorY = -1;
			if (null != mMouseElm) {
				mMouseElm.Select(false);
				mMouseElm = null;
			}
		}

		void SetGraphics() {
			if (picScope.Width != mWidth || picScope.Height != mHeight) {
				if (null != mG) {
					mG.Dispose();
					mG = null;
				}
				if (picScope.Image != null) {
					picScope.Image.Dispose();
					picScope.Image = null;
				}
				picScope.Width = mWidth;
				picScope.Height = mHeight;
				var bmp = new Bitmap(mWidth, mHeight);
				mG = new CustomGraphics(bmp);
			}
		}

		void Flush() {
			if (null != mBmp || null != mContext) {
				if (null == mContext) {
					mBmp.Dispose();
					mBmp = null;
				} else {
					mContext.Dispose();
					mContext = null;
				}
			}
			mBmp = new Bitmap(mG.Width, mG.Height);
			mContext = Graphics.FromImage(mBmp);
			mG.CopyTo(mContext);
			picScope.Image = mBmp;
		}

		public void Draw(CustomGraphics pdf) {
			SelectUI();
			SetGraphics();
			CustomGraphics g;
			if (null == pdf) {
				g = mG;
			} else {
				g = pdf;
			}

			g.Clear(ControlPanel.ChkPrintable.Checked ? Color.White : Color.Black);
			g.FontColor = CustomGraphics.TextColor;
			Setup(g.Width, g.Height);

			var ct = PlotCount;
			if (Circuit.StopMessage != null) {
				ct = 0;
			}
			for (int i = 0; i != ct; i++) {
				var plot = mPlots[i];
				plot.MouseCursorX = mMouseCursorX;
				plot.MouseCursorY = mMouseCursorY;
				plot.Draw(g);
			}

			if (Circuit.StopMessage != null) {
				g.DrawLeftText(Circuit.StopMessage, 10, -10);
			}
			Flush();
		}

		static void Setup(int width, int height) {
			/* check scopes to make sure the elements still exist, and remove
            /* unused scopes/columns */
			int index = -1;
			for (int i = 0; i < PlotCount; i++) {
				if (mPlots[i].NeedToRemove) {
					int j;
					for (j = i; j != PlotCount; j++) {
						mPlots[j] = mPlots[j + 1];
					}
					PlotCount--;
					i--;
					continue;
				}
				if (mPlots[i].Index > index + 1) {
					mPlots[i].Index = index + 1;
				}
				index = mPlots[i].Index;
			}

			while (PlotCount > 0 && mPlots[PlotCount - 1].GetUI() == null) {
				PlotCount--;
			}

			if (PlotCount <= 0) {
				return;
			}

			index = 0;
			var scopeColCount = new int[PlotCount];
			for (int i = 0; i != PlotCount; i++) {
				index = Math.Max(mPlots[i].Index, index);
				scopeColCount[mPlots[i].Index]++;
			}
			int colct = index + 1;
			int w = width / colct;
			int marg = 10;
			if (w < marg * 2) {
				w = marg * 2;
			}

			index = -1;
			int colh = 0;
			int row = 0;
			int speed = 0;
			foreach (var s in mPlots) {
				if (s == null || scopeColCount.Length <= s.Index) {
					break;
				}
				if (s.Index > index) {
					index = s.Index;
					var div = scopeColCount[index];
					if (0 < div) {
						colh = height / div;
					} else {
						colh = height;
					}
					row = 0;
					speed = s.Speed;
				}
				s.StackCount = scopeColCount[index];
				if (s.Speed != speed) {
					s.SetSpeed(speed);
					s.ResetGraph();
				}
				var r = new Rectangle(index * w, colh * row, w - marg, colh);
				row++;
				if (!r.Equals(s.BoundingBox)) {
					s.SetRect(r);
				}
			}
		}

		public static ScopePlot GetSelectedPlot() {
			if (0 <= mSelectedPlot && mSelectedPlot < mPlots.Length) {
				return mPlots[mSelectedPlot];
			}
			return null;
		}

		public static string Dump() {
			var dump = "";
			for (var i = 0; i != PlotCount; i++) {
				var d = mPlots[i].Dump();
				if (d != null) {
					dump += d + "\n";
				}
			}
			return dump;
		}

		public static void Undump(StringTokenizer st) {
			var plot = new ScopePlot();
			plot.Index = PlotCount;
			plot.Undump(st);
			mPlots[PlotCount++] = plot;
		}

		public static void ResetGraph() {
			for (int i = 0; i < PlotCount; i++) {
				mPlots[i].ResetGraph(true);
			}
		}

		public static void TimeStep() {
			for (int i = 0; i < PlotCount; i++) {
				mPlots[i].TimeStep();
			}
		}

		public static void AddPlot(BaseSymbol ui) {
			if (ui == null) {
				return;
			}
			int i;
			for (i = 0; i != PlotCount; i++) {
				if (mPlots[i].GetUI() == null) {
					break;
				}
			}
			if (i == PlotCount) {
				if (PlotCount == mPlots.Length) {
					return;
				}
				PlotCount++;
				mPlots[i] = new ScopePlot();
				mPlots[i].Index = i;
			}
			mPlots[i].Setup(ui);
			if (i > 0) {
				mPlots[i].SetSpeed(mPlots[i - 1].Speed);
			}
		}

		public static void RemoveWave() {
			if (mSelectedPlot < 0 || mPlots.Length <= mSelectedPlot) {
				return;
			}
			var plot = mPlots[mSelectedPlot];
			if (mSelectedWave < 0 || plot.Waves.Count <= mSelectedWave) {
				return;
			}
			var p = plot.Waves[mSelectedWave];
			plot.Waves.Remove(p);
		}

		public static void Stack() {
			var p = mSelectedPlot;
			if (p == 0) {
				if (PlotCount < 2) {
					return;
				}
				p = 1;
			}
			if (mPlots[p].Index == mPlots[p - 1].Index) {
				return;
			}
			mPlots[p].Index = mPlots[p - 1].Index;
			for (p++; p < PlotCount; p++) {
				mPlots[p].Index--;
			}
		}

		public static void Unstack() {
			var p = mSelectedPlot;
			if (p == 0) {
				if (PlotCount < 2) {
					return;
				}
				p = 1;
			}
			if (mPlots[p].Index != mPlots[p - 1].Index) {
				return;
			}
			for (; p < PlotCount; p++) {
				mPlots[p].Index++;
			}
		}

		public static void Combine() {
			var p = mSelectedPlot;
			if (p == 0) {
				if (PlotCount < 2) {
					return;
				}
				p = 1;
			}
			mPlots[p - 1].Combine(mPlots[p]);
			mPlots[p].Setup(null);
		}
	}
}
