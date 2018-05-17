using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MapMerge
{
    public sealed class Map
    {
        internal static String[] LetterCodes = new String[] { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
        private string _mapName;
        private bool _sizeunknown;
        internal int _minx;
        internal int _miny;
        internal int _minz;
        internal int _maxx;
        internal int _maxy;
        internal int _maxz;
        public Location MinSize
        { get { return new Location(_minx, _miny, _minz); } }
        public Location MaxSize
        { get { return new Location(_maxx, _maxy, _maxz); } }
        public ConcurrentDictionary<string, string> TileTypes;
        public ConcurrentDictionary<string, string> CodesByValue;
        public ConcurrentDictionary<Location, string> Tiles;

        public Map()
        {
            this._mapName = "unknown";
            this._sizeunknown = true;
            this.TileTypes = new ConcurrentDictionary<string, string>();
            this.CodesByValue = new ConcurrentDictionary<string, string>();
            this.Tiles = new ConcurrentDictionary<Location, string>();
        }

        public Map(string mapPath, bool skipLevels = false)
        {
            this._mapName = new FileInfo(mapPath).Name;
            this._sizeunknown = true;
            string mapFile = File.ReadAllText(mapPath); // Don't catch exceptions here, make the caller catch them.
            this.TileTypes = new ConcurrentDictionary<string, string>();
            this.CodesByValue = new ConcurrentDictionary<string, string>();
            this.Tiles = new ConcurrentDictionary<Location, string>();
            Program.WriteLine(_mapName, "Loading Tiles.");

            Regex regex = new Regex("\"[a-zA-Z]+\" = \\(.+\\)");
            MatchCollection matches = regex.Matches(mapFile);
            int codeSize = 0;
            foreach (Match match in matches)
            {
                string code = match.Value.Substring(1, match.Value.IndexOf("\"", 1) - 1);
                string value = match.Value.Substring(match.Value.IndexOf("("));
                if (code.Length > codeSize)
                { codeSize = code.Length; }
                this.TileTypes.TryAdd(code, value);
                this.CodesByValue.TryAdd(value, code);
            }

            if (!skipLevels)
            {
                Program.WriteLine(_mapName, "Loading Levels.");
                regex = new Regex("\\(([1-9]+),([1-9]+),([1-9]+)\\) = \\{\\\"\\s([a-zA-Z\\s]+)\\s\\\"\\}");
                matches = regex.Matches(mapFile);
                foreach (Match match in matches)
                {
                    int.TryParse(match.Groups[1].Value, out int x);
                    int.TryParse(match.Groups[2].Value, out int y);
                    int.TryParse(match.Groups[3].Value, out int z);
                    Program.WriteLine(_mapName, String.Concat("New map part from (", x, ",", y, ",", z, ")"));
                    if (this._sizeunknown)
                    {
                        this._maxx = this._minx = x;
                        this._maxy = this._miny = y;
                        this._maxz = this._minz = z;
                        this._sizeunknown = false;
                    }
                    if (this._minz > z)
                    { this._minz = z; }
                    if (this._maxz < z)
                    { this._maxz = z; }
                    string[] blob = Regex.Split(match.Groups[4].Value, @"\r?\n|\r");
                    foreach (string line in blob)
                    {
                        if (line == String.Empty)
                        { continue; }

                        if (this._miny > y)
                        { this._miny = y; }
                        if (this._maxy < y)
                        { this._maxy = y; }
                        this.ConsumeBlobLine(line, codeSize, x, y, z);
                        y++;
                    }
                }
            }
        }

        public void MirrorY()
        {
            for (int i = this._minz; i <= this._maxz; ++i)
            {
                for (int j = this._minx; j <= this._maxx; ++j)
                {
                    for (int k = this._miny; k < (this._miny + this._maxy) / 2; ++k)
                    {
                        int y = this._maxy - (k - this._miny);
                        string s = this.ContentAt2(j, k, i);
                        this.SetAt(j, k, i, this.ContentAt2(j, y, i));
                        this.SetAt(j, y, i, s);
                    }
                }
            }
        }

        public String ContentAt(int x, int y, int z)
        {
            Location location = new Location(x, y, z);
            string s = this.Tiles.ContainsKey(location) ? this.Tiles[location] : null;
            if (s == null)
            { Console.Error.WriteLine(String.Concat("Null at ", x, ",", y, ",", z, " Possible loading error")); }
            return s ?? "null";
        }

        public String ContentAt2(int x, int y, int z)
        {
            Location location = new Location(x, y, z);
            if (this.Tiles.ContainsKey(location))
            { return this.Tiles[location]; }
            return null;
        }

        public void SetAt(int x, int y, int z, string value)
        {
            if (this._sizeunknown)
            {
                this._minx = this._maxx = x;
                this._miny = this._maxy = y;
                this._minz = this._maxz = z;
                this._sizeunknown = false;
            }
            else
            {
                this._minx = Math.Min(this._minx, x);
                this._miny = Math.Min(this._miny, y);
                this._minz = Math.Min(this._minz, z);
                this._maxx = Math.Max(this._maxx, x);
                this._maxy = Math.Max(this._maxy, y);
                this._maxz = Math.Max(this._maxz, z);
            }
            Location location = new Location(x, y, z);
            if (this.Tiles.ContainsKey(location))
            { this.Tiles[location] = value; }
            else
            { this.Tiles.TryAdd(location, value); }
        }

        public void Save(string file)
        {
            this.SaveReferencing(file, null);
        }

        public void SaveReferencing(string filePath, Map map)
        {
            List<string> allTileValues;
            StringBuilder mapData = new StringBuilder();
            this.TileTypes.Clear();
            this.CodesByValue.Clear();
            List<string> tileValues = new List<string>();
            foreach (Location location in this.Tiles.Keys)
            {
                string s = this.Tiles[location];
                if (tileValues.Contains(s))
                { continue; }
                tileValues.Add(s);
            }
            Program.WriteLine(_mapName, String.Concat("We have ", tileValues.Count, " different tiles"));
            int letterCount = 1;
            int codeCapacity = Map.LetterCodes.Length;
            while (codeCapacity < tileValues.Count)
            {
                codeCapacity *= Map.LetterCodes.Length;
                ++letterCount;
            }
            if (map == null)
            { allTileValues = tileValues; }
            else
            {
                allTileValues = new List<string>();
                foreach (string value in tileValues)
                {
                    if (map.CodesByValue.ContainsKey(value))
                    {
                        string tileCode = map.GetIdFor(value);
                        this.TileTypes.TryAdd(tileCode, value);
                        this.CodesByValue.TryAdd(value, tileCode);
                    }
                    else
                    { allTileValues.Add(value); }
                }
                tileValues.Clear();
            }
            int position = 0;
            foreach (string tileValue in allTileValues)
            {
                string tileCode;
                do
                {
                    tileCode = this.IntToCode(position, letterCount);
                    position++;
                } while (this.TileTypes.ContainsKey(tileCode));
                this.TileTypes.TryAdd(tileCode, tileValue);
                this.CodesByValue.TryAdd(tileValue, tileCode);
            }
            allTileValues.Clear();
            position = 0;
            for (int i = 0; i < this.TileTypes.Count; i++)
            {
                string tileValue;
                string tileCode;
                do
                {
                    tileCode = this.IntToCode(position, letterCount);
                    position++;
                } while (!this.TileTypes.ContainsKey(tileCode));
                tileValue = this.TileTypes[tileCode];
                mapData.AppendLine(String.Concat("\"", tileCode, "\" = ", tileValue));
            }
            allTileValues.Clear();
            mapData.AppendLine();
            int totalZLevels = 1 + this._maxz - this._minz;
            ConcurrentBag<string> results = new ConcurrentBag<string>();
            Parallel.For(1, totalZLevels + 1, i => results.Add(SavingThread(i)));
            results.ToList().ForEach(zLevel => mapData.AppendLine(zLevel));

            File.WriteAllText(filePath, mapData.ToString());
        }

        public String GetIdFor(string value)
        {
            if (this.CodesByValue.ContainsKey(value))
            { return this.CodesByValue[value]; }
            return "???";
        }

        private String IntToCode(int tileId, int letterCount)
        {
            string tileCode = "";
            while (tileId >= Map.LetterCodes.Length)
            {
                int remainder = tileId % Map.LetterCodes.Length;
                tileCode = Map.LetterCodes[remainder] + tileCode;
                tileId -= remainder;
                tileId /= Map.LetterCodes.Length;
            }
            tileCode = Map.LetterCodes[tileId] + tileCode;
            while (tileCode.Length < letterCount)
            { tileCode = Map.LetterCodes[0] + tileCode; }
            return tileCode;
        }

        private void ConsumeBlobLine(string line, int codeSize, int x, int y, int z)
        {
            while (line.Trim().Length > 0)
            {
                if (this._minx > x)
                { this._minx = x; }
                if (this._maxx < x)
                { this._maxx = x; }

                string code = line.Substring(0, codeSize).Trim();
                if (code.Length == codeSize)
                { this.SetAt(x, y, z, this.TileTypes[code]); }
                line = line.Substring(codeSize);
                x++;
            }
        }

        private string SavingThread(int z)
        {
            StringBuilder result = new StringBuilder();
            int progress = 0;

            result.AppendLine("(" + this._minx.ToString() + "," + this._miny.ToString() + "," + z.ToString() + ") = {\"");
            int hundreth = (this._maxx - this._minx) * (this._maxy - this._miny) / 100;
            int loops = 0;
            for (int i = this._miny; i <= this._maxy; ++i)
            {
                for (int j = this._minx; j <= this._maxx; ++j)
                {
                    result.Append(this.GetIdFor(this.ContentAt(j, i, z)));
                    if (++loops < hundreth)
                    { continue; }
                    loops = 0;
                    ++progress;
                }
                result.AppendLine();
            }
            result.AppendLine("\"}");

            return result.ToString();
        }
    }
}
