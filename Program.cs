using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BTFTool
{
    internal class Program
    {
        static Regex regFormat = new Regex(@"^String\s*([0-9]+):\s*(.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static void Main(string[] args)
        {
            var arguments = ParseArguments(args);
            if (arguments.Count == 0 || arguments.ContainsKey("--help"))
            {
                PrintHelp();
                return;
            }
            if (arguments.ContainsKey("--verbose"))
            {
                ConsoleTraceListener listener = new ConsoleTraceListener();
                Trace.Listeners.Add(listener);
            }

            var btf = new BTF();
            if (File.Exists(args[0])) //load btf
            {
                var bytes = File.ReadAllBytes(args[0]);
                var ms = new MemoryStream(bytes);
                if (btf.TryParse(ms))
                {
                    Console.WriteLine($"File: {args[0]} successfully parsed");
                    Console.WriteLine($"Total records in btf: {btf.Records}");
                }
                else Console.WriteLine($"File: {args[0]} is not loaded (probably corrupted file)");
            }

            if (arguments.TryGetValue("--export", out string exportPath))
            {
                string d = Path.GetDirectoryName(Path.GetFullPath(exportPath));
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
                var data = btf.Export();
                var lines = data.Select(a => string.Format(@"String {0}: {1}", a.Key, a.Value.Escape())).ToArray();
                File.WriteAllLines(exportPath, lines, Encoding.UTF8);
                Console.WriteLine($"Export OK, Number of lines: {lines.Length}, File written: {exportPath}");
            }

            if (arguments.TryGetValue("--import", out string importPath))
            {
                var lines = File.ReadAllLines(importPath, Encoding.UTF8);
                var data = lines.Select(a => regFormat.Match(a)).Where(a => a.Success).ToDictionary(a => uint.Parse(a.Groups[1].Value), a => a.Groups[2].Value.Unescape());
                btf.Import(data);
                Console.WriteLine($"Strings replaced: {btf.Replaced}");
                if (btf.Created > 0) Console.WriteLine($"Strings created: {btf.Created}");
                if (btf.Removed > 0) Console.WriteLine($"Strings removed: {btf.Removed}");

                string d = Path.GetDirectoryName(Path.GetFullPath(args[0]));
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
                using (var fs = File.OpenWrite(args[0]))
                {
                    btf.WriteTo(fs);
                    Console.WriteLine($"Total records in btf: {btf.Records}");
                }
                Console.WriteLine($"File: {args[0]} successfully saved");
            }
            Console.WriteLine($"ALL DONE, app exit");
        }

        static void PrintHelp()
        {
            var exeVersion = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileVersionInfo.ProductVersion;
            var exeDescription = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;
            var exeFileName = Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);


            Console.WriteLine($@"Help:");
            Console.WriteLine($@"-----");
            Console.WriteLine($@"{exeDescription}");
            Console.WriteLine($@"It can export strings to a text file and import them back to a BTF file.");
            Console.WriteLine($@"Version {exeVersion}");
            Console.WriteLine($@"");
            Console.WriteLine($@"Usage: {exeFileName} btfFile [OPTIONS]");
            Console.WriteLine($@"  btfFile            Language file (*.btf) of the game Workers & Resources: Soviet Republic");
            Console.WriteLine($@"Options:");
            Console.WriteLine($@"  --help             Show this message and exit");
            Console.WriteLine($@"  --verbose          Enable verbose mode");
            Console.WriteLine($@"  --export=<file>    Specify the output file for the extracted strings from the btf file");
            Console.WriteLine($@"  --import=<file>    Provide an input file with strings to be inserted into the btf file");
            Console.WriteLine($@"");
            Console.WriteLine($@"Example:");
            Console.WriteLine($@"  {exeFileName} S:\Steam\steamapps\common\SovietRepublic\media_soviet\sovietEnglish.btf ""--export=S:\Data\EN.txt""");
            Console.WriteLine($@"  {exeFileName} S:\Steam\steamapps\common\SovietRepublic\media_soviet\sovietEnglish.btf ""--import=S:\Data\EN.txt""");
            Console.WriteLine($@"");
            Console.WriteLine($@"");
            Console.WriteLine($@"Format of the text file:");
            Console.WriteLine($@"  The format for import is the same as for export. File encoding is UTF-8 with BOM.");
            Console.WriteLine($@"  Each line is a new string. Format of the line is:");
            Console.WriteLine($@"");
            Console.WriteLine($@"  String <ID>: ""<string>""");
            Console.WriteLine($@"");
            Console.WriteLine($@"  <ID> is number of the string in the btf file.");
            Console.WriteLine($@"  <string> is the text of the string with escaped characters, so \r\n is a new line etc.");
            Console.WriteLine($@"           for more info visit: https://en.wikipedia.org/wiki/Escape_sequences_in_C");
            Console.WriteLine($@"  The exact regex used for the import (case insensitive): {regFormat}");
            Console.WriteLine($@"  If a line does not follow this format, it is ignored! So you can add your own comments.");
            Console.WriteLine($@"  If the line has the correct format but <string> is empty, the ID is removed from the btf file.");
            Console.WriteLine($@"  The imported file can contain only some strings, the rest will remain unchanged in the btf file.");
            Console.WriteLine($@"");

        }

        static Dictionary<string, string> ParseArguments(string[] args)
        {
            var arguments = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var arg in args)
            {
                string[] parts = arg.Split('=');
                if (parts.Length == 2) arguments[parts[0]] = parts[1];
                else arguments[arg] = null;
            }

            return arguments;
        }
    }
}
