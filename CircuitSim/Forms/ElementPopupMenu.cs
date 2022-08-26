using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Circuit.Elements;
using Circuit.Elements.Passive;

namespace Circuit {
    public enum ELEMENT_MENU_ITEM {
        EDIT,
        SLIDERS,
        FLIP,
        SPLIT,
        VIEW_IN_SCOPE,
        VIEW_IN_FLOAT_SCOPE
    }

    public class ElementPopupMenu {
        List<ToolStripItem> mMenuItems;
        ToolStripMenuItem mEdit;
        ToolStripMenuItem mScope;
        ToolStripMenuItem mFloatScope;
        ToolStripMenuItem mFlip;
        ToolStripMenuItem mSplit;
        ToolStripMenuItem mSlider;

        public ElementPopupMenu(CirSimForm sim) {
            mMenuItems = new List<ToolStripItem>();
            mMenuItems.Add(mEdit = new ToolStripMenuItem() { Text = "編集" });
            mEdit.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.EDIT);
            });
            mMenuItems.Add(new ToolStripSeparator());

            mMenuItems.Add(mSplit = new ToolStripMenuItem() { Text = "線の分割" });
            mSplit.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.SPLIT);
            });
            mMenuItems.Add(mFlip = new ToolStripMenuItem() { Text = "端子の入れ替え" });
            mFlip.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.FLIP);
            });
            mMenuItems.Add(new ToolStripSeparator());

            mMenuItems.Add(mScope = new ToolStripMenuItem() { Text = "画面下部にスコープを表示" });
            mScope.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.VIEW_IN_SCOPE);
            });
            mMenuItems.Add(mFloatScope = new ToolStripMenuItem() { Text = "任意の場所にスコープを表示" });
            mFloatScope.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.VIEW_IN_FLOAT_SCOPE);
            });
            mMenuItems.Add(new ToolStripSeparator());

            mMenuItems.Add(mSlider = new ToolStripMenuItem() { Text = "スライダーを作成" });
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
            if (we.DumpInfo.P1.X == we.DumpInfo.P2.X || we.DumpInfo.P1.Y == we.DumpInfo.P2.Y) {
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
