namespace PizzaTopping;

/// <summary>
/// Entry point for the PizzaTopping CLI application.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Parses command-line arguments and delegates analysis to <see cref="Pizza.PizzaTop"/>.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    static async Task Main(string[] args)
    {
        // -- Data source --
        string? filePath     = null;
        string? url          = null;

        // -- Output control --
        int     topN         = 15;
        int     offset       = 0;
        bool    sortAsc      = false;
        bool    showPercent  = false;
        bool    noHeader     = false;
        bool    verbose      = false;
        string? stdoutFormat = null;
        string? exportPath   = null;
        bool    stats        = false;

        // -- Analysis mode --
        bool    singles      = false;
        string? coOccurrence = null;

        // -- Filters --
        int     minOrders      = 1;
        string? toppingFilter  = null;
        string? excludeTopping = null;
        string? searchText     = null;
        int     comboSize      = 0;
        int     minComboSize   = 0;
        int     maxComboSize   = 0;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                // -- Data source --
                case "--file":
                    if (i + 1 < args.Length) filePath = args[++i];
                    break;
                case "--url":
                    if (i + 1 < args.Length) url = args[++i];
                    break;

                // -- Output control --
                case "--top":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int n)) topN = n;
                    break;
                case "--offset":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int off)) offset = off;
                    break;
                case "--sort":
                    if (i + 1 < args.Length) sortAsc = args[++i].Equals("asc", StringComparison.OrdinalIgnoreCase);
                    break;
                case "--percent":
                    showPercent = true;
                    break;
                case "--no-header":
                    noHeader = true;
                    break;
                case "--verbose":
                    verbose = true;
                    break;
                case "--stdout":
                    if (i + 1 < args.Length) stdoutFormat = args[++i].ToLowerInvariant();
                    break;
                case "--export":
                    if (i + 1 < args.Length) exportPath = args[++i];
                    break;
                case "--stats":
                    stats = true;
                    break;

                // -- Analysis mode --
                case "--singles":
                    singles = true;
                    break;
                case "--co-occurrence":
                    if (i + 1 < args.Length) coOccurrence = args[++i];
                    break;

                // -- Filters --
                case "--min-orders":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int m)) minOrders = m;
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

                case "--help":
                    PrintHelp();
                    return;
            }
        }

        await new Pizza().PizzaTop(
            filePath, url, topN, minOrders, exportPath, toppingFilter, comboSize,
            sortAsc, stdoutFormat, singles, excludeTopping, searchText, stats,
            minComboSize, maxComboSize, showPercent, coOccurrence, offset,
            noHeader, verbose);
    }

    /// <summary>
    /// Prints usage information and all available options to stdout.
    /// </summary>
    private static void PrintHelp()
    {
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
    }
}
