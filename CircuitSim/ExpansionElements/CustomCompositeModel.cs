using System;
using System.Collections.Generic;

namespace Circuit.Elements {
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
        static Dictionary<string, CustomCompositeModel> modelMap;

        int flags;
        public int sizeX;
        public int sizeY;
        public string name;
        public string nodeList;
        public List<ExtListEntry> extList;
        public string elmDump;
        public bool dumped;

        public void setName(string n) {
            modelMap.Remove(name);
            name = n;
            modelMap.Add(name, this);
        }

        public static CustomCompositeModel getModelWithName(string name) {
            if (modelMap == null) {
                modelMap = new Dictionary<string, CustomCompositeModel>();

                /* create default stub model */
                var extList = new List<ExtListEntry>();
                extList.Add(new ExtListEntry("gnd", 1));
                var d = createModel("default", "0", "GroundElm 1", extList);
                d.sizeX = d.sizeY = 1;
                modelMap.Add(d.name, d);
            }
            var lm = modelMap[name];
            return lm;
        }

        static CustomCompositeModel createModel(string name, string elmDump, string nodeList, List<ExtListEntry> extList) {
            var lm = new CustomCompositeModel();
            lm.name = name;
            lm.elmDump = elmDump;
            lm.nodeList = nodeList;
            lm.extList = extList;
            modelMap.Add(name, lm);
            return lm;
        }

        public static void clearDumpedFlags() {
            if (modelMap == null) {
                return;
            }
            foreach (var key in modelMap.Keys) {
                modelMap[key].dumped = false;
            }
        }

        public static List<CustomCompositeModel> getModelList() {
            var vector = new List<CustomCompositeModel>();
            foreach (var key in modelMap.Keys) {
                var dm = modelMap[key];
                vector.Add(dm);
            }
            vector.Sort();
            return vector;
        }

        public int CompareTo(CustomCompositeModel dm) {
            return name.CompareTo(dm.name);
        }

        public static void undumpModel(StringTokenizer st) {
            string name = CustomLogicModel.unescape(st.nextToken());
            CustomCompositeElm.lastModelName = name;
            var model = getModelWithName(name);
            if (model == null) {
                model = new CustomCompositeModel();
                model.name = name;
                modelMap.Add(name, model);
            }
            model.undump(st);
        }

        void undump(StringTokenizer st) {
            flags = st.nextTokenInt();
            sizeX = st.nextTokenInt();
            sizeY = st.nextTokenInt();
            int extCount = st.nextTokenInt();
            int i;
            extList = new List<ExtListEntry>();
            for (i = 0; i != extCount; i++) {
                string s = CustomLogicModel.unescape(st.nextToken());
                int n = st.nextTokenInt();
                int p = st.nextTokenInt();
                int sd = st.nextTokenInt();
                extList.Add(new ExtListEntry(s, n, p, sd));
            }
            nodeList = CustomLogicModel.unescape(st.nextToken());
            elmDump = CustomLogicModel.unescape(st.nextToken());
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

        public string dump() {
            dumped = true;
            string str = ". " + CustomLogicModel.escape(name) + " 0 " + sizeX + " " + sizeY + " " + extList.Count + " ";
            int i;
            for (i = 0; i != extList.Count; i++) {
                var ent = extList[i];
                if (i > 0) {
                    str += " ";
                }
                str += CustomLogicModel.escape(ent.name) + " " + ent.node + " " + ent.pos + " " + ent.side;
            }
            str += " " + CustomLogicModel.escape(nodeList) + " " + CustomLogicModel.escape(elmDump);
            return str;
        }
    }
}
