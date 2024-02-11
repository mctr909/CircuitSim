using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using Circuit.Elements.Active;
using Circuit.Elements.Custom;

namespace Circuit {
    class EditCompositeModelDialog : Form {
        Panel mPnlV;
        PictureBox mCanvas;
        Bitmap mBmp;
        CustomGraphics mG;
        TextBox mModelNameTextBox = null;
        CustomCompositeChipElm mChip;
        CustomCompositeModel mModel;

        bool error;
        int mPostCount;
        double mScale;
        int mSelectedPin;
        private Timer timer1;
        private System.ComponentModel.IContainer components;
        bool mDragging;

        private void InitializeComponent() {
            components = new System.ComponentModel.Container();
            timer1 = new Timer(components);
            SuspendLayout();
            timer1.Tick += new EventHandler(timer1_Tick);
            ClientSize = new Size(274, 229);
            Name = "Edit Subcircuit Model";
            Text = "Edit Subcircuit Model";
            ResumeLayout(false);
        }

        public void SetModel(CustomCompositeModel m) { mModel = m; }

        public bool CreateModel() {
            var nodeSet = new List<int>();
            mModel = CirSim.Sim.GetCircuitAsComposite();
            if (mModel == null) {
                return false;
            }
            if (mModel.ExtList.Count == 0) {
                MessageBox.Show("Device has no external inputs/outputs!");
                return false;
            }
            mModel.ExtList.Sort((ExtListEntry a, ExtListEntry b) => {
                return a.name.ToLower().CompareTo(b.name.ToLower());
            });
            int i;
            int postCount = mModel.ExtList.Count;

            mModel.SizeX = 2;
            mModel.SizeY = (postCount + 1) / 2;
            for (i = 0; i != postCount; i++) {
                bool left = i < mModel.SizeY;
                int side = (left) ? ChipElm.SIDE_W : ChipElm.SIDE_E;
                var pin = mModel.ExtList[i];
                pin.pos = left ? i : i - mModel.SizeY;
                pin.side = side;
                if (nodeSet.Contains(pin.node)) {
                    MessageBox.Show("Can't have two input/output nodes connected!");
                    return false;
                }
                nodeSet.Add(pin.node);
            }
            return true;
        }

        public void CreateDialog() {
            InitializeComponent();

            mPnlV = new Panel();
            mPnlV.AutoSize = true;

            mCanvas = new PictureBox() { Width = 320, Height = 320 };
            mBmp = new Bitmap(mCanvas.Width, mCanvas.Height);
            mG = CustomGraphics.FromImage(mBmp);
            mPnlV.Controls.Add(mCanvas);

            mChip = new CustomCompositeChipElm(new Point(50, 50));
            mChip.P2.X = 200;
            mChip.P2.Y = 50;
            createPinsFromModel();

            var lbl = new Label() { Top = mCanvas.Bottom, AutoSize = true, Text = "Model Name" };
            mPnlV.Controls.Add(lbl);
            mModelNameTextBox = new TextBox() { Top = lbl.Bottom };
            mModelNameTextBox.Enabled = string.IsNullOrEmpty(mModel.Name);
            mModelNameTextBox.Text = mModel.Name;
            mPnlV.Controls.Add(mModelNameTextBox);

            var pnlSize = new Panel();
            {
                pnlSize.Top = mModelNameTextBox.Bottom;
                pnlSize.AutoSize = true;
                int ofsY = 0;
                var lblW = new Label() { Top = ofsY, AutoSize = true, Text = "Width" };
                pnlSize.Controls.Add(lblW);
                ofsY += lblW.Height;
                /* Width+ */
                var bwp = new Button() { Top = ofsY, Width = 40, Text = "+" };
                bwp.Click += new EventHandler((s, e) => {
                    adjustChipSize(1, 0);
                });
                pnlSize.Controls.Add(bwp);
                /* Width- */
                var bwm = new Button() { Top = ofsY, Width = 40, Left = bwp.Right + 4, Text = "-" };
                bwm.Click += new EventHandler((s, e) => {
                    adjustChipSize(-1, 0);
                });
                pnlSize.Controls.Add(bwm);
                ofsY += bwm.Height + 4;

                var lblH = new Label() { Top = ofsY, AutoSize = true, Text = "Height" };
                pnlSize.Controls.Add(lblH);
                ofsY += lblH.Height;
                /* Height+ */
                var bhp = new Button() { Top = ofsY, Width = 40, Text = "+" };
                bhp.Click += new EventHandler((s, e) => {
                    adjustChipSize(0, 1);
                });
                pnlSize.Controls.Add(bhp);
                /* Height- */
                var bhm = new Button() { Top = ofsY, Width = 40, Left = bhp.Right + 4, Text = "-" };
                bhm.Click += new EventHandler((s, e) => {
                    adjustChipSize(0, -1);
                });
                pnlSize.Controls.Add(bhm);
                /* */
                mPnlV.Controls.Add(pnlSize);
            }

            var pnlButton = new Panel();
            {
                pnlButton.Top = pnlSize.Bottom;
                pnlButton.AutoSize = true;
                /* OK */
                var okButton = new Button() { Text = "OK" };
                okButton.Click += new EventHandler((s, e) => {
                    if (mModelNameTextBox != null) {
                        string name = mModelNameTextBox.Text;
                        if (name.Length == 0) {
                            MessageBox.Show("Please enter a model name.");
                            return;
                        }
                        mModel.SetName(CustomCompositeElm.lastModelName = name);
                    }
                    CirSim.Sim.UpdateModels();
                    CirSim.Sim.NeedAnalyze(); /* will get singular matrix if we don't do this */
                    closeDialog();
                });
                pnlButton.Controls.Add(okButton);
                /* */
                mPnlV.Controls.Add(pnlButton);
            }

            mCanvas.MouseDown += new MouseEventHandler((s, e) => { onMouseDown(e); });
            mCanvas.MouseUp += new MouseEventHandler((s, e) => { onMouseUp(e); });
            mCanvas.MouseMove += new MouseEventHandler((s, e) => { onMouseMove(e); });

            Controls.Add(mPnlV);

            Width = mCanvas.Width + 16;
            Height = pnlButton.Bottom;

            timer1.Interval = 33;
            timer1.Enabled = true;
            timer1.Start();
        }

        void closeDialog() {
            Close();
        }

        void onMouseUp(MouseEventArgs e) {
            mDragging = false;
        }

        void onMouseMove(MouseEventArgs e) {
            if (mDragging) {
                if (mSelectedPin < 0) {
                    return;
                }
                var pos = new int[2];
                if (mChip.getPinPos((int)(e.X * mScale), (int)(e.Y * mScale), mSelectedPin, pos)) {
                    var p = mModel.ExtList[mSelectedPin];
                    p.pos = pos[0];
                    p.side = pos[1];
                    createPinsFromModel();
                }
            } else {
                int i;
                double bestdist = 20;
                mSelectedPin = -1;
                for (i = 0; i != mPostCount; i++) {
                    var p = mChip.pins[i];
                    int dx = (int)(e.X * mScale) - p.textloc.X;
                    int dy = (int)(e.Y * mScale) - p.textloc.Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist < bestdist) {
                        bestdist = dist;
                        mSelectedPin = i;
                    }
                    p.selected = false;
                }
                if (mSelectedPin >= 0) {
                    mChip.pins[mSelectedPin].selected = true;
                }
            }
        }

        void onMouseDown(MouseEventArgs e) {
            mDragging = true;
        }

        void createPinsFromModel() {
            mPostCount = mModel.ExtList.Count;
            mChip.allocPins(mPostCount);
            mChip.sizeX = mModel.SizeX;
            mChip.sizeY = mModel.SizeY;
            for (int i = 0; i != mPostCount; i++) {
                var pin = mModel.ExtList[i];
                mChip.setPin(i, pin.pos, pin.side, pin.name);
                mChip.CirVolts[i] = 0;
                if (i == mSelectedPin) {
                    mChip.pins[i].selected = true;
                }
            }
            mChip.SetPoints();
        }

        void drawChip() {
            if(null == mCanvas) {
                return;
            }
            double scalew = mG.Width / (double)(mChip.BoundingBox.Width + mChip.BoundingBox.X * 2);
            double scaleh = mG.Height / (double)(mChip.BoundingBox.Height + mChip.BoundingBox.Y * 2);
            mScale = 1 / Math.Min(scalew, scaleh);
            mG.Clear(Color.Blue);
            mG.SetTransform(new Matrix((float)(1 / mScale), 0, 0, (float)(1 / mScale), 0, 0));
            mChip.Draw(mG);

            if (null != mCanvas.Image) {
                mCanvas.Image.Dispose();
                mCanvas.Image = null;
            }
            var tmp = new Bitmap(mBmp.Width, mBmp.Height);
            var g = Graphics.FromImage(tmp);
            g.DrawImage(mBmp, 0, 0);
            mCanvas.Image = tmp;
        }

        void adjustChipSize(int dx, int dy) {
            if (dx < 0 || dy < 0) {
                for (int i = 0; i != mPostCount; i++) {
                    var p = mChip.pins[i];
                    if (dx < 0 && (p.side == ChipElm.SIDE_N || p.side == ChipElm.SIDE_S) && p.pos >= mChip.sizeX + dx) {
                        return;
                    }
                    if (dy < 0 && (p.side == ChipElm.SIDE_E || p.side == ChipElm.SIDE_W) && p.pos >= mChip.sizeY + dy) {
                        return;
                    }
                }
            }
            if (mChip.sizeX + dx < 1 || mChip.sizeY + dy < 1) {
                return;
            }
            mModel.SizeX += dx;
            mModel.SizeY += dy;
            createPinsFromModel();
        }

        void timer1_Tick(object sender, EventArgs arg) {
            drawChip();
        }
    }
}
