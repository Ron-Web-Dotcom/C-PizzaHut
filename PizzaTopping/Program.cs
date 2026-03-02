using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace PizzaTopping
{
    class Program
    {
        static void Main(string[] args)
        {
            // Parse command-line options
            string filePath       = null;
            string url            = null;
            int    topN           = 15;
            int    minOrders      = 1;
            string exportPath     = null;
            string toppingFilter  = null;
            int    comboSize      = 0;
            bool   sortAsc        = false;
            string stdoutFormat   = null;
            bool   singles        = false;
            string excludeTopping = null;
            string searchText     = null;
            bool   stats          = false;
            int    minComboSize   = 0;
            int    maxComboSize   = 0;
            bool   showPercent    = false;
            string coOccurrence   = null;
            int    offset         = 0;
            bool   noHeader       = false;
            bool   verbose        = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--file":
                        if (i + 1 < args.Length) filePath = args[++i];
                        break;
                    case "--url":
                        if (i + 1 < args.Length) url = args[++i];
                        break;
                    case "--top":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int n)) topN = n;
                        break;
                    case "--min-orders":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int m)) minOrders = m;
                        break;
                    case "--export":
                        if (i + 1 < args.Length) exportPath = args[++i];
                        break;
                    case "--topping":
                        if (i + 1 < args.Length) toppingFilter = args[++i];
                        break;
                    case "--exclude-topping":
                        if (i + 1 < args.Length) excludeTopping = args[++i];
                        break;
                    case "--search":
                        if (i + 1 < args.Length) searchText = args[++i];
                        break;
                    case "--combo-size":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int cs)) comboSize = cs;
                        break;
                    case "--min-combo-size":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int mn)) minComboSize = mn;
                        break;
                    case "--max-combo-size":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int mx)) maxComboSize = mx;
                        break;
                    case "--sort":
                        if (i + 1 < args.Length) sortAsc = args[++i].Equals("asc", StringComparison.OrdinalIgnoreCase);
                        break;
                    case "--stdout":
                        if (i + 1 < args.Length) stdoutFormat = args[++i].ToLowerInvariant();
                        break;
                    case "--singles":
                        singles = true;
                        break;
                    case "--stats":
                        stats = true;
                        break;
                    case "--percent":
                        showPercent = true;
                        break;
                    case "--co-occurrence":
                        if (i + 1 < args.Length) coOccurrence = args[++i];
                        break;
                    case "--offset":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int off)) offset = off;
                        break;
                    case "--no-header":
                        noHeader = true;
                        break;
                    case "--verbose":
                        verbose = true;
                        break;
                    case "--help":
                        Console.WriteLine("Usage: PizzaTopping [options]");
                        Console.WriteLine();
                        Console.WriteLine("Data source:");
                        Console.WriteLine("  --file <path>             Read pizza data from a local JSON file");
                        Console.WriteLine("  --url <url>               Fetch pizza data from a custom URL");
                        Console.WriteLine();
                        Console.WriteLine("Analysis mode (pick one):");
                        Console.WriteLine("  --singles                 Rank individual toppings by frequency");
                        Console.WriteLine("  --co-occurrence <topping> Rank toppings that appear most often alongside <topping>");
                        Console.WriteLine("  (default)                 Rank topping combinations by frequency");
                        Console.WriteLine();
                        Console.WriteLine("Output control:");
                        Console.WriteLine("  --top <n>                 Show top N results (default: 15)");
                        Console.WriteLine("  --offset <n>              Skip first N results — for pagination with --top");
                        Console.WriteLine("  --sort asc|desc           Sort order — asc = least popular first (default: desc)");
                        Console.WriteLine("  --percent                 Add a % column showing share of total orders");
                        Console.WriteLine("  --no-header               Suppress header row and separator line");
                        Console.WriteLine("  --verbose                 Print data source, record count, and active filters");
                        Console.WriteLine("  --stdout json|csv         Write results as JSON or CSV to stdout instead of a table");
                        Console.WriteLine("  --export <file>           Export results to a .csv or .json file");
                        Console.WriteLine("  --stats                   Print dataset statistics after the results");
                        Console.WriteLine();
                        Console.WriteLine("Filters:");
                        Console.WriteLine("  --min-orders <n>          Only show results ordered at least N times (default: 1)");
                        Console.WriteLine("  --topping <name>          Only show combos that contain an exact topping name");
                        Console.WriteLine("  --exclude-topping <name>  Exclude combos that contain a specific topping");
                        Console.WriteLine("  --search <text>           Only show combos where any topping contains the text");
                        Console.WriteLine("  --combo-size <n>          Only show combos with exactly N toppings");
                        Console.WriteLine("  --min-combo-size <n>      Only show combos with at least N toppings");
                        Console.WriteLine("  --max-combo-size <n>      Only show combos with at most N toppings");
                        return;
                }
            }

            Pizza p = new Pizza();
            p.PizzaTop(filePath, url, topN, minOrders, exportPath, toppingFilter, comboSize,
                       sortAsc, stdoutFormat, singles, excludeTopping, searchText, stats,
                       minComboSize, maxComboSize, showPercent, coOccurrence, offset,
                       noHeader, verbose);
        }
    }
}
