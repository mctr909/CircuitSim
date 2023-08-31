using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Circuit.UI;
using Circuit.UI.Passive;

namespace Circuit {
    public enum ELEMENT_MENU_ITEM {
        EDIT,
        SCOPE_WINDOW,
        SCOPE_FLOAT,
        FLIP_POST,
        SPLIT_WIRE,
        SLIDERS
    }

    public class ElementPopupMenu {
        ToolStripItem[] mMenuItems;
        ToolStripMenuItem mEdit;
        ToolStripMenuItem mScopeWindow;
        ToolStripMenuItem mScopeFloat;
        ToolStripMenuItem mFlipPosts;
        ToolStripMenuItem mSplit;
        ToolStripMenuItem mSlider;

        public ElementPopupMenu(CirSimForm sim) {
            mEdit = new ToolStripMenuItem() { Text = "編集" };
            mEdit.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.EDIT);
            });
            mScopeWindow = new ToolStripMenuItem() { Text = "スコープをウィンドウ表示" };
            mScopeWindow.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.SCOPE_WINDOW);
            });
            mScopeFloat = new ToolStripMenuItem() { Text = "スコープを任意の場所に表示" };
            mScopeFloat.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.SCOPE_FLOAT);
            });
            mSplit = new ToolStripMenuItem() { Text = "線の分割" };
            mSplit.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.SPLIT_WIRE);
            });
            mFlipPosts = new ToolStripMenuItem() { Text = "端子の入れ替え" };
            mFlipPosts.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.FLIP_POST);
            });
            mSlider = new ToolStripMenuItem() { Text = "スライダーを作成" };
            mSlider.Click += new EventHandler((s, e) => {
                sim.Performed(ELEMENT_MENU_ITEM.SLIDERS);
            });
            mMenuItems = new List<ToolStripItem>() {
                mEdit,
                new ToolStripSeparator(),
                mScopeWindow,
                mScopeFloat,
                new ToolStripSeparator(),
                mSplit,
                mFlipPosts,
                new ToolStripSeparator(),
                mSlider
            }.ToArray();
        }

        public ContextMenuStrip Show(int px, int py, BaseUI mouseElm) {
            mEdit.Enabled = mouseElm.GetElementInfo(0, 0) != null;
            mScopeWindow.Enabled = mouseElm.CanViewInScope;
            mScopeFloat.Enabled = mouseElm.CanViewInScope;
            mFlipPosts.Enabled = 2 == mouseElm.Elm.PostCount;
            mSplit.Enabled = canSplit(mouseElm);
            mSlider.Enabled = sliderItemEnabled(mouseElm);

            var popupMenu = new ContextMenuStrip();
            popupMenu.Items.AddRange(mMenuItems);
            popupMenu.Show();
            popupMenu.Location = new Point(px, py);
            return popupMenu;
        }

        bool canSplit(BaseUI ce) {
            if (!(ce is Wire)) {
                return false;
            }
            var we = (Wire)ce;
            if (we.Post.A.X == we.Post.B.X || we.Post.A.Y == we.Post.B.Y) {
                return true;
            }
            return false;
        }

        bool sliderItemEnabled(BaseUI elm) {
            if (elm is Pot) {
                return false;
            }
            for (int i = 0; ; i++) {
                var ei = elm.GetElementInfo(i, 0);
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
