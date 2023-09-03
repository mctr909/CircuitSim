using Circuit.UI;
using Circuit.UI.Output;
using System;
using System.Drawing;
using System.Windows.Forms;
using static Circuit.CirSimForm;

namespace Circuit.Forms {
    public partial class ScopeForm : Form {
        public static int MouseCursorX { get; private set; } = -1;
        public static int MouseCursorY { get; private set; } = -1;
        public static int SelectedScope = -1;
        static ScopePopupMenu mScopePopupMenu = new ScopePopupMenu();

        BaseUI mMouseElm = null;
        CustomGraphics mG;
        Bitmap mBmp;
        Graphics mContext;
        int mWidth;
        int mHeight;

        public ScopeForm() {
            InitializeComponent();
            mWidth = Width - 16;
            mHeight = Height - 39;
            SetGraphics();
        }

        private void ScopeForm_SizeChanged(object sender, EventArgs e) {
            mWidth = Width - 16;
            mHeight = Height - 39;
        }

        private void picScope_MouseMove(object sender, MouseEventArgs e) {
            MouseCursorX = e.X;
            MouseCursorY = e.Y;
            SelectedScope = -1;
        }

        private void picScope_MouseLeave(object sender, EventArgs e) {
            MouseCursorX = -1;
            MouseCursorY = -1;
            SelectedScope = -1;
            if (null != mMouseElm) {
                mMouseElm.SetMouseElm(false);
                mMouseElm = null;
            }
        }

        private void picScope_Click(object sender, EventArgs e) {
            var ev = (MouseEventArgs)e;
            switch (ev.Button) {
            case MouseButtons.Right:
                Scope.MenuScope = -1;
                Scope.MenuPlotWave = -1;
                if (SelectedScope != -1) {
                    if (Scope.List[SelectedScope].CanMenu) {
                        Scope.MenuScope = SelectedScope;
                        Scope.MenuPlotWave = Scope.List[SelectedScope].SelectedPlot;
                        mScopePopupMenu.Show(Left + MouseCursorX, Top + MouseCursorY, Scope.List, SelectedScope, false);
                    }
                }
                break;
            }
        }

        private void picScope_DoubleClick(object sender, EventArgs e) {
            Scope.MenuScope = -1;
            Scope.MenuPlotWave = -1;
            if (SelectedScope != -1) {
                if (Scope.List[SelectedScope].CanMenu) {
                    var ev = (MouseEventArgs)e;
                    Scope.MenuScope = SelectedScope;
                    var scope = Scope.List[SelectedScope];
                    Scope.MenuPlotWave = scope.SelectedPlot;
                    scope.Properties(ev.X + Left, ev.Y + Top);
                }
            }
        }

        void SelectElm() {
            BaseUI selectElm = null;
            for (int i = 0; i != Scope.Count; i++) {
                var s = Scope.List[i];
                if (s.BoundingBox.Contains(MouseCursorX, MouseCursorY)) {
                    selectElm = s.UI;
                    SelectedScope = i;
                    break;
                }
            }
            if (null == selectElm) {
                if (null != mMouseElm) {
                    mMouseElm.SetMouseElm(false);
                    mMouseElm = null;
                }
            } else {
                if (selectElm != Mouse.GripElm) {
                    if (null != Mouse.GripElm) {
                        Mouse.GripElm.SetMouseElm(false);
                    }
                    if (null != mMouseElm) {
                        mMouseElm.SetMouseElm(false);
                    }
                    selectElm.SetMouseElm(true);
                    Mouse.GripElm = selectElm;
                    mMouseElm = selectElm;
                }
            }
        }

        void SetGraphics() {
            if (picScope.Width != mWidth || picScope.Height != mHeight) {
                if (null != mG) {
                    mG.Dispose();
                    mG = null;
                }
                if (picScope.Image != null) {
                    picScope.Image.Dispose();
                    picScope.Image = null;
                }
                picScope.Width = mWidth;
                picScope.Height = mHeight;
                var bmp = new Bitmap(mWidth, mHeight);
                mG = new CustomGraphics(bmp);
            }
        }

        void Flush() {
            if (null != mBmp || null != mContext) {
                if (null == mContext) {
                    mBmp.Dispose();
                    mBmp = null;
                } else {
                    mContext.Dispose();
                    mContext = null;
                }
            }
            mBmp = new Bitmap(mG.Width, mG.Height);
            mContext = Graphics.FromImage(mBmp);
            mG.CopyTo(mContext);
            picScope.Image = mBmp;
        }

        public void Draw(CustomGraphics pdf) {
            SelectElm();
            SetGraphics();
            CustomGraphics g;
            if (null == pdf) {
                g = mG;
            } else {
                g = pdf;
            }

            g.Clear(ControlPanel.ChkPrintable.Checked ? Color.White : Color.Black);
            Scope.Setup(g);

            var ct = Scope.Count;
            if (Circuit.StopMessage != null) {
                ct = 0;
            }
            for (int i = 0; i != ct; i++) {
                Scope.List[i].Draw(g);
            }

            if (Circuit.StopMessage != null) {
                g.DrawLeftText(Circuit.StopMessage, 10, -10);
            } else {
                var info = new string[10];
                info[0] = "時間：" + Utils.TimeText(Circuit.Time);
                info[1] = "単位：" + Utils.TimeText(ControlPanel.TimeStep);
                int x;
                if (ct == 0) {
                    x = 0;
                } else {
                    x = Scope.List[ct - 1].RightEdge + 4;
                }
                for (int i = 0; i < info.Length && info[i] != null; i++) {
                    g.DrawElementText(info[i], x, 15 * (i + 1));
                }
            }

            Flush();
        }
    }
}
