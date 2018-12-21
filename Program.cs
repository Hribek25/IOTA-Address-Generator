using System;
using Tangle.Net.Entity;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;

namespace IOTAAddressGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("******************************");
            Console.WriteLine("Offline IOTA Address Generator based on a seed.");
            Console.WriteLine("More info: https://github.com/Hribek25");
            Console.WriteLine("******************************");
            Console.WriteLine();

            // default parameters
            var CmdParams = new Dictionary<string, string>()
            {
                { "level", "2" },
                { "seed", null },
                { "count", "10" },
                { "outfile", null},
                { "seedinfile", null}
            };

            if (args.Length>0 && args[0]=="--help") // help is needed
            {
                Console.WriteLine("Available params and their default values:");
                foreach (var item in CmdParams)
                {
                    Console.WriteLine($"{item.Key}={item.Value ?? "<not default value>"}");
                }
                Console.WriteLine();
                Console.WriteLine("Please note: Seed is a mandatory input either via \"seed\" or \"seedinfile\" parameters.");
                Console.WriteLine("Exiting...");
                return;
            }

            // checking commad-line parameters
            foreach (var item in args)
            {
                var param = item.Split("=");
                if (param.Length==2 && CmdParams.ContainsKey(param[0].ToLower()))
                {
                    CmdParams[param[0]] = param[1];
                }
            }

            // Params checks

            if (!(CmdParams["seedinfile"] is null)) // seed in file is specified
            {
                string seedfile;
                try
                {
                    seedfile = File.ReadAllText(CmdParams["seedinfile"]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Can't read a file with a seed at {CmdParams["seedinfile"]}");
                    Console.WriteLine($"ERROR: {ex.Message}. Exiting...");
                    return;
                }
                CmdParams["seed"] = seedfile;
            }

            if (CmdParams["seed"] is null)
            {
                Console.WriteLine("ERROR: Please specify a Seed or run the program with --help parameter.");
                Console.WriteLine("Exiting...");
                return;
            }
            else
            {
                if (CmdParams["seed"].Length!=81)
                {
                    Console.WriteLine($"ERROR: Seed should be exactly 81 chars long. The given seed is {CmdParams["seed"].Length} chars. Exiting...");
                    return;
                }

                if (!CommonHelpers.IsValidHash(CmdParams["seed"]))
                {
                    Console.WriteLine($"ERROR: Incorrect Seed specified. It should consist of A..Z9 chars only. The given seed is {CmdParams["seed"]}. Exiting...");
                    return;
                }
            }

            if (!int.TryParse(CmdParams["level"], out var Level) || !(Level == 1 | Level == 2 | Level == 3))
            {
                Console.WriteLine($"ERROR: Please specify a correct level. It should be 1..3. You specified {CmdParams["level"]}. Exiting...");
                return;
            }

            
            if (!int.TryParse(CmdParams["count"], out var CountAdr) || CountAdr < 1)
            {
                Console.WriteLine($"ERROR: Please specify a correct count. It should be numeric value. You specified {CmdParams["count"]}. Exiting.");
                return;
            }

            FileStream outfile = null;

            if (!(CmdParams["outfile"] is null))
            {
                try
                {
                    outfile = File.Create(CmdParams["outfile"]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Can't create the given file at {CmdParams["outfile"]}");
                    Console.WriteLine($"ERROR: {ex.Message}. Exiting...");
                    return;                    
                }
                outfile.Close();
            }
            else //outfile is not specified
            {
                Console.WriteLine($"Please note: OUTFILE param is not specified and so addresses are generated to STDOUT only.");
            }

            // Params checks
            
            // params overview
            Console.WriteLine("Parameters overview:");
            foreach (var item in CmdParams)
            {
                switch (item.Key)
                {
                    case "seed":
                        Console.WriteLine($"{item.Key} : {item.Value.Substring(0, 9)}{new String('*', item.Value.Length - 9)}");
                        break;
                    default:
                        Console.WriteLine($"{item.Key} : {item.Value}");
                        break;
                }
            }
            // params overview

            Console.WriteLine();
            Console.WriteLine("Generating IOTA addresses...");
            var sw = new Stopwatch();
            var Seed = new Seed(CmdParams["seed"]);
            var Addresses = new string[CountAdr];
            sw.Start();                       

            Parallel.For(
                fromInclusive: 0,
                toExclusive: CountAdr,
                body: (index) =>
                {
                    var AdrGenerator = new Tangle.Net.Cryptography.AddressGenerator();
                    var item = AdrGenerator.GetAddress(Seed, Level, index).WithChecksum();
                    Addresses[index] = $"{index}: {item.Value}{item.Checksum}";
                    Console.WriteLine($"{index}: {item.Value} {item.Checksum}");
                });            
            
            sw.Stop();
            Console.WriteLine($"It took {sw.ElapsedMilliseconds} ms in total. Meaning {(sw.ElapsedMilliseconds / CountAdr)} ms per address.");
            Console.WriteLine();

            if (!(outfile is null)) //let's write addresses to the outfile
            {
                File.WriteAllLines(CmdParams["outfile"], Addresses);
                Console.WriteLine($"Output written to: {CmdParams["outfile"]}");
            }
            Console.WriteLine();
        }

        public static class CommonHelpers
        {
            public static bool IsValidAddress(string address) =>
                Regex.IsMatch(address, @"^(([A-Z9]{90})|([A-Z9]{81}))$");
            
            public static bool IsValidHash(string hash) =>
                Regex.IsMatch(hash, @"^([A-Z9]{81})$");            
        }
    }
}
