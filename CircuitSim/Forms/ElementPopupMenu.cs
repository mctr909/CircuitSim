using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Circuit.Elements;
using Circuit.Elements.Passive;

namespace Circuit {
    public enum ELEMENT_MENU_ITEM {
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

    public class ElementPopupMenu {
        List<ToolStripMenuItem> mMenuItems;
        ToolStripMenuItem mEdit;
        ToolStripMenuItem mScope;
        ToolStripMenuItem mFloatScope;
        ToolStripMenuItem mFlip;
        ToolStripMenuItem mSplit;
        ToolStripMenuItem mSlider;

        public ElementPopupMenu(CirSimForm sim) {
            mMenuItems = new List<ToolStripMenuItem>();
            mMenuItems.Add(mEdit = new ToolStripMenuItem() { Text = "Edit..." });
            mEdit.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.EDIT);
            });
            var cut = new ToolStripMenuItem() { Text = "Cut" };
            mMenuItems.Add(cut);
            cut.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.CUT);
            });
            var copy = new ToolStripMenuItem() { Text = "Copy" };
            mMenuItems.Add(copy);
            copy.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.COPY);
            });
            var delete = new ToolStripMenuItem() { Text = "Delete" };
            mMenuItems.Add(delete);
            delete.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.DELETE);
            });
            var dup = new ToolStripMenuItem() { Text = "Duplicate" };
            mMenuItems.Add(dup);
            dup.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.DUPLICATE);
            });
            mMenuItems.Add(mScope = new ToolStripMenuItem() { Text = "View in Scope" });
            mScope.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.VIEW_IN_SCOPE);
            });
            mMenuItems.Add(mFloatScope = new ToolStripMenuItem() { Text = "View in Undocked Scope" });
            mFloatScope.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.VIEW_IN_FLOAT_SCOPE);
            });
            mMenuItems.Add(mFlip = new ToolStripMenuItem() { Text = "Swap Terminals" });
            mFlip.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.FLIP);
            });
            mMenuItems.Add(mSplit = new ToolStripMenuItem() { Text = "Split Wire" });
            mSplit.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.SPLIT);
            });
            mMenuItems.Add(mSlider = new ToolStripMenuItem() { Text = "Sliders..." });
            mSlider.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.SLIDERS);
            });
        }

        public ContextMenuStrip Show(int px, int py, BaseUI mouseElm) {
            mScope.Enabled = mouseElm.CanViewInScope;
            mFloatScope.Enabled = mouseElm.CanViewInScope;
            mEdit.Enabled = mouseElm.GetElementInfo(0) != null;
            mFlip.Enabled = 2 == mouseElm.Elm.PostCount;
            mSplit.Enabled = canSplit(mouseElm);
            mSlider.Enabled = sliderItemEnabled(mouseElm);

            var popupMenu = new ContextMenuStrip();
            popupMenu.Items.AddRange(mMenuItems.ToArray());
            popupMenu.Show();
            popupMenu.Location = new Point(px, py);
            return popupMenu;
        }

        bool canSplit(BaseUI ce) {
            if (!(ce is WireUI)) {
                return false;
            }
            var we = (WireUI)ce;
            if (we.P1.X == we.P2.X || we.P1.Y == we.P2.Y) {
                return true;
            }
            return false;
        }

        bool sliderItemEnabled(BaseUI elm) {
            if (elm is PotUI) {
                return false;
            }
            for (int i = 0; ; i++) {
                var ei = elm.GetElementInfo(i);
                if (ei == null) {
                    return false;
                }
                if (ei.CanCreateAdjustable()) {
                    return true;
                }
            }
        }
    }
}
