using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;

namespace PizzaTopping
{
    /// <summary>
    /// Data model for a single pizza order as deserialized from the source JSON.
    /// Also exposes <see cref="PizzaTop"/>, the main entry point that orchestrates
    /// data loading, analysis, filtering, and output.
    /// </summary>
    internal class Pizza
    {
        /// <summary>
        /// Raw comma-separated topping string for this pizza order, mapped directly
        /// from the <c>"toppings"</c> field in the source JSON.
        /// Example: <c>"pepperoni, Mushrooms, CHEESE"</c>.
        /// Normalisation (trim, lower-case, sort) is applied during analysis, not here.
        /// </summary>
        public string toppings { get; set; }

        /// <summary>
        /// Loads pizza order data, runs the requested analysis, and writes results
        /// to the console and/or an export file.
        /// </summary>
        /// <param name="filePath">
        /// Path to a local JSON file. When supplied, <paramref name="url"/> is ignored.
        /// </param>
        /// <param name="url">
        /// Custom URL to fetch JSON from. Falls back to the built-in default URL when
        /// <c>null</c> and <paramref name="filePath"/> is also <c>null</c>.
        /// </param>
        /// <param name="topN">Maximum number of results to display (must be &gt; 0).</param>
        /// <param name="minOrders">
        /// Minimum order-count threshold — entries below this value are excluded.
        /// </param>
        /// <param name="exportPath">
        /// Optional path for a results export file. Extension determines format:
        /// <c>.json</c> produces indented JSON; anything else produces CSV.
        /// </param>
        /// <param name="toppingFilter">
        /// When set, only results whose canonical topping key contains this value
        /// (case-insensitive, trimmed) are shown.
        /// </param>
        /// <param name="comboSize">
        /// When &gt; 0, only combos with exactly this many toppings are shown.
        /// </param>
        /// <param name="sortAsc">
        /// <c>true</c> = sort least-popular first (ascending);
        /// <c>false</c> (default) = most-popular first (descending).
        /// </param>
        /// <param name="stdoutFormat">
        /// Structured output format for stdout: <c>"json"</c> or <c>"csv"</c>.
        /// When <c>null</c> a human-readable table is printed instead.
        /// </param>
        /// <param name="singles">
        /// When <c>true</c>, run singles mode: rank individual toppings by how many
        /// pizzas they appear on, rather than ranking full combos.
        /// Mutually exclusive with <paramref name="pairs"/>.
        /// </param>
        /// <param name="showChart">
        /// When <c>true</c>, append a 20-character Unicode block bar (████░░░) to each
        /// table row, scaled proportionally to the top result.
        /// No effect when <paramref name="stdoutFormat"/> is set.
        /// </param>
        /// <param name="showStats">
        /// When <c>true</c>, print a dataset summary (total pizzas, unique toppings,
        /// average toppings per pizza, etc.) before the results. When
        /// <paramref name="stdoutFormat"/> is also set the summary goes to
        /// <see cref="Console.Error"/> so stdout remains pipe-safe.
        /// </param>
        /// <param name="pairs">
        /// When <c>true</c>, run pairs mode: rank every unique 2-topping co-occurrence
        /// by how many pizzas share that pair (market-basket style).
        /// Mutually exclusive with <paramref name="singles"/>.
        /// </param>
        /// <param name="excludeTopping">
        /// When set, all pizzas containing this topping are removed from the dataset
        /// before any analysis runs. Useful for "what do non-pepperoni orders look like?"
        /// </param>
        public void PizzaTop(string filePath = null, string url = null, int topN = 15, int minOrders = 1,
            string exportPath = null, string toppingFilter = null, int comboSize = 0,
            bool sortAsc = false, string stdoutFormat = null, bool singles = false,
            bool showChart = false, bool showStats = false, bool pairs = false,
            string excludeTopping = null)
        {
            List<Pizza> pizzaList = GetPizza(filePath, url);
            if (pizzaList == null) return;

            // --exclude: strip pizzas containing the specified topping before any
            // analysis so all downstream modes operate on the filtered dataset.
            if (excludeTopping != null)
            {
                string excl = excludeTopping.Trim().ToLowerInvariant();
                pizzaList = pizzaList
                    .Where(p => p.toppings == null || !p.toppings.Split(',')
                        .Select(t => t.Trim().ToLowerInvariant())
                        .Contains(excl))
                    .ToList();

                if (pizzaList.Count == 0)
                {
                    Console.Error.WriteLine($"Warning: No pizzas remain after excluding '{excl}'.");
                    return;
                }
            }

            // Select the analysis mode. Priority: pairs > singles > full combos (default).
            IEnumerable<ToppingCombination> topcombination =
                pairs   ? GetPairsCombo(pizzaList)   :
                singles ? GetSinglesCombo(pizzaList)  :
                          GetTopCombo(pizzaList);

            // Normalise the topping filter once so all comparisons are lower-case.
            string normalizedFilter = toppingFilter?.Trim().ToLowerInvariant();

            // Apply filters, sort, and truncate to topN.
            // Each Where guard checks tc.Toppings != null before calling Split to
            // prevent a NullReferenceException if a degenerate entry slips through.
            List<ToppingCombination> results = topcombination
                .Where(tc => tc.Count >= minOrders)
                .Where(tc => normalizedFilter == null || (tc.Toppings != null && tc.Toppings.Split(',').Contains(normalizedFilter)))
                .Where(tc => comboSize == 0          || (tc.Toppings != null && tc.Toppings.Split(',').Length == comboSize))
                .OrderBy(oi => sortAsc ? oi.Count : -oi.Count)
                .Take(topN)
                .ToList();

            // Strip any accidental leading comma from the canonical topping key.
            // Defensive: string.Join never produces a leading comma, but belt-and-braces.
            string CleanToppings(ToppingCombination r) => (r.Toppings ?? "").TrimStart(',');

            // --stats: write summary to stdout normally; redirect to stderr when
            // --stdout is active so the structured output remains pipe-safe.
            if (showStats)
            {
                TextWriter statsOut = stdoutFormat != null ? Console.Error : Console.Out;
                PrintStats(pizzaList, results, statsOut);
            }

            if (stdoutFormat != null)
            {
                // Structured output modes — intended for piping into other tools.
                if (stdoutFormat == "json")
                {
                    var data = results.Select((r, i) => new { Rank = i + 1, Toppings = CleanToppings(r), Orders = r.Count });
                    Console.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));
                }
                else if (stdoutFormat == "csv")
                {
                    var csvRows = results.Select((r, i) => $"{i + 1},\"{CleanToppings(r)}\",{r.Count}");
                    Console.WriteLine("Rank,Toppings,Orders");
                    Console.WriteLine(string.Join(Environment.NewLine, csvRows));
                }
                else
                {
                    // Unknown format — fail fast with a clear message rather than producing
                    // silent empty output, which is hard for the caller to diagnose.
                    Console.Error.WriteLine($"Error: Unknown --stdout format '{stdoutFormat}'. Valid values are: json, csv");
                    Environment.Exit(1);
                }
            }
            else
            {
                // Human-readable table with dynamically-sized columns.
                int rankWidth     = Math.Max(4, results.Count.ToString().Length);
                int toppingsWidth = results.Count > 0 ? Math.Max(8, results.Max(r => CleanToppings(r).Length)) : 8;
                int ordersWidth   = results.Count > 0 ? Math.Max(6, results.Max(r => r.Count.ToString().Length)) : 6;

                // --chart: fixed 20-char bar column using Unicode block elements (█ / ░).
                // The top result always fills all 20 blocks; every other bar is scaled
                // proportionally so relative differences are immediately visible.
                const int barWidth = 20;
                int maxCount = results.Count > 0 ? results.Max(r => r.Count) : 1;

                string header    = $"{"Rank".PadLeft(rankWidth)}  {"Toppings".PadRight(toppingsWidth)}  {"Orders".PadLeft(ordersWidth)}";
                int separatorLen = rankWidth + 2 + toppingsWidth + 2 + ordersWidth;
                if (showChart)
                {
                    header       += $"  {"Distribution".PadRight(barWidth)}";
                    separatorLen += 2 + barWidth;
                }

                Console.WriteLine(header);
                Console.WriteLine(new string('-', separatorLen));

                int num = 1;
                foreach (ToppingCombination taste in results)
                {
                    string line = $"{num.ToString().PadLeft(rankWidth)}  {CleanToppings(taste).PadRight(toppingsWidth)}  {taste.Count.ToString().PadLeft(ordersWidth)}";
                    if (showChart)
                    {
                        int filled = maxCount > 0 ? (int)Math.Round((double)taste.Count / maxCount * barWidth) : 0;
                        // '█' = full block █   '░' = light shade ░
                        string bar = new string('█', filled).PadRight(barWidth, '░');
                        line += $"  {bar}";
                    }
                    Console.WriteLine(line);
                    num++;
                }

                if (results.Count == 0)
                    Console.WriteLine("No combinations match the specified filters.");
            }

            // Export to file if requested. Runs after console output so the
            // "Results exported to:" confirmation appears at the bottom.
            if (exportPath != null)
                Export(results, exportPath);


            // ── local functions ───────────────────────────────────────────────────

            /// <summary>
            /// Loads pizza order data from <paramref name="path"/> (local file) or
            /// <paramref name="customUrl"/> (HTTP/S). Returns <c>null</c> on any error
            /// after printing a descriptive message to stderr.
            /// </summary>
            List<Pizza> GetPizza(string path, string customUrl)
            {
                try
                {
                    string json;

                    if (path != null)
                    {
                        // Read the entire file at once; FileNotFoundException is caught below.
                        json = File.ReadAllText(path);
                    }
                    else
                    {
                        // HttpClient replaces the obsolete HttpWebRequest API.
                        // GetStringAsync throws HttpRequestException for non-success status
                        // codes, so no manual status check is required.
                        string fetchUrl = customUrl ?? "https://brightway.com/CodeTests/pizzas.json";
                        using var client = new HttpClient();
                        json = client.GetStringAsync(fetchUrl).GetAwaiter().GetResult();
                    }

                    return JsonConvert.DeserializeObject<List<Pizza>>(json);
                }
                catch (HttpRequestException ex)
                {
                    Console.Error.WriteLine($"Error: Could not fetch data from server. {ex.Message}");
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
            /// Writes a human-readable dataset summary to <paramref name="output"/>.
            /// Called before the main results so the user sees context first.
            /// Routed to stderr by the caller when <c>--stdout</c> is active.
            /// </summary>
            void PrintStats(List<Pizza> list, List<ToppingCombination> res, TextWriter output)
            {
                // Flatten all individual toppings across the dataset for aggregate stats.
                var allToppings = list
                    .Where(p => p.toppings != null)
                    .SelectMany(p => p.toppings.Split(',')
                        .Select(t => t.Trim().ToLowerInvariant())
                        .Where(t => !string.IsNullOrEmpty(t)))
                    .ToList();

                int pizzasWithToppings = list.Count(p => !string.IsNullOrWhiteSpace(p.toppings));
                // Average based only on pizzas that actually had toppings to avoid
                // skewing the figure with blank/null entries.
                double avgToppings = pizzasWithToppings > 0 ? (double)allToppings.Count / pizzasWithToppings : 0;

                output.WriteLine("=== Dataset Summary ===");
                output.WriteLine($"  Total pizzas loaded : {list.Count}");
                output.WriteLine($"  Pizzas with toppings: {pizzasWithToppings}");
                output.WriteLine($"  Unique toppings     : {allToppings.Distinct().Count()}");
                output.WriteLine($"  Total topping uses  : {allToppings.Count}");
                output.WriteLine($"  Avg toppings/pizza  : {avgToppings:F2}");
                output.WriteLine($"  Results shown       : {res.Count}");
                output.WriteLine();
            }

            /// <summary>
            /// Singles mode: counts how many pizzas each individual topping appears on.
            /// Toppings are normalised (trimmed, lower-cased) before grouping so
            /// "Bacon", " bacon ", and "BACON" all contribute to the same bucket.
            /// </summary>
            IEnumerable<ToppingCombination> GetSinglesCombo(List<Pizza> list)
            {
                return list
                    .Where(pizza => pizza.toppings != null)
                    .SelectMany(pizza => pizza.toppings.Split(',')
                        .Select(t => t.Trim().ToLowerInvariant())
                        .Where(t => !string.IsNullOrEmpty(t)))
                    .GroupBy(t => t)
                    .Select(g => new ToppingCombination { Toppings = g.Key, Count = g.Count() });
            }

            /// <summary>
            /// Pairs mode: for each pizza, generates every unique 2-topping combination
            /// C(n,2) from its normalised topping list and counts how many pizzas share
            /// each pair — regardless of what else is on the pizza.
            /// <para>
            /// This is a market-basket / association-rule style analysis: a pair is counted
            /// once per pizza that contains both toppings, so counts are higher than in full
            /// combo mode where the entire topping list must match exactly.
            /// </para>
            /// <para>
            /// Duplicate toppings on the same pizza (e.g. "bacon,bacon,cheese") are
            /// deduplicated with <c>Distinct()</c> before pairing so each pair is counted
            /// at most once per pizza.
            /// </para>
            /// </summary>
            IEnumerable<ToppingCombination> GetPairsCombo(List<Pizza> list)
            {
                return list
                    .Where(pizza => pizza.toppings != null)
                    .SelectMany(pizza =>
                    {
                        // Normalise, deduplicate, and sort so pairs are always in canonical
                        // alphabetical order (e.g. "bacon,cheese" not "cheese,bacon").
                        var tops = pizza.toppings.Split(',')
                            .Select(t => t.Trim().ToLowerInvariant())
                            .Where(t => !string.IsNullOrEmpty(t))
                            .Distinct()
                            .OrderBy(t => t)
                            .ToList();

                        // Emit every unique C(n,2) pair from this pizza.
                        var pairList = new List<string>();
                        for (int a = 0; a < tops.Count; a++)
                            for (int b = a + 1; b < tops.Count; b++)
                                pairList.Add($"{tops[a]},{tops[b]}");
                        return pairList;
                    })
                    .GroupBy(pair => pair)
                    .Select(g => new ToppingCombination { Toppings = g.Key, Count = g.Count() });
            }

            /// <summary>
            /// Default combo mode: each pizza's toppings are normalised and sorted into a
            /// canonical comma-joined key so that order-of-entry differences do not create
            /// duplicate groups — "cheese,pepperoni" and "pepperoni,cheese" both become
            /// <c>"cheese,pepperoni"</c>. Empty-topping pizzas are silently excluded.
            /// </summary>
            IEnumerable<ToppingCombination> GetTopCombo(List<Pizza> list)
            {
                var pizzas = list
                    .Where(pizza => pizza.toppings != null)
                    .Select(pizza => pizza.toppings.Split(',')
                        .Select(t => t.Trim().ToLowerInvariant())
                        .Where(t => !string.IsNullOrEmpty(t))
                        .OrderBy(t => t));   // sort so the join is always alphabetical

                // Join the sorted toppings back into a comma-separated key, then filter
                // out the empty string produced when all toppings were blank after trimming.
                IEnumerable<string> aggregated = pizzas
                    .Select(sortedToppings => string.Join(",", sortedToppings))
                    .Where(combo => !string.IsNullOrEmpty(combo));

                return aggregated
                    .GroupBy(combo => combo)
                    .Select(g => new ToppingCombination { Toppings = g.Key, Count = g.Count() });
            }

            /// <summary>
            /// Exports <paramref name="exportResults"/> to <paramref name="path"/>.
            /// Format is determined by file extension: <c>.json</c> produces indented JSON;
            /// all other extensions produce CSV with a header row. Topping values are
            /// double-quoted in CSV to handle embedded commas correctly.
            /// </summary>
            void Export(List<ToppingCombination> exportResults, string path)
            {
                try
                {
                    if (Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        var data = exportResults.Select((r, i) => new
                        {
                            Rank     = i + 1,
                            Toppings = CleanToppings(r),
                            Orders   = r.Count
                        });
                        File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented));
                    }
                    else
                    {
                        // Default export format: CSV with a header row.
                        var csvRows = exportResults.Select((r, i) => $"{i + 1},\"{CleanToppings(r)}\",{r.Count}");
                        File.WriteAllText(path, "Rank,Toppings,Orders" + Environment.NewLine + string.Join(Environment.NewLine, csvRows));
                    }
                    Console.WriteLine($"Results exported to: {path}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error exporting results: {ex.Message}");
                }
            }
        }
    }
}
