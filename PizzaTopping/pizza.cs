using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;

namespace PizzaTopping
{
    internal class Pizza
    {
        public string toppings { get; set; }

        public void PizzaTop(string filePath = null, string url = null, int topN = 15, int minOrders = 1,
            string exportPath = null, string toppingFilter = null, int comboSize = 0,
            bool sortAsc = false, string stdoutFormat = null, bool singles = false,
            bool showChart = false, bool showStats = false, bool pairs = false,
            string excludeTopping = null)
        {
            List<Pizza> pizzaList = GetPizza(filePath, url);
            if (pizzaList == null) return;

            // --exclude: remove pizzas that contain the specified topping before analysis
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

            // Choose analysis mode: pairs, singles, or full combinations
            IEnumerable<ToppingCombination> topcombination =
                pairs   ? GetPairsCombo(pizzaList)  :
                singles ? GetSinglesCombo(pizzaList) :
                          GetTopCombo(pizzaList);

            // Normalise the filter once so comparisons are always lowercase
            string normalizedFilter = toppingFilter?.Trim().ToLowerInvariant();

            // Filter, sort, and take topN results
            List<ToppingCombination> results = topcombination
                .Where(tc => tc.Count >= minOrders)
                .Where(tc => normalizedFilter == null || (tc.Toppings != null && tc.Toppings.Split(',').Contains(normalizedFilter)))
                .Where(tc => comboSize == 0 || (tc.Toppings != null && tc.Toppings.Split(',').Length == comboSize))
                .OrderBy(oi => sortAsc ? oi.Count : -oi.Count)
                .Take(topN)
                .ToList();

            string CleanToppings(ToppingCombination r) => (r.Toppings ?? "").TrimStart(',');

            // --stats: print dataset summary; use stderr when --stdout is active so piping still works
            if (showStats)
            {
                TextWriter statsOut = stdoutFormat != null ? Console.Error : Console.Out;
                PrintStats(pizzaList, results, statsOut);
            }

            if (stdoutFormat != null)
            {
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
                    Console.Error.WriteLine($"Error: Unknown --stdout format '{stdoutFormat}'. Valid values are: json, csv");
                    Environment.Exit(1);
                }
            }
            else
            {
                // Print table-style output with dynamic column widths
                int rankWidth     = Math.Max(4, results.Count.ToString().Length);
                int toppingsWidth = results.Count > 0 ? Math.Max(8, results.Max(r => CleanToppings(r).Length)) : 8;
                int ordersWidth   = results.Count > 0 ? Math.Max(6, results.Max(r => r.Count.ToString().Length)) : 6;

                // --chart: bar column is always 20 chars wide
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
                        string bar = new string('\u2588', filled).PadRight(barWidth, '\u2591');
                        line += $"  {bar}";
                    }
                    Console.WriteLine(line);
                    num++;
                }

                if (results.Count == 0)
                    Console.WriteLine("No combinations match the specified filters.");
            }

            // Export results if requested
            if (exportPath != null)
                Export(results, exportPath);


            // Load pizza data from a local file or URL
            List<Pizza> GetPizza(string path, string customUrl)
            {
                try
                {
                    string json;

                    if (path != null)
                    {
                        json = File.ReadAllText(path);
                    }
                    else
                    {
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

            // --stats: summary of the loaded dataset
            void PrintStats(List<Pizza> list, List<ToppingCombination> res, TextWriter output)
            {
                var allToppings = list
                    .Where(p => p.toppings != null)
                    .SelectMany(p => p.toppings.Split(',')
                        .Select(t => t.Trim().ToLowerInvariant())
                        .Where(t => !string.IsNullOrEmpty(t)))
                    .ToList();

                int pizzasWithToppings = list.Count(p => !string.IsNullOrWhiteSpace(p.toppings));
                double avgToppings     = pizzasWithToppings > 0 ? (double)allToppings.Count / pizzasWithToppings : 0;

                output.WriteLine("=== Dataset Summary ===");
                output.WriteLine($"  Total pizzas loaded : {list.Count}");
                output.WriteLine($"  Pizzas with toppings: {pizzasWithToppings}");
                output.WriteLine($"  Unique toppings     : {allToppings.Distinct().Count()}");
                output.WriteLine($"  Total topping uses  : {allToppings.Count}");
                output.WriteLine($"  Avg toppings/pizza  : {avgToppings:F2}");
                output.WriteLine($"  Results shown       : {res.Count}");
                output.WriteLine();
            }

            // Rank individual toppings by frequency
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

            // --pairs: rank every 2-topping co-occurrence by how many pizzas share that pair,
            // regardless of other toppings on the pizza (market-basket style)
            IEnumerable<ToppingCombination> GetPairsCombo(List<Pizza> list)
            {
                return list
                    .Where(pizza => pizza.toppings != null)
                    .SelectMany(pizza =>
                    {
                        var tops = pizza.toppings.Split(',')
                            .Select(t => t.Trim().ToLowerInvariant())
                            .Where(t => !string.IsNullOrEmpty(t))
                            .Distinct()
                            .OrderBy(t => t)
                            .ToList();

                        // Emit every unique pair from this pizza
                        var pairList = new List<string>();
                        for (int a = 0; a < tops.Count; a++)
                            for (int b = a + 1; b < tops.Count; b++)
                                pairList.Add($"{tops[a]},{tops[b]}");
                        return pairList;
                    })
                    .GroupBy(pair => pair)
                    .Select(g => new ToppingCombination { Toppings = g.Key, Count = g.Count() });
            }

            // Rank topping combinations (exact full combos) by frequency
            IEnumerable<ToppingCombination> GetTopCombo(List<Pizza> list)
            {
                var pizzas = list
                    .Where(pizza => pizza.toppings != null)
                    .Select(pizza => pizza.toppings.Split(',')
                        .Select(t => t.Trim().ToLowerInvariant())
                        .Where(t => !string.IsNullOrEmpty(t))
                        .OrderBy(t => t));

                IEnumerable<string> aggregated = pizzas
                    .Select(sortedToppings => string.Join(",", sortedToppings))
                    .Where(combo => !string.IsNullOrEmpty(combo));

                return aggregated
                    .GroupBy(combo => combo)
                    .Select(g => new ToppingCombination { Toppings = g.Key, Count = g.Count() });
            }

            // Export results to CSV or JSON
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
                        // Default: CSV
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
