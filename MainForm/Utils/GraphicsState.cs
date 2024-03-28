using System.Drawing.Drawing2D;

public class GraphicsState {
	public float[] matrix = new float[6];
	public float fontSize = 9.0f;
	public float scaleX = 1.0f;
	public float scaleY = 1.0f;
	public float lineWidth = 1.0f;
	public LineCap lineCap = LineCap.Flat;
	public Color strokeColor = Color.Black;
	public Color fillColor = Color.Black;
	public Font font = new("Arial", 9.0f);

	public void CopyTo(GraphicsState target) {
		Array.Copy(matrix, target.matrix, 6);
		target.fontSize = fontSize;
		target.scaleX = scaleX;
		target.scaleY = scaleY;
		target.lineWidth = lineWidth;
		target.lineCap = lineCap;
		target.strokeColor = strokeColor;
		target.fillColor = fillColor;
		target.font = new Font(font.FontFamily, font.Size);
	}

	public void SetMatrix(Graphics? g) {
		if (g == null) return;
		g.Transform.MatrixElements = new System.Numerics.Matrix3x2(
			matrix[0] * scaleX, matrix[1] * scaleX,
			matrix[2] * scaleY, matrix[3] * scaleY,
			matrix[4], matrix[5]
		);
	}
}
