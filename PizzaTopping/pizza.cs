using Newtonsoft.Json;

namespace PizzaTopping;

/// <summary>
/// Represents a single pizza order as loaded from the JSON data source.
/// </summary>
public class Pizza
{
    // Shared HttpClient — must not be disposed between calls (HttpClient is thread-safe).
    private static readonly HttpClient HttpClient = new();

    /// <summary>
    /// Gets or sets the list of toppings chosen for this pizza order.
    /// Populated automatically when deserialising the JSON data source.
    /// </summary>
    [JsonProperty("toppings")]
    public List<string>? Toppings { get; set; }

    /// <summary>
    /// Loads pizza orders from <paramref name="filePath"/> or <paramref name="url"/>,
    /// applies the requested analysis mode and filters, then writes results to stdout
    /// and optionally to an export file.
    /// </summary>
    /// <param name="filePath">Path to a local JSON file. Takes priority over <paramref name="url"/>.</param>
    /// <param name="url">Remote URL to fetch JSON data from. Used when <paramref name="filePath"/> is <see langword="null"/>.</param>
    /// <param name="topN">Maximum number of results to display (default: 15).</param>
    /// <param name="minOrders">Minimum order count a combination must have to be included (default: 1).</param>
    /// <param name="exportPath">File path to export results to (.csv or .json). Optional.</param>
    /// <param name="toppingFilter">Only include combinations that contain this exact topping. Optional.</param>
    /// <param name="comboSize">Only include combinations with exactly this many toppings (0 = any).</param>
    /// <param name="sortAsc">When <see langword="true"/>, sort results ascending (least popular first).</param>
    /// <param name="stdoutFormat">Structured output format: <c>"json"</c> or <c>"csv"</c>. <see langword="null"/> prints a formatted table.</param>
    /// <param name="singles">When <see langword="true"/>, rank individual toppings instead of combinations.</param>
    /// <param name="excludeTopping">Exclude combinations that contain this topping. Optional.</param>
    /// <param name="searchText">Only include combinations where at least one topping contains this substring. Optional.</param>
    /// <param name="stats">When <see langword="true"/>, print dataset statistics after the results.</param>
    /// <param name="minComboSize">Only include combinations with at least this many toppings (0 = no minimum).</param>
    /// <param name="maxComboSize">Only include combinations with at most this many toppings (0 = no maximum).</param>
    /// <param name="showPercent">When <see langword="true"/>, add a column showing each result's share of total orders.</param>
    /// <param name="coOccurrence">Rank toppings that most often appear alongside this topping. Optional.</param>
    /// <param name="offset">Number of results to skip before taking <paramref name="topN"/> (for pagination).</param>
    /// <param name="noHeader">When <see langword="true"/>, suppress the table header row and separator line.</param>
    /// <param name="verbose">When <see langword="true"/>, print source, record count, and active filters before results.</param>
    public async Task PizzaTop(
        string? filePath = null, string? url = null, int topN = 15, int minOrders = 1,
        string? exportPath = null, string? toppingFilter = null, int comboSize = 0,
        bool sortAsc = false, string? stdoutFormat = null, bool singles = false,
        string? excludeTopping = null, string? searchText = null, bool stats = false,
        int minComboSize = 0, int maxComboSize = 0,
        bool showPercent = false, string? coOccurrence = null, int offset = 0,
        bool noHeader = false, bool verbose = false)
    {
        List<Pizza>? pizzas = await LoadPizzas(filePath, url);
        if (pizzas == null) return;

        // Total orders is computed before any filtering, so --percent reflects the full dataset.
        int totalOrders = pizzas.Count;

        // Choose analysis mode: co-occurrence takes priority over singles, which takes priority over combos.
        IEnumerable<ToppingCombination> combinations;
        if (coOccurrence != null)
            combinations = GetCoOccurrence(pizzas, coOccurrence);
        else if (singles)
            combinations = GetSinglesCombo(pizzas);
        else
            combinations = GetTopCombo(pizzas);

        // Normalise filter values once so all comparisons are case-insensitive.
        string? normalizedFilter  = toppingFilter?.Trim().ToLowerInvariant();
        string? normalizedExclude = excludeTopping?.Trim().ToLowerInvariant();
        string? normalizedSearch  = searchText?.Trim().ToLowerInvariant();

        // Apply filters, ordering, and pagination.
        List<ToppingCombination> results = combinations
            .Where(tc => tc.Count >= minOrders)
            // --topping: combination must include this exact topping.
            .Where(tc => normalizedFilter  == null || SplitToppings(tc.Toppings).Contains(normalizedFilter))
            // --exclude-topping: combination must NOT include this topping.
            .Where(tc => normalizedExclude == null || !SplitToppings(tc.Toppings).Contains(normalizedExclude))
            // --search: at least one topping must contain the search text as a substring.
            .Where(tc => normalizedSearch  == null || SplitToppings(tc.Toppings).Any(t => t.Contains(normalizedSearch)))
            // --combo-size: exact topping count.
            .Where(tc => comboSize    == 0 || SplitToppings(tc.Toppings).Length == comboSize)
            // --min-combo-size / --max-combo-size: topping count range.
            .Where(tc => minComboSize == 0 || SplitToppings(tc.Toppings).Length >= minComboSize)
            .Where(tc => maxComboSize == 0 || SplitToppings(tc.Toppings).Length <= maxComboSize)
            .OrderBy(tc => sortAsc ? tc.Count : -tc.Count)
            // --offset / --top: pagination.
            .Skip(offset)
            .Take(topN)
            .ToList();

        // Local helper: percentage share of total orders as a formatted string.
        string PctStr(ToppingCombination r) =>
            $"{r.Count * 100.0 / (totalOrders == 0 ? 1 : totalOrders):F1}%";

        // --verbose: print source/mode/filter summary before results (table mode only).
        if (verbose && stdoutFormat == null)
        {
            string modeLabel = coOccurrence != null
                ? $"co-occurrence with '{coOccurrence.Trim().ToLowerInvariant()}'"
                : singles ? "singles" : "combos";

            Console.WriteLine($"Source:  {(filePath != null ? $"file: {filePath}" : $"url: {url ?? "default"}")}");
            Console.WriteLine($"Loaded:  {totalOrders} orders");
            Console.WriteLine($"Mode:    {modeLabel}");

            var activeFilters = new List<string>();
            if (normalizedFilter  != null) activeFilters.Add($"topping={normalizedFilter}");
            if (normalizedExclude != null) activeFilters.Add($"exclude={normalizedExclude}");
            if (normalizedSearch  != null) activeFilters.Add($"search={normalizedSearch}");
            if (comboSize    > 0) activeFilters.Add($"combo-size={comboSize}");
            if (minComboSize > 0) activeFilters.Add($"min-combo-size={minComboSize}");
            if (maxComboSize > 0) activeFilters.Add($"max-combo-size={maxComboSize}");
            if (minOrders    > 1) activeFilters.Add($"min-orders={minOrders}");
            if (offset       > 0) activeFilters.Add($"offset={offset}");
            Console.WriteLine($"Filters: {(activeFilters.Count > 0 ? string.Join(", ", activeFilters) : "none")}");
            Console.WriteLine();
        }

        if (stdoutFormat != null)
        {
            // Structured output — suitable for piping into other tools.
            if (stdoutFormat == "json")
            {
                if (showPercent)
                {
                    var data = results.Select((r, i) => new { Rank = offset + i + 1, Toppings = CleanToppings(r), Orders = r.Count, Percent = PctStr(r) });
                    Console.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));
                }
                else
                {
                    var data = results.Select((r, i) => new { Rank = offset + i + 1, Toppings = CleanToppings(r), Orders = r.Count });
                    Console.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));
                }
            }
            else if (stdoutFormat == "csv")
            {
                string header = showPercent ? "Rank,Toppings,Orders,Percent" : "Rank,Toppings,Orders";
                string RowFmt(ToppingCombination r, int i) => showPercent
                    ? $"{offset + i + 1},\"{CleanToppings(r)}\",{r.Count},{PctStr(r)}"
                    : $"{offset + i + 1},\"{CleanToppings(r)}\",{r.Count}";

                if (!noHeader) Console.WriteLine(header);
                Console.WriteLine(string.Join(Environment.NewLine, results.Select((r, i) => RowFmt(r, i))));
            }
        }
        else
        {
            // Table output with dynamically computed column widths.
            int startRank     = offset + 1;
            int endRank       = offset + results.Count;
            int rankWidth     = Math.Max(4, endRank.ToString().Length);
            int toppingsWidth = results.Count > 0 ? Math.Max(8, results.Max(r => CleanToppings(r).Length)) : 8;
            int ordersWidth   = results.Count > 0 ? Math.Max(6, results.Max(r => r.Count.ToString().Length)) : 6;
            int pctWidth      = showPercent ? 7 : 0; // "100.0%" = 6 chars; 7 with one space of padding.

            int    totalWidth = rankWidth + 2 + toppingsWidth + 2 + ordersWidth + (showPercent ? 2 + pctWidth : 0);
            string separator  = new string('-', totalWidth);

            if (!noHeader)
            {
                string header = showPercent
                    ? $"{"Rank".PadLeft(rankWidth)}  {"Toppings".PadRight(toppingsWidth)}  {"Orders".PadLeft(ordersWidth)}  {"%".PadLeft(pctWidth)}"
                    : $"{"Rank".PadLeft(rankWidth)}  {"Toppings".PadRight(toppingsWidth)}  {"Orders".PadLeft(ordersWidth)}";
                Console.WriteLine(header);
                Console.WriteLine(separator);
            }

            int rank = startRank;
            foreach (ToppingCombination combo in results)
            {
                string row = showPercent
                    ? $"{rank.ToString().PadLeft(rankWidth)}  {CleanToppings(combo).PadRight(toppingsWidth)}  {combo.Count.ToString().PadLeft(ordersWidth)}  {PctStr(combo).PadLeft(pctWidth)}"
                    : $"{rank.ToString().PadLeft(rankWidth)}  {CleanToppings(combo).PadRight(toppingsWidth)}  {combo.Count.ToString().PadLeft(ordersWidth)}";
                Console.WriteLine(row);
                rank++;
            }

            if (results.Count == 0)
                Console.WriteLine("No combinations match the specified filters.");
        }

        // --stats: dataset summary printed after the results table.
        if (stats)
        {
            var allToppings = pizzas
                .Where(p => p.Toppings != null)
                .SelectMany(p => p.Toppings!
                    .Select(t => t.Trim().ToLowerInvariant())
                    .Where(t => !string.IsNullOrEmpty(t)))
                .ToList();

            int uniqueToppings = allToppings.Distinct().Count();

            // DefaultIfEmpty(0) on the projected int sequence safely handles an empty dataset
            // without the risk of calling Average() on an empty collection (which throws).
            double avgComboSize = pizzas
                .Where(p => p.Toppings != null && p.Toppings.Count > 0)
                .Select(p => p.Toppings!.Count)
                .DefaultIfEmpty(0)
                .Average();

            string topTopping = allToppings
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? "N/A";

            Console.WriteLine();
            Console.WriteLine("--- Dataset Statistics ---");
            Console.WriteLine($"Total orders:     {totalOrders}");
            Console.WriteLine($"Unique toppings:  {uniqueToppings}");
            Console.WriteLine($"Avg combo size:   {avgComboSize:F2}");
            Console.WriteLine($"Most popular:     {topTopping}");
        }

        // Export results to file if a path was supplied.
        if (exportPath != null)
            Export(results, exportPath, offset);
    }

    // -------------------------------------------------------------------------
    // Private static helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Loads pizza orders from a local file or a remote URL.
    /// </summary>
    /// <param name="path">Local file path. When non-<see langword="null"/>, <paramref name="customUrl"/> is ignored.</param>
    /// <param name="customUrl">Remote URL. Falls back to the default dataset when <see langword="null"/>.</param>
    /// <returns>Deserialised list of pizza orders, or <see langword="null"/> on failure.</returns>
    private static async Task<List<Pizza>?> LoadPizzas(string? path, string? customUrl)
    {
        try
        {
            string json;

            if (path != null)
            {
                json = await File.ReadAllTextAsync(path);
            }
            else
            {
                // Always use HTTPS to ensure data is transmitted securely.
                string fetchUrl = customUrl ?? "https://brightway.com/CodeTests/pizzas.json";
                json = await HttpClient.GetStringAsync(fetchUrl);
            }

            return JsonConvert.DeserializeObject<List<Pizza>>(json);
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"Error: Could not reach the server. {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Error: Invalid JSON data. {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Returns toppings ranked by how often they appear alongside <paramref name="target"/>.
    /// </summary>
    /// <param name="pizzaList">The full list of pizza orders.</param>
    /// <param name="target">The reference topping name (case-insensitive).</param>
    /// <returns>
    /// Sequence of <see cref="ToppingCombination"/> objects — one per co-occurring topping —
    /// with <see cref="ToppingCombination.Toppings"/> set to the single topping name.
    /// </returns>
    private static IEnumerable<ToppingCombination> GetCoOccurrence(List<Pizza> pizzaList, string target)
    {
        string norm = target.Trim().ToLowerInvariant();
        return pizzaList
            .Where(p => p.Toppings != null && p.Toppings.Any(t => t.Trim().ToLowerInvariant() == norm))
            .SelectMany(p => p.Toppings!
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => !string.IsNullOrEmpty(t) && t != norm))
            .GroupBy(t => t)
            .Select(g => new ToppingCombination { Toppings = g.Key, Count = g.Count() });
    }

    /// <summary>
    /// Ranks individual toppings by the number of orders in which they appear.
    /// </summary>
    /// <param name="pizzaList">The full list of pizza orders.</param>
    /// <returns>
    /// Sequence of <see cref="ToppingCombination"/> objects — one per unique topping —
    /// with <see cref="ToppingCombination.Toppings"/> set to the single topping name.
    /// </returns>
    private static IEnumerable<ToppingCombination> GetSinglesCombo(List<Pizza> pizzaList)
    {
        return pizzaList
            .Where(p => p.Toppings != null)
            .SelectMany(p => p.Toppings!
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => !string.IsNullOrEmpty(t)))
            .GroupBy(t => t)
            .Select(g => new ToppingCombination { Toppings = g.Key, Count = g.Count() });
    }

    /// <summary>
    /// Groups pizza orders by their normalised, sorted topping combination and ranks by frequency.
    /// Toppings are lower-cased and sorted alphabetically before grouping so that
    /// order-variant duplicates (e.g. "bacon,cheese" vs "cheese,bacon") are merged.
    /// </summary>
    /// <param name="pizzaList">The full list of pizza orders.</param>
    /// <returns>
    /// Sequence of <see cref="ToppingCombination"/> objects with toppings joined as a
    /// comma-separated, alphabetically sorted string.
    /// </returns>
    private static IEnumerable<ToppingCombination> GetTopCombo(List<Pizza> pizzaList)
    {
        return pizzaList
            .Where(p => p.Toppings != null && p.Toppings.Count > 0)
            .Select(p => string.Join(",", p.Toppings!
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => !string.IsNullOrEmpty(t))
                .OrderBy(t => t)))
            .GroupBy(combo => combo)
            .Select(g => new ToppingCombination { Toppings = g.Key, Count = g.Count() });
    }

    /// <summary>
    /// Exports <paramref name="exportResults"/> to a CSV or JSON file.
    /// The format is inferred from the file extension (<c>.json</c> → JSON; anything else → CSV).
    /// </summary>
    /// <param name="exportResults">The ranked results to export.</param>
    /// <param name="path">Destination file path.</param>
    /// <param name="offset">Rank offset applied to result numbering.</param>
    private static void Export(List<ToppingCombination> exportResults, string path, int offset)
    {
        try
        {
            if (Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                var data = exportResults.Select((r, i) => new
                {
                    Rank     = offset + i + 1,
                    Toppings = CleanToppings(r),
                    Orders   = r.Count
                });
                File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented));
            }
            else
            {
                // Default export format is CSV.
                var csvRows = exportResults.Select((r, i) =>
                    $"{offset + i + 1},\"{CleanToppings(r)}\",{r.Count}");
                File.WriteAllText(path,
                    "Rank,Toppings,Orders" + Environment.NewLine +
                    string.Join(Environment.NewLine, csvRows));
            }
            Console.WriteLine($"Results exported to: {path}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error exporting results: {ex.Message}");
        }
    }

    /// <summary>
    /// Strips any leading comma from the combination's topping string.
    /// </summary>
    /// <param name="r">The combination whose topping string to clean.</param>
    /// <returns>The topping string with any leading comma removed.</returns>
    private static string CleanToppings(ToppingCombination r) => r.Toppings.TrimStart(',');

    /// <summary>
    /// Splits a comma-separated topping string into an array, removing any empty entries.
    /// </summary>
    /// <param name="toppings">The comma-separated topping string (e.g. <c>"bacon,pepperoni"</c>).</param>
    /// <returns>Array of individual topping names with empty entries removed.</returns>
    private static string[] SplitToppings(string toppings) =>
        toppings.Split(',', StringSplitOptions.RemoveEmptyEntries);
}
