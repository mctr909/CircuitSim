using System;
using System.Collections.Generic;

namespace Circuit.Elements {
    class DiodeModel : Editable {
        static Dictionary<string, DiodeModel> modelMap;

        int flags;
        public string name { get; private set; }
        string description;
        public double saturationCurrent;
        public double seriesResistance { get; private set; }
        public double emissionCoefficient { get; private set; }
        public double breakdownVoltage { get; private set; }
        public bool dumped { get; private set; }
        public bool readOnly { get; private set; }
        public bool builtIn { get; private set; }
        public bool oldStyle { get; private set; }

        /* Electron thermal voltage at SPICE's default temperature of 27 C (300.15 K): */
        const double vt = 0.025865;
        /* The diode's "scale voltage", the voltage increase which will raise current by a factor of e. */
        public double vscale { get; private set; }
        /* The multiplicative equivalent of dividing by vscale (for speed). */
        public double vdcoef { get; private set; }
        /* voltage drop @ 1A */
        public double fwdrop { get; private set; }

        protected DiodeModel(double sc, double sr, double ec, double bv, string d) {
            saturationCurrent = sc;
            seriesResistance = sr;
            emissionCoefficient = ec;
            breakdownVoltage = bv;
            description = d;
            /*Console.WriteLine("creating diode model " + this);
            CirSim.debugger(); */
            updateModel();
        }

        static DiodeModel getModelWithName(string name) {
            createModelMap();
            var lm = modelMap[name];
            if (lm != null) {
                return lm;
            }
            lm = new DiodeModel();
            lm.name = name;
            modelMap.Add(name, lm);
            return lm;
        }

        public static DiodeModel getModelWithNameOrCopy(string name, DiodeModel oldmodel) {
            createModelMap();
            var lm = modelMap[name];
            if (lm != null) {
                return lm;
            }
            if (oldmodel == null) {
                Console.WriteLine("model not found: " + name);
                return getDefaultModel();
            }
            /*Console.WriteLine("copying to " + name); */
            lm = new DiodeModel(oldmodel);
            lm.name = name;
            modelMap.Add(name, lm);
            return lm;
        }

        static void createModelMap() {
            if (modelMap != null) {
                return;
            }
            modelMap = new  Dictionary<string, DiodeModel>();
            addDefaultModel("spice-default", new DiodeModel(1e-14, 0, 1, 0, null));
            addDefaultModel("default", new DiodeModel(1.7143528192808883e-7, 0, 2, 0, null));
            addDefaultModel("default-zener", new DiodeModel(1.7143528192808883e-7, 0, 2, 5.6, null));

            /* old default LED with saturation current that is way too small (causes numerical errors) */
            addDefaultModel("old-default-led", new DiodeModel(2.2349907006671927e-18, 0, 2, 0, null));

            /* default for newly created LEDs, https://www.diyaudio.com/forums/software-tools/25884-spice-models-led.html */
            addDefaultModel("default-led", new DiodeModel(93.2e-12, .042, 3.73, 0, null));

            /* https://www.allaboutcircuits.com/textbook/semiconductors/chpt-3/spice-models/ */
            addDefaultModel("1N5711", new DiodeModel(315e-9, 2.8, 2.03, 70, "Schottky"));
            addDefaultModel("1N5712", new DiodeModel(680e-12, 12, 1.003, 20, "Schottky"));
            addDefaultModel("1N34", new DiodeModel(200e-12, 84e-3, 2.19, 60, "germanium"));
            addDefaultModel("1N4004", new DiodeModel(18.8e-9, 28.6e-3, 2, 400, "general purpose"));

            /* http://users.skynet.be/hugocoolens/spice/diodes/1n4148.htm */
            addDefaultModel("1N4148", new DiodeModel(4.352e-9, .6458, 1.906, 75, "switching"));
        }

        static void addDefaultModel(String name, DiodeModel dm) {
            modelMap.Add(name, dm);
            dm.readOnly = dm.builtIn = true;
            dm.name = name;
        }

        /* create a new model using given parameters, keeping backward compatibility.
         * The method we use has problems, as explained below, but we don't want to
         * change circuit behavior */
        public static DiodeModel getModelWithParameters(double fwdrop, double zvoltage) {
            createModelMap();

            const double emcoef = 2;

            /* look for existing model with same parameters */
            foreach (var it in modelMap) {
                var val = it.Value;
                if (Math.Abs(val.fwdrop - fwdrop) < 1e-8
                    && val.seriesResistance == 0
                    && Math.Abs(val.breakdownVoltage - zvoltage) < 1e-8 && val.emissionCoefficient == emcoef) {
                    return val;
                }
            }

            /* create a new one, converting to new parameter values */
            const double vscale = emcoef * vt;
            const double vdcoef = 1 / vscale;
            double leakage = 1 / (Math.Exp(fwdrop * vdcoef) - 1);
            string name = "fwdrop=" + fwdrop;
            if (zvoltage != 0) {
                name = name + " zvoltage=" + zvoltage;
            }
            var dm = getModelWithName(name);
            /*Console.WriteLine("got model with name " + name); */
            dm.saturationCurrent = leakage;
            dm.emissionCoefficient = emcoef;
            dm.breakdownVoltage = zvoltage;
            dm.readOnly = dm.oldStyle = true;
            /*Console.WriteLine("at drop current is " + (leakage*(Math.Exp(fwdrop*vdcoef)-1)));
            Console.WriteLine("sat " + leakage + " em " + emcoef); */
            dm.updateModel();
            return dm;
        }

        /* create a new model using given fwdrop, using older method (pre-Aug 2017) 
         * that keeps a constant leakage current, but changes the emission coefficient.
         * We discovered that changing the leakage current to get a given fwdrop does not work well;
         * the leakage currents can be way too high or low. */
        public static DiodeModel getModelWithVoltageDrop(double fwdrop) {
            createModelMap();

            /* look for existing model with same parameters */
            foreach (var it in modelMap) {
                var val = it.Value;
                if (Math.Abs(val.fwdrop - fwdrop) < 1e-8 && Math.Abs(val.breakdownVoltage) < 1e-8) {
                    return val;
                }
            }

            /* create a new one, converting to new parameter values */
            double leakage = 100e-9;
            double vdcoef = Math.Log(1 / leakage + 1) / fwdrop;
            double emcoef = 1 / (vdcoef * vt);
            string name = "fwdrop=" + fwdrop;
            var dm = getModelWithName(name);
            /*Console.WriteLine("got model with name " + name); */
            dm.saturationCurrent = leakage;
            dm.emissionCoefficient = emcoef;
            dm.breakdownVoltage = 0;
            dm.updateModel();
            return dm;
        }

        /* create a new model using given zener voltage, otherwise the same as default */
        public static DiodeModel getZenerModel(double zvoltage) {
            createModelMap();

            /* look for existing model with same parameters */
            foreach (var it in modelMap) {
                var val = it.Value;
                if (Math.Abs(val.breakdownVoltage - zvoltage) < 1e-8) {
                    return val;
                }
            }

            /* create a new one from default */
            var dd = getModelWithName("default");

            string name = "zvoltage=" + zvoltage;
            var dm = getModelWithName(name);
            dm.saturationCurrent = dd.saturationCurrent;
            dm.emissionCoefficient = dd.emissionCoefficient;
            dm.breakdownVoltage = zvoltage;
            dm.updateModel();
            return dm;
        }

        public static DiodeModel getDefaultModel() {
            return getModelWithName("default");
        }

        public static void clearDumpedFlags() {
            if (modelMap == null) {
                return;
            }
            foreach (var k in modelMap.Keys) {
                modelMap[k].dumped = false;
            }
        }

        public static List<DiodeModel> getModelList(bool zener) {
            var vector = new List<DiodeModel>();
            foreach (var it in modelMap) {
                var dm = it.Value;
                if (zener && dm.breakdownVoltage == 0) {
                    continue;
                }
                vector.Add(dm);
            }
            return vector;
        }

        public int compareTo(DiodeModel dm) {
            return name.CompareTo(dm.name);
        }

        public string getDescription() {
            if (description == null) {
                return name;
            }
            return name + " (" + description + ")";
        }

        DiodeModel() {
            saturationCurrent = 1e-14;
            seriesResistance = 0;
            emissionCoefficient = 1;
            breakdownVoltage = 0;
            updateModel();
        }

        DiodeModel(DiodeModel copy) {
            flags = copy.flags;
            saturationCurrent = copy.saturationCurrent;
            seriesResistance = copy.seriesResistance;
            emissionCoefficient = copy.emissionCoefficient;
            breakdownVoltage = copy.breakdownVoltage;
            updateModel();
        }

        public static void undumpModel(StringTokenizer st) {
            string name = CustomLogicModel.unescape(st.nextToken());
            var dm = getModelWithName(name);
            dm.undump(st);
        }

        void undump(StringTokenizer st) {
            flags = st.nextTokenInt();
            saturationCurrent = st.nextTokenDouble();
            seriesResistance = st.nextTokenDouble();
            emissionCoefficient = st.nextTokenDouble();
            breakdownVoltage = st.nextTokenDouble();
            updateModel();
        }

        public EditInfo GetEditInfo(int n) {
            if (n == 0) {
                return new EditInfo("Saturation Current", saturationCurrent, -1, -1);
            }
            if (n == 1) {
                return new EditInfo("Series Resistance", seriesResistance, -1, -1);
            }
            if (n == 2) {
                return new EditInfo(EditInfo.MakeLink("diodecalc.html", "Emission Coefficient"), emissionCoefficient, -1, -1);
            }
            if (n == 3) {
                return new EditInfo("Breakdown Voltage", breakdownVoltage, -1, -1);
            }
            return null;
        }

        public void SetEditValue(int n, EditInfo ei) {
            if (n == 0) {
                saturationCurrent = ei.Value;
            }
            if (n == 1) {
                seriesResistance = ei.Value;
            }
            if (n == 2) {
                emissionCoefficient = ei.Value;
            }
            if (n == 3) {
                breakdownVoltage = Math.Abs(ei.Value);
            }
            updateModel();
            CirSim.theSim.updateModels();
        }

        void updateModel() {
            vscale = emissionCoefficient * vt;
            vdcoef = 1 / vscale;
            fwdrop = Math.Log(1 / saturationCurrent + 1) * emissionCoefficient * vt;
        }

        public string dump() {
            dumped = true;
            return "34 " + CustomLogicModel.escape(name)
                + " " + flags
                + " " + saturationCurrent
                + " " + seriesResistance
                + " " + emissionCoefficient
                + " " + breakdownVoltage;
        }
    }
}
