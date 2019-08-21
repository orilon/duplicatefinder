using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DuplicateFinder
{
    class Program
    {
        class LineItem
        {
            public string FilePath { get; set; }
            public int LineNumber { get; set; }
        }
        class DuplicateItem
        {
            public DuplicateItem()
            {
                LineItems = new List<LineItem>();
            }
            public string Key { get; set; }
            public string Value { get; set; }
            public int Count { get; set; }
            public List<LineItem> LineItems { get; set; }
        }
        static void Main(string[] args)
        {
            bool _verbose = args.Contains("-v") || args.Contains("--verbose");
            bool _debug = args.Contains("-d") || args.Contains("--debug");

            Stopwatch sw = new Stopwatch();
            ConsoleColor _defaultColor = Console.ForegroundColor;

        rerun:
            sw.Restart();
            Console.WriteLine("Please specify the minimum count as duplicates (default is 2):");
            if (_verbose)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($" eg. this will be used to return the items that match and exceed a count of \"x\" items.");
                Console.ForegroundColor = _defaultColor;
            }

            int _minCount = 2;
            string? _minCountGiven = Console.ReadLine();
            if (!String.IsNullOrEmpty(_minCountGiven) && int.TryParse(_minCountGiven, out _minCount))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($" successfully changed minimum count for duplicate validations to {_minCount}");
                Console.ForegroundColor = _defaultColor;
            }
            if (_debug)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($" debug: mincount set to {_minCount}");
                Console.ForegroundColor = _defaultColor;
            }

            Console.WriteLine("Please specify the directory where the files are:");
            string _folder = Console.ReadLine();

            string folder = System.IO.Directory.GetCurrentDirectory();
            if (!String.IsNullOrEmpty(_folder) && System.IO.Directory.Exists(_folder))
                folder = _folder;

            if (_debug)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($" debug: folder is set to {folder}");
                Console.ForegroundColor = _defaultColor;
            }

            Console.WriteLine("Please specify the file extensions of files you want inspected:");
            string _extension = Console.ReadLine();
            if (String.IsNullOrEmpty(_extension))
                _extension = $"*";

            if (_debug)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($" debug: files with the {_extension} extension will be analysed");
                Console.ForegroundColor = _defaultColor;
            }

            Console.WriteLine("Please specify the content that will be looked for (key, etc):");
            string _contains = Console.ReadLine();

            if (_debug)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($" debug: lines containing {_contains} will be analysed");
                Console.ForegroundColor = _defaultColor;
            }

            Console.WriteLine($" analysing files for duplicates in {folder} ...");

            List<String> files = new List<string>();
            List<DuplicateItem> _duplicates = new List<DuplicateItem>();
            int totalFiles = 0;
            int totalDuplicates = 0;

            foreach (var filepath in System.IO.Directory.GetFiles(folder, $"*.{_extension}", System.IO.SearchOption.AllDirectories))
            {
                if (_debug)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($" debug: analysing file \"{System.IO.Path.GetFileName(filepath)}\"");
                    Console.ForegroundColor = _defaultColor;
                }

                totalFiles++;
                int _linenumber = 0;
                string line;
                System.IO.StreamReader file = new System.IO.StreamReader(filepath);
                while ((line = file.ReadLine()) != null)
                {
                    _linenumber++;
                    if (line.Contains(_contains))
                    {
                        //string _key = $"{System.Math.Abs(filepath.GetHashCode())}-{System.Math.Abs(_contains.GetHashCode())}";
                        string _key = $"{System.Math.Abs(line.GetHashCode())}";
                        var _duplicate = _duplicates.Find(x => x.Key == _key) ?? null;
                        if (_duplicate == null)
                        {
                            _duplicate = new DuplicateItem();
                            _duplicate.Count = 1;
                            _duplicate.LineItems = new List<LineItem>() { new LineItem() { FilePath = $"{filepath.Trim()}", LineNumber = _linenumber } };
                            _duplicate.Key = _key;
                            _duplicate.Value = line.Trim();
                            _duplicates.Add(_duplicate);
                        }
                        else
                        {
                            _duplicate.LineItems.Add(new LineItem() { FilePath = $"{filepath.Trim()}", LineNumber = _linenumber });
                            _duplicate.Count++;
                        }
                        totalDuplicates++;
                    }
                }
            }

            Console.WriteLine($"");
            Console.WriteLine($"Here are all the files with duplicate {_contains}");
            foreach (var _duplicate in _duplicates.FindAll(x => x.Count >= _minCount))
            {
                Console.Write($"Found ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"{_duplicate.Count} duplicates");
                Console.ForegroundColor = _defaultColor;
                Console.Write($" of ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{_duplicate.Value}");
                Console.ForegroundColor = _defaultColor;
                foreach (var line in _duplicate.LineItems)
                {
                    Console.Write($" in: {line.FilePath} ");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"(line {line.LineNumber})");
                    Console.ForegroundColor = _defaultColor;
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Would you like to export these results? (Y)es or (N)o, default is (N)o.");
            ConsoleKey _keyExport = Console.ReadKey(true).Key;
            if (_keyExport == ConsoleKey.Y)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"Results:");

                Console.ForegroundColor = _defaultColor;
                var _outputFile = $"duplicatefinder-{DateTime.Now.ToString("yyyyMMdd")}";

                var _outputJson = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(folder), $"{_outputFile}.json");
                var _json = JsonConvert.SerializeObject(_duplicates);
                Console.WriteLine($" creating json output to {_outputJson} ...");
                System.IO.File.WriteAllText(_outputJson, _json);

                var _outputYaml = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(folder), $"{_outputFile}.yaml");
                var serializer = new YamlDotNet.Serialization.Serializer();
                //serializer.Serialize(Console.Out, _duplicates);
                Console.WriteLine($" creating yaml output to {_outputYaml} ...");
                serializer.Serialize(System.IO.File.CreateText(_outputYaml), _duplicates);
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($" analysis of {totalDuplicates} duplicates in {totalFiles} files completed in {sw.Elapsed}");
            Console.ForegroundColor = _defaultColor;
            Console.WriteLine($"-------------------------------------------------------------------------------------");
            sw.Reset();
            goto rerun;
        }
    }
}
