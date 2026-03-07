using System;

namespace PizzaTopping
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Parse command-line options
            string filePath      = null;
            string url           = null;
            int    topN          = 15;
            int    minOrders     = 1;
            string exportPath    = null;
            string toppingFilter = null;
            int    comboSize     = 0;
            bool   sortAsc       = false;
            string stdoutFormat  = null;
            bool   singles       = false;

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
                        // Issue 3 fix: --top 0 or negative caused ArgumentOutOfRangeException
                        //   (.NET Core 3.1) or silently returned empty results. Require > 0.
                        if (i + 1 < args.Length)
                        {
                            if (int.TryParse(args[++i], out int n) && n > 0)
                                topN = n;
                            else
                            {
                                Console.Error.WriteLine($"Error: --top must be a positive integer (got '{args[i]}').");
                                return;
                            }
                        }
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
                    case "--combo-size":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int cs)) comboSize = cs;
                        break;
                    case "--sort":
                        // Issue 2 fix: any value other than "asc"/"desc" was silently treated as
                        //   "desc" with no feedback; now warn the user about the unrecognised value.
                        if (i + 1 < args.Length)
                        {
                            string sortVal = args[++i];
                            if (sortVal.Equals("asc", StringComparison.OrdinalIgnoreCase))
                                sortAsc = true;
                            else if (sortVal.Equals("desc", StringComparison.OrdinalIgnoreCase))
                                sortAsc = false;
                            else
                                Console.Error.WriteLine($"Warning: Unknown --sort value '{sortVal}'. Expected 'asc' or 'desc'. Defaulting to 'desc'.");
                        }
                        break;
                    case "--stdout":
                        if (i + 1 < args.Length) stdoutFormat = args[++i].ToLowerInvariant();
                        break;
                    case "--singles":
                        singles = true;
                        break;
                    case "--help":
                        Console.WriteLine("Usage: PizzaTopping [options]");
                        Console.WriteLine("  --file <path>       Read pizza data from a local JSON file");
                        Console.WriteLine("  --url <url>         Fetch pizza data from a custom URL");
                        Console.WriteLine("  --top <n>           Show top N combinations (default: 15, must be > 0)");
                        Console.WriteLine("  --min-orders <n>    Only show combos ordered at least N times (default: 1)");
                        Console.WriteLine("  --export <file>     Export results to a .csv or .json file");
                        Console.WriteLine("  --topping <name>    Only show combos that contain a specific topping");
                        Console.WriteLine("  --combo-size <n>    Only show combos with exactly N toppings");
                        Console.WriteLine("  --sort asc|desc     Sort order — asc = least popular first (default: desc)");
                        Console.WriteLine("  --stdout json|csv   Print results as JSON or CSV to stdout instead of a table");
                        Console.WriteLine("  --singles           Rank individual toppings by frequency instead of combos");
                        return;
                }
            }

            Pizza p = new Pizza();
            p.PizzaTop(filePath, url, topN, minOrders, exportPath, toppingFilter, comboSize, sortAsc, stdoutFormat, singles);
        }
    }
}
