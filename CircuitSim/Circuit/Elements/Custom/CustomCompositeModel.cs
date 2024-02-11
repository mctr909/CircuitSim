using System;
using System.Collections.Generic;

using Circuit.Elements.Active;

namespace Circuit.Elements.Custom {
    class ExtListEntry {
        public string name;
        public int node;
        public int pos;
        public int side;

        public ExtListEntry(string s, int n) {
            name = s;
            node = n;
            side = ChipElm.SIDE_W;
        }

        public ExtListEntry(string s, int n, int p, int sd) {
            name = s;
            node = n;
            pos = p;
            side = sd;
        }
    }

    class CustomCompositeModel : IComparable<CustomCompositeModel> {
        static Dictionary<string, CustomCompositeModel> mModelMap = new Dictionary<string, CustomCompositeModel>();

        public List<ExtListEntry> ExtList { get; set; }
        public int SizeX { get; set; }
        public int SizeY { get; set; }
        public string Name { get; private set; } = "";
        public string NodeList { get; set; }
        public string ElmDump { get; set; }
        public bool Dumped { get; private set; }

        public static CustomCompositeModel GetModelWithName(string name) {
            if (mModelMap == null) {
                mModelMap = new Dictionary<string, CustomCompositeModel>();

                /* create default stub model */
                var extList = new List<ExtListEntry>();
                extList.Add(new ExtListEntry("gnd", 1));
                var d = createModel("default", "0", ELEMENTS.GROUND + " 1", extList);
                d.SizeX = d.SizeY = 1;
                mModelMap.Add(d.Name, d);
            }
            if (mModelMap.ContainsKey(name)) {
                return mModelMap[name];
            } else {
                return null;
            }
        }

        public static void ClearDumpedFlags() {
            if (mModelMap == null) {
                return;
            }
            foreach (var key in mModelMap.Keys) {
                mModelMap[key].Dumped = false;
            }
        }

        public static List<CustomCompositeModel> GetModelList() {
            var vector = new List<CustomCompositeModel>();
            foreach (var key in mModelMap.Keys) {
                var dm = mModelMap[key];
                vector.Add(dm);
            }
            vector.Sort();
            return vector;
        }

        public static void UndumpModel(StringTokenizer st) {
            string name = CustomLogicModel.unescape(st.nextToken());
            CustomCompositeElm.lastModelName = name;
            var model = GetModelWithName(name);
            if (model == null) {
                model = new CustomCompositeModel();
                model.Name = name;
                mModelMap.Add(name, model);
            }
            model.undump(st);
        }

        public void SetName(string n) {
            mModelMap.Remove(n);
            Name = n;
            mModelMap.Add(Name, this);
        }

        public int CompareTo(CustomCompositeModel dm) {
            return Name.CompareTo(dm.Name);
        }

        public string Dump() {
            Dumped = true;
            string str = ". " + CustomLogicModel.escape(Name) + " 0 " + SizeX + " " + SizeY + " " + ExtList.Count + " ";
            int i;
            for (i = 0; i != ExtList.Count; i++) {
                var ent = ExtList[i];
                if (i > 0) {
                    str += " ";
                }
                str += CustomLogicModel.escape(ent.name) + " " + ent.node + " " + ent.pos + " " + ent.side;
            }
            str += " " + CustomLogicModel.escape(NodeList) + " " + CustomLogicModel.escape(ElmDump);
            return str;
        }

        static CustomCompositeModel createModel(string name, string elmDump, string nodeList, List<ExtListEntry> extList) {
            var lm = new CustomCompositeModel();
            lm.Name = name;
            lm.ElmDump = elmDump;
            lm.NodeList = nodeList;
            lm.ExtList = extList;
            mModelMap.Add(name, lm);
            return lm;
        }

        void undump(StringTokenizer st) {
            st.nextTokenInt();
            SizeX = st.nextTokenInt();
            SizeY = st.nextTokenInt();
            int extCount = st.nextTokenInt();
            int i;
            ExtList = new List<ExtListEntry>();
            for (i = 0; i != extCount; i++) {
                string s = CustomLogicModel.unescape(st.nextToken());
                int n = st.nextTokenInt();
                int p = st.nextTokenInt();
                int sd = st.nextTokenInt();
                ExtList.Add(new ExtListEntry(s, n, p, sd));
            }
            NodeList = CustomLogicModel.unescape(st.nextToken());
            ElmDump = CustomLogicModel.unescape(st.nextToken());
        }

        string arrayToList(string[] arr) {
            if (arr == null) {
                return "";
            }
            if (arr.Length == 0) {
                return "";
            }
            string x = arr[0];
            int i;
            for (i = 1; i < arr.Length; i++) {
                x += "," + arr[i];
            }
            return x;
        }

        string[] listToArray(string arr) {
            return arr.Split(',');
        }
    }
}
