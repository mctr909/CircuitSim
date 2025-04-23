using Circuit.Symbol;
using MainForm.Forms;

namespace Circuit.Forms {
	public partial class ScopeForm : Form {
		public static int PlotCount { get; set; } = 0;
		public static ScopePlot[] Plots = new ScopePlot[20];

		static int mSelectedPlot = -1;
		static int mSelectedWave = -1;

		ContextMenuStrip mScopePopupMenu = null;
		Form mScopeProperties = null;
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

		private void picScope_Click(object sender, EventArgs e) {
			var ev = (MouseEventArgs)e;
			switch (ev.Button) {
			case MouseButtons.Right:
				mSelectedWave = -1;
				if (0 <= mSelectedPlot) {
					if (Plots[mSelectedPlot].CanMenu) {
						mSelectedWave = Plots[mSelectedPlot].SelectedWave;
						var fm = new ScopePopupMenu();
						mScopePopupMenu = fm.Show(Left + mMouseCursorX, Top + mMouseCursorY, Plots, mSelectedPlot, (fm) => {
							mScopeProperties = fm;
						});
					}
				}
				break;
			}
		}

		private void picScope_DoubleClick(object sender, EventArgs e) {
			mSelectedWave = -1;
			if (0 <= mSelectedPlot) {
				if (Plots[mSelectedPlot].CanMenu) {
					var ev = (MouseEventArgs)e;
					var plot = Plots[mSelectedPlot];
					mSelectedWave = plot.SelectedWave;
					mScopeProperties = ScopeProperties.Show(plot, ev.X + Left, ev.Y + Top);
				}
			}
		}

		private void picScope_MouseMove(object sender, MouseEventArgs e) {
			if ((null == mScopePopupMenu || !mScopePopupMenu.Visible)
			&& (null == mScopeProperties || !mScopeProperties.Visible)) {
				mMouseCursorX = e.X;
				mMouseCursorY = e.Y;
			}
		}

		private void picScope_MouseLeave(object sender, EventArgs e) {
			if ((null == mScopePopupMenu || !mScopePopupMenu.Visible)
			&& (null == mScopeProperties || !mScopeProperties.Visible)) {
				ClearSelect();
			}
		}

		void SelectPlot() {
			mSelectedPlot = -1;
			for (int i = 0; i != PlotCount; i++) {
				if (Plots[i].BoundingBox.Contains(mMouseCursorX, mMouseCursorY)) {
					mSelectedPlot = i;
					break;
				}
			}
			if (mSelectedPlot < 0) {
				return;
			}
			var selectElm = Plots[mSelectedPlot].GetSelectedSymbol();
			if (null == selectElm) {
				if (null != mMouseElm) {
					mMouseElm.Select(false);
					mMouseElm = null;
				}
			} else {
				if (selectElm != MouseInfo.GrippedElm) {
					MouseInfo.GrippedElm?.Select(false);
					mMouseElm?.Select(false);
					MouseInfo.GripElm(selectElm);
					mMouseElm = selectElm;
				}
			}
		}

		void ClearSelect() {
			if (0 <= mSelectedPlot) {
				Plots[mSelectedPlot].SelectedWave = -1;
				mSelectedPlot = -1;
				mSelectedWave = -1;
			}
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
			SelectPlot();
			SetGraphics();
			CustomGraphics g;
			if (null == pdf) {
				g = mG;
			} else {
				g = pdf;
			}

			g.Clear(ControlPanel.ChkPrintable.Checked ? Color.White : Color.Black);
			g.DrawColor = CustomGraphics.TextColor;
			Setup(g.Width, g.Height);

			var ct = PlotCount;
			for (int i = 0; i != ct; i++) {
				var plot = Plots[i];
				plot.MouseCursorX = mMouseCursorX;
				plot.MouseCursorY = mMouseCursorY;
				plot.Draw(g);
			}
			Flush();
		}

		static void Setup(int width, int height) {
			/* check scopes to make sure the elements still exist, and remove
            /* unused scopes/columns */
			int index = -1;
			for (int i = 0; i < PlotCount; i++) {
				if (Plots[i].NeedToRemove) {
					int j;
					for (j = i; j != PlotCount; j++) {
						Plots[j] = Plots[j + 1];
					}
					PlotCount--;
					i--;
					continue;
				}
				if (Plots[i].Index > index + 1) {
					Plots[i].Index = index + 1;
				}
				index = Plots[i].Index;
			}

			while (PlotCount > 0 && Plots[PlotCount - 1].GetSelectedSymbol() == null) {
				PlotCount--;
			}

			if (PlotCount <= 0) {
				return;
			}

			index = 0;
			var scopeColCount = new int[PlotCount];
			for (int i = 0; i != PlotCount; i++) {
				index = Math.Max(Plots[i].Index, index);
				scopeColCount[Plots[i].Index]++;
			}
			int colct = index + 1;
			int w = width / colct;
			int marg = 20;
			if (w < marg * 2) {
				w = marg * 2;
			}

			index = -1;
			int colh = 0;
			int row = 0;
			int speed = 0;
			foreach (var s in Plots) {
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
			if (0 <= mSelectedPlot && mSelectedPlot < Plots.Length) {
				return Plots[mSelectedPlot];
			}
			return null;
		}

		public static string Dump() {
			var dump = "";
			for (var i = 0; i != PlotCount; i++) {
				var d = Plots[i].Dump();
				if (d != null) {
					dump += d + "\n";
				}
			}
			return dump;
		}

		public static void Undump(StringTokenizer st) {
			var plot = new ScopePlot {
				Index = PlotCount
			};
			plot.Undump(st);
			Plots[PlotCount++] = plot;
		}

		public static void ResetGraph() {
			for (int i = 0; i < PlotCount; i++) {
				Plots[i].ResetGraph(true);
			}
		}

		public static void AddPlot(BaseSymbol ui) {
			if (ui == null) {
				return;
			}
			int i;
			for (i = 0; i != PlotCount; i++) {
				if (Plots[i].GetSelectedSymbol() == null) {
					break;
				}
			}
			if (i == PlotCount) {
				if (PlotCount == Plots.Length) {
					return;
				}
				PlotCount++;
				Plots[i] = new ScopePlot {
					Index = i
				};
			}
			Plots[i].Setup(ui);
			if (i > 0) {
				Plots[i].SetSpeed(Plots[i - 1].Speed);
			}
		}

		public static void RemoveWave() {
			if (mSelectedPlot < 0 || Plots.Length <= mSelectedPlot) {
				return;
			}
			Plots[mSelectedPlot].Remove(mSelectedWave);
		}

		public static void Stack() {
			var p = mSelectedPlot;
			if (p == 0) {
				if (PlotCount < 2) {
					return;
				}
				p = 1;
			}
			if (Plots[p].Index == Plots[p - 1].Index) {
				return;
			}
			Plots[p].Index = Plots[p - 1].Index;
			for (p++; p < PlotCount; p++) {
				Plots[p].Index--;
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
			if (Plots[p].Index != Plots[p - 1].Index) {
				return;
			}
			for (; p < PlotCount; p++) {
				Plots[p].Index++;
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
			Plots[p - 1].Combine(Plots[p]);
			Plots[p].Setup(null);
		}
	}
}
