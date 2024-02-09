using System;
using System.Drawing;
using System.Windows.Forms;

using Circuit.Symbol.Passive;

namespace Circuit.Forms {
    public partial class ElementPopupMenu : ContextMenuStrip {
        public enum Item {
            EDIT,
            SCOPE_WINDOW,
            SCOPE_FLOAT,
            FLIP_POST,
            SPLIT_WIRE,
            SLIDERS
        }

        public delegate void Callback(Item item);

        public ElementPopupMenu(Callback callback) {
            InitializeComponent();
            Edit.Click += new EventHandler((s, e) => {
                callback(Item.EDIT);
            });
            ScopeWindow.Click += new EventHandler((s, e) => {
                callback(Item.SCOPE_WINDOW);
            });
            ScopeFloat.Click += new EventHandler((s, e) => {
                callback(Item.SCOPE_FLOAT);
            });
            SplitWire.Click += new EventHandler((s, e) => {
                callback(Item.SPLIT_WIRE);
            });
            FlipPosts.Click += new EventHandler((s, e) => {
                callback(Item.FLIP_POST);
            });
            Slider.Click += new EventHandler((s, e) => {
                callback(Item.SLIDERS);
            });
        }

        public ContextMenuStrip Show(Point pos, BaseSymbol mouse) {
            Edit.Enabled = mouse.GetElementInfo(0, 0) != null;
            ScopeWindow.Enabled = mouse.CanViewInScope;
            ScopeFloat.Enabled = mouse.CanViewInScope;
            FlipPosts.Enabled = 2 == mouse.Element.TermCount;
            SplitWire.Enabled = canSplit(mouse);
            Slider.Enabled = sliderItemEnabled(mouse);
            Show();
            Location = pos;
            return this;
        }

        bool canSplit(BaseSymbol ce) {
            if (!(ce is Wire)) {
                return false;
            }
            var we = (Wire)ce;
            if (we.Post.A.X == we.Post.B.X || we.Post.A.Y == we.Post.B.Y) {
                return true;
            }
            return false;
        }

        bool sliderItemEnabled(BaseSymbol elm) {
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
