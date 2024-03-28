namespace MainForm;

internal class InductorElm : ElmBase {
	Inductor ind;
	public double inductance;

	public InductorElm(int xx, int yy) : base(xx, yy) {
		ind = new Inductor(sim);
		inductance = 1;
		ind.setup(inductance, current, flags);
	}

	public InductorElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
		ind = new Inductor(sim);
		inductance = st.nextTokenDouble();
		current = st.nextTokenDouble();
		ind.setup(inductance, current, flags);
	}

	public override int getDumpType() {
		return 'l';
	}

	public override string dump() {
		return base.dump() + " " + inductance + " " + current;
	}

	public override void setPoints() {
		base.setPoints();
		calcLeads(32);
	}

	public override void draw(CustomGraphics g) {
		var v1 = volts[0];
		var v2 = volts[1];
		int hs = 8;
		setBbox(point1, point2, hs);
		draw2Leads(g);
		setPowerColor(g, false);
		drawCoil(g, 8, lead1, lead2, v1, v2);
		if (sim.showValuesCheckItem.Checked) {
			var s = getShortUnitText(inductance, "H");
			drawValues(g, s, hs);
		}
		doDots(g);
		drawPosts(g);
	}

	public override void reset() {
		current = volts[0] = volts[1] = curcount = 0;
		ind.reset();
	}

	public override void stamp() {
		ind.stamp(nodes[0], nodes[1]);
	}

	public override void startIteration() {
		ind.startIteration(volts[0] - volts[1]);
	}

	public override bool nonLinear() {
		return ind.nonLinear();
	}

	public override void calculateCurrent() {
		var voltdiff = volts[0] - volts[1];
		current = ind.calculateCurrent(voltdiff);
	}

	public override void doStep() {
		var voltdiff = volts[0] - volts[1];
		ind.doStep(voltdiff);
	}

	public override void getInfo(string[] arr) {
		arr[0] = "inductor";
		getBasicInfo(arr);
		arr[3] = "L = " + getUnitText(inductance, "H");
		arr[4] = "P = " + getUnitText(getPower(), "W");
	}

	public override EditInfo? getEditInfo(int n) {
		if (n == 0)
			return new EditInfo("Inductance (H)", inductance, 0, 0);
		if (n == 1) {
			return new EditInfo("Trapezoidal Approximation", ind.isTrapezoidal());
		}
		return null;
	}

	public override void setEditValue(int n, EditInfo ei) {
		if (n == 0 && ei.value > 0)
			inductance = ei.value;
		if (n == 1) {
			if (ei.checkbox.Checked)
				flags &= ~Inductor.FLAG_BACK_EULER;
			else
				flags |= Inductor.FLAG_BACK_EULER;
		}
		ind.setup(inductance, current, flags);
	}

	public override int getShortcut() {
		return 'L';
	}

	public double getInductance() {
		return inductance;
	}

	public void setInductance(double l) {
		inductance = l;
		ind.setup(inductance, current, flags);
	}
}
