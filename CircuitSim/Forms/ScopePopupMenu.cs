using System;
using System.Drawing;
using System.Windows.Forms;

using Circuit.Symbol.Output;

namespace Circuit.Forms {
	public class ScopePopupMenu {
		enum SCOPE_MENU_ITEM {
			REMOVE_SCOPE,
			REMOVE_WAVE,
			SPEED_UP,
			SPEED_DOWN,
			MAX_SCALE,
			STACK,
			UNSTACK,
			COMBINE,
			RESET,
			PROPERTIES
		}
		ScopePlot mPlot;
		ContextMenuStrip mPopupMenu;
		ToolStripMenuItem mCombine;
		ToolStripMenuItem mStack;
		ToolStripMenuItem mUnstack;
		ToolStripSeparator mStackSeparator;
		ToolStripMenuItem mRemoveScope;
		ToolStripMenuItem mRemoveWave;
		ToolStripMenuItem mMaxScale;
		ToolStripMenuItem mReset;
		ToolStripMenuItem mProperties;

		public ScopePopupMenu() {
			if (MouseInfo.GrippedElm is Scope) {
				mPlot = ((Scope)MouseInfo.GrippedElm).Plot;
			} else {
				mPlot = ScopeForm.GetSelectedPlot();
			}
			if (null == mPlot) {
				return;
			}
			mPopupMenu = new ContextMenuStrip();
			/* 波形の配置 */
			mPopupMenu.Items.Add(mCombine = new ToolStripMenuItem() { Text = "左のスコープに重ねる" });
			mCombine.Click += new EventHandler((s, e) => {
				performed(SCOPE_MENU_ITEM.COMBINE);
			});
			mPopupMenu.Items.Add(mStack = new ToolStripMenuItem() { Text = "左のスコープの下に並べる" });
			mStack.Click += new EventHandler((s, e) => {
				performed(SCOPE_MENU_ITEM.STACK);
			});
			mPopupMenu.Items.Add(mUnstack = new ToolStripMenuItem() { Text = "右横に並べる" });
			mUnstack.Click += new EventHandler((s, e) => {
				performed(SCOPE_MENU_ITEM.UNSTACK);
			});
			mPopupMenu.Items.Add(mStackSeparator = new ToolStripSeparator());
			/* 削除 */
			mPopupMenu.Items.Add(mRemoveScope = new ToolStripMenuItem() { Text = "スコープの削除" });
			mRemoveScope.Click += new EventHandler((s, e) => {
				performed(SCOPE_MENU_ITEM.REMOVE_SCOPE);
			});
			mPopupMenu.Items.Add(mRemoveWave = new ToolStripMenuItem() { Text = "選択波形の削除" });
			mRemoveWave.Click += new EventHandler((s, e) => {
				performed(SCOPE_MENU_ITEM.REMOVE_WAVE);
			});
			mPopupMenu.Items.Add(new ToolStripSeparator());
			/* 波形の状態更新 */
			mPopupMenu.Items.Add(mMaxScale = new ToolStripMenuItem()
			{
				Text = mPlot.Normarize ? "振幅を最適化しない" : "振幅を最適化"
			});
			mMaxScale.Click += new EventHandler((s, e) => {
				performed(SCOPE_MENU_ITEM.MAX_SCALE);
			});
			mPopupMenu.Items.Add(mReset = new ToolStripMenuItem() { Text = "リセット" });
			mReset.Click += new EventHandler((s, e) => {
				performed(SCOPE_MENU_ITEM.RESET);
			});
			mPopupMenu.Items.Add(new ToolStripSeparator());
			var speedUp = new ToolStripMenuItem() { Text = "速度を上げる" };
			mPopupMenu.Items.Add(speedUp);
			speedUp.Click += new EventHandler((s, e) => {
				performed(SCOPE_MENU_ITEM.SPEED_UP);
			});
			var speedDown = new ToolStripMenuItem() { Text = "速度を下げる" };
			mPopupMenu.Items.Add(speedDown);
			speedDown.Click += new EventHandler((s, e) => {
				performed(SCOPE_MENU_ITEM.SPEED_DOWN);
			});
			mPopupMenu.Items.Add(new ToolStripSeparator());
			/* 設定 */
			mPopupMenu.Items.Add(mProperties = new ToolStripMenuItem() { Text = "詳細設定" });
			mProperties.Click += new EventHandler((s, e) => {
				performed(SCOPE_MENU_ITEM.PROPERTIES);
			});
		}

		public ContextMenuStrip Show(int px, int py, ScopePlot[] scopes, int selectedScopeIndex, bool floating) {
			doScopePopupChecks(scopes, selectedScopeIndex, floating);
			mPopupMenu.Show();
			mPopupMenu.Location = new Point(px, py - 8);
			return mPopupMenu;
		}

		void doScopePopupChecks(ScopePlot[] plots, int selectedPlotIndex, bool floating) {
			var hasStacks = false;
			var hasLeft = false;
			var selectedPlot = plots[selectedPlotIndex];
			for (int i = 0; i < plots.Length && null != plots[i]; i++) {
				if (i == selectedPlotIndex) {
					continue;
				}
				var plot = plots[i];
				if (plot.Index == selectedPlot.Index) {
					hasStacks = true;
				}
				if (plot.Index < selectedPlot.Index) {
					hasLeft = true;
				}
			}
			mCombine.Visible = !floating && hasLeft;
			mStack.Visible = !floating && hasLeft;
			mUnstack.Visible = !floating && hasStacks;
			mStackSeparator.Visible = !floating && (hasLeft || hasStacks);
			mRemoveWave.Visible = 1 < selectedPlot.Waves.Count;
		}

		void performed(SCOPE_MENU_ITEM item) {
			if (!(MouseInfo.GrippedElm is Scope)) {
				switch (item) {
				case SCOPE_MENU_ITEM.REMOVE_WAVE:
					ScopeForm.RemoveWave();
					break;
				case SCOPE_MENU_ITEM.STACK:
					ScopeForm.Stack();
					break;
				case SCOPE_MENU_ITEM.UNSTACK:
					ScopeForm.Unstack();
					break;
				case SCOPE_MENU_ITEM.COMBINE:
					ScopeForm.Combine();
					break;
				}
			}
			switch (item) {
			case SCOPE_MENU_ITEM.REMOVE_SCOPE:
				mPlot.Setup(null);
				break;
			case SCOPE_MENU_ITEM.SPEED_UP:
				mPlot.SpeedUp();
				break;
			case SCOPE_MENU_ITEM.SPEED_DOWN:
				mPlot.SlowDown();
				break;
			case SCOPE_MENU_ITEM.MAX_SCALE:
				mPlot.MaxScale();
				break;
			case SCOPE_MENU_ITEM.RESET:
				mPlot.ResetGraph(true);
				break;
			case SCOPE_MENU_ITEM.PROPERTIES: {
				var fm = new ScopeProperties(mPlot);
				fm.Show(0, 0);
				CirSimForm.ScopeDialog = fm;
				break;
			}
			}
			CirSimForm.DeleteUnusedScopeElms();
		}
	}
}
