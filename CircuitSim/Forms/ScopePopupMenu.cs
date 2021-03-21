using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Circuit {
    class ScopePopupMenu {
        CirSim mSim;
        List<ToolStripMenuItem> m;
        ToolStripMenuItem removeScopeItem;
        ToolStripMenuItem maxScaleItem;
        ToolStripMenuItem stackItem;
        ToolStripMenuItem unstackItem;
        ToolStripMenuItem combineItem;
        ToolStripMenuItem removePlotItem;
        ToolStripMenuItem resetItem;
        ToolStripMenuItem propertiesItem;
        ToolStripMenuItem dockItem;
        ToolStripMenuItem undockItem;

        public ToolStripMenuItem[] MenuBar { get { return m.ToArray(); } }

        public ScopePopupMenu(CirSim sim) {
            mSim = sim;
            m = new List<ToolStripMenuItem>();
            /* */
            m.Add(removeScopeItem = new ToolStripMenuItem() { Text = "Remove Scope" });
            removeScopeItem.Click += new EventHandler((s, e) => {
                mSim.MenuPerformed(MENU_CATEGORY.SCOPE_POP, MENU_ITEM.REMOVE_SCOPE);
            });
            m.Add(dockItem = new ToolStripMenuItem() { Text = "Dock Scope" });
            dockItem.Click += new EventHandler((s, e) => {
                mSim.MenuPerformed(MENU_CATEGORY.SCOPE_POP, MENU_ITEM.DOCK);
            });
            m.Add(undockItem = new ToolStripMenuItem() { Text = "Undock Scope" });
            undockItem.Click += new EventHandler((s, e) => {
                mSim.MenuPerformed(MENU_CATEGORY.SCOPE_POP, MENU_ITEM.UNDOCK);
            });
            m.Add(maxScaleItem = new ToolStripMenuItem() { Text = "Max Scale" });
            maxScaleItem.Click += new EventHandler((s, e) => {
                mSim.MenuPerformed(MENU_CATEGORY.SCOPE_POP, MENU_ITEM.MAX_SCALE);
            });
            m.Add(stackItem = new ToolStripMenuItem() { Text = "Stack" });
            stackItem.Click += new EventHandler((s, e) => {
                mSim.MenuPerformed(MENU_CATEGORY.SCOPE_POP, MENU_ITEM.STACK);
            });
            m.Add(unstackItem = new ToolStripMenuItem() { Text = "Unstack" });
            unstackItem.Click += new EventHandler((s, e) => {
                mSim.MenuPerformed(MENU_CATEGORY.SCOPE_POP, MENU_ITEM.UNSTACK);
            });
            m.Add(combineItem = new ToolStripMenuItem() { Text = "Combine" });
            combineItem.Click += new EventHandler((s, e) => {
                mSim.MenuPerformed(MENU_CATEGORY.SCOPE_POP, MENU_ITEM.COMBINE);
            });
            m.Add(removePlotItem = new ToolStripMenuItem() { Text = "Remove Plot" });
            removePlotItem.Click += new EventHandler((s, e) => {
                mSim.MenuPerformed(MENU_CATEGORY.SCOPE_POP, MENU_ITEM.REMOVE_PLOT);
            });
            m.Add(resetItem = new ToolStripMenuItem() { Text = "Reset" });
            resetItem.Click += new EventHandler((s, e) => {
                mSim.MenuPerformed(MENU_CATEGORY.SCOPE_POP, MENU_ITEM.RESET);
            });
            m.Add(propertiesItem = new ToolStripMenuItem() { Text = "Properties..." });
            propertiesItem.Click += new EventHandler((s, e) => {
                mSim.MenuPerformed(MENU_CATEGORY.SCOPE_POP, MENU_ITEM.PROPERTIES);
            });
        }

        public void DoScopePopupChecks(bool floating) {
            stackItem.Visible = !floating;
            unstackItem.Visible = !floating;
            combineItem.Visible = !floating;
            dockItem.Visible = floating;
            undockItem.Visible = !floating;
        }
    }
}
