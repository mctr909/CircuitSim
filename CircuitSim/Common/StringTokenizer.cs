using System;
using System.Linq;

namespace Circuit {
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

        public bool nextTokenEnum<T>(out T returnValue, T defaultValue) {
            try {
                returnValue = (T)Enum.Parse(typeof(T), mList[mIndex++]);
                return true;
            } catch (Exception e) {
                Console.WriteLine(e);
                returnValue = defaultValue;
                return false;
            }
        }

        public bool nextTokenBool(out bool returnValue, bool defaultValue) {
            try {
                returnValue = bool.Parse(mList[mIndex++]);
                return true;
            } catch (Exception e) {
                Console.WriteLine(e);
                returnValue = defaultValue;
                return false;
            }
        }

        public bool nextTokenInt(out int returnValue, int defaultValue = 0) {
            try {
                returnValue = int.Parse(mList[mIndex++]);
                return true;
            } catch (Exception e) {
                Console.WriteLine(e);
                returnValue = defaultValue;
                return false;
            }
        }

        public double nextTokenDouble() {
            try {
                return double.Parse(mList[mIndex++]);
            } catch {
                return 0;
            }
        }
    }
}
