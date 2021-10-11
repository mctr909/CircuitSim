using System;
using System.Drawing;

namespace Circuit.Elements.Active {
    class ZenerElm : DiodeElm {
        static string lastZenerModelName = "default-zener";
        const double default_zvoltage = 5.6;

        const int hs = 6;
        Point[] poly;
        Point[] cathode;
        Point[] wing;

        public ZenerElm(Point pos) : base(pos) {
            modelName = lastZenerModelName;
            setup();
        }

        public ZenerElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            if ((f & FLAG_MODEL) == 0) {
                double zvoltage = st.nextTokenDouble();
                model = DiodeModel.getModelWithParameters(model.fwdrop, zvoltage);
                modelName = model.name;
                Console.WriteLine("model name wparams = " + modelName);
            }
            setup();
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.INVALID; } }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(12);
            cathode = new Point[2];
            wing = new Point[2];
            var pa = new Point[2];
            interpLeadAB(ref pa[0], ref pa[1], 0, hs);
            interpLeadAB(ref cathode[0], ref cathode[1], 1, hs);
            Utils.InterpPoint(cathode[0], cathode[1], ref wing[0], -0.2, -hs);
            Utils.InterpPoint(cathode[1], cathode[0], ref wing[1], -0.2, -hs);
            poly = new Point[] { pa[0], pa[1], mLead2 };
        }

        public override void Draw(CustomGraphics g) {
            setBbox(mPoint1, mPoint2, hs);

            double v2 = Volts[1];

            draw2Leads(g);

            /* draw arrow thingy */
            drawVoltage(g, 0, poly);
            /* draw thing arrow is pointing to */
            g.ThickLineColor = getVoltageColor(v2);
            g.DrawThickLine(cathode[0], cathode[1]);
            /* draw wings on cathode */
            g.DrawThickLine(wing[0], cathode[0]);
            g.DrawThickLine(wing[1], cathode[1]);

            doDots(g);
            drawPosts(g);
        }

        public override void GetInfo(string[] arr) {
            base.GetInfo(arr);
            arr[0] = "Zener diode";
            arr[5] = "Vz = " + Utils.VoltageText(model.breakdownVoltage);
        }

        void setLastModelName(string n) {
            lastZenerModelName = n;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 2) {
                var val = new InputDialog("Breakdown Voltage", "5.6");
                try {
                    double zvoltage = double.Parse(val.Value);
                    zvoltage = Math.Abs(zvoltage);
                    if (zvoltage > 0) {
                        model = DiodeModel.getZenerModel(zvoltage);
                        modelName = model.name;
                        ei.NewDialog = true;
                        return;
                    }
                } catch { }
            }
            base.SetElementValue(n, ei);
        }
    }
}
