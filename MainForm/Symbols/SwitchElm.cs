namespace MainForm;

internal class SwitchElm : ElmBase {
	protected bool momentary;
	// position 0 == closed, position 1 == open
	protected int position, posCount;

	public SwitchElm(int xx, int yy) : base(xx, yy) {
		momentary = false;
		position = 0;
		posCount = 2;
	}

	protected SwitchElm(int xx, int yy, bool mm) : base(xx, yy) {
		position = (mm) ? 1 : 0;
		momentary = mm;
		posCount = 2;
	}

	public SwitchElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
		var str = st.nextToken();
		if (str == "true")
			position = (GetType() == typeof(LogicInputElm)) ? 0 : 1;
		else if (str == "false")
			position = (GetType() == typeof(LogicInputElm)) ? 1 : 0;
		else
			position = int.Parse(str);
		momentary = st.nextTokenBool(false);
		posCount = 2;
	}

	public override int getDumpType() {
		return 's';
	}

	public override string dump() {
		return base.dump() + " " + position + " " + momentary;
	}

	Point ps, ps2;

	public override void setPoints() {
		base.setPoints();
		calcLeads(32);
		ps = new Point();
		ps2 = new Point();
	}

	const int openhs = 16;

	public override void draw(CustomGraphics g) {
		var hs1 = (position == 1) ? 0 : 2;
		var hs2 = (position == 1) ? openhs : 2;
		setBbox(point1, point2, openhs);

		draw2Leads(g);

		if (position == 0)
			doDots(g);

		if (!needsHighlight())
			g.setColor(whiteColor);
		interpPoint(lead1, lead2, ref ps, 0, hs1);
		interpPoint(lead1, lead2, ref ps2, 1, hs2);

		drawThickLine(g, ps, ps2);
		drawPosts(g);
	}

	protected virtual Rectangle getSwitchRect() {
		interpPoint(lead1, lead2, ref ps, 0, openhs);
		var w = Math.Max(lead2.X, ps.X) - lead1.X + 1;
		var h = Math.Max(lead2.Y, ps.Y) - lead1.Y + 1;
		return new Rectangle(lead1.X, lead1.Y, w, h);
	}

	public override void calculateCurrent() {
		if (position == 1)
			current = 0;
	}

	public override void stamp() {
		if (position == 0)
			sim.stampVoltageSource(nodes[0], nodes[1], voltSource, 0);
	}

	public override int getVoltageSourceCount() {
		return (position == 1) ? 0 : 1;
	}

	void mouseUp() {
		if (momentary)
			toggle();
	}

	protected virtual void toggle() {
		position++;
		if (position >= posCount)
			position = 0;
	}

	public override void getInfo(string[] arr) {
		arr[0] = (momentary) ? "push switch (SPST)" : "switch (SPST)";
		if (position == 1) {
			arr[1] = "open";
			arr[2] = "Vd = " + getVoltageDText(getVoltageDiff());
		} else {
			arr[1] = "closed";
			arr[2] = "V = " + getVoltageText(volts[0]);
			arr[3] = "I = " + getCurrentDText(getCurrent());
		}
	}

	public override bool getConnection(int n1, int n2) {
		return position == 0;
	}

	public override bool isWire() {
		return position == 0;
	}

	public override EditInfo? getEditInfo(int n) {
		if (n == 0) {
			return new EditInfo("Momentary Switch", momentary);
		}
		return null;
	}

	public override void setEditValue(int n, EditInfo ei) {
		if (n == 0)
			momentary = ei.checkbox.Checked;
	}

	public override int getShortcut() {
		return 's';
	}
}
