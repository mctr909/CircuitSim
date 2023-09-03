using System;
using System.Drawing;
using System.Windows.Forms;

using Circuit.UI.Output;

namespace Circuit {
    public enum SCOPE_MENU_ITEM {
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

    public class ScopePopupMenu {
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
            mPopupMenu = new ContextMenuStrip();
            /* 波形の配置 */
            mPopupMenu.Items.Add(mCombine = new ToolStripMenuItem() { Text = "左のスコープに重ねる" });
            mCombine.Click += new EventHandler((s, e) => {
                Scope.Performed(SCOPE_MENU_ITEM.COMBINE);
            });
            mPopupMenu.Items.Add(mStack = new ToolStripMenuItem() { Text = "左のスコープの下に並べる" });
            mStack.Click += new EventHandler((s, e) => {
                Scope.Performed(SCOPE_MENU_ITEM.STACK);
            });
            mPopupMenu.Items.Add(mUnstack = new ToolStripMenuItem() { Text = "右横に並べる" });
            mUnstack.Click += new EventHandler((s, e) => {
                Scope.Performed(SCOPE_MENU_ITEM.UNSTACK);
            });
            mPopupMenu.Items.Add(mStackSeparator = new ToolStripSeparator());
            /* 削除 */
            mPopupMenu.Items.Add(mRemoveScope = new ToolStripMenuItem() { Text = "スコープの削除" });
            mRemoveScope.Click += new EventHandler((s, e) => {
                Scope.Performed(SCOPE_MENU_ITEM.REMOVE_SCOPE);
            });
            mPopupMenu.Items.Add(mRemoveWave = new ToolStripMenuItem() { Text = "選択波形の削除" });
            mRemoveWave.Click += new EventHandler((s, e) => {
                Scope.Performed(SCOPE_MENU_ITEM.REMOVE_WAVE);
            });
            mPopupMenu.Items.Add(new ToolStripSeparator());
            /* 波形の状態更新 */
            mPopupMenu.Items.Add(mMaxScale = new ToolStripMenuItem() { Text = "振幅の最大化" });
            mMaxScale.Click += new EventHandler((s, e) => {
                Scope.Performed(SCOPE_MENU_ITEM.MAX_SCALE);
            });
            mPopupMenu.Items.Add(mReset = new ToolStripMenuItem() { Text = "リセット" });
            mReset.Click += new EventHandler((s, e) => {
                Scope.Performed(SCOPE_MENU_ITEM.RESET);
            });
            mPopupMenu.Items.Add(new ToolStripSeparator());
            var speedUp = new ToolStripMenuItem() { Text = "速度を上げる" };
            mPopupMenu.Items.Add(speedUp);
            speedUp.Click += new EventHandler((s, e) => {
                Scope.Performed(SCOPE_MENU_ITEM.SPEED_UP);
            });
            var speedDown = new ToolStripMenuItem() { Text = "速度を下げる" };
            mPopupMenu.Items.Add(speedDown);
            speedDown.Click += new EventHandler((s, e) => {
                Scope.Performed(SCOPE_MENU_ITEM.SPEED_DOWN);
            });
            mPopupMenu.Items.Add(new ToolStripSeparator());
            /* 設定 */
            mPopupMenu.Items.Add(mProperties = new ToolStripMenuItem() { Text = "詳細設定" });
            mProperties.Click += new EventHandler((s, e) => {
                Scope.Performed(SCOPE_MENU_ITEM.PROPERTIES);
            });
        }

        public ContextMenuStrip Show(int px, int py, Scope.Property[] scopes, int selectedScopeIndex, bool floating) {
            doScopePopupChecks(scopes, selectedScopeIndex, floating);
            mPopupMenu.Show();
            mPopupMenu.Location = new Point(px, py - 8);
            return mPopupMenu;
        }

        void doScopePopupChecks(Scope.Property[] scopes, int selectedScopeIndex, bool floating) {
            var hasStacks = false;
            var hasLeft = false;
            var selectedScope = scopes[selectedScopeIndex];
            for (int i = 0; i < scopes.Length && null != scopes[i]; i++) {
                if (i == selectedScopeIndex) {
                    continue;
                }
                var scope = scopes[i];
                if (scope.Position == selectedScope.Position) {
                    hasStacks = true;
                }
                if (scope.Position < selectedScope.Position) {
                    hasLeft = true;
                }
            }
            mCombine.Visible = !floating && hasLeft;
            mStack.Visible = !floating && hasLeft;
            mUnstack.Visible = !floating && hasStacks;
            mStackSeparator.Visible = !floating && (hasLeft || hasStacks);
            mRemoveWave.Visible = 1 < selectedScope.Plots.Count;
        }
    }
}
