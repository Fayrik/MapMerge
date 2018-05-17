using System;
using System.IO;
using System.Threading.Tasks;

namespace MapMerge
{
    internal static class Jobs
    {
        internal static void Diff(string original, string revised, string output)
        {
            if (!ValidateFilePath(ref original, true) || !ValidateFilePath(ref revised, true) || !ValidateFilePath(ref output, false, true))
            { return; }
            try
            {
                Map oldMap = null;
                Map newMap = null;
                Action[] tasks = new Action[] { () => oldMap = new Map(original), () => newMap = new Map(revised) };
                Parallel.ForEach(tasks, task => task.Invoke());
                int minX = Math.Max(oldMap._minx, newMap._minx);
                int maxX = Math.Min(oldMap._maxx, newMap._maxx);
                int minY = Math.Max(oldMap._miny, newMap._miny);
                int maxY = Math.Min(oldMap._maxy, newMap._maxy);
                int minZ = Math.Max(oldMap._minz, newMap._minz);
                int maxZ = Math.Min(oldMap._maxz, newMap._maxz);
                Program.WriteLine(String.Concat("Comparing: x(", minX, "-", maxX, ") y(", minY, "-", maxY, ") z(", minZ, "-", maxZ, ")"));

                int differences = 0;
                for (int i = minZ; i <= maxZ; ++i)
                {
                    Program.WriteLine("Z-level " + i);
                    for (int j = minY; j <= maxY; ++j)
                    {
                        for (int k = minX; k <= maxX; ++k)
                        {
                            if (oldMap.ContentAt(k, j, i).Equals(newMap.ContentAt(k, j, i))) continue;
                            File.AppendAllText(output, String.Concat("(", k.ToString(), ",", (1 + newMap._maxy - j).ToString(), ",", i.ToString(), ")=", newMap.ContentAt(k, j, i), Environment.NewLine));
                            differences++;
                        }
                    }
                }
                if (differences == 0)
                {
                    Program.WriteLine("Files do match");
                }
                else
                {
                    Program.WriteLine("Wrote out " + differences + " differences");
                }
                Program.WriteLine("Done");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return;
            }
        }

        internal static void Patch(string original, string diffFile, string output)
        {
            if (!ValidateFilePath(ref original, true) || !ValidateFilePath(ref diffFile, false) || !ValidateFilePath(ref output, true, true))
            { return; }
            try
            {
                Map map = new Map(original);
                string[] fileLines = File.ReadAllLines(diffFile);
                foreach (string lineSpace in fileLines)
                {
                    string line = lineSpace.Trim();
                    if (line.Length == 0) continue;
                    int split = line.IndexOf(",", 1);
                    int.TryParse(line.Substring(1, split - 1), out int x);
                    line = line.Substring(split);
                    split = line.IndexOf(",", 1);
                    int.TryParse(line.Substring(1, split - 1), out int y);
                    line = line.Substring(split);
                    split = line.IndexOf(")", 1);
                    int.TryParse(line.Substring(1, split - 1), out int z);
                    string value = line.Substring(line.IndexOf("=") + 1);
                    map.SetAt(x, 1 + map._maxy - y, z, value);
                }
                map.Save(output);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return;
            }
            Program.WriteLine("Done");
        }

        internal static void Pack(string unpacked, string output)
        {
            if (!ValidateFilePath(ref unpacked, false) || !ValidateFilePath(ref output, true, true))
            { return; }
            try
            {
                Map map = new Map();
                Program.WriteLine("Loading");
                string[] fileLines = File.ReadAllLines(unpacked);
                foreach (string lineSpace in fileLines)
                {
                    string line = lineSpace.Trim();
                    if (line.Length == 0) continue;
                    int split = line.IndexOf(",", 1);
                    int.TryParse(line.Substring(1, split - 1), out int x);
                    line = line.Substring(split);
                    split = line.IndexOf(",", 1);
                    int.TryParse(line.Substring(1, split - 1), out int y);
                    line = line.Substring(split);
                    split = line.IndexOf(")", 1);
                    int.TryParse(line.Substring(1, split - 1), out int z);
                    string value = line.Substring(line.IndexOf("=") + 1);
                    map.SetAt(x, y, z, value);
                }
                Program.WriteLine("Flipping");
                map.MirrorY();
                Program.WriteLine(String.Concat("Saving, bounds: x{", map._minx, " - ", map._maxx, "}, y{", map._miny, " - ", map._maxy, "}, z{", map._minz, " - ", map._maxz, "}"));
                map.Save(output);
                Program.WriteLine("Done");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return;
            }
        }

        internal static void UnPack(string packed, string output)
        {
            if (!ValidateFilePath(ref packed, true) || !ValidateFilePath(ref output, false, true))
            { return; }
            try
            {
                Program.WriteLine("Loading");
                Map map = new Map(packed);
                Program.WriteLine("Saving");
                for (int i = map._minz; i <= map._maxz; ++i)
                {
                    Program.WriteLine("Z-level " + i);
                    for (int j = map._miny; j <= map._maxy; ++j)
                    {
                        for (int k = map._minx; k <= map._maxx; ++k)
                        { File.AppendAllText(output, String.Concat("(", k.ToString(), ",", (1 + map._maxy - j).ToString(), ",", i.ToString(), ")=", map.ContentAt(k, j, i), Environment.NewLine)); }
                    }
                    File.AppendAllText(output, Environment.NewLine);
                }
                Program.WriteLine("Done");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return;
            }
        }

        internal static void Clean(string original, string revised, string output)
        {
            if (!ValidateFilePath(ref original, true) || !ValidateFilePath(ref revised, true) || !ValidateFilePath(ref output, true, true))
            { return; }
            try
            {
                Map oldMap = null;
                Map newMap = null;
                Action[] tasks = new Action[] { () => oldMap = new Map(original, true), () => newMap = new Map(revised) };
                Parallel.ForEach(tasks, task => task.Invoke());

                newMap.SaveReferencing(output, oldMap);
                Program.WriteLine("Done");
            }
            catch (Exception ex)
            { Console.Error.WriteLine(ex); }
        }

        internal static void Merge(string original, string local, string remote, string output)
        {
            if (!ValidateFilePath(ref original, true) || !ValidateFilePath(ref local, true) || !ValidateFilePath(ref remote, true) || !ValidateFilePath(ref original, true, true))
            { return; }
            try
            {
                Map originalMap = null;
                Map localMap = null;
                Map remoteMap = null;
                Map outputMap = new Map();
                Action[] tasks = new Action[] { () => originalMap = new Map(original), () => localMap = new Map(local), () => remoteMap = new Map(remote) };
                Parallel.ForEach(tasks, task => task.Invoke());
                bool alwaysLocal = false;
                bool alwaysRemote = false;
                if (originalMap.MinSize != localMap.MinSize || originalMap.MinSize != remoteMap.MinSize || originalMap.MaxSize != localMap.MaxSize || originalMap.MaxSize != remoteMap.MaxSize)
                {
                    Program.WriteLine("Map sizes differ");
                    Environment.Exit(1);
                }

                for (int i = originalMap._minz; i <= originalMap._maxz; ++i)
                {
                    for (int j = originalMap._miny; j <= originalMap._maxy; ++j)
                    {
                        for (int k = originalMap._minx; k <= originalMap._maxx; ++k)
                        {
                            bool localUnchanged = originalMap.ContentAt(k, j, i).Equals(localMap.ContentAt(k, j, i));
                            bool remoteUnchanged = originalMap.ContentAt(k, j, i).Equals(remoteMap.ContentAt(k, j, i));
                            bool remoteMatch = localMap.ContentAt(k, j, i).Equals(remoteMap.ContentAt(k, j, i));
                            if (!localUnchanged && !remoteUnchanged)
                            {
                                if (!remoteMatch)
                                {
                                    if (alwaysLocal)
                                    {
                                        outputMap.SetAt(k, j, i, localMap.ContentAt(k, j, i));
                                        continue;
                                    }
                                    if (alwaysRemote)
                                    {
                                        outputMap.SetAt(k, j, i, remoteMap.ContentAt(k, j, i));
                                        continue;
                                    }
                                    Console.WriteLine(String.Concat("(", k, ",", j, ",", i, ") local and remote don't match original and differ [\n\nLOCAL:  ", localMap.ContentAt(k, j, i), " \n\nREMOTE: ", remoteMap.ContentAt(k, j, i), " \n\nORIGIN: ", originalMap.ContentAt(k, j, i), "\n\n], please choose [local] [alwayslocal] [remote] [alwaysremote] [custom] or [exit]"));
                                    while (true)
                                    {
                                        String input = Console.ReadLine().ToLower();
                                        if (input == "local")
                                        {
                                            outputMap.SetAt(k, j, i, localMap.ContentAt(k, j, i));
                                            break;
                                        }
                                        if (input == "alwayslocal")
                                        {
                                            alwaysLocal = true;
                                            outputMap.SetAt(k, j, i, localMap.ContentAt(k, j, i));
                                            break;
                                        }
                                        if (input == "remote")
                                        {
                                            outputMap.SetAt(k, j, i, remoteMap.ContentAt(k, j, i));
                                            break;
                                        }
                                        if (input == "alwaysremote")
                                        {
                                            alwaysRemote = true;
                                            outputMap.SetAt(k, j, i, remoteMap.ContentAt(k, j, i));
                                            break;
                                        }
                                        if (input == "custom")
                                        {
                                            Console.Write("Tile data: ");
                                            String tileData = Console.ReadLine();
                                            outputMap.SetAt(k, j, i, tileData);
                                            break;
                                        }
                                        if (input == "exit" || input == "quit")
                                        { return; }
                                        Program.WriteLine("Invalid input.");
                                    }
                                }
                                outputMap.SetAt(k, j, i, localMap.ContentAt(k, j, i));
                                continue;
                            }
                            if (!localUnchanged)
                            {
                                outputMap.SetAt(k, j, i, localMap.ContentAt(k, j, i));
                                continue;
                            }
                            if (!remoteUnchanged)
                            {
                                outputMap.SetAt(k, j, i, remoteMap.ContentAt(k, j, i));
                                continue;
                            }
                            outputMap.SetAt(k, j, i, originalMap.ContentAt(k, j, i));
                        }
                    }
                }
                Program.WriteLine("Saving");
                outputMap.SaveReferencing(output, originalMap);
                Program.WriteLine("Done");
            }
            catch (Exception ex)
            { Console.Error.WriteLine(ex); }
        }

        private static bool ValidateFilePath(ref string filePath, bool dmm = false, bool write = false)
        {
            bool badPath = false;
            try { Path.Combine(filePath); }
            catch (Exception) { badPath = true; }
            if (badPath)
            {
                if (Program.silent)
                { return false; }

                Program.WriteLine("Error, invalid path.");
                string testPath = Program.FileDialogue(write, dmm);
                if (testPath == null)
                { return false; }

                filePath = testPath;
            }
            if (!write)
            {
                if (!File.Exists(filePath))
                {
                    if (Program.silent)
                    { return false; }

                    Program.WriteLine("Error, file not found. Please select new file.");
                    string testPath = Program.FileDialogue(write, dmm);
                    if (testPath == null)
                    { return false; }

                    filePath = testPath;
                }
            }
            else
            {
                if (File.Exists(filePath))
                {
                    if (Program.silent)
                    { return false; }

                    Console.WriteLine("File exists! Overwite?");
                    Console.Write("[y/N] >");
                    if (Console.ReadKey().Key != ConsoleKey.Y)
                    { return false; }
                }
            }
            return true;
        }
    }
}