using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Circuit {
    enum ELEMENT_MENU_ITEM {
        CUT,
        COPY,
        DUPLICATE,
        DELETE,
        EDIT,
        SLIDERS,
        FLIP,
        SPLIT,
        VIEW_IN_SCOPE,
        VIEW_IN_FLOAT_SCOPE
    }

    class ElementMenu {
        public ToolStripMenuItem[] MenuBar;
        public ToolStripMenuItem Edit;
        public ToolStripMenuItem Scope;
        public ToolStripMenuItem FloatScope;
        public ToolStripMenuItem Flip;
        public ToolStripMenuItem Split;
        public ToolStripMenuItem Slider;

        public ElementMenu(CirSim sim) {
            var ctxMenuItem = new List<ToolStripMenuItem>();
            ctxMenuItem.Add(Edit = new ToolStripMenuItem() { Text = "Edit..." });
            Edit.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.EDIT);
            });
            var cut = new ToolStripMenuItem() { Text = "Cut" };
            ctxMenuItem.Add(cut);
            cut.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.CUT);
            });
            var copy = new ToolStripMenuItem() { Text = "Copy" };
            ctxMenuItem.Add(copy);
            copy.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.COPY);
            });
            var delete = new ToolStripMenuItem() { Text = "Delete" };
            ctxMenuItem.Add(delete);
            delete.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.DELETE);
            });
            var dup = new ToolStripMenuItem() { Text = "Duplicate" };
            ctxMenuItem.Add(dup);
            dup.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.DUPLICATE);
            });
            ctxMenuItem.Add(Scope = new ToolStripMenuItem() { Text = "View in Scope" });
            Scope.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.VIEW_IN_SCOPE);
            });
            ctxMenuItem.Add(FloatScope = new ToolStripMenuItem() { Text = "View in Undocked Scope" });
            FloatScope.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.VIEW_IN_FLOAT_SCOPE);
            });
            ctxMenuItem.Add(Flip = new ToolStripMenuItem() { Text = "Swap Terminals" });
            Flip.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.FLIP);
            });
            ctxMenuItem.Add(Split = new ToolStripMenuItem() { Text = "Split Wire" });
            Split.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.SPLIT);
            });
            ctxMenuItem.Add(Slider = new ToolStripMenuItem() { Text = "Sliders..." });
            Slider.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.SLIDERS);
            });
            /* */
            MenuBar = ctxMenuItem.ToArray();
        }
    }
}
