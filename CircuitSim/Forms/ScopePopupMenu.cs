using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit {
    public enum SCOPE_MENU_ITEM {
        DOCK,
        UNDOCK,
        REMOVE_SCOPE,
        REMOVE_PLOT,
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
        List<ToolStripMenuItem> mMenuItems;
        ToolStripMenuItem mRemoveScope;
        ToolStripMenuItem mMaxScale;
        ToolStripMenuItem mStack;
        ToolStripMenuItem mUnstack;
        ToolStripMenuItem mCombine;
        ToolStripMenuItem mRemovePlot;
        ToolStripMenuItem mReset;
        ToolStripMenuItem mDock;
        ToolStripMenuItem mUndock;
        ToolStripMenuItem mProperties;

        public ScopePopupMenu(CirSimForm sim) {
            mMenuItems = new List<ToolStripMenuItem>();
            /* */
            mMenuItems.Add(mRemoveScope = new ToolStripMenuItem() { Text = "削除" });
            mRemoveScope.Click += new EventHandler((s, e) => {
                sim.Performed(SCOPE_MENU_ITEM.REMOVE_SCOPE);
            });
            mMenuItems.Add(mRemovePlot = new ToolStripMenuItem() { Text = "Remove Plot" });
            mRemovePlot.Click += new EventHandler((s, e) => {
                sim.Performed(SCOPE_MENU_ITEM.REMOVE_PLOT);
            });
            mMenuItems.Add(mDock = new ToolStripMenuItem() { Text = "画面下部に表示" });
            mDock.Click += new EventHandler((s, e) => {
                sim.Performed(SCOPE_MENU_ITEM.DOCK);
            });
            mMenuItems.Add(mUndock = new ToolStripMenuItem() { Text = "任意の場所に表示" });
            mUndock.Click += new EventHandler((s, e) => {
                sim.Performed(SCOPE_MENU_ITEM.UNDOCK);
            });
            mMenuItems.Add(mCombine = new ToolStripMenuItem() { Text = "左のスコープに重ねる" });
            mCombine.Click += new EventHandler((s, e) => {
                sim.Performed(SCOPE_MENU_ITEM.COMBINE);
            });
            mMenuItems.Add(mStack = new ToolStripMenuItem() { Text = "左のスコープの下に並べる" });
            mStack.Click += new EventHandler((s, e) => {
                sim.Performed(SCOPE_MENU_ITEM.STACK);
            });
            mMenuItems.Add(mUnstack = new ToolStripMenuItem() { Text = "右横に並べる" });
            mUnstack.Click += new EventHandler((s, e) => {
                sim.Performed(SCOPE_MENU_ITEM.UNSTACK);
            });
            mMenuItems.Add(mMaxScale = new ToolStripMenuItem() { Text = "振幅の最大化" });
            mMaxScale.Click += new EventHandler((s, e) => {
                sim.Performed(SCOPE_MENU_ITEM.MAX_SCALE);
            });
            mMenuItems.Add(mReset = new ToolStripMenuItem() { Text = "リセット" });
            mReset.Click += new EventHandler((s, e) => {
                sim.Performed(SCOPE_MENU_ITEM.RESET);
            });
            mMenuItems.Add(mProperties = new ToolStripMenuItem() { Text = "詳細設定" });
            mProperties.Click += new EventHandler((s, e) => {
                sim.Performed(SCOPE_MENU_ITEM.PROPERTIES);
            });
        }

        public ContextMenuStrip Show(int px, int py, bool floating) {
            doScopePopupChecks(floating);
            var popupMenu = new ContextMenuStrip();
            popupMenu.Items.AddRange(mMenuItems.ToArray());
            popupMenu.Show();
            popupMenu.Location = new Point(px, py - popupMenu.Height - 8);
            return popupMenu;
        }

        void doScopePopupChecks(bool floating) {
            mStack.Visible = !floating;
            mUnstack.Visible = !floating;
            mCombine.Visible = !floating;
            mDock.Visible = floating;
            mUndock.Visible = !floating;
        }
    }
}
