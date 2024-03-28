using MainForm.Forms;

namespace MainForm;

internal class TransistorElm : ElmBase {
	// node 0 = base
	// node 1 = collector
	// node 2 = emitter
	int pnp;
	double beta;
	// double fgain, inv_fgain;
	double gmin;
	string modelName;
	TransistorModel model;
	static string lastModelName = "default";
	const int FLAG_FLIP = 1;

	TransistorElm(int xx, int yy, bool pnpflag) : base(xx, yy) {
		pnp = pnpflag ? -1 : 1;
		beta = 100;
		modelName = lastModelName;
		setup();
	}

	public TransistorElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
		pnp = st.nextTokenInt();
		lastvbe = st.nextTokenDouble();
		lastvbc = st.nextTokenDouble();
		volts[0] = 0;
		volts[1] = -lastvbe;
		volts[2] = -lastvbc;
		beta = st.nextTokenDouble(100);
		modelName = CustomLogicModel.unescape(st.nextToken("default"));
		setup();
	}

	void setup() {
		model = TransistorModel.getModelWithNameOrCopy(modelName, model);
		modelName = model.name; // in case we couldn't find that model
		vcrit = vt * Math.Log(vt / (Math.Sqrt(2) * model.satCur));
		noDiagonal = true;
	}

	public override bool nonLinear() {
		return true;
	}

	public override void reset() {
		volts[0] = volts[1] = volts[2] = 0;
		lastvbc = lastvbe = curcount_c = curcount_e = curcount_b = 0;
	}

	public override int getDumpType() {
		return 't';
	}

	public override string dump() {
		return base.dump() + " " + pnp + " " + (volts[0] - volts[1]) + " " +
				(volts[0] - volts[2]) + " " + beta + " " + CustomLogicModel.escape(modelName);
	}

	public override void updateModels() {
		setup();
	}

	public override string dumpModel() {
		if (model.builtIn || model.dumped)
			return null;
		return model.dump();
	}

	double ic, ie, ib, curcount_c, curcount_e, curcount_b;

	Point[] rectPoly, arrowPoly;

	public override void draw(CustomGraphics g) {
		setBbox(point1, point2, 16);
		setPowerColor(g, true);
		// draw collector
		setVoltageColor(g, volts[1]);
		drawThickLine(g, coll[0], coll[1]);
		// draw emitter
		setVoltageColor(g, volts[2]);
		drawThickLine(g, emit[0], emit[1]);
		// draw arrow
		g.setColor(lightGrayColor);
		g.fillPolygon(arrowPoly);
		// draw base
		setVoltageColor(g, volts[0]);
		if (sim.powerCheckItem.Checked)
			g.setColor(Color.Gray);
		drawThickLine(g, point1, @base);
		// draw dots
		curcount_b = updateDotCount(-ib, curcount_b);
		drawDots(g, @base, point1, curcount_b);
		curcount_c = updateDotCount(-ic, curcount_c);
		drawDots(g, coll[1], coll[0], curcount_c);
		curcount_e = updateDotCount(-ie, curcount_e);
		drawDots(g, emit[1], emit[0], curcount_e);
		// draw base rectangle
		setVoltageColor(g, volts[0]);
		setPowerColor(g, true);
		g.fillPolygon(rectPoly);

		if ((needsHighlight() || sim.dragElm == this) && dy == 0) {
			g.setColor(Color.White);
			// IES
			// g.setFont(unitsFont);
			var ds = Math.Sign(dx);
			g.drawString("B", @base.X - 10 * ds, @base.Y - 5);
			g.drawString("C", coll[0].X - 3 + 9 * ds, coll[0].Y + 4); // x+6 if ds=1, -12 if -1
			g.drawString("E", emit[0].X - 3 + 9 * ds, emit[0].Y + 4);
		}
		drawPosts(g);
	}

	public override Point getPost(int n) {
		return (n == 0) ? point1 : (n == 1) ? coll[0] : emit[0];
	}

	public override int getPostCount() {
		return 3;
	}

	public override double getPower() {
		return (volts[0] - volts[2]) * ib + (volts[1] - volts[2]) * ic;
	}

	Point[] rect, coll, emit;
	Point @base;

	public override void setPoints() {
		base.setPoints();
		int hs = 16;
		if ((flags & FLAG_FLIP) != 0)
			dsign = -dsign;
		int hs2 = hs * dsign * pnp;
		// calc collector, emitter posts
		coll = newPointArray(2);
		emit = newPointArray(2);
		interpPoint2(point1, point2, ref coll[0], ref emit[0], 1, hs2);
		// calc rectangle edges
		rect = newPointArray(4);
		interpPoint2(point1, point2, ref rect[0], ref rect[1], 1 - 16 / dn, hs);
		interpPoint2(point1, point2, ref rect[2], ref rect[3], 1 - 13 / dn, hs);
		// calc points where collector/emitter leads contact rectangle
		interpPoint2(point1, point2, ref coll[1], ref emit[1], 1 - 13 / dn, 6 * dsign * pnp);
		// calc point where base lead contacts rectangle
		@base = new Point();
		interpPoint(point1, point2, ref @base, 1 - 16 / dn);
		// rectangle
		rectPoly = createPolygon(rect[0], rect[2], rect[3], rect[1]);

		// arrow
		if (pnp == 1)
			arrowPoly = calcArrow(emit[1], emit[0], 8, 4);
		else {
			Point pt = interpPoint(point1, point2, 1 - 11 / dn, -5 * dsign * pnp);
			arrowPoly = calcArrow(emit[0], pt, 8, 4);
		}
	}

	const double leakage = 1e-13; // 1e-6;
								  // Electron thermal voltage at SPICE's default temperature of 27 C (300.15 K):
	const double vt = 0.025865;
	double vcrit;
	double lastvbc, lastvbe;

	double limitStep(double vnew, double vold) {
		double arg;
		double oo = vnew;

		if (vnew > vcrit && Math.Abs(vnew - vold) > (vt + vt)) {
			if (vold > 0) {
				arg = 1 + (vnew - vold) / vt;
				if (arg > 0) {
					vnew = vold + vt * Math.Log(arg);
				} else {
					vnew = vcrit;
				}
			} else {
				vnew = vt * Math.Log(vnew / vt);
			}
			sim.converged = false;
			// System.out.println(vnew + " " + oo + " " + vold);
		}
		return (vnew);
	}

	public override void stamp() {
		sim.stampNonLinear(nodes[0]);
		sim.stampNonLinear(nodes[1]);
		sim.stampNonLinear(nodes[2]);
	}

	public override void doStep() {
		var vbc = pnp * (volts[0] - volts[1]); // typically negative
		var vbe = pnp * (volts[0] - volts[2]); // typically positive
		if (Math.Abs(vbc - lastvbc) > .01 || // .01
				Math.Abs(vbe - lastvbe) > .01)
			sim.converged = false;

		// To prevent a possible singular matrix, put a tiny conductance in parallel
		// with each P-N junction.
		// gmin = leakage * 0.01;
		gmin = 1e-12;

		if (sim.subIterations > 100) {
			// if we have trouble converging, put a conductance in parallel with all P-N
			// junctions.
			// Gradually increase the conductance value for each iteration.
			gmin = Math.Exp(-9 * Math.Log(10) * (1 - sim.subIterations / 300.0));
			if (gmin > .1)
				gmin = .1;
		}

		// System.out.print("T " + vbc + " " + vbe + "\n");
		vbc = limitStep(vbc, lastvbc);
		vbe = limitStep(vbe, lastvbe);
		lastvbc = vbc;
		lastvbe = vbe;

		/*
		 * dc model paramters (from Spice 3f5, bjtload.c)
		 */
		var csat = model.satCur;
		var oik = model.invRollOffF;
		var c2 = model.BEleakCur;
		var vte = model.leakBEemissionCoeff * vt;
		var oikr = model.invRollOffR;
		var c4 = model.BCleakCur;
		var vtc = model.leakBCemissionCoeff * vt;

		// double rbpr=model.minBaseResist;
		// double rbpi=model.baseResist-rbpr;
		// double xjrb=model.baseCurrentHalfResist;

		double vtn = vt * model.emissionCoeffF;
		double evbe, cbe, gbe, cben, gben, evben, evbc, cbc, gbc, cbcn, gbcn, evbcn;
		double qb, dqbdve, dqbdvc, q2, sqarg, arg;
		if (vbe > -5 * vtn) {
			evbe = Math.Exp(vbe / vtn);
			cbe = csat * (evbe - 1) + gmin * vbe;
			gbe = csat * evbe / vtn + gmin;
			if (c2 == 0) {
				cben = 0;
				gben = 0;
			} else {
				evben = Math.Exp(vbe / vte);
				cben = c2 * (evben - 1);
				gben = c2 * evben / vte;
			}
		} else {
			gbe = -csat / vbe + gmin;
			cbe = gbe * vbe;
			gben = -c2 / vbe;
			cben = gben * vbe;
		}
		vtn = vt * model.emissionCoeffR;
		if (vbc > -5 * vtn) {
			evbc = Math.Exp(vbc / vtn);
			cbc = csat * (evbc - 1) + gmin * vbc;
			gbc = csat * evbc / vtn + gmin;
			if (c4 == 0) {
				cbcn = 0;
				gbcn = 0;
			} else {
				evbcn = Math.Exp(vbc / vtc);
				cbcn = c4 * (evbcn - 1);
				gbcn = c4 * evbcn / vtc;
			}
		} else {
			gbc = -csat / vbc + gmin;
			cbc = gbc * vbc;
			gbcn = -c4 / vbc;
			cbcn = gbcn * vbc;
		}
		/*
		 * determine base charge terms
		 */
		double q1 = 1 / (1 - model.invEarlyVoltF * vbc - model.invEarlyVoltR * vbe);
		if (oik == 0 && oikr == 0) {
			qb = q1;
			dqbdve = q1 * qb * model.invEarlyVoltR;
			dqbdvc = q1 * qb * model.invEarlyVoltF;
		} else {
			q2 = oik * cbe + oikr * cbc;
			arg = Math.Max(0, 1 + 4 * q2);
			sqarg = 1;
			if (arg != 0)
				sqarg = Math.Sqrt(arg);
			qb = q1 * (1 + sqarg) / 2;
			dqbdve = q1 * (qb * model.invEarlyVoltR + oik * gbe / sqarg);
			dqbdvc = q1 * (qb * model.invEarlyVoltF + oikr * gbc / sqarg);
		}

		double cc = 0;
		double cex = cbe;
		double gex = gbe;
		/*
		 * determine dc incremental conductances
		 */
		cc = cc + (cex - cbc) / qb - cbc / model.betaR - cbcn;
		double cb = cbe / beta + cben + cbc / model.betaR + cbcn;

		// get currents
		ic = pnp * cc;
		ib = pnp * cb;
		ie = pnp * (-cc - cb);

		/*
		 * double gx=rbpr+rbpi/qb; // base resistance commented out for now
		 * if(xjrb != 0) {
		 * double arg1=Math.max(cb/xjrb,1e-9);
		 * double arg2=(-1+Math.sqrt(1+14.59025*arg1))/2.4317/Math.sqrt(arg1);
		 * arg1=Math.tan(arg2);
		 * gx=rbpr+3*rbpi*(arg1-arg2)/arg2/arg1/arg1;
		 * }
		 * if(gx != 0) gx=1/gx;
		 */
		var gpi = gbe / beta + gben;
		var gmu = gbc / model.betaR + gbcn;
		var go = (gbc + (cex - cbc) * dqbdvc / qb) / qb;
		var gm = (gex - (cex - cbc) * dqbdve / qb) / qb - go;

		var ceqbe = pnp * (cc + cb - vbe * (gm + go + gpi) + vbc * go);
		var ceqbc = pnp * (-cc + vbe * (gm + go) - vbc * (gmu + go));

		// stamp matrix.
		// Node 0 is the base, node 1 the collector, node 2 the emitter.
		sim.stampMatrix(nodes[1], nodes[1], gmu + go);
		sim.stampMatrix(nodes[1], nodes[0], -gmu + gm);
		sim.stampMatrix(nodes[1], nodes[2], -gm - go);
		sim.stampMatrix(nodes[0], nodes[0], gpi + gmu);
		sim.stampMatrix(nodes[0], nodes[2], -gpi);
		sim.stampMatrix(nodes[0], nodes[1], -gmu);
		sim.stampMatrix(nodes[2], nodes[0], -gpi - gm);
		sim.stampMatrix(nodes[2], nodes[1], -go);
		sim.stampMatrix(nodes[2], nodes[2], gpi + gm + go);

		/*
		 * load current excitation vector (right side)
		 */
		sim.stampRightSide(nodes[0], -ceqbe - ceqbc);
		sim.stampRightSide(nodes[1], ceqbc);
		sim.stampRightSide(nodes[2], ceqbe);
	}

	public override string getScopeText(int x) {
		var t = "";
		switch (x) {
		case Scope.VAL_IB:
			t = "Ib";
			break;
		case Scope.VAL_IC:
			t = "Ic";
			break;
		case Scope.VAL_IE:
			t = "Ie";
			break;
		case Scope.VAL_VBE:
			t = "Vbe";
			break;
		case Scope.VAL_VBC:
			t = "Vbc";
			break;
		case Scope.VAL_VCE:
			t = "Vce";
			break;
		case Scope.VAL_POWER:
			t = "P";
			break;
		}
		return "transistor, " + t;
	}

	public override void getInfo(string[] arr) {
		arr[0] = "transistor (" + ((pnp == -1) ? "PNP)" : "NPN)") + " β=" + beta.ToString(showFormat);
		var vbc = volts[0] - volts[1];
		var vbe = volts[0] - volts[2];
		var vce = volts[1] - volts[2];
		if (vbc * pnp > .2)
			arr[1] = vbe * pnp > .2 ? "saturation" : "reverse active";
		else
			arr[1] = vbe * pnp > .2 ? "fwd active" : "cutoff";
		arr[2] = "Ic = " + getCurrentText(ic);
		arr[3] = "Ib = " + getCurrentText(ib);
		arr[4] = "Vbe = " + getVoltageText(vbe);
		arr[5] = "Vbc = " + getVoltageText(vbc);
		arr[6] = "Vce = " + getVoltageText(vce);
		arr[7] = "P = " + getUnitText(getPower(), "W");
	}

	public override double getScopeValue(int x) {
		return x switch {
			Scope.VAL_IB => ib,
			Scope.VAL_IC => ic,
			Scope.VAL_IE => ie,
			Scope.VAL_VBE => volts[0] - volts[2],
			Scope.VAL_VBC => volts[0] - volts[1],
			Scope.VAL_VCE => volts[1] - volts[2],
			Scope.VAL_POWER => getPower(),
			_ => 0,
		};
	}

	public override int getScopeUnits(int x) {
		return x switch {
			Scope.VAL_IB or Scope.VAL_IC or Scope.VAL_IE => Scope.UNITS_A,
			Scope.VAL_POWER => Scope.UNITS_W,
			_ => Scope.UNITS_V,
		};
	}

	List<TransistorModel> models;

	public override EditInfo? getEditInfo(int n) {
		if (n == 0)
			return new EditInfo("Beta/hFE", beta, 10, 1000).setDimensionless();
		if (n == 1) {
			return new EditInfo("Swap E/C", (flags & FLAG_FLIP) != 0);
		}
		if (n == 2) {
			var ei = new EditInfo("Model", 0, -1, -1);
			models = TransistorModel.getModelList();
			ei.choice = new ComboBox();
			int i;
			for (i = 0; i != models.Count; i++) {
				var dm = models[i];
				ei.choice.Items.Add(dm.getDescription());
				if (dm == model)
					ei.choice.SelectedIndex = i;
			}
			return ei;
		}
		if (n == 3) {
			var ei = new EditInfo("", 0, -1, -1);
			ei.button = new Button() {
				Text = "Create New Model"
			};
			return ei;
		}
		if (n == 4) {
			if (model.readOnly)
				return null;
			var ei = new EditInfo("", 0, -1, -1);
			ei.button = new Button() {
				Text = "Edit Model"
			};
			return ei;
		}
		return null;
	}

	public void newModelCreated(TransistorModel tm) {
		model = tm;
		modelName = model.name;
		setup();
	}

	public override void setEditValue(int n, EditInfo ei) {
		if (n == 0) {
			beta = ei.value;
			setup();
		}
		if (n == 1) {
			if (ei.checkbox.Checked)
				flags |= FLAG_FLIP;
			else
				flags &= ~FLAG_FLIP;
			setPoints();
		}
		if (n == 2) {
			model = models[ei.choice.SelectedIndex];
			modelName = model.name;
			setup();
			ei.newDialog = true;
			return;
		}
		if (n == 3) {
			var newModel = new TransistorModel(model);
			var editDialog = new EditTransistorModelDialog(newModel, sim, this);
			CirSim.diodeModelEditDialog = editDialog;
			editDialog.Show();
			return;
		}
		if (n == 4) {
			if (model.readOnly) {
				// probably never reached
				MessageBox.Show("This model cannot be modified.  Change the model name to allow customization.");
				return;
			}
			var editDialog = new EditTransistorModelDialog(model, sim, null);
			CirSim.diodeModelEditDialog = editDialog;
			editDialog.Show();
			return;
		}
	}

	void setBeta(double b) {
		beta = b;
		setup();
	}

	public override void stepFinished() {
		// stop for huge currents that make simulator act weird
		if (Math.Abs(ic) > 1e12 || Math.Abs(ib) > 1e12) {
			sim.stop("max current exceeded");
		}
	}

	public override bool canViewInScope() {
		return true;
	}

	public override double getCurrentIntoNode(int n) {
		if (n == 0)
			return -ib;
		if (n == 1)
			return -ic;
		return -ie;
	}
}
