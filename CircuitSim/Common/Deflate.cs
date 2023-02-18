using System;
using System.Collections.Generic;
using System.Linq;

class ByteStream {
    byte[] buffer;
    int maxSize;
    int extendedSize;

    public int Index { get; private set; }
    
    public ByteStream(int extendedUnit) {
        buffer = new byte[extendedUnit];
        maxSize = extendedUnit;
        extendedSize = extendedUnit;
        Index = 0;
    }

    public void Write(int value) {
        if (maxSize <= Index) {
            maxSize += extendedSize;
            var newBuffer = new byte[maxSize];
            var nowSize = buffer.Length;
            for (int i = 0; i < nowSize; i++) {
                newBuffer[i] = buffer[i];
            }
            buffer = newBuffer;
        }
        buffer[Index] = (byte)value;
        Index++;
    }

    public void WriteFrom(ByteStream source, int start, int length) {
        var end = start + length;
        for (int i = start; i < end; i++) {
            Write(source.buffer[i]);
        }
    }

    public byte[] ToArray() {
        var ret = new byte[Index];
        Array.Copy(buffer, 0, ret, 0, ret.Length);
        return ret;
    }
}

class BitStream {
    byte[] buffer;
    int bufferIndex;
    int nowBits;

    public int NowBitsIndex { get; private set; }
    public int NowBitsLength { get; private set; }
    public bool IsEnd { get; private set; }

    public BitStream(byte[] buffer, int offset = 0) {
        this.buffer = buffer;
        bufferIndex = offset;
        nowBits = buffer[offset];
        NowBitsLength = 8;
        IsEnd = false;
    }

    public int Read() {
        if (IsEnd) {
            throw new Exception("Lack of data length");
        }
        var bit = nowBits & 1;
        if (NowBitsLength > 1) {
            NowBitsLength--;
            nowBits >>= 1;
        } else {
            bufferIndex++;
            if (bufferIndex < buffer.Length) {
                nowBits = buffer[bufferIndex];
                NowBitsLength = 8;
            } else {
                NowBitsLength = 0;
                IsEnd = true;
            }
        }
        return bit;
    }

    public int ReadRange(int length) {
        while (NowBitsLength <= length) {
            nowBits |= buffer[++bufferIndex] << NowBitsLength;
            NowBitsLength += 8;
        }
        var bits = nowBits & ((1 << length) - 1);
        nowBits >>= length;
        NowBitsLength -= length;
        return bits;
    }

    public int ReadRangeCoded(int length) {
        var bits = 0;
        for (int i = 0; i < length; i++) {
            bits <<= 1;
            bits |= Read();
        }
        return bits;
    }

    public void WriteRange(int value, int length) {
        var mask = 1;
        for (int i = 0; i < length; i++) {
            var bit = 0 < (value & mask) ? 1 : 0;
            write(bit);
            mask <<= 1;
        }
    }

    public void WriteRangeCoded(Code code) {
        var mask = 1 << (code.BitLen - 1);
        for (int i = 0; i < code.BitLen; i++) {
            var bit = 0 < (code.Value & mask) ? 1 : 0;
            write(bit);
            mask >>= 1;
        }
    }

    public byte[] ToArray() {
        var ret = new byte[bufferIndex];
        Array.Copy(buffer, 0, ret, 0, ret.Length);
        return ret;
    }

    void write(int bit) {
        if (IsEnd) {
            throw new Exception("Lack of data length");
        }
        bit <<= NowBitsIndex;
        nowBits += bit;
        NowBitsIndex++;
        if (NowBitsIndex >= 8) {
            buffer[bufferIndex] = (byte)nowBits;
            bufferIndex++;
            nowBits = 0;
            NowBitsIndex = 0;
            if (buffer.Length <= bufferIndex) {
                IsEnd = true;
            }
        }
    }
}

struct Code {
    public int Value;
    public int BitLen;
    public Code(int value, int length) {
        Value = value;
        BitLen = length;
    }
}

class LZ77 {
    const int REPEAT_LEN_MIN = 3;
    const int FAST_INDEX_CHECK_MAX = 128;
    const int FAST_INDEX_CHECK_MIN = 16;
    const int FAST_REPEAT_LENGTH = 8;

    static Dictionary<int, List<int>> generateIndexMap(byte[] input, int startIndex, int targetLength) {
        var end = startIndex + targetLength - REPEAT_LEN_MIN;
        var indexMap = new Dictionary<int, List<int>>();
        for (int i = startIndex; i <= end; i++) {
            var indexKey = input[i] << 16 | input[i + 1] << 8 | input[i + 2];
            if (indexMap.ContainsKey(indexKey)) {
                indexMap[indexKey].Add(i);
            } else {
                indexMap.Add(indexKey, new List<int>());
            }
        }
        return indexMap;
    }

    public static List<int[]> GenerateCodes(byte[] input, int startIndex, int targetLength) {
        var nowIndex = startIndex;
        var endIndex = startIndex + targetLength - REPEAT_LEN_MIN;
        var repeatLengthCodeValue = 0;
        var repeatDistanceCodeValue = 0;

        var indexMap = generateIndexMap(input, startIndex, targetLength);

        var startIndexMap = new Dictionary<int, int>();
        var endIndexMap = new Dictionary<int, int>();
        var codeTargetValues = new List<int[]>();
        while (nowIndex <= endIndex) {
            var indexKey = input[nowIndex] << 16 | input[nowIndex + 1] << 8 | input[nowIndex + 2];
            if (!indexMap.ContainsKey(indexKey) || indexMap[indexKey].Count <= 1) {
                codeTargetValues.Add(new int[] { input[nowIndex] });
                nowIndex++;
                continue;
            }
            var indexes = indexMap[indexKey];

            {
                var slideIndexBase = (nowIndex > 0x8000) ? nowIndex - 0x8000 : 0;
                var hasStartIndex = startIndexMap.ContainsKey(indexKey);
                var skipindexes = hasStartIndex ? startIndexMap[indexKey] : 0;
                while (indexes[skipindexes] < slideIndexBase) {
                    skipindexes = (skipindexes + 1) | 0;
                }
                if (hasStartIndex) {
                    startIndexMap[indexKey] = skipindexes;
                } else {
                    startIndexMap.Add(indexKey, skipindexes);
                }
            }
            {
                var hasEndIndex = endIndexMap.ContainsKey(indexKey);
                var skipindexes = hasEndIndex ? endIndexMap[indexKey] : 0;
                while (indexes[skipindexes] < nowIndex) {
                    skipindexes = (skipindexes + 1) | 0;
                }
                if (hasEndIndex) {
                    endIndexMap[indexKey] = skipindexes;
                } else {
                    endIndexMap.Add(indexKey, skipindexes);
                }
            }

            var repeatLengthMax = 0;
            var repeatLengthMaxIndex = 0;
            var checkCount = 0;
            var idx = endIndexMap[indexKey] - 1;
            var iMin = startIndexMap[indexKey];
        indexMapLoop:
            for (; iMin <= idx; idx--) {
                if (checkCount >= FAST_INDEX_CHECK_MAX
                    || (repeatLengthMax >= FAST_REPEAT_LENGTH && checkCount >= FAST_INDEX_CHECK_MIN)) {
                    break;
                }
                checkCount++;

                var index = indexes[idx];
                for (int j = repeatLengthMax - 1; 0 < j; j--) {
                    if (input[index + j] != input[nowIndex + j]) {
                        idx--;
                        goto indexMapLoop;
                    }
                }

                var repeatLength = 258;
                for (int j = repeatLengthMax; j <= 258; j++) {
                    if (input.Length <= (index + j) || input.Length <= (nowIndex + j) || input[index + j] != input[nowIndex + j]) {
                        repeatLength = j;
                        break;
                    }
                }
                if (repeatLengthMax < repeatLength) {
                    repeatLengthMax = repeatLength;
                    repeatLengthMaxIndex = index;
                    if (258 <= repeatLength) {
                        break;
                    }
                }
            }

            if (repeatLengthMax >= 3 && nowIndex + repeatLengthMax <= endIndex) {
                var distance = nowIndex - repeatLengthMaxIndex;
                for (int i = 0; i < Deflate.LENGTH_EXTRA_BIT_BASE.Length; i++) {
                    if (Deflate.LENGTH_EXTRA_BIT_BASE[i] > repeatLengthMax) {
                        break;
                    }
                    repeatLengthCodeValue = i;
                }
                for (int i = 0; i < Deflate.DISTANCE_EXTRA_BIT_BASE.Length; i++) {
                    if (Deflate.DISTANCE_EXTRA_BIT_BASE[i] > distance) {
                        break;
                    }
                    repeatDistanceCodeValue = i;
                }
                codeTargetValues.Add(new int[] {
                    repeatLengthCodeValue,
                    repeatDistanceCodeValue,
                    repeatLengthMax,
                    distance
                });
                nowIndex += repeatLengthMax;
            } else {
                codeTargetValues.Add(new int[] { input[nowIndex] });
                nowIndex++;
            }
        }

        codeTargetValues.Add(new int[] { input[nowIndex] });
        codeTargetValues.Add(new int[] { input[nowIndex + 1] });
        return codeTargetValues;
    }
}

class Deflate {
    const int BLOCK_MAX_BUFFER_LEN = 131072;

    static readonly Dictionary<int, Dictionary<int, int>> FIXED_HUFFMAN_TABLE
        = generateHuffmanTable(makeFixedHuffmanCodelenValues());

    public static readonly int[] LENGTH_EXTRA_BIT_BASE = {
        3, 4, 5, 6, 7, 8, 9, 10, 11, 13,
        15, 17, 19, 23, 27, 31, 35, 43, 51, 59,
        67, 83, 99, 115, 131, 163, 195, 227, 258,
    };
    public static readonly int[] DISTANCE_EXTRA_BIT_BASE = {
        1, 2, 3, 4, 5, 7, 9, 13, 17, 25,
        33, 49, 65, 97, 129, 193, 257, 385, 513, 769,
        1025, 1537, 2049, 3073, 4097, 6145,
        8193, 12289, 16385, 24577,
    };
    static readonly int[] LENGTH_EXTRA_BIT_LEN = {
        0, 0, 0, 0, 0, 0, 0, 0, 1, 1,
        1, 1, 2, 2, 2, 2, 3, 3, 3, 3,
        4, 4, 4, 4, 5, 5, 5, 5, 0,
    };
    static readonly int[] DISTANCE_EXTRA_BIT_LEN = {
        0, 0, 0, 0, 1, 1, 2, 2, 3, 3,
        4, 4, 5, 5, 6, 6, 7, 7, 8, 8,
        9, 9, 10, 10, 11, 11, 12, 12, 13, 13,
    };
    static readonly int[] CODELEN_VALUES = {
        16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15,
    };

    enum BTYPE {
        UNCOMPRESSED = 0,
        FIXED = 1,
        DYNAMIC = 2
    }

    struct Pack {
        public int Count;
        public List<int> Simbles;
        public Pack(int count, int[] simbles) {
            Count = count;
            Simbles = new List<int>();
            Simbles.AddRange(simbles);
        }
    }

    static List<Pack> createPackages(int[] values, int maxLength) {
        var valuesCount = new Dictionary<int, int>();
        foreach (var value in values) {
            if (!valuesCount.ContainsKey(value)) {
                valuesCount.Add(value, 1);
            } else {
                valuesCount[value]++;
            }
        }

        if (valuesCount.Count == 1) {
            var ret = new List<Pack>();
            var v = valuesCount.ElementAt(0);
            ret.Add(new Pack(
                v.Value,
                new int[] { v.Key }
            ));
            return ret;
        }

        var packages = new List<Pack>();
        var tmpPackages = new List<Pack>();
        for (int i = 0; i < maxLength; i++) {
            packages = new List<Pack>();
            foreach (var kv in valuesCount) {
                var pack = new Pack(
                    kv.Value,
                    new int[] { kv.Key }
                );
                packages.Add(pack);
            }

            var tmpPackageIndex = 0;
            while (tmpPackageIndex + 2 <= tmpPackages.Count) {
                var pack = new Pack();
                pack.Count = tmpPackages[tmpPackageIndex].Count + tmpPackages[tmpPackageIndex + 1].Count;
                pack.Simbles = new List<int>();
                pack.Simbles.AddRange(tmpPackages[tmpPackageIndex].Simbles.ToArray());
                pack.Simbles.AddRange(tmpPackages[tmpPackageIndex + 1].Simbles.ToArray());
                packages.Add(pack);
                tmpPackageIndex += 2;
            }

            packages.Sort(new Comparison<Pack>((a, b) => {
                if (a.Count < b.Count) {
                    return -1;
                }
                if (a.Count > b.Count) {
                    return 1;
                }
                if (a.Simbles.Count < b.Simbles.Count) {
                    return -1;
                }
                if (a.Simbles.Count > b.Simbles.Count) {
                    return 1;
                }
                if (a.Simbles[0] < b.Simbles[0]) {
                    return -1;
                }
                if (a.Simbles[0] > b.Simbles[0]) {
                    return 1;
                }
                return 0;
            }));

            if (packages.Count % 2 != 0) {
                packages.RemoveAt(packages.Count - 1);
            }
            tmpPackages = packages;
        }
        return packages;
    }

    static Dictionary<int, Code> generateDeflateHuffmanTable(int[] values, int maxLength = 15) {
        var valuesCodeLen = new Dictionary<int, int>();
        var packages = createPackages(values, maxLength);
        foreach (var pack in packages) {
            foreach (var symble in pack.Simbles) {
                if (!valuesCodeLen.ContainsKey(symble)) {
                    valuesCodeLen.Add(symble, 1);
                } else {
                    valuesCodeLen[symble]++;
                }
            };
        };

        var codeLenGroup = new Dictionary<int, List<int>>();
        var codeLenValueMin = int.MaxValue;
        var codeLenValueMax = 0;
        foreach (var kv in valuesCodeLen) {
            var codeLen = kv.Value;
            var symble = kv.Key;
            if (!codeLenGroup.ContainsKey(codeLen)) {
                codeLenGroup.Add(codeLen, new List<int>());
                if (codeLenValueMin > codeLen) {
                    codeLenValueMin = codeLen;
                }
                if (codeLenValueMax < codeLen) {
                    codeLenValueMax = codeLen;
                }
            }
            codeLenGroup[codeLen].Add(symble);
        };

        var table = new Dictionary<int, Code>();
        var code = 0;
        for (int i = codeLenValueMin; i <= codeLenValueMax; i++) {
            if (codeLenGroup.ContainsKey(i)) {
                var group = codeLenGroup[i];
                group.Sort(new Comparison<int>((a, b) => {
                    if (a < b) {
                        return -1;
                    }
                    if (a > b) {
                        return 1;
                    }
                    return 0;
                }));
                foreach (var value in group) {
                    table.Add(value, new Code(code, i));
                    code++;
                }
            }
            code <<= 1;
        }
        return table;
    }

    static void deflateDynamicBlock(BitStream stream, byte[] input, int startIndex, int targetLength) {
        var lz77Codes = LZ77.GenerateCodes(input, startIndex, targetLength);
        var clCodeValues = new List<int>() { 256 }; // character or matching length
        var distanceCodeValues = new List<int>();
        var clCodeValueMax = 256;
        var distanceCodeValueMax = 0;
        for (int i = 0, iMax = lz77Codes.Count; i < iMax; i++) {
            var values = lz77Codes[i];
            var cl = values[0];
            if (2 <= values.Length) {
                cl += 257;
                var distance = values[1];
                distanceCodeValues.Add(distance);
                if (distanceCodeValueMax < distance) {
                    distanceCodeValueMax = distance;
                }
            }
            clCodeValues.Add(cl);
            if (clCodeValueMax < cl) {
                clCodeValueMax = cl;
            }
        }
        var dataHuffmanTables = generateDeflateHuffmanTable(clCodeValues.ToArray());
        var distanceHuffmanTables = generateDeflateHuffmanTable(distanceCodeValues.ToArray());

        var codelens = new List<int>();
        for (int i = 0; i <= clCodeValueMax; i++) {
            if (dataHuffmanTables.ContainsKey(i)) {
                codelens.Add(dataHuffmanTables[i].BitLen);
            } else {
                codelens.Add(0);
            }
        }

        var HLIT = codelens.Count;
        for (int i = 0; i <= distanceCodeValueMax; i++) {
            if (distanceHuffmanTables.ContainsKey(i)) {
                codelens.Add(distanceHuffmanTables[i].BitLen);
            } else {
                codelens.Add(0);
            }
        }
        var HDIST = codelens.Count - HLIT;

        var runLengthCodes = new List<int>();
        var runLengthRepeatCount = new List<int>();
        for (int i = 0; i < codelens.Count; i++) {
            var codelen = codelens[i];
            var repeatLength = 1;
            while ((i + 1) < codelens.Count && codelen == codelens[i + 1]) {
                repeatLength++;
                i++;
                if (codelen == 0) {
                    if (138 <= repeatLength) {
                        break;
                    }
                } else {
                    if (6 <= repeatLength) {
                        break;
                    }
                }
            }
            if (4 <= repeatLength) {
                if (codelen == 0) {
                    if (11 <= repeatLength) {
                        runLengthCodes.Add(18);
                    } else {
                        runLengthCodes.Add(17);
                    }
                } else {
                    runLengthCodes.Add(codelen);
                    runLengthRepeatCount.Add(1);
                    repeatLength--;
                    runLengthCodes.Add(16);
                }
                runLengthRepeatCount.Add(repeatLength);
            } else {
                for (int j = 0; j < repeatLength; j++) {
                    runLengthCodes.Add(codelen);
                    runLengthRepeatCount.Add(1);
                }
            }
        }

        var codelenHuffmanTable = generateDeflateHuffmanTable(runLengthCodes.ToArray(), 7);
        var HCLEN = 0;
        for (int i = 0; i < CODELEN_VALUES.Length; i++) {
            if (codelenHuffmanTable.ContainsKey(CODELEN_VALUES[i])) {
                HCLEN = i + 1;
            }
        }

        // HLIT
        stream.WriteRange(HLIT - 257, 5);
        // HDIST
        stream.WriteRange(HDIST - 1, 5);
        // HCLEN
        stream.WriteRange(HCLEN - 4, 4);

        // codelenHuffmanTable
        for (int i = 0; i < HCLEN; i++) {
            if (codelenHuffmanTable.ContainsKey(CODELEN_VALUES[i])) {
                var codelenTableObj = codelenHuffmanTable[CODELEN_VALUES[i]];
                stream.WriteRange(codelenTableObj.BitLen, 3);
            } else {
                stream.WriteRange(0, 3);
            }
        }

        for (int i = 0; i < runLengthCodes.Count; i++) {
            var value = runLengthCodes[i];
            if (codelenHuffmanTable.ContainsKey(value)) {
                var codelenTableObj = codelenHuffmanTable[value];
                stream.WriteRangeCoded(codelenTableObj);
            } else {
                throw new Exception("Data is corrupted");
            }
            if (value == 18) {
                stream.WriteRange(runLengthRepeatCount[i] - 11, 7);
            } else if (value == 17) {
                stream.WriteRange(runLengthRepeatCount[i] - 3, 3);
            } else if (value == 16) {
                stream.WriteRange(runLengthRepeatCount[i] - 3, 2);
            }
        }

        for (int i = 0, iMax = lz77Codes.Count; i < iMax; i++) {
            var values = lz77Codes[i];
            var clCodeValue = values[0];
            if (2 <= values.Length) {
                var distanceCodeValue = values[1];
                if (!dataHuffmanTables.ContainsKey(clCodeValue + 257)) {
                    throw new Exception("Data is corrupted");
                }
                var codelenTableObj = dataHuffmanTables[clCodeValue + 257];
                stream.WriteRangeCoded(codelenTableObj);
                if (0 < LENGTH_EXTRA_BIT_LEN[clCodeValue]) {
                    var repeatLength = values[2];
                    stream.WriteRange(
                        repeatLength - LENGTH_EXTRA_BIT_BASE[clCodeValue],
                        LENGTH_EXTRA_BIT_LEN[clCodeValue]
                    );
                }
                if (!distanceHuffmanTables.ContainsKey(distanceCodeValue)) {
                    throw new Exception("Data is corrupted");
                }
                var distanceTableObj = distanceHuffmanTables[distanceCodeValue];
                stream.WriteRangeCoded(distanceTableObj);
                if (0 < DISTANCE_EXTRA_BIT_LEN[distanceCodeValue]) {
                    var distance = values[3];
                    stream.WriteRange(
                        distance - DISTANCE_EXTRA_BIT_BASE[distanceCodeValue],
                        DISTANCE_EXTRA_BIT_LEN[distanceCodeValue]
                    );
                }
            } else {
                if (!dataHuffmanTables.ContainsKey(clCodeValue)) {
                    throw new Exception("Data is corrupted");
                }
                var codelenTableObj = dataHuffmanTables[clCodeValue];
                stream.WriteRangeCoded(codelenTableObj);
            }
        }
        if (!dataHuffmanTables.ContainsKey(256)) {
            throw new Exception("Data is corrupted");
        }
        var codelenTable256 = dataHuffmanTables[256];
        stream.WriteRangeCoded(codelenTable256);
    }

    static Dictionary<int, Dictionary<int, int>> generateHuffmanTable(Dictionary<int, List<int>> codelenValues) {
        var codelens = codelenValues.Keys;
        var codelen = 0;
        var codelenMax = 0;
        var codelenMin = int.MaxValue;
        foreach(var key in codelens) {
            codelen = key;
            if (codelenMax < codelen) {
                codelenMax = codelen;
            }
            if (codelenMin > codelen) {
                codelenMin = codelen;
            }
        }

        var code = 0;
        var bitlenTables = new Dictionary<int, Dictionary<int, int>>();
        for (int bitlen = codelenMin; bitlen <= codelenMax; bitlen++) {
            List<int> values;
            if (codelenValues.ContainsKey(bitlen)) {
                values = codelenValues[bitlen];
            } else {
                values = new List<int>();
            }
            
            values.Sort(new Comparison<int>((a, b) => {
                if (a < b) {
                    return -1;
                }
                if (a > b) {
                    return 1;
                }
                return 0;
            }));

            var table = new Dictionary<int, int>();
            foreach(var value in values) {
                table.Add(code, value);
                code++;
            }
            bitlenTables.Add(bitlen, table);
            code <<= 1;
        }
        return bitlenTables;
    }

    static Dictionary<int, List<int>> makeFixedHuffmanCodelenValues() {
        var codelenValues = new Dictionary<int, List<int>>() {
            { 7, new List<int>() },
            { 8, new List<int>() },
            { 9, new List<int>() }
        };
        for (int i = 0; i <= 287; i++) {
            if (i <= 143) {
                codelenValues[8].Add(i);
            } else if (i <= 255) {
                codelenValues[9].Add(i);
            } else if (i <= 279) {
                codelenValues[7].Add(i);
            } else {
                codelenValues[8].Add(i);
            }
        }
        return codelenValues;
    }

    static void inflateUncompressedBlock(BitStream stream, ByteStream buffer) {
        // Skip to byte boundary
        if (stream.NowBitsLength < 8) {
            stream.ReadRange(stream.NowBitsLength);
        }
        var LEN = stream.ReadRange(8) | stream.ReadRange(8) << 8;
        var NLEN = stream.ReadRange(8) | stream.ReadRange(8) << 8;
        if ((LEN + NLEN) != 65535) {
            throw new Exception("Data is corrupted");
        }
        for (int i = 0; i < LEN; i++) {
            buffer.Write(stream.ReadRange(8));
        }
    }

    static void inflateFixedBlock(BitStream stream, ByteStream buffer) {
        var tables = FIXED_HUFFMAN_TABLE;
        var codeLenMax = 0;
        var codeLenMin = int.MaxValue;
        foreach (var key in tables.Keys) {
            if (codeLenMax < key) {
                codeLenMax = key;
            }
            if (codeLenMin > key) {
                codeLenMin = key;
            }
        }

        int value;
        while (!stream.IsEnd) {
            var codeLen = codeLenMin;
            var code = stream.ReadRangeCoded(codeLenMin);
            while (true) {
                if (tables.ContainsKey(codeLen) && tables[codeLen].ContainsKey(code)) {
                    value = tables[codeLen][code];
                    break;
                }
                if (codeLenMax <= codeLen) {
                    throw new Exception("Data is corrupted");
                }
                codeLen++;
                code <<= 1;
                code |= stream.Read();
            }
            if (value < 256) {
                buffer.Write(value);
                continue;
            }
            if (value == 256) {
                break;
            }
            var repeatLengthCode = value - 257;
            var repeatLengthValue = LENGTH_EXTRA_BIT_BASE[repeatLengthCode];
            var repeatLengthExt = LENGTH_EXTRA_BIT_LEN[repeatLengthCode];
            if (0 < repeatLengthExt) {
                repeatLengthValue += stream.ReadRange(repeatLengthExt);
            }
            var repeatDistanceCode = stream.ReadRangeCoded(5);
            var repeatDistanceValue = DISTANCE_EXTRA_BIT_BASE[repeatDistanceCode];
            var repeatDistanceExt = DISTANCE_EXTRA_BIT_LEN[repeatDistanceCode];
            if (0 < repeatDistanceExt) {
                repeatDistanceValue += stream.ReadRange(repeatDistanceExt);
            }
            var repeatStartIndex = buffer.Index - repeatDistanceValue;
            buffer.WriteFrom(buffer, repeatStartIndex, repeatLengthValue);
        }
    }

    static void inflateDynamicBlock(BitStream stream, ByteStream buffer) {
        var HLIT = stream.ReadRange(5) + 257;
        var HDIST = stream.ReadRange(5) + 1;
        var HCLEN = stream.ReadRange(4) + 4;

        var codelenCodelenValues = new Dictionary<int, List<int>>();
        for (int i = 0; i < HCLEN; i++) {
            var codelenCodelen = stream.ReadRange(3);
            if (codelenCodelen == 0) {
                continue;
            }
            if (!codelenCodelenValues.ContainsKey(codelenCodelen)) {
                codelenCodelenValues.Add(codelenCodelen, new List<int>());
            }
            codelenCodelenValues[codelenCodelen].Add(CODELEN_VALUES[i]);
        }
        var codelenHuffmanTables = generateHuffmanTable(codelenCodelenValues);

        var codelenCodelenMax = 0;
        var codelenCodelenMin = int.MaxValue;
        foreach (var key in codelenHuffmanTables.Keys) {
            if (codelenCodelenMax < key) {
                codelenCodelenMax = key;
            }
            if (codelenCodelenMin > key) {
                codelenCodelenMin = key;
            }
        }

        var dataCodelenValues = new Dictionary<int, List<int>>();
        var distanceCodelenValues = new Dictionary<int, List<int>>();
        var codelen = 0;
        var codesNumber = HLIT + HDIST;
        for (int i = 0; i < codesNumber;) {
            int runlengthCode;
            var codelenCodelen = codelenCodelenMin;
            var codelenCode = stream.ReadRangeCoded(codelenCodelenMin);
            while (true) {
                if (codelenHuffmanTables.ContainsKey(codelenCodelen) &&
                    codelenHuffmanTables[codelenCodelen].ContainsKey(codelenCode)) {
                    runlengthCode = codelenHuffmanTables[codelenCodelen][codelenCode];
                    break;
                }
                if (codelenCodelenMax <= codelenCodelen) {
                    throw new Exception("Data is corrupted");
                }
                codelenCodelen++;
                codelenCode <<= 1;
                codelenCode |= stream.Read();
            }
            int repeat;
            if (runlengthCode == 16) {
                repeat = 3 + stream.ReadRange(2);
            } else if (runlengthCode == 17) {
                repeat = 3 + stream.ReadRange(3);
                codelen = 0;
            } else if (runlengthCode == 18) {
                repeat = 11 + stream.ReadRange(7);
                codelen = 0;
            } else {
                repeat = 1;
                codelen = runlengthCode;
            }
            if (codelen <= 0) {
                i += repeat;
            } else {
                while (0 < repeat) {
                    if (i < HLIT) {
                        if (!dataCodelenValues.ContainsKey(codelen)) {
                            dataCodelenValues.Add(codelen, new List<int>());
                        }
                        dataCodelenValues[codelen].Add(i++);
                    } else {
                        if (!distanceCodelenValues.ContainsKey(codelen)) {
                            distanceCodelenValues.Add(codelen, new List<int>());
                        }
                        distanceCodelenValues[codelen].Add(i++ - HLIT);
                    }
                    repeat--;
                }
            }
        }

        var dataHuffmanTables = generateHuffmanTable(dataCodelenValues);
        var distanceHuffmanTables = generateHuffmanTable(distanceCodelenValues);

        var dataCodelenMax = 0;
        var dataCodelenMin = int.MaxValue;
        foreach (var key in dataHuffmanTables.Keys) {
            if (dataCodelenMax < key) {
                dataCodelenMax = key;
            }
            if (dataCodelenMin > key) {
                dataCodelenMin = key;
            }
        }

        var distanceCodelenMax = 0;
        var distanceCodelenMin = int.MaxValue;
        foreach (var key in distanceHuffmanTables.Keys) {
            if (distanceCodelenMax < key) {
                distanceCodelenMax = key;
            }
            if (distanceCodelenMin > key) {
                distanceCodelenMin = key;
            }
        }

        while (!stream.IsEnd) {
            int data;
            var dataCodelen = dataCodelenMin;
            var dataCode = stream.ReadRangeCoded(dataCodelenMin);
            while (true) {
                if (dataHuffmanTables.ContainsKey(dataCodelen) &&
                    dataHuffmanTables[dataCodelen].ContainsKey(dataCode)) {
                    data = dataHuffmanTables[dataCodelen][dataCode];
                    break;
                }
                if (dataCodelenMax <= dataCodelen) {
                    throw new Exception("Data is corrupted");
                }
                dataCodelen++;
                dataCode <<= 1;
                dataCode |= stream.Read();
            }
            if (data < 256) {
                buffer.Write(data);
                continue;
            }
            if (data == 256) {
                break;
            }

            var repeatLengthCode = data - 257;
            var repeatLengthValue = LENGTH_EXTRA_BIT_BASE[repeatLengthCode];
            var repeatLengthExt = LENGTH_EXTRA_BIT_LEN[repeatLengthCode];
            if (0 < repeatLengthExt) {
                repeatLengthValue += stream.ReadRange(repeatLengthExt);
            }

            int repeatDistanceCode;
            var repeatDistanceCodeCodelen = distanceCodelenMin;
            var repeatDistanceCodeCode = stream.ReadRangeCoded(distanceCodelenMin);
            while (true) {
                if (distanceHuffmanTables.ContainsKey(repeatDistanceCodeCodelen) &&
                    distanceHuffmanTables[repeatDistanceCodeCodelen].ContainsKey(repeatDistanceCodeCode)) {
                    repeatDistanceCode = distanceHuffmanTables[repeatDistanceCodeCodelen][repeatDistanceCodeCode];
                    break;
                }
                if (distanceCodelenMax <= repeatDistanceCodeCodelen) {
                    throw new Exception("Data is corrupted");
                }
                repeatDistanceCodeCodelen++;
                repeatDistanceCodeCode <<= 1;
                repeatDistanceCodeCode |= stream.Read();
            }

            var repeatDistanceValue = DISTANCE_EXTRA_BIT_BASE[repeatDistanceCode];
            var repeatDistanceExt = DISTANCE_EXTRA_BIT_LEN[repeatDistanceCode];
            if (0 < repeatDistanceExt) {
                repeatDistanceValue += stream.ReadRange(repeatDistanceExt);
            }
            var repeatStartIndex = buffer.Index - repeatDistanceValue;
            buffer.WriteFrom(buffer, repeatStartIndex, repeatLengthValue);
        }
    }

    public static byte[] Compress(byte[] input) {
        var inputLength = input.Length;
        var streamHeap = (inputLength < BLOCK_MAX_BUFFER_LEN / 2) ? BLOCK_MAX_BUFFER_LEN : inputLength * 2;
        var stream = new BitStream(new byte[streamHeap]);
        var processedLength = 0;
        var targetLength = 0;
        while (true) {
            if (processedLength + BLOCK_MAX_BUFFER_LEN >= inputLength) {
                targetLength = inputLength - processedLength;
                stream.WriteRange(1, 1);
            } else {
                targetLength = BLOCK_MAX_BUFFER_LEN;
                stream.WriteRange(0, 1);
            }
            stream.WriteRange((int)BTYPE.DYNAMIC, 2);
            deflateDynamicBlock(stream, input, processedLength, targetLength);
            processedLength += BLOCK_MAX_BUFFER_LEN;
            if (processedLength >= inputLength) {
                break;
            }
        }
        if (stream.NowBitsIndex != 0) {
            stream.WriteRange(0, 8 - stream.NowBitsIndex);
        }
        return stream.ToArray();
    }

    public static byte[] UnCompress(byte[] input, int offset = 0) {
        var buffer = new ByteStream(input.Length * 10);
        var stream = new BitStream(input, offset);
        var bFinal = 0;
        while (bFinal != 1) {
            bFinal = stream.ReadRange(1);
            var bType = (BTYPE)stream.ReadRange(2);
            if (bType == BTYPE.UNCOMPRESSED) {
                inflateUncompressedBlock(stream, buffer);
            } else if (bType == BTYPE.FIXED) {
                inflateFixedBlock(stream, buffer);
            } else if (bType == BTYPE.DYNAMIC) {
                inflateDynamicBlock(stream, buffer);
            } else {
                throw new Exception("Not supported BTYPE : " + bType);
            }
            if (bFinal == 0 && stream.IsEnd) {
                throw new Exception("Data length is insufficient");
            }
        }
        return buffer.ToArray();
    }
}
