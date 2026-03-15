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
    /// <param name="distribution">When <see langword="true"/>, show a topping-count histogram instead of the normal results table.</param>
    /// <param name="groupBySize">When <see langword="true"/>, insert section headers between combo-size groups in table output.</param>
    /// <param name="compareSource">File path or URL of a second dataset to compare against. Shows Current/Baseline/Delta columns.</param>
    public async Task PizzaTop(
        string? filePath = null, string? url = null, int topN = 15, int minOrders = 1,
        string? exportPath = null, string? toppingFilter = null, int comboSize = 0,
        bool sortAsc = false, string? stdoutFormat = null, bool singles = false,
        string? excludeTopping = null, string? searchText = null, bool stats = false,
        int minComboSize = 0, int maxComboSize = 0,
        bool showPercent = false, string? coOccurrence = null, int offset = 0,
        bool noHeader = false, bool verbose = false,
        bool distribution = false, bool groupBySize = false, string? compareSource = null)
    {
        List<Pizza>? pizzas = await LoadPizzas(filePath, url);
        if (pizzas == null) return;

        // Total orders is computed before any filtering, so --percent reflects the full dataset.
        int totalOrders = pizzas.Count;

        // --distribution: show a topping-count histogram and exit.
        if (distribution)
        {
            PrintDistribution(pizzas, totalOrders, showPercent, noHeader);
            if (exportPath != null) ExportDistribution(pizzas, totalOrders, exportPath);
            return;
        }

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

        // --compare: load a second dataset and show a side-by-side Current/Baseline/Delta table.
        if (compareSource != null)
        {
            bool isUrl = compareSource.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                      || compareSource.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

            List<Pizza>? baseline = await LoadPizzas(
                isUrl ? null : compareSource,
                isUrl ? compareSource : null);
            if (baseline == null) return;

            int baselineTotal = baseline.Count;

            // Build the baseline combo set using the same analysis mode as the current run.
            IEnumerable<ToppingCombination> baselineCombinations = coOccurrence != null
                ? GetCoOccurrence(baseline, coOccurrence)
                : singles ? GetSinglesCombo(baseline) : GetTopCombo(baseline);

            Dictionary<string, int> baselineLookup =
                baselineCombinations.ToDictionary(tc => tc.Toppings, tc => tc.Count);

            if (verbose)
            {
                Console.WriteLine($"Current:  {(filePath != null ? $"file: {filePath}" : $"url: {url ?? "default"}")} ({totalOrders} orders)");
                Console.WriteLine($"Baseline: {compareSource} ({baselineTotal} orders)");
                Console.WriteLine();
            }

            PrintComparison(results, baselineLookup, totalOrders, baselineTotal, offset, noHeader, showPercent);

            if (exportPath != null)
                ExportComparison(results, baselineLookup, totalOrders, baselineTotal, exportPath, offset, showPercent);

            return;
        }

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

            int rank             = startRank;
            int currentGroupSize = -1;

            foreach (ToppingCombination combo in results)
            {
                // --group-by-size: insert a labelled section header whenever the topping count changes.
                if (groupBySize)
                {
                    int size = SplitToppings(combo.Toppings).Length;
                    if (size != currentGroupSize)
                    {
                        if (currentGroupSize != -1) Console.WriteLine();
                        Console.WriteLine($"── {size} topping{(size == 1 ? "" : "s")} ──");
                        currentGroupSize = size;
                    }
                }

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
    // Private static helpers — data loading & analysis
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

    // -------------------------------------------------------------------------
    // Private static helpers — output & export
    // -------------------------------------------------------------------------

    /// <summary>
    /// Prints a histogram of how many pizza orders contain each number of toppings.
    /// Supports <c>--percent</c> and <c>--no-header</c>.
    /// </summary>
    /// <param name="pizzas">The full list of pizza orders.</param>
    /// <param name="totalOrders">Pre-computed total order count used for percentage calculations.</param>
    /// <param name="showPercent">When <see langword="true"/>, add a percentage column.</param>
    /// <param name="noHeader">When <see langword="true"/>, suppress the header row and separator.</param>
    private static void PrintDistribution(List<Pizza> pizzas, int totalOrders, bool showPercent, bool noHeader)
    {
        var dist = pizzas
            .GroupBy(p => p.Toppings?.Count ?? 0)
            .OrderBy(g => g.Key)
            .Select(g => (Size: g.Key, Count: g.Count()))
            .ToList();

        if (dist.Count == 0)
        {
            Console.WriteLine("No topping data available.");
            return;
        }

        int sizeWidth  = Math.Max("Toppings".Length, dist.Max(d => d.Size.ToString().Length));
        int countWidth = Math.Max("Orders".Length,   totalOrders.ToString().Length);
        int pctWidth   = showPercent ? "% of Total".Length : 0;

        int    totalWidth = sizeWidth + 2 + countWidth + (showPercent ? 2 + pctWidth : 0);
        string separator  = new string('-', totalWidth);

        if (!noHeader)
        {
            string header = showPercent
                ? $"{"Toppings".PadLeft(sizeWidth)}  {"Orders".PadLeft(countWidth)}  {"% of Total".PadLeft(pctWidth)}"
                : $"{"Toppings".PadLeft(sizeWidth)}  {"Orders".PadLeft(countWidth)}";
            Console.WriteLine(header);
            Console.WriteLine(separator);
        }

        foreach (var (size, count) in dist)
        {
            string pct = $"{count * 100.0 / (totalOrders == 0 ? 1 : totalOrders):F1}%";
            string row = showPercent
                ? $"{size.ToString().PadLeft(sizeWidth)}  {count.ToString().PadLeft(countWidth)}  {pct.PadLeft(pctWidth)}"
                : $"{size.ToString().PadLeft(sizeWidth)}  {count.ToString().PadLeft(countWidth)}";
            Console.WriteLine(row);
        }

        // Always print a total row so the table is self-contained.
        Console.WriteLine(separator);
        string totalRow = showPercent
            ? $"{"Total".PadLeft(sizeWidth)}  {totalOrders.ToString().PadLeft(countWidth)}  {"100.0%".PadLeft(pctWidth)}"
            : $"{"Total".PadLeft(sizeWidth)}  {totalOrders.ToString().PadLeft(countWidth)}";
        Console.WriteLine(totalRow);
    }

    /// <summary>
    /// Prints a side-by-side comparison table showing the current dataset's results
    /// against counts from a baseline dataset, with a signed delta column.
    /// </summary>
    /// <param name="results">Filtered and ranked results from the current dataset.</param>
    /// <param name="baselineLookup">Topping-string → order-count map built from the baseline dataset.</param>
    /// <param name="currentTotal">Total orders in the current dataset (for percentage calculations).</param>
    /// <param name="baselineTotal">Total orders in the baseline dataset (for percentage calculations).</param>
    /// <param name="offset">Rank offset applied to result numbering.</param>
    /// <param name="noHeader">When <see langword="true"/>, suppress the header row and separator.</param>
    /// <param name="showPercent">When <see langword="true"/>, show percentage share instead of raw counts; delta is in percentage points.</param>
    private static void PrintComparison(
        List<ToppingCombination> results,
        Dictionary<string, int> baselineLookup,
        int currentTotal, int baselineTotal,
        int offset, bool noHeader, bool showPercent)
    {
        int startRank     = offset + 1;
        int endRank       = offset + results.Count;
        int rankWidth     = Math.Max(4, endRank.ToString().Length);
        int toppingsWidth = results.Count > 0 ? Math.Max(8, results.Max(r => CleanToppings(r).Length)) : 8;

        // When showing percentages the value columns are fixed-width ("100.0%" = 6 chars).
        // When showing raw counts, size them to the largest value present.
        int currentWidth  = showPercent
            ? Math.Max(7,  "Current".Length)
            : Math.Max(7,  results.Count > 0 ? results.Max(r => r.Count.ToString().Length) : 7);
        int baselineWidth = showPercent
            ? Math.Max(8,  "Baseline".Length)
            : Math.Max(8,  baselineLookup.Values.DefaultIfEmpty(0).Max().ToString().Length);
        int deltaWidth    = showPercent
            ? Math.Max(8,  "Delta".Length)   // e.g. "+100.0%"
            : Math.Max(7,  "Delta".Length);  // e.g. "+99999"

        int    totalWidth = rankWidth + 2 + toppingsWidth + 2 + currentWidth + 2 + baselineWidth + 2 + deltaWidth;
        string separator  = new string('-', totalWidth);

        if (!noHeader)
        {
            string header = $"{"Rank".PadLeft(rankWidth)}  {"Toppings".PadRight(toppingsWidth)}  {"Current".PadLeft(currentWidth)}  {"Baseline".PadLeft(baselineWidth)}  {"Delta".PadLeft(deltaWidth)}";
            Console.WriteLine(header);
            Console.WriteLine(separator);
        }

        int rank = startRank;
        foreach (ToppingCombination combo in results)
        {
            bool inBaseline = baselineLookup.TryGetValue(combo.Toppings, out int baseCount);

            string currentStr, baselineStr, deltaStr;

            if (showPercent)
            {
                double curPct  = combo.Count * 100.0 / (currentTotal  == 0 ? 1 : currentTotal);
                double basePct = inBaseline ? baseCount * 100.0 / (baselineTotal == 0 ? 1 : baselineTotal) : 0.0;
                double delta   = curPct - basePct;

                currentStr  = $"{curPct:F1}%";
                baselineStr = inBaseline ? $"{basePct:F1}%" : "N/A";
                deltaStr    = !inBaseline ? "(new)" : (delta >= 0 ? $"+{delta:F1}%" : $"{delta:F1}%");
            }
            else
            {
                int delta = combo.Count - baseCount; // baseCount is 0 when !inBaseline

                currentStr  = combo.Count.ToString();
                baselineStr = inBaseline ? baseCount.ToString() : "N/A";
                deltaStr    = !inBaseline ? "(new)" : (delta >= 0 ? $"+{delta}" : delta.ToString());
            }

            string row = $"{rank.ToString().PadLeft(rankWidth)}  {CleanToppings(combo).PadRight(toppingsWidth)}  {currentStr.PadLeft(currentWidth)}  {baselineStr.PadLeft(baselineWidth)}  {deltaStr.PadLeft(deltaWidth)}";
            Console.WriteLine(row);
            rank++;
        }

        if (results.Count == 0)
            Console.WriteLine("No combinations match the specified filters.");
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
    /// Exports the topping-count distribution to a CSV or JSON file.
    /// Always includes a Percent column in the exported data.
    /// </summary>
    /// <param name="pizzas">The full list of pizza orders.</param>
    /// <param name="totalOrders">Pre-computed total order count.</param>
    /// <param name="path">Destination file path.</param>
    private static void ExportDistribution(List<Pizza> pizzas, int totalOrders, string path)
    {
        try
        {
            var dist = pizzas
                .GroupBy(p => p.Toppings?.Count ?? 0)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Size    = g.Key,
                    Orders  = g.Count(),
                    Percent = $"{g.Count() * 100.0 / (totalOrders == 0 ? 1 : totalOrders):F1}%"
                })
                .ToList();

            if (Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(dist, Formatting.Indented));
            }
            else
            {
                var csvRows = dist.Select(d => $"{d.Size},{d.Orders},{d.Percent}");
                File.WriteAllText(path,
                    "Size,Orders,Percent" + Environment.NewLine +
                    string.Join(Environment.NewLine, csvRows));
            }
            Console.WriteLine($"Distribution exported to: {path}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error exporting distribution: {ex.Message}");
        }
    }

    /// <summary>
    /// Exports a comparison result set to a CSV or JSON file, including Current, Baseline, and Delta columns.
    /// </summary>
    /// <param name="results">Filtered and ranked results from the current dataset.</param>
    /// <param name="baselineLookup">Topping-string → order-count map built from the baseline dataset.</param>
    /// <param name="currentTotal">Total orders in the current dataset.</param>
    /// <param name="baselineTotal">Total orders in the baseline dataset.</param>
    /// <param name="path">Destination file path.</param>
    /// <param name="offset">Rank offset applied to result numbering.</param>
    /// <param name="showPercent">When <see langword="true"/>, include percentage columns in addition to raw counts.</param>
    private static void ExportComparison(
        List<ToppingCombination> results,
        Dictionary<string, int> baselineLookup,
        int currentTotal, int baselineTotal,
        string path, int offset, bool showPercent)
    {
        try
        {
            // Build a unified row shape regardless of format so we don't duplicate the logic.
            var rows = results.Select((r, i) =>
            {
                bool found  = baselineLookup.TryGetValue(r.Toppings, out int bc);
                int  delta  = r.Count - bc; // bc is 0 when not found
                double curPct  = r.Count * 100.0 / (currentTotal  == 0 ? 1 : currentTotal);
                double basePct = found ? bc * 100.0 / (baselineTotal == 0 ? 1 : baselineTotal) : 0.0;

                return new
                {
                    Rank        = offset + i + 1,
                    Toppings    = CleanToppings(r),
                    Current     = r.Count,
                    Baseline    = found ? (int?)bc    : null,
                    Delta       = found ? (int?)delta : null,
                    CurrentPct  = $"{curPct:F1}%",
                    BaselinePct = found ? $"{basePct:F1}%" : "N/A",
                    DeltaPct    = found ? (delta >= 0 ? $"+{curPct - basePct:F1}%" : $"{curPct - basePct:F1}%") : "(new)"
                };
            }).ToList();

            if (Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                // Always write all fields to JSON so the file is self-describing.
                File.WriteAllText(path, JsonConvert.SerializeObject(rows, Formatting.Indented));
            }
            else
            {
                string FormatDelta(int? d) =>
                    d == null ? "(new)" : (d >= 0 ? $"+{d}" : d.ToString()!);

                string header = showPercent
                    ? "Rank,Toppings,Current,Baseline,Delta,Current%,Baseline%,Delta%"
                    : "Rank,Toppings,Current,Baseline,Delta";

                var csvRows = rows.Select(r => showPercent
                    ? $"{r.Rank},\"{r.Toppings}\",{r.Current},{r.Baseline?.ToString() ?? "N/A"},{FormatDelta(r.Delta)},{r.CurrentPct},{r.BaselinePct},{r.DeltaPct}"
                    : $"{r.Rank},\"{r.Toppings}\",{r.Current},{r.Baseline?.ToString() ?? "N/A"},{FormatDelta(r.Delta)}");

                File.WriteAllText(path,
                    header + Environment.NewLine +
                    string.Join(Environment.NewLine, csvRows));
            }
            Console.WriteLine($"Comparison exported to: {path}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error exporting comparison: {ex.Message}");
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
