using System;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;


//Created a Class name Pizza
class Pizza : ToppingCombination
{
    //Created Properties  get
    public string toppings
    {
        get;
        set;

    }

    public void PizzaTop(string filePath = null, int topN = 15, int minOrders = 1, string exportPath = null)
    {

        // Create Type list of Objects that can be accessed by index
        List<Pizza> toppings = GetPizza(filePath);
        // if the  object is  empty it will return
        if (toppings == null) return;
        //The
        IEnumerable<ToppingCombination> topcombination = GetTopCombo(toppings);
        //The top toppings in Descending Order, filtered by minOrders, then take topN
        List<ToppingCombination> results = topcombination
            .Where(tc => tc.countt >= minOrders)
            .OrderByDescending(oi => oi.countt)
            .Take(topN)
            .ToList();

        // Print table-style output with dynamic column widths
        int rankWidth     = Math.Max(4, results.Count.ToString().Length);
        int toppingsWidth = results.Count > 0 ? Math.Max(8, results.Max(r => (r.toppingss ?? "").TrimStart(',').Length)) : 8;
        int ordersWidth   = results.Count > 0 ? Math.Max(6, results.Max(r => r.countt.ToString().Length)) : 6;

        string separator = new string('-', rankWidth + 2 + toppingsWidth + 2 + ordersWidth);
        Console.WriteLine($"{"Rank".PadLeft(rankWidth)}  {"Toppings".PadRight(toppingsWidth)}  {"Orders".PadLeft(ordersWidth)}");
        Console.WriteLine(separator);

        int num = 1;
        foreach (ToppingCombination taste in results)
        {
            string toppingDisplay = (taste.toppingss ?? "").TrimStart(',');
            Console.WriteLine($"{num.ToString().PadLeft(rankWidth)}  {toppingDisplay.PadRight(toppingsWidth)}  {taste.countt.ToString().PadLeft(ordersWidth)}");
            num++;
        }

        if (results.Count == 0)
            Console.WriteLine("No combinations match the specified filters.");

        // Export results if requested
        if (exportPath != null)
            Export(results, exportPath);



        // This help to  get current elements from the collection
        List<Pizza> GetPizza(string path)
        {
            try
            {
                string json;

                if (path != null)
                {
                    // Feature: local file input
                    if (!File.Exists(path))
                    {
                        Console.Error.WriteLine($"Error: File not found: {path}");
                        return null;
                    }
                    json = File.ReadAllText(path);
                }
                else
                {
                    //Create a variable  to capture data from the link
                    string url = "http://brightway.com/CodeTests/pizzas.json";
                    //Creating  the Request for the variable
                    HttpWebRequest httpWebRequest = System.Net.WebRequest.Create(url) as HttpWebRequest;

                    // When the request is sent  from the url variable and the check if the web request get called to
                    using (HttpWebResponse httpWebResponse = httpWebRequest.GetResponse() as HttpWebResponse)
                    {
                        // throw and expection or throw and catch if needed
                        if (httpWebResponse.StatusCode != HttpStatusCode.OK)
                        {
                            Console.Error.WriteLine($"Error: Server returned {httpWebResponse.StatusCode} {httpWebResponse.StatusDescription}");
                            return null;
                        }
                        //Creating an instance for the the request that is called it creates a smooth additional layer between layer and application
                        Stream create = httpWebResponse.GetResponseStream();
                        json = new StreamReader(create).ReadToEnd();
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

        // This help to  get current elements from the collection
        IEnumerable<ToppingCombination> GetTopCombo(List<Pizza> toppings)
        {

            //Create a variable using the datatype
            var Pizzahut = toppings.Select(pizza => pizza.toppings.Split(',').Select(t => t.Trim()).OrderBy(toppin => toppin));


            IEnumerable<string> aggregated = Pizzahut.Select(sortedToppings => sortedToppings.Aggregate("", (pepper, sauces) => pepper + ',' + sauces));

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
                string ext = Path.GetExtension(path).ToLower();
                if (ext == ".json")
                {
                    var data = exportResults.Select((r, i) => new
                    {
                        Rank     = i + 1,
                        Toppings = (r.toppingss ?? "").TrimStart(','),
                        Orders   = r.countt
                    });
                    File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented));
                }
                else
                {
                    // Default: CSV
                    var lines = new List<string> { "Rank,Toppings,Orders" };
                    int rank = 1;
                    foreach (var r in exportResults)
                        lines.Add($"{rank++},\"{(r.toppingss ?? "").TrimStart(',')}\",{r.countt}");
                    File.WriteAllText(path, string.Join(Environment.NewLine, lines));
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
