using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using Circuit.Elements;

namespace Circuit {
    class EditCompositeModelDialog : Form {
        Panel mPnlV;
        PictureBox mCanvas;
        Bitmap mBmp;
        TextBox mModelNameTextBox = null;
        CustomCompositeChipElm mChip;
        CustomCompositeModel mModel;

        bool error;
        int mPostCount;
        double mScale;
        int mSelectedPin;
        bool mDragging;

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
            // TODO: createModel
            /*Collections.sort(model.extList, new Comparator<ExtListEntry>() {
                public int compare(ExtListEntry a, ExtListEntry b) {
                    return a.name.toLowerCase().compareTo(b.name.toLowerCase());
                }
            });*/
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
            Text = "Edit Subcircuit Model";

            mPnlV = new Panel();
            mPnlV.AutoSize = true;
            mPnlV.Controls.Add(new Label() { Text = "Drag the pins to the desired position" });

            mCanvas = new PictureBox() { Width = 400, Height = 400 };
            mBmp = new Bitmap(mCanvas.Width, mCanvas.Height);
            mCanvas.Image = mBmp;
            mPnlV.Controls.Add(mCanvas);

            mChip = new CustomCompositeChipElm(50, 50);
            mChip.X2 = 200;
            mChip.Y2 = 50;
            createPinsFromModel();

            if (mModel.Name == null) {
                mPnlV.Controls.Add(new Label() { Text = "Model Name" });
                mModelNameTextBox = new TextBox();
                mPnlV.Controls.Add(mModelNameTextBox);
            }

            var hp1 = new Panel();
            {
                hp1.AutoSize = true;
                int ofsY = 0;
                var lblW = new Label() { Top = ofsY, Text = "Width" };
                hp1.Controls.Add(lblW);
                ofsY += lblW.Height + 2;
                /* Width+ */
                var bwp = new Button() { Top = ofsY, Text = "+" };
                bwp.Click += new EventHandler((s, e) => {
                    adjustChipSize(1, 0);
                });
                hp1.Controls.Add(bwp);
                /* Width- */
                var bwm = new Button() { Top = ofsY, Left = bwp.Right + 4, Text = "-" };
                bwm.Click += new EventHandler((s, e) => {
                    adjustChipSize(-1, 0);
                });
                hp1.Controls.Add(bwm);
                ofsY += bwm.Height + 4;

                var lblH = new Label() { Top = ofsY, Text = "Height" };
                hp1.Controls.Add(lblH);
                ofsY += lblH.Height + 2;
                /* Height+ */
                var bhp = new Button() { Top = ofsY, Text = "+" };
                bhp.Click += new EventHandler((s, e) => {
                    adjustChipSize(0, 1);
                });
                hp1.Controls.Add(bhp);
                /* Height- */
                var bhm = new Button() { Top = ofsY, Left = bhp.Right + 4, Text = "-" };
                bhm.Click += new EventHandler((s, e) => {
                    adjustChipSize(0, -1);
                });
                hp1.Controls.Add(bhm);
                /* */
                mPnlV.Controls.Add(hp1);
            }

            var hp2 = new Panel();
            {
                hp2.AutoSize = true;
                /* OK */
                var okButton = new Button() { Text = "OK" };
                okButton.Click += new EventHandler((s, e) => {
                    if (mModelNameTextBox != null) {
                        string name = mModelNameTextBox.Text;
                        if (name.Length == 0) {
                            MessageBox.Show("Please enter a model name.");
                            return;
                        }
                        mModel.setName(CustomCompositeElm.lastModelName = name);
                    }
                    CirSim.Sim.UpdateModels();
                    CirSim.Sim.NeedAnalyze(); /* will get singular matrix if we don't do this */
                    closeDialog();
                });
                hp2.Controls.Add(okButton);
                /* Cancel */
                if (mModel.Name == null) {
                    var cancelButton = new Button() { Left = okButton.Right + 4, Text = "Cancel" };
                    cancelButton.Click += new EventHandler((s, e) => {
                        closeDialog();
                    });
                    hp2.Controls.Add(cancelButton);
                }
                /* */
                mPnlV.Controls.Add(hp2);
            }

            MouseDown += new MouseEventHandler((s, e) => { onMouseDown(e); });
            MouseUp += new MouseEventHandler((s, e) => { onMouseUp(e); });
            MouseMove += new MouseEventHandler((s, e) => { onMouseMove(e); });

            Controls.Add(mPnlV);
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
                    drawChip();
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
                drawChip();
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
                mChip.Volts[i] = 0;
                if (i == mSelectedPin) {
                    mChip.pins[i].selected = true;
                }
            }
            mChip.SetPoints();
        }

        void drawChip() {
            var g = CustomGraphics.FromImage(mBmp);
            double scalew = g.Width / (double)(mChip.BoundingBox.Width + mChip.BoundingBox.X * 2);
            double scaleh = g.Height / (double)(mChip.BoundingBox.Height + mChip.BoundingBox.Y * 2);
            mScale = 1 / Math.Min(scalew, scaleh);
            g.Clear(ControlPanel.ChkPrintable.Checked ? Color.White : Color.Black);
            g.FillRectangle(Brushes.Blue, 0, 0, g.Width, g.Height);
            g.SetTransform(new Matrix((float)(1 / mScale), 0, 0, (float)(1 / mScale), 0, 0));
            mChip.Draw(g);
            if (null != mCanvas.Image) {
                mCanvas.Image.Dispose();
                mCanvas.Image = null;
            }
            mCanvas.Image = mBmp;
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
            drawChip();
        }
    }
}
