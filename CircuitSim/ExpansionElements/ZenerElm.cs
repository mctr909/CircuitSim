using System;
using System.Drawing;

namespace Circuit.Elements {
    class ZenerElm : DiodeElm {
        static string lastZenerModelName = "default-zener";
        const double default_zvoltage = 5.6;

        const int hs = 8;
        Point[] poly;
        Point[] cathode;
        Point[] wing;

        public ZenerElm(int xx, int yy) : base(xx, yy) {
            modelName = lastZenerModelName;
            setup();
        }

        public ZenerElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f, st) {
            if ((f & FLAG_MODEL) == 0) {
                double zvoltage = st.nextTokenDouble();
                model = DiodeModel.getModelWithParameters(model.fwdrop, zvoltage);
                modelName = model.name;
                Console.WriteLine("model name wparams = " + modelName);
            }
            setup();
        }

        public override void setPoints() {
            base.setPoints();
            calcLeads(16);
            cathode = newPointArray(2);
            wing = newPointArray(2);
            var pa = newPointArray(2);
            interpPoint(mLead1, mLead2, ref pa[0], ref pa[1], 0, hs);
            interpPoint(mLead1, mLead2, ref cathode[0], ref cathode[1], 1, hs);
            interpPoint(cathode[0], cathode[1], ref wing[0], -0.2, -hs);
            interpPoint(cathode[1], cathode[0], ref wing[1], -0.2, -hs);
            poly = createPolygon(pa[0], pa[1], mLead2).ToArray();
        }

        public override void draw(Graphics g) {
            setBbox(mPoint1, mPoint2, hs);

            double v1 = Volts[0];
            double v2 = Volts[1];

            draw2Leads(g);

            /* draw arrow thingy */
            fillPolygon(g, getVoltageColor(v1), poly);
            /* draw thing arrow is pointing to */
            PEN_THICK_LINE.Color = getVoltageColor(v2);
            drawThickLine(g, cathode[0], cathode[1]);
            /* draw wings on cathode */
            drawThickLine(g, wing[0], cathode[0]);
            drawThickLine(g, wing[1], cathode[1]);

            doDots(g);
            drawPosts(g);
        }

        public override void getInfo(string[] arr) {
            base.getInfo(arr);
            arr[0] = "Zener diode";
            arr[5] = "Vz = " + getVoltageText(model.breakdownVoltage);
        }

        public override DUMP_ID getShortcut() { return DUMP_ID.INVALID; }

        void setLastModelName(string n) {
            lastZenerModelName = n;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (n == 2) {
                var val = new InputDialog("Breakdown Voltage", "5.6");
                try {
                    double zvoltage = double.Parse(val.Value);
                    zvoltage = Math.Abs(zvoltage);
                    if (zvoltage > 0) {
                        model = DiodeModel.getZenerModel(zvoltage);
                        modelName = model.name;
                        ei.newDialog = true;
                        return;
                    }
                } catch { }
            }
            base.setEditValue(n, ei);
        }
    }
}
