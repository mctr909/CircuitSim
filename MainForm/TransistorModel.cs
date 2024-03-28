namespace MainForm {
	internal class TransistorModel {
		static Dictionary<string, TransistorModel> modelMap;

		int flags;
		public string name, description;
		public double satCur, invRollOffF, BEleakCur, leakBEemissionCoeff, invRollOffR, BCleakCur, leakBCemissionCoeff;
		public double emissionCoeffF, emissionCoeffR, invEarlyVoltF, invEarlyVoltR, betaR;

		public bool dumped;
		public bool readOnly;
		public bool builtIn;

		TransistorModel(string d, double sc) {
			description = d;
			satCur = sc;
			emissionCoeffF = emissionCoeffR = 1;
			leakBEemissionCoeff = 1.5;
			leakBCemissionCoeff = 2;
			betaR = 1;
			updateModel();
		}

		static TransistorModel getModelWithName(string name) {
			createModelMap();
			if (modelMap.ContainsKey(name))
				return modelMap[name];
			var lm = new TransistorModel();
			lm.name = name;
			modelMap.Add(name, lm);
			return lm;
		}

		public static TransistorModel getModelWithNameOrCopy(string name, TransistorModel oldmodel) {
			createModelMap();
			if (modelMap.ContainsKey(name))
				return modelMap[name];
			if (oldmodel == null) {
				Console.WriteLine("model not found: " + name);
				return getDefaultModel();
			}
			var lm = new TransistorModel(oldmodel);
			lm.name = name;
			modelMap.Add(name, lm);
			return lm;
		}

		static void createModelMap() {
			if (modelMap != null)
				return;
			modelMap = [];
			addDefaultModel("default", new TransistorModel("default", 1e-13));
			addDefaultModel("spice-default", new TransistorModel("spice-default", 1e-16));
		}

		static void addDefaultModel(string name, TransistorModel dm) {
			modelMap.Add(name, dm);
			dm.readOnly = dm.builtIn = true;
			dm.name = name;
		}

		static TransistorModel getDefaultModel() {
			return getModelWithName("default");
		}

		static void clearDumpedFlags() {
			if (modelMap == null)
				return;
			foreach (var e in modelMap) {
				e.Value.dumped = false;
			}
		}

		public static List<TransistorModel> getModelList() {
			var vector = new List<TransistorModel>();
			foreach (var e in modelMap) {
				vector.Add(e.Value);
			}
			vector.Sort((a, b) => a.compareTo(b));
			return vector;
		}

		public int compareTo(TransistorModel dm) {
			return string.Compare(name, dm.name);
		}

		public string getDescription() {
			if (description == null)
				return name;
			return name + $" ({description})";
		}

		TransistorModel() {
			updateModel();
		}

		public TransistorModel(TransistorModel copy) {
			flags = copy.flags;
			satCur = copy.satCur;
			invRollOffF = copy.invRollOffF;
			BEleakCur = copy.BEleakCur;
			leakBEemissionCoeff = copy.leakBEemissionCoeff;
			invRollOffR = copy.invRollOffR;
			BCleakCur = copy.BCleakCur;
			leakBCemissionCoeff = copy.leakBCemissionCoeff;
			emissionCoeffF = copy.emissionCoeffF;
			emissionCoeffR = copy.emissionCoeffR;
			invEarlyVoltF = copy.invEarlyVoltF;
			invEarlyVoltR = copy.invEarlyVoltR;
			betaR = copy.betaR;
			updateModel();
		}

		static void undumpModel(StringTokenizer st) {
			var name = CustomLogicModel.unescape(st.nextToken());
			var dm = getModelWithName(name);
			dm.undump(st);
		}

		void undump(StringTokenizer st) {
			flags = st.nextTokenInt();

			satCur = st.nextTokenDouble();
			invRollOffF = st.nextTokenDouble();
			BEleakCur = st.nextTokenDouble();
			leakBEemissionCoeff = st.nextTokenDouble();
			invRollOffR = st.nextTokenDouble();
			BCleakCur = st.nextTokenDouble();
			leakBCemissionCoeff = st.nextTokenDouble();
			emissionCoeffF = st.nextTokenDouble();
			emissionCoeffR = st.nextTokenDouble();
			invEarlyVoltF = st.nextTokenDouble();
			invEarlyVoltR = st.nextTokenDouble();
			betaR = st.nextTokenDouble();

			updateModel();
		}

		public EditInfo? getEditInfo(int n) {
			if (n == 0) {
				var ei = new EditInfo("Model Name", 0);
				ei.text = name == null ? "" : name;
				return ei;
			}
			if (n == 1)
				return new EditInfo("Transport Saturation Current (IS)", satCur);
			if (n == 2)
				return new EditInfo("Reverse Beta (BR)", betaR);
			if (n == 3)
				return new EditInfo("Forward Early Voltage (VAF)", 1 / invEarlyVoltF);
			if (n == 4)
				return new EditInfo("Reverse Early Voltage (VAR)", 1 / invEarlyVoltR);
			if (n == 5)
				return new EditInfo("Corner For Forward Beta High Current Roll-Off (IKF)", 1 / invRollOffF);
			if (n == 6)
				return new EditInfo("Corner For Reverse Beta High Current Roll-Off (IKR)", 1 / invRollOffR);
			if (n == 7)
				return new EditInfo("Forward Current Emission Coefficient (NF)", emissionCoeffF);
			if (n == 8)
				return new EditInfo("Reverse Current Emission Coefficient (NR)", emissionCoeffR);
			if (n == 9)
				return new EditInfo("B-E Leakage Saturation Current (ISE)", BEleakCur);
			if (n == 10)
				return new EditInfo("B-E Leakage Emission Coefficient (NE)", leakBEemissionCoeff);
			if (n == 11)
				return new EditInfo("B-C Leakage Saturation Current (ISC)", BCleakCur);
			if (n == 12)
				return new EditInfo("B-C Leakage Emission Coefficient (NC)", leakBCemissionCoeff);
			return null;
		}

		public void setEditValue(int n, EditInfo ei) {
			if (n == 0) {
				name = ei.textf.Text;
				if (name.Length > 0)
					modelMap.Add(name, this);
			}
			if (n == 1)
				satCur = ei.value;
			if (n == 2)
				betaR = ei.value;
			if (n == 3)
				invEarlyVoltF = 1 / ei.value;
			if (n == 4)
				invEarlyVoltR = 1 / ei.value;
			if (n == 5)
				invRollOffF = 1 / ei.value;
			if (n == 6)
				invRollOffR = 1 / ei.value;
			if (n == 7)
				emissionCoeffF = ei.value;
			if (n == 8)
				emissionCoeffR = ei.value;
			if (n == 9)
				BEleakCur = ei.value;
			if (n == 10)
				leakBEemissionCoeff = ei.value;
			if (n == 11)
				BCleakCur = ei.value;
			if (n == 12)
				leakBCemissionCoeff = ei.value;
			updateModel();
			CirSim.theSim.updateModels();
		}

		void updateModel() {
		}

		public string dump() {
			dumped = true;
			return "32 " + CustomLogicModel.escape(name) + " " + flags + " " +
					satCur + " " + invRollOffF + " " + BEleakCur + " " + leakBEemissionCoeff + " " + invRollOffR + " " +
					BCleakCur + " " + leakBCemissionCoeff + " " + emissionCoeffF + " " + emissionCoeffR + " "
					+ invEarlyVoltF + " " + invEarlyVoltR + " " + betaR;
		}
	}
}
