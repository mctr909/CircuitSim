using System.Linq;

namespace Circuit {
    class StringTokenizer {
        int mIndex;
        string mValue;
        string[] mList;
        char[] mDelimiter;

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

        public string nextToken() {
            return mList[mIndex++];
        }

        public bool nextTokenBool() {
            return bool.Parse(mList[mIndex++]);
        }

        public int nextTokenInt() {
            return int.Parse(mList[mIndex++]);
        }

        public double nextTokenDouble() {
            return double.Parse(mList[mIndex++]);
        }

        public bool hasMoreTokens() {
            return mIndex < mList.Length;
        }
    }
}
