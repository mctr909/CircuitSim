namespace MainForm;

internal class RowInfo {
	public const int ROW_NORMAL = 0; // ordinary value
	public const int ROW_CONST = 1; // value is constant

	public int type, mapCol, mapRow;
	public double value;
	public bool rsChanges; // row's right side changes
	public bool lsChanges; // row's left side changes
	public bool dropRow; // row is not needed in matrix

	public RowInfo() {
		type = ROW_NORMAL;
	}
}
