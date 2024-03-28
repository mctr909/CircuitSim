namespace MainForm;

internal class ResistorElm : ElmBase {
	public double resistance;
	Point ps3, ps4;

	public ResistorElm(int xx, int yy) : base(xx, yy) {
		resistance = 1000;
	}

	public ResistorElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
		resistance = st.nextTokenDouble(1000);
	}

	public override int getDumpType() {
		return 'r';
	}

	public override string dump() {
		return base.dump() + " " + resistance;
	}

	public override void setPoints() {
		base.setPoints();
		calcLeads(32);
		ps3 = new Point();
		ps4 = new Point();
	}

	public override void draw(CustomGraphics g) {
		int segments = 16;
		int i;
		int ox = 0;
		// int hs = sim.euroResistorCheckItem.getState() ? 6 : 8;
		int hs = 6;
		var v1 = volts[0];
		var v2 = volts[1];
		setBbox(point1, point2, hs);
		draw2Leads(g);

		// double segf = 1./segments;
		var len = distance(lead1, lead2);
		g.save();
		g.setLineWidth(3.0f);
		g.transform(
			(float)((lead2.X - lead1.X) / len), (float)((lead2.Y - lead1.Y) / len),
			-(float)((lead2.Y - lead1.Y) / len), (float)((lead2.X - lead1.X) / len),
			lead1.X, lead1.Y
		);
		setPowerColor(g, true);
		if (!sim.euroResistorCheckItem.Checked) {
			g.beginPath();
			g.moveTo(0, 0);
			for (i = 0; i < 4; i++) {
				g.lineTo((1 + 4 * i) * (float)len / 16, hs);
				g.lineTo((3 + 4 * i) * (float)len / 16, -hs);
			}
			g.lineTo((float)len, 0);
			g.stroke();
		} else {
			g.strokeRect(0, -hs, (float)len, 2.0f * hs);
		}
		g.restore();
		if (sim.showValuesCheckItem.Checked) {
			var s = getShortUnitText(resistance, "");
			drawValues(g, s, hs + 2);
		}
		doDots(g);
		drawPosts(g);
	}

	public override void calculateCurrent() {
		current = (volts[0] - volts[1]) / resistance;
	}

	public override void stamp() {
		sim.stampResistor(nodes[0], nodes[1], resistance);
	}

	public override void getInfo(string[] arr) {
		arr[0] = "resistor";
		getBasicInfo(arr);
		arr[3] = "R = " + getUnitText(resistance, "Ω");
		arr[4] = "P = " + getUnitText(getPower(), "W");
	}

	public override string getScopeText(int v) {
		return $"resistor, {getUnitText(resistance, "Ω")}";
	}

	public override EditInfo? getEditInfo(int n) {
		if (n == 0)
			return new EditInfo("Resistance (ohms)", resistance, 0, 0);
		return null;
	}

	public override void setEditValue(int n, EditInfo ei) {
		if (ei.value > 0)
			resistance = ei.value;
	}

	public override int getShortcut() {
		return 'r';
	}

	public double getResistance() {
		return resistance;
	}

	public void setResistance(double r) {
		resistance = r;
	}
}
