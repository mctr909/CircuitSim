using Windows.Networking.NetworkOperators;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MainForm;

internal class Switch2Elm : SwitchElm {
	int link;
	int throwCount;
	const int FLAG_CENTER_OFF = 1;

	public Switch2Elm(int xx, int yy) : base(xx, yy, false) {
		noDiagonal = true;
		throwCount = 2;
	}

	Switch2Elm(int xx, int yy, bool mm) : base(xx, yy, mm) {
		noDiagonal = true;
		throwCount = 2;
	}

	public Switch2Elm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f, st) {
		link = st.nextTokenInt();
		throwCount = st.nextTokenInt(2);
		noDiagonal = true;
	}

	public override int getDumpType() {
		return 'S';
	}

	public override string dump() {
		return base.dump() + " " + link + " " + throwCount;
	}

	const int openhs = 16;
	Point[] swposts, swpoles;

	public override void setPoints() {
		base.setPoints();
		calcLeads(32);
		swposts = newPointArray(throwCount);
		swpoles = newPointArray(2 + throwCount);
		int i;
		for (i = 0; i != throwCount; i++) {
			var hs = -openhs * (i - (throwCount - 1) / 2);
			if (throwCount == 2 && i == 0)
				hs = openhs;
			interpPoint(lead1, lead2, ref swpoles[i], 1, hs);
			interpPoint(point1, point2, ref swposts[i], 1, hs);
		}
		swpoles[i] = lead2; // for center off
		posCount = hasCenterOff() ? 3 : throwCount;
	}

	public override void draw(CustomGraphics g) {
		setBbox(point1, point2, openhs);
		adjustBbox(swposts[0], swposts[throwCount - 1]);

		// draw first lead
		setVoltageColor(g, volts[0]);
		drawThickLine(g, point1, lead1);

		// draw other leads
		int i;
		for (i = 0; i != throwCount; i++) {
			setVoltageColor(g, volts[i + 1]);
			drawThickLine(g, swpoles[i], swposts[i]);
		}

		// draw switch
		if (!needsHighlight())
			g.setColor(whiteColor);
		drawThickLine(g, lead1, swpoles[position]);

		updateDotCount();
		drawDots(g, point1, lead1, curcount);
		if (position != 2)
			drawDots(g, swpoles[position], swposts[position], curcount);
		drawPosts(g);
	}

	public override double getCurrentIntoNode(int n) {
		if (n == 0)
			return -current;
		if (n == position + 1)
			return current;
		return 0;
	}

	protected override Rectangle getSwitchRect() {
		var w = Math.Max(swpoles[0].X, swpoles[throwCount - 1].X) - lead1.X + 1;
		var h = Math.Max(swpoles[0].Y, swpoles[throwCount - 1].Y) - lead1.Y + 1;
		return new Rectangle(lead1.X, lead1.Y, w, h);
	}

	public override Point getPost(int n) {
		return (n == 0) ? point1 : swposts[n - 1];
	}

	public override int getPostCount() {
		return 1 + throwCount;
	}

	public override void calculateCurrent() {
		if (position == 2 && hasCenterOff())
			current = 0;
	}

	public override void stamp() {
		if (position == 2 && hasCenterOff()) // in center?
			return;
		sim.stampVoltageSource(nodes[0], nodes[position + 1], voltSource, 0);
	}

	public override int getVoltageSourceCount() {
		return (position == 2 && hasCenterOff()) ? 0 : 1;
	}

	protected override void toggle() {
		base.toggle();
		if (link != 0) {
			foreach (var o in sim.elmList) {
				if (o is Switch2Elm s2) {
					if (s2.link == link)
						s2.position = position;
				}
			}
		}
	}

	public override bool getConnection(int n1, int n2) {
		if (position == 2 && hasCenterOff())
			return false;
		return comparePair(n1, n2, 0, 1 + position);
	}

	public override bool isWire() {
		return true;
	}

	public override void getInfo(string[] arr) {
		arr[0] = "switch (" + (link == 0 ? "S" : "D") + "P" +
				((throwCount > 2) ? throwCount + "T)" : "DT)");
		arr[1] = "I = " + getCurrentDText(getCurrent());
	}

	public override EditInfo? getEditInfo(int n) {
		/*
		 * if (n == 1) {
		 * EditInfo ei = new EditInfo("", 0, -1, -1);
		 * ei.checkbox = new Checkbox("Center Off", hasCenterOff());
		 * return ei;
		 * }
		 */
		if (n == 1)
			return new EditInfo("Switch Group", link, 0, 100).setDimensionless();
		if (n == 2)
			return new EditInfo("# of Throws", throwCount, 2, 10).setDimensionless();
		return base.getEditInfo(n);
	}

	public override void setEditValue(int n, EditInfo ei) {
		/*
		 * if (n == 1) {
		 * flags &= ~FLAG_CENTER_OFF;
		 * if (ei.checkbox.getState())
		 * flags |= FLAG_CENTER_OFF;
		 * if (hasCenterOff())
		 * momentary = false;
		 * setPoints();
		 * } else
		 */
		if (n == 1) {
			link = (int)ei.value;
		} else if (n == 2) {
			if (ei.value >= 2)
				throwCount = (int)ei.value;
			if (throwCount > 2)
				momentary = false;
			allocNodes();
			setPoints();
		} else
			base.setEditValue(n, ei);
	}

	// this is for backwards compatibility only. we only support it if throwCount =
	// 2
	bool hasCenterOff() {
		return (flags & FLAG_CENTER_OFF) != 0 && throwCount == 2;
	}

	public override int getShortcut() {
		return 'S';
	}
}
