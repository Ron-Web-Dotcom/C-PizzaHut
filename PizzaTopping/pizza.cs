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
            bool sortAsc = false, string stdoutFormat = null, bool singles = false)
        {
            List<Pizza> pizzaList = GetPizza(filePath, url);
            if (pizzaList == null) return;

            // Choose analysis mode: individual toppings vs. combinations
            IEnumerable<ToppingCombination> topcombination = singles ? GetSinglesCombo(pizzaList) : GetTopCombo(pizzaList);

            // Normalise the filter once so comparisons are always lowercase
            string normalizedFilter = toppingFilter?.Trim().ToLowerInvariant();

            // Filter, sort, and take topN results
            List<ToppingCombination> results = topcombination
                .Where(tc => tc.Count >= minOrders)
                // Issue 6 fix: guard against null Toppings before calling Split
                .Where(tc => normalizedFilter == null || (tc.Toppings != null && tc.Toppings.Split(',').Contains(normalizedFilter)))
                .Where(tc => comboSize == 0 || (tc.Toppings != null && tc.Toppings.Split(',').Length == comboSize))
                .OrderBy(oi => sortAsc ? oi.Count : -oi.Count)
                .Take(topN)
                .ToList();

            string CleanToppings(ToppingCombination r) => (r.Toppings ?? "").TrimStart(',');

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
                    // Issue 1 fix: unknown --stdout format was silently swallowed; now emit a clear error
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

                string separator = new string('-', rankWidth + 2 + toppingsWidth + 2 + ordersWidth);
                Console.WriteLine($"{"Rank".PadLeft(rankWidth)}  {"Toppings".PadRight(toppingsWidth)}  {"Orders".PadLeft(ordersWidth)}");
                Console.WriteLine(separator);

                int num = 1;
                foreach (ToppingCombination taste in results)
                {
                    Console.WriteLine($"{num.ToString().PadLeft(rankWidth)}  {CleanToppings(taste).PadRight(toppingsWidth)}  {taste.Count.ToString().PadLeft(ordersWidth)}");
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
                        // Issue 7 fix: use https (was http — unencrypted, MITM-susceptible)
                        // Issue 4 & 10 fix: replace obsolete HttpWebRequest with HttpClient;
                        //   the old `WebRequest.Create(url) as HttpWebRequest` also returned null
                        //   for non-HTTP URLs, causing a NullReferenceException.
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

            // Rank topping combinations by frequency
            IEnumerable<ToppingCombination> GetTopCombo(List<Pizza> list)
            {
                // Issue 12 fix: rename PascalCase local variable `Pizzahut` to camelCase `pizzas`
                var pizzas = list
                    .Where(pizza => pizza.toppings != null)
                    .Select(pizza => pizza.toppings.Split(',')
                        .Select(t => t.Trim().ToLowerInvariant())
                        .Where(t => !string.IsNullOrEmpty(t))
                        .OrderBy(t => t));

                // Issue 5 fix: filter out empty-string combos produced when all toppings
                //   on a pizza were blank after trimming (string.Join returns "" in that case)
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
