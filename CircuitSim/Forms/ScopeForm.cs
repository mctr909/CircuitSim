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
            Instance.doPopupMenu(Left + MouseCursorX, Top + MouseCursorY);
        }

        void SelectElm() {
            BaseUI selectElm = null;
            for (int i = 0; i != Scope.Property.Count; i++) {
                var s = Scope.Property.List[i];
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
            Scope.Property.Setup(g);

            var ct = Scope.Property.Count;
            if (Circuit.StopMessage != null) {
                ct = 0;
            }
            for (int i = 0; i != ct; i++) {
                Scope.Property.List[i].Draw(g);
            }

            if (Circuit.StopMessage != null) {
                g.DrawLeftText(Circuit.StopMessage, 10, -10);
            } else {
                var info = new string[10];
                if (Mouse.GripElm != null) {
                    if (Mouse.Post == -1) {
                        Mouse.GripElm.GetInfo(info);
                    } else {
                        info[0] = "V = " + Mouse.GripElm.DispPostVoltage(Mouse.Post);
                    }
                } else {
                    info[0] = "t = " + Utils.TimeText(Circuit.Time);
                    info[1] = "time step = " + Utils.TimeText(ControlPanel.TimeStep);
                }

                /* count lines of data */
                {
                    int infoIdx;
                    for (infoIdx = 0; infoIdx < info.Length - 1 && info[infoIdx] != null; infoIdx++)
                        ;
                    int badnodes = Circuit.BadConnectionList.Count;
                    if (badnodes > 0) {
                        info[infoIdx++] = badnodes + ((badnodes == 1) ? " bad connection" : " bad connections");
                    }
                }

                int x = 0;
                if (ct != 0) {
                    x = Scope.Property.List[ct - 1].RightEdge + 20;
                }
                {
                    for (int i = 0; i < info.Length && info[i] != null; i++) {
                        g.DrawLeftText(info[i], x, 15 * (i + 1));
                    }
                }
            }

            Flush();
        }
    }
}
