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
            mMenuItems.Add(mRemoveScope = new ToolStripMenuItem() { Text = "Remove Scope" });
            mRemoveScope.Click += new EventHandler((s, e) => {
                sim.Performed(SCOPE_MENU_ITEM.REMOVE_SCOPE);
            });
            mMenuItems.Add(mDock = new ToolStripMenuItem() { Text = "Dock Scope" });
            mDock.Click += new EventHandler((s, e) => {
                sim.Performed(SCOPE_MENU_ITEM.DOCK);
            });
            mMenuItems.Add(mUndock = new ToolStripMenuItem() { Text = "Undock Scope" });
            mUndock.Click += new EventHandler((s, e) => {
                sim.Performed(SCOPE_MENU_ITEM.UNDOCK);
            });
            mMenuItems.Add(mMaxScale = new ToolStripMenuItem() { Text = "Max Scale" });
            mMaxScale.Click += new EventHandler((s, e) => {
                sim.Performed(SCOPE_MENU_ITEM.MAX_SCALE);
            });
            mMenuItems.Add(mStack = new ToolStripMenuItem() { Text = "Stack" });
            mStack.Click += new EventHandler((s, e) => {
                sim.Performed(SCOPE_MENU_ITEM.STACK);
            });
            mMenuItems.Add(mUnstack = new ToolStripMenuItem() { Text = "Unstack" });
            mUnstack.Click += new EventHandler((s, e) => {
                sim.Performed(SCOPE_MENU_ITEM.UNSTACK);
            });
            mMenuItems.Add(mCombine = new ToolStripMenuItem() { Text = "Combine" });
            mCombine.Click += new EventHandler((s, e) => {
                sim.Performed(SCOPE_MENU_ITEM.COMBINE);
            });
            mMenuItems.Add(mRemovePlot = new ToolStripMenuItem() { Text = "Remove Plot" });
            mRemovePlot.Click += new EventHandler((s, e) => {
                sim.Performed(SCOPE_MENU_ITEM.REMOVE_PLOT);
            });
            mMenuItems.Add(mReset = new ToolStripMenuItem() { Text = "Reset" });
            mReset.Click += new EventHandler((s, e) => {
                sim.Performed(SCOPE_MENU_ITEM.RESET);
            });
            mMenuItems.Add(mProperties = new ToolStripMenuItem() { Text = "Properties..." });
            mProperties.Click += new EventHandler((s, e) => {
                sim.Performed(SCOPE_MENU_ITEM.PROPERTIES);
            });
        }

        public ContextMenuStrip Show(int px, int py, bool floating) {
            doScopePopupChecks(floating);
            var popupMenu = new ContextMenuStrip();
            popupMenu.Items.AddRange(mMenuItems.ToArray());
            popupMenu.Show();
            popupMenu.Location = new Point(px, py);
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
