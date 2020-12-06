using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using Circuit.Elements;

namespace Circuit {
    class EditCompositeModelDialog : Form {
        Panel vp;
        bool error;
        CustomCompositeChipElm chip;
        int postCount;
        PictureBox canvas;
        Bitmap context;
        CustomCompositeModel model;
        TextBox modelNameTextBox = null;

        double scale;
        int selectedPin;
        bool dragging;

        void onMouseOver(EventArgs e) { }

        void onMouseOut(EventArgs e) { }

        void onMouseUp(MouseEventArgs e) {
            dragging = false;
        }

        void onMouseMove(MouseEventArgs e) {
            if (dragging) {
                if (selectedPin < 0) {
                    return;
                }
                var pos = new int[2];
                if (chip.getPinPos((int)(e.X * scale), (int)(e.Y * scale), selectedPin, pos)) {
                    var p = model.extList[selectedPin];
                    p.pos = pos[0];
                    p.side = pos[1];
                    createPinsFromModel();
                    drawChip();
                }
            } else {
                int i;
                double bestdist = 20;
                selectedPin = -1;
                for (i = 0; i != postCount; i++) {
                    var p = chip.pins[i];
                    int dx = (int)(e.X * scale) - p.textloc.X;
                    int dy = (int)(e.Y * scale) - p.textloc.Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist < bestdist) {
                        bestdist = dist;
                        selectedPin = i;
                    }
                    p.selected = false;
                }
                if (selectedPin >= 0) {
                    chip.pins[selectedPin].selected = true;
                }
                drawChip();
            }
        }

        void onMouseDown(MouseEventArgs e) {
            dragging = true;
        }

        public void setModel(CustomCompositeModel m) { model = m; }

        public bool createModel() {
            var nodeSet = new List<int>();
            model = CirSim.theSim.getCircuitAsComposite();
            if (model == null) {
                return false;
            }
            if (model.extList.Count == 0) {
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
            int postCount = model.extList.Count;

            model.sizeX = 2;
            model.sizeY = (postCount + 1) / 2;
            for (i = 0; i != postCount; i++) {
                bool left = i < model.sizeY;
                int side = (left) ? ChipElm.SIDE_W : ChipElm.SIDE_E;
                var pin = model.extList[i];
                pin.pos = left ? i : i - model.sizeY;
                pin.side = side;
                if (nodeSet.Contains(pin.node)) {
                    MessageBox.Show("Can't have two input/output nodes connected!");
                    return false;
                }
                nodeSet.Add(pin.node);
            }
            return true;
        }

        public void createDialog() {
            Text = "Edit Subcircuit Model";

            vp = new Panel();
            vp.AutoSize = true;
            vp.Controls.Add(new Label() { Text = "Drag the pins to the desired position" });

            canvas = new PictureBox() { Width = 400, Height = 400 };
            context = new Bitmap(canvas.Width, canvas.Height);
            canvas.Image = context;
            vp.Controls.Add(canvas);

            chip = new CustomCompositeChipElm(50, 50);
            chip.X2 = 200;
            chip.Y2 = 50;
            createPinsFromModel();

            if (model.name == null) {
                vp.Controls.Add(new Label() { Text = "Model Name" });
                modelNameTextBox = new TextBox();
                vp.Controls.Add(modelNameTextBox);
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
                vp.Controls.Add(hp1);
            }

            var hp2 = new Panel();
            {
                hp2.AutoSize = true;
                /* OK */
                var okButton = new Button() { Text = "OK" };
                okButton.Click += new EventHandler((s, e) => {
                    if (modelNameTextBox != null) {
                        string name = modelNameTextBox.Text;
                        if (name.Length == 0) {
                            MessageBox.Show("Please enter a model name.");
                            return;
                        }
                        model.setName(CustomCompositeElm.lastModelName = name);
                    }
                    CirSim.theSim.updateModels();
                    CirSim.theSim.needAnalyze(); /* will get singular matrix if we don't do this */
                    closeDialog();
                });
                hp2.Controls.Add(okButton);
                /* Cancel */
                if (model.name == null) {
                    var cancelButton = new Button() { Left = okButton.Right + 4, Text = "Cancel" };
                    cancelButton.Click += new EventHandler((s, e) => {
                        closeDialog();
                    });
                    hp2.Controls.Add(cancelButton);
                }
                /* */
                vp.Controls.Add(hp2);
            }

            MouseDown += new MouseEventHandler((s, e) => { onMouseDown(e); });
            MouseUp += new MouseEventHandler((s, e) => { onMouseUp(e); });
            MouseMove += new MouseEventHandler((s, e) => { onMouseMove(e); });
            MouseLeave += new EventHandler((s, e) => { onMouseOut(e); });
            MouseHover += new EventHandler((s, e) => { onMouseOver(e); });

            Controls.Add(vp);
        }

        void createPinsFromModel() {
            postCount = model.extList.Count;
            chip.allocPins(postCount);
            chip.sizeX = model.sizeX;
            chip.sizeY = model.sizeY;
            for (int i = 0; i != postCount; i++) {
                var pin = model.extList[i];
                chip.setPin(i, pin.pos, pin.side, pin.name);
                chip.Volts[i] = 0;
                if (i == selectedPin) {
                    chip.pins[i].selected = true;
                }
            }
            chip.SetPoints();
        }

        void drawChip() {
            var g = CustomGraphics.FromImage(context);
            double scalew = context.Width / (double)(chip.BoundingBox.Width + chip.BoundingBox.X * 2);
            double scaleh = context.Height / (double)(chip.BoundingBox.Height + chip.BoundingBox.Y * 2);
            scale = 1 / Math.Min(scalew, scaleh);
            g.Clear(CirSim.theSim.getBackgroundColor());
            g.FillRectangle(Color.Blue, 0, 0, context.Width, context.Height);
            g.SetTransform(new Matrix((float)(1 / scale), 0, 0, (float)(1 / scale), 0, 0));
            chip.Draw(g);
            if (null != canvas.Image) {
                canvas.Image.Dispose();
                canvas.Image = null;
            }
            canvas.Image = context;
        }

        void adjustChipSize(int dx, int dy) {
            if (dx < 0 || dy < 0) {
                for (int i = 0; i != postCount; i++) {
                    var p = chip.pins[i];
                    if (dx < 0 && (p.side == ChipElm.SIDE_N || p.side == ChipElm.SIDE_S) && p.pos >= chip.sizeX + dx) {
                        return;
                    }
                    if (dy < 0 && (p.side == ChipElm.SIDE_E || p.side == ChipElm.SIDE_W) && p.pos >= chip.sizeY + dy) {
                        return;
                    }
                }
            }
            if (chip.sizeX + dx < 1 || chip.sizeY + dy < 1) {
                return;
            }
            model.sizeX += dx;
            model.sizeY += dy;
            createPinsFromModel();
            drawChip();
        }

        protected void closeDialog() {
            Close();
        }
    }
}
