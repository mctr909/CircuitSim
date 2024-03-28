using System.Drawing.Drawing2D;

namespace MainForm;

public class ResistorElm : ElmBase {
	double resistance;

	public ResistorElm(int xx, int yy) : base(xx, yy) {
		resistance = 1000;
	}

	public ResistorElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
		resistance = st.nextTokenDouble();
	}

	protected override int getDumpType() {
		return 'r';
	}

	public double getResistance() {
		return resistance;
	}

	public void setResistance(double r) {
		resistance = r;
	}

	public override string dump() {
		return base.dump() + " " + resistance;
	}

	public override void setPoints() {
		base.setPoints();
		calcLeads(32);
	}

	public override void calculateCurrent() {
		current = (volts[0] - volts[1]) / resistance;
		// System.out.print(this + " res current set to " + current + "\n");
	}

	public override void stamp() {
		sim.stampResistor(nodes[0], nodes[1], resistance);
	}

	public override void getInfo(params string[] arr) {
		arr[0] = "resistor";
		getBasicInfo(arr);
		arr[3] = "R = " + getUnitText(resistance, "Ω");
		arr[4] = "P = " + getUnitText(getPower(), "W");
	}

	public override string getScopeText(int v) {
		return "resistor, " + getUnitText(resistance, "Ω");
	}

	public override int getShortcut() {
		return 'r';
	}

	public override EditInfo? getEditInfo(int n) {
		// ohmString doesn't work here on linux
		if (n == 0)
			return new EditInfo("Resistance (ohms)", resistance, 0, 0);
		return null;
	}

	public override void setEditValue(int n, EditInfo ei) {
		if (ei.value > 0)
			resistance = ei.value;
	}

	public override void draw(CustomGraphics g) {
		int i;
		int hs = 6;
		var v1 = volts[0];
		var v2 = volts[1];
		setBbox(point1, point2, hs);
		draw2Leads(g);

		var len = (float)distance(lead1, lead2);
		g.save();
		g.setLineWidth(3.0f);
		g.transform(
			(lead2.X - lead1.X) / len,
			(lead2.Y - lead1.Y) / len,
			-(lead2.Y - lead1.Y) / len,
			(lead2.X - lead1.X) / len,
			lead1.X,
			lead1.Y
		);
		if (sim.voltsCheckItem.Checked) {
			var grad = new LinearGradientBrush(
				Point.Empty,
				new PointF(len, 0),
				getVoltageColor(g, v1),
				getVoltageColor(g, v2)
			);
			g.setStrokeStyle(grad);
		} else {
			setPowerColor(g, true);
		}
		if (!sim.euroResistorCheckItem.Checked) {
			var p = new List<PointF>();
			p.Add(Point.Empty);
			for (i = 0; i < 4; i++) {
				p.Add(new PointF((1 + 4 * i) * len / 16, hs));
				p.Add(new PointF((3 + 4 * i) * len / 16, -hs));
			}
			p.Add(new PointF(len, 0));
			g.drawPolygon([.. p]);
		} else {
			g.drawRect(0, -hs, len, 2 * hs);
		}
		g.restore();

		if (sim.showValuesCheckItem.Checked) {
			var s = getShortUnitText(resistance, "");
			drawValues(g, s, hs + 2);
		}
		doDots(g);
		drawPosts(g);
	}
}
