using System;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;

namespace PizzaTopping
{
    class Pizza
    {
        public string toppings
        {
            get;
            set;
        }

        public void PizzaTop(string filePath = null, string url = null, int topN = 15, int minOrders = 1,
            string exportPath = null, string toppingFilter = null, int comboSize = 0,
            bool sortAsc = false, string stdoutFormat = null, bool singles = false)
        {
            // Create a list of Pizza objects from the data source
            List<Pizza> pizzaList = GetPizza(filePath, url);
            // if the list is empty return
            if (pizzaList == null) return;

            // Choose analysis mode: individual toppings vs. combinations
            IEnumerable<ToppingCombination> topcombination = singles ? GetSinglesCombo(pizzaList) : GetTopCombo(pizzaList);

            // Normalise the filter once so comparisons are always lowercase
            string normalizedFilter = toppingFilter?.Trim().ToLowerInvariant();

            //The top toppings in Descending Order, filtered, then take topN
            List<ToppingCombination> results = topcombination
                .Where(tc => tc.Count >= minOrders)
                .Where(tc => normalizedFilter == null || tc.Toppings.Split(',').Contains(normalizedFilter))
                .Where(tc => comboSize == 0 || tc.Toppings.Split(',').Length == comboSize)
                .OrderBy(oi => sortAsc ? oi.Count : -oi.Count)
                .Take(topN)
                .ToList();

            string CleanToppings(ToppingCombination r) => (r.Toppings ?? "").TrimStart(',');

            if (stdoutFormat != null)
            {
                // Structured stdout output — clean for piping into other tools
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
                        // FileNotFoundException is caught below — no pre-check needed
                        json = File.ReadAllText(path);
                    }
                    else
                    {
                        string fetchUrl = customUrl ?? "http://brightway.com/CodeTests/pizzas.json";
                        HttpWebRequest httpWebRequest = System.Net.WebRequest.Create(fetchUrl) as HttpWebRequest;

                        using (HttpWebResponse httpWebResponse = httpWebRequest.GetResponse() as HttpWebResponse)
                        {
                            if (httpWebResponse.StatusCode != HttpStatusCode.OK)
                            {
                                Console.Error.WriteLine($"Error: Server returned {httpWebResponse.StatusCode} {httpWebResponse.StatusDescription}");
                                return null;
                            }
                            using (var reader = new StreamReader(httpWebResponse.GetResponseStream()))
                                json = reader.ReadToEnd();
                        }
                    }

                    return JsonConvert.DeserializeObject<List<Pizza>>(json);
                }
                catch (WebException ex)
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
                var Pizzahut = list
                    .Where(pizza => pizza.toppings != null)
                    .Select(pizza => pizza.toppings.Split(',')
                        .Select(t => t.Trim().ToLowerInvariant())
                        .Where(t => !string.IsNullOrEmpty(t))
                        .OrderBy(t => t));

                IEnumerable<string> aggregated = Pizzahut.Select(sortedToppings => string.Join(",", sortedToppings));

                IEnumerable<ToppingCombination> grouped = aggregated
                   .GroupBy(toppingsGroup => toppingsGroup)
                   .Select(toppingsGroup => new ToppingCombination()
                   {
                       Toppings = toppingsGroup.Key,
                       Count    = toppingsGroup.Count()
                   });
                return grouped;
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
