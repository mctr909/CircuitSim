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

        public ScopePopupMenu(CirSim sim) {
            mSim = sim;
            m = new List<ToolStripMenuItem>();
            /* */
            m.Add(removeScopeItem = new ToolStripMenuItem() { Text = "Remove Scope" });
            removeScopeItem.Click += new EventHandler((s, e) => {
                mSim.menuPerformed(MENU_CATEGORY.SCOPE_POP, MENU_ITEM.remove);
            });
            m.Add(dockItem = new ToolStripMenuItem() { Text = "Dock Scope" });
            dockItem.Click += new EventHandler((s, e) => {
                mSim.menuPerformed(MENU_CATEGORY.SCOPE_POP, MENU_ITEM.dock);
            });
            m.Add(undockItem = new ToolStripMenuItem() { Text = "Undock Scope" });
            undockItem.Click += new EventHandler((s, e) => {
                mSim.menuPerformed(MENU_CATEGORY.SCOPE_POP, MENU_ITEM.undock);
            });
            m.Add(maxScaleItem = new ToolStripMenuItem() { Text = "Max Scale" });
            maxScaleItem.Click += new EventHandler((s, e) => {
                mSim.menuPerformed(MENU_CATEGORY.SCOPE_POP, MENU_ITEM.maxscale);
            });
            m.Add(stackItem = new ToolStripMenuItem() { Text = "Stack" });
            stackItem.Click += new EventHandler((s, e) => {
                mSim.menuPerformed(MENU_CATEGORY.SCOPE_POP, MENU_ITEM.stack);
            });
            m.Add(unstackItem = new ToolStripMenuItem() { Text = "Unstack" });
            unstackItem.Click += new EventHandler((s, e) => {
                mSim.menuPerformed(MENU_CATEGORY.SCOPE_POP, MENU_ITEM.unstack);
            });
            m.Add(combineItem = new ToolStripMenuItem() { Text = "Combine" });
            combineItem.Click += new EventHandler((s, e) => {
                mSim.menuPerformed(MENU_CATEGORY.SCOPE_POP, MENU_ITEM.combine);
            });
            m.Add(removePlotItem = new ToolStripMenuItem() { Text = "Remove Plot" });
            removePlotItem.Click += new EventHandler((s, e) => {
                mSim.menuPerformed(MENU_CATEGORY.SCOPE_POP, MENU_ITEM.removeplot);
            });
            m.Add(resetItem = new ToolStripMenuItem() { Text = "Reset" });
            resetItem.Click += new EventHandler((s, e) => {
                mSim.menuPerformed(MENU_CATEGORY.SCOPE_POP, MENU_ITEM.reset);
            });
            m.Add(propertiesItem = new ToolStripMenuItem() { Text = "Properties..." });
            propertiesItem.Click += new EventHandler((s, e) => {
                mSim.menuPerformed(MENU_CATEGORY.SCOPE_POP, MENU_ITEM.properties);
            });
        }

        public void doScopePopupChecks(bool floating, Scope s) {
            /* maxScaleItem.Visible = s.maxScale; */
            stackItem.Visible = !floating;
            unstackItem.Visible = !floating;
            combineItem.Visible = !floating;
            dockItem.Visible = floating;
            undockItem.Visible = !floating;
        }

        public ToolStripMenuItem[] getMenuBar() {
            return m.ToArray();
        }
    }
}
