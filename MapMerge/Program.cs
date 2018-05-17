using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace MapMerge
{
    class Program
    {
        private const string diffHelp = "usage: [exe] -diff [old_map] [new_map] [diff_file]";
        private const string patchHelp = "usage: [exe] -patch [old_map] [diff_file] [new_map]";
        private const string packHelp = "usage: [exe] -pack [unpacked] [packed.dmm]";
        private const string unpackHelp = "usage: [exe] -unpack [packed.dmm] [unpacked]";
        private const string cleanHelp = "usage: [exe] -clean [old_map] [new_map] [clean_map]";
        private const string mergeHelp = "usage: [exe] -merge [original] [local] [remote] [output]";
        internal static bool silent = false;

        [STAThread]
        static void Main(string[] args)
        {
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            if (args.Length == 0)
            {
                InteractiveMode();
                return;
            }
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower() != "-silent")
                { continue; }
                silent = true;
                while (i < args.Length - 1)
                {
                    args[i] = args[i + 1];
                    ++i;
                }
                string[] old = args;
                args = new string[args.Length - 1];
                Array.Copy(old, args, args.Length - 1);
                break;
            }
            if (args.Length > 0 && args[0].ToLower() == "-merge")
            {
                if (args.Length < 5)
                {
                    Console.WriteLine(mergeHelp);
                    Program.ReadKey();
                    return;
                }
                Jobs.Merge(args[1], args[2], args[3], args[4]);
            }
            else if (args.Length > 0 && args[0].ToLower() == "-diff")
            {
                if (args.Length < 4)
                {
                    Console.WriteLine(diffHelp);
                    Program.ReadKey();
                    return;
                }
                Jobs.Diff(args[1], args[2], args[3]);
            }
            else if (args.Length > 0 && args[0].ToLower() == "-patch")
            {
                if (args.Length < 4)
                {
                    Console.WriteLine(patchHelp);
                    Program.ReadKey();
                    return;
                }
                Jobs.Patch(args[1], args[2], args[3]);
            }
            else if (args.Length > 0 && args[0].ToLower() == "-pack")
            {
                if (args.Length < 3)
                {
                    Console.WriteLine(packHelp);
                    Program.ReadKey();
                    return;
                }
                Jobs.Pack(args[1], args[2]);
            }
            else if (args.Length > 0 && args[0].ToLower() == "-unpack")
            {
                if (args.Length < 3)
                {
                    Console.WriteLine(unpackHelp);
                    Program.ReadKey();
                    return;
                }
                Jobs.UnPack(args[1], args[2]);
            }
            else if (args.Length > 0 && args[0].ToLower() == "-clean")
            {
                if (args.Length < 4)
                {
                    Console.WriteLine(cleanHelp);
                    Program.ReadKey();
                    return;
                }
                Jobs.Clean(args[1], args[2], args[3]);
            }
            else
            {
                Console.WriteLine(diffHelp);
                Console.WriteLine(patchHelp);
                Console.WriteLine(packHelp);
                Console.WriteLine(unpackHelp);
                Console.WriteLine(cleanHelp);
                Console.WriteLine(mergeHelp);
                Program.ReadKey();
            }
        }

        internal static void WriteLine(string name, string value)
        { WriteLine(String.Concat("[", name, "]: ", value)); }

        internal static void WriteLine(string value)
        {
            if (!silent)
            { Console.WriteLine(String.Concat("[", DateTime.Now.ToString("HH:mm:ss"), "] ", value)); }
        }

        internal static void Write(string value)
        {
            if (!silent)
            { Console.Write(value); }
        }

        internal static ConsoleKeyInfo ReadKey()
        {
            if (!Program.silent)
            { return Console.ReadKey(); }
            return new ConsoleKeyInfo('\0', ConsoleKey.Escape, false, false, false);
        }

        internal static string ReadValidInput(string[] inputs, char[] chars = null)
        {
            string value = String.Empty;
            if (chars != null)
            {
                ConsoleKeyInfo cki = Console.ReadKey();
                if (cki.Key == ConsoleKey.Escape || cki.Key == ConsoleKey.Enter)
                { return null; }
                else if (chars.Contains(cki.KeyChar))
                { return cki.KeyChar.ToString(); }
                else
                { value = cki.KeyChar.ToString().ToLower(); }
            }
            while (true)
            {
                ConsoleKeyInfo cki = Console.ReadKey();
                if (cki.Key == ConsoleKey.Escape || cki.Key == ConsoleKey.Enter)
                { return null; }
                else if (cki.Key == ConsoleKey.Backspace)
                {
                    value = value.Substring(0, value.Length - 1);
                    Console.CursorLeft = Console.CursorLeft - 1;
                    Console.Write(" ");
                    Console.CursorLeft = Console.CursorLeft - 1;
                }
                value = (value + cki.KeyChar.ToString()).ToLower();
                if (inputs.Contains(value))
                { return value; }
            }
        }

        internal static string FileDialogue(bool save, bool dmm)
        {
            string defaultExt = "*";
            string filter = "All files (*.*)|*.*";
            if (dmm)
            { defaultExt = "dmm";  filter = "Map files (*.dmm)|*.dmm|All files (*.*)|*.*"; }
            if (!save)
            {
                OpenFileDialog ofd = new OpenFileDialog
                { CheckPathExists = true, CheckFileExists = true, DefaultExt = defaultExt, Filter = filter };
                DialogResult dr = ofd.ShowDialog();
                if (!dr.HasFlag(DialogResult.OK) || !System.IO.File.Exists(ofd.FileName))
                { return null; }

                return ofd.FileName;
            }
            else
            {
                SaveFileDialog sfd = new SaveFileDialog
                { CheckPathExists = true, OverwritePrompt = false, DefaultExt = defaultExt, Filter = filter };
                DialogResult dr = sfd.ShowDialog();
                if (!dr.HasFlag(DialogResult.OK))
                { return null; }

                return sfd.FileName;
            }
        }

        private static void InteractiveMode()
        {
            // Getting here means silent has to be false. So ignore it.
            Console.WriteLine("Available commands:");
            Console.WriteLine(" 0: diff");
            Console.WriteLine(" 1: patch");
            Console.WriteLine(" 2: pack");
            Console.WriteLine(" 3: unpack");
            Console.WriteLine(" 4: clean");
            Console.WriteLine(" 5: merge");
            Console.WriteLine(" 6: quit");
            Console.Write("[0-6]>");
            string input = null;
            while (input == null)
            {
                input = Program.ReadValidInput(new string[] { "diff", "patch", "pack", "unpack", "clean", "merge", "exit", "quit" }, new char[] { '0', '1', '2', '3', '4', '5', '6' });
                Console.WriteLine();
                if (input == null)
                {
                    Console.WriteLine("Invald input");
                    Console.Write("[0-6]>");
                }
            }
            if (input == "0" || input == "diff")
            {
                Console.WriteLine("Please select original map.");
                string original = Program.FileDialogue(false, true);
                if (original == null)
                { Console.WriteLine("Aborting."); return; }
                Console.WriteLine("Please select revised map.");
                string revised = Program.FileDialogue(false, true);
                if (revised == null)
                { Console.WriteLine("Aborting."); return; }
                Console.WriteLine("Please select where to save the output.");
                string output = Program.FileDialogue(true, false);
                if (output == null)
                { Console.WriteLine("Aborting."); return; }
                Jobs.Diff(original, revised, output);
            }
            else if (input == "1" || input == "patch")
            {
                Console.WriteLine("Please select original map.");
                string original = Program.FileDialogue(false, true);
                if (original == null)
                { Console.WriteLine("Aborting."); return; }

                Console.WriteLine("Please select the diff file.");
                string diff = Program.FileDialogue(false, false);
                if (diff == null)
                { Console.WriteLine("Aborting."); return; }

                Console.WriteLine("Please select where to save the output.");
                string output = Program.FileDialogue(true, true);
                if (output == null)
                { Console.WriteLine("Aborting."); return; }

                Jobs.Patch(original, diff, output);
            }
            else if (input == "2" || input == "pack")
            {
                Console.WriteLine("Please select unpacked file.");
                string unpacked = Program.FileDialogue(false, false);
                if (unpacked == null)
                { Console.WriteLine("Aborting."); return; }

                Console.WriteLine("Please select where to save the output.");
                string output = Program.FileDialogue(true, true);
                if (output == null)
                { Console.WriteLine("Aborting."); return; }

                Jobs.Pack(unpacked, output);
            }
            else if (input == "3" || input == "unpack")
            {
                Console.WriteLine("Please select packed map.");
                string packed = Program.FileDialogue(false, true);
                if (packed == null)
                { Console.WriteLine("Aborting."); return; }

                Console.WriteLine("Please select where to save the output.");
                string output = Program.FileDialogue(true, false);
                if (output == null)
                { Console.WriteLine("Aborting."); return; }

                Jobs.UnPack(packed, output);
            }
            else if (input == "4" || input == "clean")
            {
                Console.WriteLine("Please select original map.");
                string original = Program.FileDialogue(false, true);
                if (original == null)
                { Console.WriteLine("Aborting."); return; }

                Console.WriteLine("Please select revised map.");
                string revised = Program.FileDialogue(false, true);
                if (revised == null)
                { Console.WriteLine("Aborting."); return; }

                Console.WriteLine("Please select where to save the output.");
                string output = Program.FileDialogue(true, true);
                if (output == null)
                { Console.WriteLine("Aborting."); return; }

                Jobs.Clean(original, revised, output);
            }
            else if (input == "5" || input == "merge")
            {
                Console.WriteLine("Please select original map.");
                string original = Program.FileDialogue(false, true);
                if (original == null)
                { Console.WriteLine("Aborting."); return; }

                Console.WriteLine("Please select local map.");
                string local = Program.FileDialogue(false, true);
                if (local == null)
                { Console.WriteLine("Aborting."); return; }

                Console.WriteLine("Please select remote map.");
                string remote = Program.FileDialogue(false, true);
                if (remote == null)
                { Console.WriteLine("Aborting."); return; }

                Console.WriteLine("Please select where to save the output.");
                string output = Program.FileDialogue(true, true);
                if (output == null)
                { Console.WriteLine("Aborting."); return; }

                Jobs.Merge(original, local, remote, output);
            }
            else
            { return; }
            Console.WriteLine("Done.");
            System.Threading.Thread.Sleep(1000);
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            Console.WriteLine(e.Exception);
            Environment.Exit(1);
        }
    }
}
