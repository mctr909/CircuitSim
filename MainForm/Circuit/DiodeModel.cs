namespace Circuit {
	class DiodeModel {
		/* Electron thermal voltage at SPICE's default temperature of 27 C (300.15 K): */
		const double VT = 0.025865;

		static Dictionary<string, DiodeModel> mModelMap;
		string mDescription;

		public string Name { get; private set; }
		public double SaturationCurrent { get; private set; }
		public double SeriesResistance { get; private set; }
		public double EmissionCoefficient { get; private set; }
		public double BreakdownVoltage { get; private set; }

		/* The diode's "scale voltage", the voltage increase which will raise current by a factor of e. */
		public double VScale { get; private set; }
		/* The multiplicative equivalent of dividing by vscale (for speed). */
		public double VdCoef { get; private set; }
		/* voltage drop @ 1A */
		public double FwDrop { get; private set; }

		protected DiodeModel(double sc, double sr, double ec, double bv, string d) {
			SaturationCurrent = sc;
			SeriesResistance = sr;
			EmissionCoefficient = ec;
			BreakdownVoltage = bv;
			mDescription = d;
			updateModel();
		}

		public static DiodeModel GetModelWithNameOrCopy(string name, DiodeModel oldmodel) {
			createModelMap();
			var lm = mModelMap[name];
			if (lm != null) {
				return lm;
			}
			if (oldmodel == null) {
				Console.WriteLine("model not found: " + name);
				return GetDefaultModel();
			}
			lm = new DiodeModel(oldmodel) {
				Name = name
			};
			mModelMap.Add(name, lm);
			return lm;
		}

		/* create a new model using given parameters, keeping backward compatibility.
         * The method we use has problems, as explained below, but we don't want to
         * change circuit behavior */
		public static DiodeModel GetModelWithParameters(double fwdrop, double zvoltage) {
			createModelMap();

			const double emcoef = 2;

			/* look for existing model with same parameters */
			foreach (var it in mModelMap) {
				var val = it.Value;
				if (Math.Abs(val.FwDrop - fwdrop) < 1e-8
					&& val.SeriesResistance == 0
					&& Math.Abs(val.BreakdownVoltage - zvoltage) < 1e-8 && val.EmissionCoefficient == emcoef) {
					return val;
				}
			}

			/* create a new one, converting to new parameter values */
			const double vscale = emcoef * VT;
			const double vdcoef = 1 / vscale;
			var leakage = 1 / (Math.Exp(fwdrop * vdcoef) - 1);
			var name = "fwdrop=" + fwdrop;
			if (zvoltage != 0) {
				name = name + " zvoltage=" + zvoltage;
			}
			var dm = getModelWithName(name);
			/*Console.WriteLine("got model with name " + name); */
			dm.SaturationCurrent = leakage;
			dm.EmissionCoefficient = emcoef;
			dm.BreakdownVoltage = zvoltage;
			/*Console.WriteLine("at drop current is " + (leakage*(Math.Exp(fwdrop*vdcoef)-1)));
            Console.WriteLine("sat " + leakage + " em " + emcoef); */
			dm.updateModel();
			return dm;
		}

		/* create a new model using given zener voltage, otherwise the same as default */
		public static DiodeModel GetZenerModel(double zvoltage) {
			createModelMap();

			/* look for existing model with same parameters */
			foreach (var it in mModelMap) {
				var val = it.Value;
				if (Math.Abs(val.BreakdownVoltage - zvoltage) < 1e-8) {
					return val;
				}
			}

			/* create a new one from default */
			var dd = getModelWithName("default");

			string name = "zvoltage=" + zvoltage;
			var dm = getModelWithName(name);
			dm.SaturationCurrent = dd.SaturationCurrent;
			dm.EmissionCoefficient = dd.EmissionCoefficient;
			dm.BreakdownVoltage = zvoltage;
			dm.updateModel();
			return dm;
		}

		public static DiodeModel GetDefaultModel() {
			return getModelWithName("default");
		}

		public static List<DiodeModel> GetModelList(bool zener) {
			var vector = new List<DiodeModel>();
			foreach (var it in mModelMap) {
				var dm = it.Value;
				if (zener && dm.BreakdownVoltage == 0) {
					continue;
				}
				vector.Add(dm);
			}
			return vector;
		}

		static void createModelMap() {
			if (mModelMap != null) {
				return;
			}

			mModelMap = new Dictionary<string, DiodeModel>();
			addDefaultModel("spice-default", new DiodeModel(1e-14, 0, 1, 0, null));
			addDefaultModel("default", new DiodeModel(1.7143528192808883e-7, 0, 2, 0, null));
			addDefaultModel("default-zener", new DiodeModel(1.7143528192808883e-7, 0, 2, 5.6, null));
			addDefaultModel("default-led", new DiodeModel(93.2e-12, 0.042, 3.73, 0, null));

			/* https://www.allaboutcircuits.com/textbook/semiconductors/chpt-3/spice-models/ */
			addDefaultModel("1N5711", new DiodeModel(315e-9, 2.8, 2.03, 70, "Schottky"));
			addDefaultModel("1N5712", new DiodeModel(680e-12, 12, 1.003, 20, "Schottky"));
			addDefaultModel("1N34", new DiodeModel(200e-12, 84e-3, 2.19, 60, "germanium"));
			addDefaultModel("1N4004", new DiodeModel(18.8e-9, 28.6e-3, 2, 400, "general purpose"));

			/* http://users.skynet.be/hugocoolens/spice/diodes/1n4148.htm */
			addDefaultModel("1N4148", new DiodeModel(4.352e-9, 0.6458, 1.906, 75, "switching"));
		}

		static void addDefaultModel(string name, DiodeModel dm) {
			mModelMap.Add(name, dm);
			dm.Name = name;
		}

		static DiodeModel getModelWithName(string name) {
			createModelMap();
			var lm = mModelMap[name];
			if (lm != null) {
				return lm;
			}
			lm = new DiodeModel {
				Name = name
			};
			mModelMap.Add(name, lm);
			return lm;
		}

		void updateModel() {
			VScale = EmissionCoefficient * VT;
			VdCoef = 1 / VScale;
			FwDrop = Math.Log(1 / SaturationCurrent + 1) * EmissionCoefficient * VT;
		}

		DiodeModel() {
			SaturationCurrent = 1e-14;
			SeriesResistance = 0;
			EmissionCoefficient = 1;
			BreakdownVoltage = 0;
			updateModel();
		}

		DiodeModel(DiodeModel copy) {
			SaturationCurrent = copy.SaturationCurrent;
			SeriesResistance = copy.SeriesResistance;
			EmissionCoefficient = copy.EmissionCoefficient;
			BreakdownVoltage = copy.BreakdownVoltage;
			updateModel();
		}

		public string GetDescription() {
			if (mDescription == null) {
				return Name;
			}
			return Name + " (" + mDescription + ")";
		}
	}
}
