using System;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;


//Created a Class name Pizza
class Pizza : ToppingCombination
{
    // Deserialise JSON array directly — no splitting needed
    public List<string> toppings
    {
        get;
        set;
    }

    public void PizzaTop(string filePath = null, string url = null, int topN = 15, int minOrders = 1,
        string exportPath = null, string toppingFilter = null, int comboSize = 0,
        bool sortAsc = false, string stdoutFormat = null, bool singles = false,
        string excludeTopping = null, string searchText = null, bool stats = false,
        int minComboSize = 0, int maxComboSize = 0)
    {

        // Create Type list of Objects that can be accessed by index
        List<Pizza> pizzas = GetPizza(filePath, url);
        // if the  object is  empty it will return
        if (pizzas == null) return;

        // Choose analysis mode: individual toppings vs. combinations
        IEnumerable<ToppingCombination> topcombination = singles ? GetSinglesCombo(pizzas) : GetTopCombo(pizzas);

        // Normalise filters once so comparisons are always lowercase
        string normalizedFilter  = toppingFilter?.Trim().ToLowerInvariant();
        string normalizedExclude = excludeTopping?.Trim().ToLowerInvariant();
        string normalizedSearch  = searchText?.Trim().ToLowerInvariant();

        //The top toppings in Descending Order, filtered, then take topN
        List<ToppingCombination> results = topcombination
            .Where(tc => tc.countt >= minOrders)
            // --topping: must contain this exact topping
            .Where(tc => normalizedFilter  == null || tc.toppingss.Split(',').Contains(normalizedFilter))
            // --exclude-topping: must NOT contain this topping
            .Where(tc => normalizedExclude == null || !tc.toppingss.Split(',').Contains(normalizedExclude))
            // --search: any topping in the combo must contain the search text as a substring
            .Where(tc => normalizedSearch  == null || tc.toppingss.Split(',').Any(t => t.Contains(normalizedSearch)))
            // --combo-size: exact number of toppings
            .Where(tc => comboSize    == 0 || tc.toppingss.Split(',').Length == comboSize)
            // --min-combo-size / --max-combo-size: range
            .Where(tc => minComboSize == 0 || tc.toppingss.Split(',').Length >= minComboSize)
            .Where(tc => maxComboSize == 0 || tc.toppingss.Split(',').Length <= maxComboSize)
            .OrderBy(oi => sortAsc ? oi.countt : -oi.countt)
            .Take(topN)
            .ToList();

        string CleanToppings(ToppingCombination r) => (r.toppingss ?? "").TrimStart(',');

        if (stdoutFormat != null)
        {
            // Structured stdout output — clean for piping into other tools
            if (stdoutFormat == "json")
            {
                var data = results.Select((r, i) => new { Rank = i + 1, Toppings = CleanToppings(r), Orders = r.countt });
                Console.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));
            }
            else if (stdoutFormat == "csv")
            {
                var csvRows = results.Select((r, i) => $"{i + 1},\"{CleanToppings(r)}\",{r.countt}");
                Console.WriteLine("Rank,Toppings,Orders");
                Console.WriteLine(string.Join(Environment.NewLine, csvRows));
            }
        }
        else
        {
            // Print table-style output with dynamic column widths
            int rankWidth     = Math.Max(4, results.Count.ToString().Length);
            int toppingsWidth = results.Count > 0 ? Math.Max(8, results.Max(r => CleanToppings(r).Length)) : 8;
            int ordersWidth   = results.Count > 0 ? Math.Max(6, results.Max(r => r.countt.ToString().Length)) : 6;

            string separator = new string('-', rankWidth + 2 + toppingsWidth + 2 + ordersWidth);
            Console.WriteLine($"{"Rank".PadLeft(rankWidth)}  {"Toppings".PadRight(toppingsWidth)}  {"Orders".PadLeft(ordersWidth)}");
            Console.WriteLine(separator);

            int num = 1;
            foreach (ToppingCombination taste in results)
            {
                Console.WriteLine($"{num.ToString().PadLeft(rankWidth)}  {CleanToppings(taste).PadRight(toppingsWidth)}  {taste.countt.ToString().PadLeft(ordersWidth)}");
                num++;
            }

            if (results.Count == 0)
                Console.WriteLine("No combinations match the specified filters.");
        }

        // --stats: dataset summary printed after results
        if (stats)
        {
            var allToppings = pizzas
                .Where(p => p.toppings != null)
                .SelectMany(p => p.toppings.Select(t => t.Trim().ToLowerInvariant()).Where(t => !string.IsNullOrEmpty(t)))
                .ToList();

            int    totalOrders    = pizzas.Count;
            int    uniqueToppings = allToppings.Distinct().Count();
            double avgComboSize   = pizzas
                .Where(p => p.toppings != null && p.toppings.Count > 0)
                .DefaultIfEmpty()
                .Average(p => p?.toppings?.Count ?? 0);
            string topTopping     = allToppings
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

        // Export results if requested
        if (exportPath != null)
            Export(results, exportPath);



        // This help to  get current elements from the collection
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
                    //Create a variable  to capture data from the link
                    string fetchUrl = customUrl ?? "http://brightway.com/CodeTests/pizzas.json";
                    //Creating  the Request for the variable
                    HttpWebRequest httpWebRequest = System.Net.WebRequest.Create(fetchUrl) as HttpWebRequest;

                    // When the request is sent  from the url variable and the check if the web request get called to
                    using (HttpWebResponse httpWebResponse = httpWebRequest.GetResponse() as HttpWebResponse)
                    {
                        // throw and expection or throw and catch if needed
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
        IEnumerable<ToppingCombination> GetSinglesCombo(List<Pizza> pizzaList)
        {
            return pizzaList
                .Where(pizza => pizza.toppings != null)
                .SelectMany(pizza => pizza.toppings
                    .Select(t => t.Trim().ToLowerInvariant())
                    .Where(t => !string.IsNullOrEmpty(t)))
                .GroupBy(t => t)
                .Select(g => new ToppingCombination { toppingss = g.Key, countt = g.Count() });
        }

        // This help to  get current elements from the collection
        IEnumerable<ToppingCombination> GetTopCombo(List<Pizza> pizzaList)
        {
            //Create a variable using the datatype — normalise case so "Bacon" and "bacon" group together
            var Pizzahut = pizzaList
                .Where(pizza => pizza.toppings != null && pizza.toppings.Count > 0)
                .Select(pizza => pizza.toppings
                    .Select(t => t.Trim().ToLowerInvariant())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .OrderBy(t => t));

            IEnumerable<string> aggregated = Pizzahut.Select(sortedToppings => string.Join(",", sortedToppings));

            //DAta is Grouped and  Displayed
            IEnumerable<ToppingCombination> grouped = aggregated
               .GroupBy(toppingsGroup => toppingsGroup)
               .Select(toppingsGroup => new ToppingCombination()
               {
                   toppingss = toppingsGroup.Key,
                   countt = toppingsGroup.Count()
               });
            return grouped;

        }

        // Feature: export results to CSV or JSON
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
                        Orders   = r.countt
                    });
                    File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented));
                }
                else
                {
                    // Default: CSV
                    var csvRows = exportResults.Select((r, i) => $"{i + 1},\"{CleanToppings(r)}\",{r.countt}");
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
