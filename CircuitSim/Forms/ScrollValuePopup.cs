using System;
using System.Windows.Forms;

using Circuit.Elements;

namespace Circuit {
    class ScrollValuePopup : Form {
        static readonly double[] e24 = { 1.0, 1.1, 1.2, 1.3, 1.5, 1.6, 1.8, 2.0, 2.2, 2.4, 2.7, 3.0,
            3.3, 3.6, 3.9, 4.3, 4.7, 5.1, 5.6, 6.2, 6.8, 7.5, 8.2, 9.1 };
    
        const int labMax = 5;
        double[] values;
        int minpow = 0;
        int maxpow = 1;
        int nvalues;
        int currentidx;
        int lastidx;
        Panel vp;
        CircuitElm myElm;
        Label labels;
        TrackBar trbValue;
        int deltaY;
        string name;
        EditInfo inf;
        CirSim sim;
        string unit;

        public ScrollValuePopup(int dy, CircuitElm e, CirSim s) : base() {
            myElm = e;
            deltaY = 0;
            sim = s;
            sim.pushUndo();
            setupValues();

            Text = name;

            vp = new Panel();
            {
                vp.Left = 4;
                vp.Top = 4;
                int ofsY = 0;
                /* label */
                labels = new Label() { Text = "---" };
                labels.AutoSize = true;
                labels.Left = 4;
                labels.Top = ofsY;
                vp.Controls.Add(labels);
                ofsY += labels.Height;
                /* trbValue */
                trbValue = new TrackBar() {
                    Minimum = 0,
                    Maximum = nvalues - 1,
                    SmallChange = 1,
                    LargeChange = 1,
                    TickFrequency = nvalues / 24,
                    TickStyle = TickStyle.TopLeft,
                    Width = 300,
                    Height = 21,
                    Left = 4,
                    Top = ofsY
                };
                ofsY += trbValue.Height * 2 / 3;
                trbValue.ValueChanged += new EventHandler((sender, ev) => { setElmValue((TrackBar)sender); });
                vp.Width = trbValue.Width + 8;
                vp.Height = ofsY + 4;
                vp.Controls.Add(trbValue);
                /* */
                Controls.Add(vp);
            }

            doDeltaY(dy);
            Width = vp.Width + 24;
            Height = vp.Height + 48;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
        }

        public void Show(int x, int y) {
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Show();
            Left = x - Width / 2;
            Top = y - Height / 2;
            Visible = true;
        }

        void setupValues() {
            if (myElm is ResistorElm) {
                minpow = 0;
                maxpow = 6;
                unit = "Ω";
            }
            if (myElm is CapacitorElm) {
                minpow = -11;
                maxpow = -3;
                unit = "F";
            }
            if (myElm is InductorElm) {
                minpow = -6;
                maxpow = 0;
                unit = "H";
            }
            values = new double[2 + (maxpow - minpow) * 24];
            int ptr = 0;
            for (int i = minpow; i <= maxpow; i++) {
                for (int j = 0; j < ((i != maxpow) ? 24 : 1); j++, ptr++) {
                    values[ptr] = Math.Pow(10.0, i) * e24[j];
                }
            }
            nvalues = ptr;
            values[nvalues] = 1E99;
            inf = myElm.GetEditInfo(0);
            double currentvalue = inf.Value;
            for (int i = 0; i < nvalues + 1; i++) {
                if (CircuitElm.getShortUnitText(currentvalue, "") == CircuitElm.getShortUnitText(values[i], "")) { /* match to an existing value */
                    values[i] = currentvalue; /* Just in case it isn't 100% identical */
                    currentidx = i;
                    break;
                }
                if (currentvalue < values[i]) { /* overshot - need to insert value */
                    currentidx = i;
                    for (int j = nvalues - 1; j >= i; j--) {
                        values[j + 1] = values[j];
                    }
                    values[i] = currentvalue;
                    nvalues++;
                    break;
                }
            }
            name = inf.Name;
            lastidx = currentidx;
            /*for (int i = 0; i < nvalues; i++) {
                Console.WriteLine("i=" + i + " values=" + values[i] + " current? " + (i == currentidx));
            }*/
        }

        void setupLabels() {
            int thissel = getSelIdx();
            labels.Text = CircuitElm.getShortUnitText(values[thissel], unit);
            trbValue.Value = thissel;
        }

        public void close(bool keepChanges) {
            if (!keepChanges) {
                setElmValue(currentidx);
            } else {
                setElmValue();
            }
            Close();
        }

        public void onMouseDown(MouseEventArgs e) {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle) {
                close(true);
            } else {
                close(false);
            }
        }

        public void doDeltaY(int dy) {
            deltaY += dy;
            if (currentidx + deltaY / 3 < 0) {
                deltaY = -3 * currentidx;
            }
            if (currentidx + deltaY / 3 >= nvalues) {
                deltaY = (nvalues - currentidx - 1) * 3;
            }
            setElmValue();
            setupLabels();
        }

        public void setElmValue() {
            int idx = getSelIdx();
            setElmValue(idx);
        }

        public void setElmValue(TrackBar tr) {
            lastidx = currentidx;
            currentidx = tr.Value;
            int thissel = getSelIdx();
            inf.Value = values[thissel];
            myElm.SetEditValue(0, inf);
            sim.needAnalyze();
            labels.Text = CircuitElm.getShortUnitText(values[thissel], unit);
        }

        public void setElmValue(int i) {
            if (i != lastidx) {
                trbValue.Value = i;
                lastidx = i;
                inf.Value = values[i];
                myElm.SetEditValue(0, inf);
                sim.needAnalyze();
            }
        }

        public int getSelIdx() {
            int r;
            r = currentidx + deltaY / 3;
            if (r < 0) {
                r = 0;
            }
            if (r >= nvalues) {
                r = nvalues - 1;
            }
            return r;
        }
    }
}
