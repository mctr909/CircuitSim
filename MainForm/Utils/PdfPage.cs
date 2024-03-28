public class PdfPage : CustomGraphics {
	public readonly float Width;
	public readonly float Height;
	const float FONT_SCALE = 1.2f;
	const float PIX_SCALE = FONT_SCALE * 0.65f;

	readonly MemoryStream mMs;
	readonly StreamWriter mSw;

	float mFontSize = 9.0f;

	public PdfPage(float width = 841.92f, float height = 595.32f) {
		Width = width;
		Height = height;
		mMs = new MemoryStream();
		mSw = new StreamWriter(mMs);
	}

	#region public override
	public override void save() {
		base.save();
		mSw.WriteLine("q");
	}

	public override void restore() {
		base.restore();
		mSw.WriteLine("Q");
	}

	public override void scale(float scaleX, float scaleY) {
		base.scale(scaleX, scaleY);
		WriteCM(mState.matrix);
	}

	public override void rotate(float angle) {
		base.rotate(angle);
		WriteCM(mState.matrix);
	}

	public override void translate(float x, float y) {
		base.translate(x, y);
		WriteCM(mState.matrix);
	}

	public override void transform(params float[] m) {
		base.transform(m);
		WriteCM(mState.matrix);
	}

	public override void setLineWidth(float width) {
		base.setLineWidth(width);
	}

	public override void setStrokeStyle(Color color) {
		base.setStrokeStyle(color);
		WriteStrokeColor(color);
	}

	public override void setFillStyle(Color color) {
		base.setFillStyle(color);
		WriteFillColor(color);
	}

	public override void drawLine(float x1, float y1, float x2, float y2) {
		WriteW();
		WriteM(x1, y1);
		WriteLS(x2, y2);
	}

	public override void drawRect(float x, float y, float width, float height) {
		WriteW();
		WriteM(x, y);
		WriteL(x + width, y);
		WriteL(x + width, y + height);
		WriteL(x, y + height);
		WriteLS(x, y);
	}

	public override void drawArc(float cx, float cy, float width, float height, float startAngle, float sweepAngle) {
		var p = CreateArc(cx, cy, width, height, startAngle, sweepAngle);
		WriteW();
		WriteM(p[0].X, p[0].Y);
		for (int i = 1; i < p.Length; i++) {
			WriteL(p[i].X, p[i].Y);
		}
		WriteLS(p[0].X, p[0].Y);
	}

	public override void drawPolygon(params PointF[] points) {
		WriteW();
		WriteM(points[0].X, points[0].Y);
		for (int i = 1; i < points.Length; i++) {
			WriteL(points[i].X, points[i].Y);
		}
		WriteLS(points[0].X, points[0].Y);
	}

	public override void fillRect(float x, float y, float width, float height) {
		WriteM(x, y);
		WriteL(x + width, y);
		WriteL(x + width, y + height);
		WriteL(x, y + height);
		WriteLF(x, y);
	}

	public override void fillPolygon(params PointF[] points) {
		WriteM(points[0].X, points[0].Y);
		for (int i = 1; i < points.Length; i++) {
			WriteL(points[i].X, points[i].Y);
		}
		WriteLF(points[0].X, points[0].Y);
	}

	public override void fillText(string text, float x, float y) {
		WriteText(text, x, y);
	}
	#endregion

	internal void Flush(FileStream fs) {
		mSw.Flush();
		mMs.Seek(0, SeekOrigin.Begin);

		var msTemp = new MemoryStream();
		var swTemp = new StreamWriter(msTemp);
		swTemp.WriteLine("q");
		swTemp.WriteLine("0 w");
		swTemp.WriteLine("0.5 0 0 -0.5 0 {0} cm", Height);
		swTemp.WriteLine("BT");
		var sr = new StreamReader(mMs);
		while (!sr.EndOfStream) {
			swTemp.WriteLine(sr.ReadLine());
		}
		swTemp.WriteLine("ET");
		swTemp.WriteLine("Q");
		swTemp.Flush();

		var cmp = Deflate.Compress(msTemp.ToArray());
		var sw = new StreamWriter(fs);
		sw.NewLine = "\n";
		sw.WriteLine("<</Filter /FlateDecode /Length {0}>>stream", cmp.Length + 2);
		sw.Flush();
		fs.WriteByte(0x68);
		fs.WriteByte(0xDE);
		fs.Write(cmp, 0, cmp.Length);
		fs.Flush();
		sw.WriteLine();
		sw.WriteLine("endstream");
		sw.Flush();

		swTemp.Dispose();
		msTemp.Dispose();
		mSw.Dispose();
		mMs.Dispose();
	}

	private void WriteText(string text, float x, float y, float ofsX = 0.0f) {
		x += mState.matrix[4];
		y += mState.matrix[5];
		x -= ofsX * PIX_SCALE;
		y += mFontSize * PIX_SCALE * 0.5f;
		var lines = text.Replace("\r", "").Split('\n');
		WriteFontSize(mFontSize);
		foreach (var line in lines) {
			WriteTM(1, 0, 0, -1, x, y);
			WriteText(line.Replace("\n", ""));
			y += mFontSize * (PIX_SCALE + 0.2f);
		}
	}

	private void WriteTextR(string text, float x, float y, double angle, float ofsX = 0.0f) {
		x += mState.matrix[4];
		y += mState.matrix[5];
		var lines = text.Replace("\r", "").Split('\n');
		var ofsY = mFontSize * (2 - lines.Length) * 0.5f;
		var c = (float)Math.Cos(angle);
		var s = (float)Math.Sin(angle);
		WriteFontSize(mFontSize);
		foreach (var line in lines) {
			var rx = ofsX * c + ofsY * s;
			var ry = ofsX * s - ofsY * c;
			var tx = x - rx * PIX_SCALE;
			var ty = y - ry * PIX_SCALE;
			WriteTM(c, s, s, -c, tx, ty);
			WriteText(line);
			ofsY += mFontSize + 0.5f;
		}
	}

	#region PDF Operators
	private void WriteW() {
		mSw.WriteLine($"{mState.lineWidth:0.###} w");
	}

	private void WriteM(float x, float y) {
		mSw.WriteLine($"{x:0.###} {y:0.###} m");
	}

	private void WriteL(float x, float y) {
		mSw.WriteLine($"{x:0.###} {y:0.###} l");
	}

	private void WriteLS(float x, float y) {
		mSw.WriteLine($"{x:0.###} {y:0.###} l S");
	}

	private void WriteLF(float x, float y) {
		mSw.WriteLine($"{x:0.###} {y:0.###} l f");
	}

	private void WriteStrokeColor(Color color) {
		mSw.WriteLine($"{color.R / 255.0:0.##} {color.G / 255.0:0.##} {color.B / 255.0:0.##} RG");
	}

	private void WriteFillColor(Color color) {
		mSw.WriteLine($"{color.R / 255.0:0.##} {color.G / 255.0:0.##} {color.B / 255.0:0.##} rg");
	}

	private void WriteFontSize(float size) {
		mSw.WriteLine($"/F0 {size * FONT_SCALE:0.##} Tf");
	}

	private void WriteText(string text) {
		mSw.WriteLine($"({text}) Tj");
	}

	private void WriteCM(params float[] m) {
		mSw.WriteLine($"{m[0]:0.###} {m[1]:0.###} {m[2]:0.###} {m[3]:0.###} {m[4]:0.###} {m[5]:0.###} cm");
	}

	private void WriteTM(params float[] m) {
		mSw.WriteLine($"{m[0]:0.###} {m[1]:0.###} {m[2]:0.###} {m[3]:0.###} {m[4]:0.###} {m[5]:0.###} Tm");
	}
	#endregion
}
