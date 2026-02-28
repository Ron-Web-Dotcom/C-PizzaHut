using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace PizzaTopping
{
    class Program
    {
        static void Main(string[] args)
        {
            // Parse command-line options
            string filePath   = null;
            string url        = null;
            int    topN       = 15;
            int    minOrders  = 1;
            string exportPath = null;

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
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int n)) topN = n;
                        break;
                    case "--min-orders":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int m)) minOrders = m;
                        break;
                    case "--export":
                        if (i + 1 < args.Length) exportPath = args[++i];
                        break;
                    case "--help":
                        Console.WriteLine("Usage: PizzaTopping [options]");
                        Console.WriteLine("  --file <path>       Read pizza data from a local JSON file");
                        Console.WriteLine("  --url <url>         Fetch pizza data from a custom URL");
                        Console.WriteLine("  --top <n>           Show top N combinations (default: 15)");
                        Console.WriteLine("  --min-orders <n>    Only show combos ordered at least N times (default: 1)");
                        Console.WriteLine("  --export <file>     Export results to a .csv or .json file");
                        return;
                }
            }

            Pizza p = new Pizza();
            p.PizzaTop(filePath, url, topN, minOrders, exportPath);
        }
    }
}
