using System;
using System.Linq;

public class StringTokenizer {
	int mIndex;
	string mValue;
	string[] mList;
	char[] mDelimiter;

	public bool HasMoreTokens { get { return mIndex < mList.Length; } }

	public StringTokenizer(string value, string delimiter) {
		mValue = value;
		mDelimiter = delimiter.ToArray();
		foreach (var d in mDelimiter) {
			var s = mValue.Split(d);
			if (1 < s.Length) {
				mList = s;
				break;
			}
		}
		if (mList == null) {
			mList = mValue.Split(' ');
		}
	}

	public bool nextToken(out string returnValue, string defaultValue = "") {
		try {
			returnValue = mList[mIndex++];
			return true;
		} catch (Exception e) {
			Console.WriteLine(e);
			returnValue = defaultValue;
			return false;
		}
	}

	public T nextTokenEnum<T>(T defaultValue) {
		try {
			return (T)Enum.Parse(typeof(T), mList[mIndex++]);
		} catch (Exception e) {
			Console.WriteLine(e);
			return defaultValue;
		}
	}

	public bool nextTokenBool(bool defaultValue) {
		try {
			return bool.Parse(mList[mIndex++]);
		} catch (Exception e) {
			Console.WriteLine(e);
			return defaultValue;
		}
	}

	public int nextTokenInt(int defaultValue = 0) {
		try {
			return int.Parse(mList[mIndex++]);
		} catch (Exception e) {
			Console.WriteLine(e);
			return defaultValue;
		}
	}

	public double nextTokenDouble(double defaultValue = 0) {
		try {
			return double.Parse(mList[mIndex++]);
		} catch (Exception e) {
			Console.WriteLine(e);
			return defaultValue;
		}
	}
}
