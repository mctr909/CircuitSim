namespace MainForm;

internal class RailElm : VoltageElm {
	public RailElm(int xx, int yy) : base(xx, yy, WF_DC) {
	}

	RailElm(int xx, int yy, int wf) : base(xx, yy, wf) {
	}

	public RailElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f, st) {
	}

	const int FLAG_CLOCK = 1;

	public override int getDumpType() {
		return 'R';
	}

	public override int getPostCount() {
		return 1;
	}

	public override void setPoints() {
		base.setPoints();
		lead1 = interpPoint(point1, point2, 1 - circleSize / dn);
	}

	string getRailText() {
		return null;
	}

	public override void draw(CustomGraphics g) {
		var rt = getRailText();
		var w = rt == null ? circleSize : g.measureText(rt).Width / 2.0;
		if (w > dn * .8)
			w = dn * .8;
		lead1 = interpPoint(point1, point2, 1 - w / dn);
		setBbox(point1, point2, circleSize);
		setVoltageColor(g, volts[0]);
		drawThickLine(g, point1, lead1);
		drawRail(g);
		drawPosts(g);
		curcount = updateDotCount(-current, curcount);
		if (sim.dragElm != this)
			drawDots(g, point1, lead1, curcount);
	}

	void drawRail(CustomGraphics g) {
		if (waveform == WF_SQUARE && (flags & FLAG_CLOCK) != 0)
			drawRailText(g, "CLK");
		else if (waveform == WF_DC || waveform == WF_VAR) {
			g.setColor(needsHighlight() ? selectColor : whiteColor);
			setPowerColor(g, false);
			var v = getVoltage();
			string s;
			if (Math.Abs(v) < 1)
				s = v.ToString(showFormat) + " V";
			else
				s = getShortUnitText(v, "V");
			if (getVoltage() > 0)
				s = "+" + s;
			drawCenteredText(g, s, x2, y2, true);
		} else {
			drawWaveform(g, point2);
		}
	}

	void drawRailText(CustomGraphics g, string s) {
		g.setColor(needsHighlight() ? selectColor : whiteColor);
		setPowerColor(g, false);
		drawCenteredText(g, s, x2, y2, true);
	}

	public override double getVoltageDiff() {
		return volts[0];
	}

	public override void stamp() {
		if (waveform == WF_DC)
			sim.stampVoltageSource(0, nodes[0], voltSource, getVoltage());
		else
			sim.stampVoltageSource(0, nodes[0], voltSource);
	}

	public override void doStep() {
		if (waveform != WF_DC)
			sim.updateVoltageSource(0, nodes[0], voltSource, getVoltage());
	}

	public override bool hasGroundConnection(int n1) {
		return true;
	}

	public override int getShortcut() {
		return 'V';
	}
}
