using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace PizzaTopping.Tests;

// Console I/O redirection is not thread-safe; run all tests sequentially.
[CollectionDefinition("NonParallel", DisableParallelization = true)]
public class NonParallelCollection { }

[Collection("NonParallel")]
public class PizzaToppingTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    public void Dispose()
    {
        foreach (var f in _tempFiles)
            if (File.Exists(f)) File.Delete(f);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>Serialize <paramref name="pizzas"/> to a temp JSON file and return its path.</summary>
    private string TempJson(params object[] pizzas)
    {
        string path = Path.GetTempFileName();
        File.WriteAllText(path, JsonConvert.SerializeObject(pizzas));
        _tempFiles.Add(path);
        return path;
    }

    /// <summary>Run <paramref name="action"/> while capturing Console.Out and Console.Error.</summary>
    private static (string Stdout, string Stderr) Capture(Action action)
    {
        TextWriter origOut = Console.Out;
        TextWriter origErr = Console.Error;
        var sbOut = new StringBuilder();
        var sbErr = new StringBuilder();
        Console.SetOut(new StringWriter(sbOut));
        Console.SetError(new StringWriter(sbErr));
        try   { action(); }
        finally
        {
            Console.SetOut(origOut);
            Console.SetError(origErr);
        }
        return (sbOut.ToString(), sbErr.ToString());
    }

    /// <summary>Convenience wrapper that calls <see cref="Pizza.PizzaTop"/> with named defaults.</summary>
    private static void Run(
        Pizza  pizza,
        string file,
        int    topN           = 100,
        string stdoutFormat   = null,
        bool   singles        = false,
        bool   pairs          = false,
        bool   showStats      = false,
        bool   showChart      = false,
        string toppingFilter  = null,
        int    comboSize      = 0,
        int    minOrders      = 1,
        bool   sortAsc        = false,
        string excludeTopping = null,
        string exportPath     = null)
        => pizza.PizzaTop(
            filePath:       file,
            topN:           topN,
            stdoutFormat:   stdoutFormat,
            singles:        singles,
            pairs:          pairs,
            showStats:      showStats,
            showChart:      showChart,
            toppingFilter:  toppingFilter,
            comboSize:      comboSize,
            minOrders:      minOrders,
            sortAsc:        sortAsc,
            excludeTopping: excludeTopping,
            exportPath:     exportPath);

    // ── combo mode ────────────────────────────────────────────────────────────

    [Fact]
    public void TopCombo_BasicCounting_ReturnsCorrectRanking()
    {
        string file = TempJson(
            new { toppings = "pepperoni,cheese" },
            new { toppings = "pepperoni,cheese" },
            new { toppings = "pepperoni,cheese" },
            new { toppings = "bacon" }
        );

        var (stdout, _) = Capture(() => Run(new Pizza(), file, stdoutFormat: "json"));
        var arr = JArray.Parse(stdout);

        Assert.Equal(2, arr.Count);
        Assert.Equal("cheese,pepperoni", arr[0]["Toppings"]!.Value<string>());
        Assert.Equal(3,                  arr[0]["Orders"]!.Value<int>());
        Assert.Equal("bacon",            arr[1]["Toppings"]!.Value<string>());
        Assert.Equal(1,                  arr[1]["Orders"]!.Value<int>());
    }

    [Fact]
    public void TopCombo_CaseInsensitive_GroupsTogether()
    {
        string file = TempJson(
            new { toppings = "Pepperoni" },
            new { toppings = "PEPPERONI" },
            new { toppings = "pepperoni" }
        );

        var (stdout, _) = Capture(() => Run(new Pizza(), file, stdoutFormat: "json"));
        var arr = JArray.Parse(stdout);

        Assert.Single(arr);
        Assert.Equal(3, arr[0]["Orders"]!.Value<int>());
    }

    [Fact]
    public void TopCombo_OrderIndependent_SameComboCounted()
    {
        // "cheese,pepperoni" and "pepperoni,cheese" must resolve to the same canonical combo.
        string file = TempJson(
            new { toppings = "cheese,pepperoni" },
            new { toppings = "pepperoni,cheese" }
        );

        var (stdout, _) = Capture(() => Run(new Pizza(), file, stdoutFormat: "json"));
        var arr = JArray.Parse(stdout);

        Assert.Single(arr);
        Assert.Equal(2, arr[0]["Orders"]!.Value<int>());
    }

    [Fact]
    public void TopCombo_AllBlankToppings_NotIncluded()
    {
        // A pizza whose toppings are all whitespace/commas must not produce an empty combo entry.
        string file = TempJson(
            new { toppings = " , , " },
            new { toppings = "" },
            new { toppings = "bacon" }
        );

        var (stdout, _) = Capture(() => Run(new Pizza(), file, stdoutFormat: "json"));
        var arr = JArray.Parse(stdout);

        Assert.Single(arr);
        Assert.Equal("bacon", arr[0]["Toppings"]!.Value<string>());
    }

    [Fact]
    public void TopCombo_NullToppingsField_Ignored()
    {
        // A JSON entry with "toppings": null must not crash and must not contribute to combos.
        string file = Path.GetTempFileName();
        File.WriteAllText(file, "[{\"toppings\":null},{\"toppings\":\"cheese\"}]");
        _tempFiles.Add(file);

        var (stdout, _) = Capture(() => Run(new Pizza(), file, stdoutFormat: "json"));
        var arr = JArray.Parse(stdout);

        Assert.Single(arr);
        Assert.Equal("cheese", arr[0]["Toppings"]!.Value<string>());
    }

    // ── singles mode ──────────────────────────────────────────────────────────

    [Fact]
    public void Singles_BasicFrequency_CorrectCounts()
    {
        string file = TempJson(
            new { toppings = "bacon,cheese" },
            new { toppings = "bacon,mushrooms" },
            new { toppings = "cheese" }
        );

        var (stdout, _) = Capture(() => Run(new Pizza(), file, stdoutFormat: "json", singles: true));
        var arr = JArray.Parse(stdout);

        int OrdersFor(string name) => arr.First(t => t["Toppings"]!.Value<string>() == name)["Orders"]!.Value<int>();

        Assert.Equal(2, OrdersFor("bacon"));
        Assert.Equal(2, OrdersFor("cheese"));
        Assert.Equal(1, OrdersFor("mushrooms"));
    }

    [Fact]
    public void Singles_TrimsWhitespace_SameToppingGrouped()
    {
        string file = TempJson(
            new { toppings = " bacon " },
            new { toppings = "bacon" }
        );

        var (stdout, _) = Capture(() => Run(new Pizza(), file, stdoutFormat: "json", singles: true));
        var arr = JArray.Parse(stdout);

        Assert.Single(arr);
        Assert.Equal(2, arr[0]["Orders"]!.Value<int>());
    }

    // ── pairs mode ────────────────────────────────────────────────────────────

    [Fact]
    public void Pairs_FindsTopCoOccurrence()
    {
        // bacon+cheese appear together on 3 of 4 pizzas.
        string file = TempJson(
            new { toppings = "bacon,cheese" },
            new { toppings = "cheese,bacon,onions" },
            new { toppings = "bacon,cheese,mushrooms" },
            new { toppings = "mushrooms,onions" }
        );

        var (stdout, _) = Capture(() => Run(new Pizza(), file, stdoutFormat: "json", pairs: true));
        var arr = JArray.Parse(stdout);

        Assert.Equal("bacon,cheese", arr[0]["Toppings"]!.Value<string>());
        Assert.Equal(3,              arr[0]["Orders"]!.Value<int>());
    }

    [Fact]
    public void Pairs_ThreeToppingPizza_ProducesThreePairs()
    {
        // C(3,2) = 3 unique pairs from a single three-topping pizza.
        string file = TempJson(new { toppings = "a,b,c" });

        var (stdout, _) = Capture(() => Run(new Pizza(), file, stdoutFormat: "json", pairs: true));
        var arr = JArray.Parse(stdout);

        Assert.Equal(3, arr.Count);
    }

    [Fact]
    public void Pairs_DuplicateToppingsOnPizza_CountedOnce()
    {
        // "bacon,bacon,cheese" should deduplicate before pairing → only one bacon+cheese pair.
        string file = TempJson(new { toppings = "bacon,bacon,cheese" });

        var (stdout, _) = Capture(() => Run(new Pizza(), file, stdoutFormat: "json", pairs: true));
        var arr = JArray.Parse(stdout);

        Assert.Single(arr);
        Assert.Equal(1, arr[0]["Orders"]!.Value<int>());
    }

    // ── filters ───────────────────────────────────────────────────────────────

    [Fact]
    public void Filter_Topping_OnlyMatchingCombosReturned()
    {
        string file = TempJson(
            new { toppings = "bacon,cheese" },
            new { toppings = "mushrooms,onions" }
        );

        var (stdout, _) = Capture(() => Run(new Pizza(), file, stdoutFormat: "json", toppingFilter: "bacon"));
        var arr = JArray.Parse(stdout);

        Assert.Single(arr);
        Assert.Contains("bacon", arr[0]["Toppings"]!.Value<string>());
    }

    [Fact]
    public void Filter_ComboSize_OnlyExactSizeReturned()
    {
        string file = TempJson(
            new { toppings = "bacon" },
            new { toppings = "bacon,cheese" },
            new { toppings = "bacon,cheese,mushrooms" }
        );

        var (stdout, _) = Capture(() => Run(new Pizza(), file, stdoutFormat: "json", comboSize: 2));
        var arr = JArray.Parse(stdout);

        Assert.Single(arr);
        Assert.Equal("bacon,cheese", arr[0]["Toppings"]!.Value<string>());
    }

    [Fact]
    public void Filter_MinOrders_FiltersLowCount()
    {
        string file = TempJson(
            new { toppings = "bacon" },
            new { toppings = "bacon" },
            new { toppings = "cheese" }   // only 1 order — should be excluded
        );

        var (stdout, _) = Capture(() => Run(new Pizza(), file, stdoutFormat: "json", minOrders: 2));
        var arr = JArray.Parse(stdout);

        Assert.Single(arr);
        Assert.Equal("bacon", arr[0]["Toppings"]!.Value<string>());
    }

    [Fact]
    public void Filter_TopN_LimitsResultCount()
    {
        // 5 distinct single-topping pizzas; topN: 3 should return exactly 3.
        string file = TempJson(
            new { toppings = "a" }, new { toppings = "b" }, new { toppings = "c" },
            new { toppings = "d" }, new { toppings = "e" }
        );

        var (stdout, _) = Capture(() => Run(new Pizza(), file, topN: 3, stdoutFormat: "json"));
        var arr = JArray.Parse(stdout);

        Assert.Equal(3, arr.Count);
    }

    [Fact]
    public void Filter_Exclude_RemovesPizzasContainingTopping()
    {
        string file = TempJson(
            new { toppings = "pepperoni,cheese" },
            new { toppings = "pepperoni,mushrooms" },
            new { toppings = "bacon,cheese" }
        );

        var (stdout, _) = Capture(() => Run(new Pizza(), file, stdoutFormat: "json", excludeTopping: "pepperoni"));
        var arr = JArray.Parse(stdout);

        Assert.Single(arr);
        Assert.DoesNotContain("pepperoni", arr[0]["Toppings"]!.Value<string>());
    }

    [Fact]
    public void Filter_ExcludeAll_PrintsWarningToStderr()
    {
        string file = TempJson(new { toppings = "pepperoni" });

        var (_, stderr) = Capture(() => Run(new Pizza(), file, excludeTopping: "pepperoni"));

        Assert.Contains("Warning", stderr);
        Assert.Contains("pepperoni", stderr);
    }

    // ── sorting ───────────────────────────────────────────────────────────────

    [Fact]
    public void Sort_Descending_MostPopularFirst()
    {
        string file = TempJson(
            new { toppings = "bacon" },
            new { toppings = "cheese" },
            new { toppings = "cheese" }
        );

        var (stdout, _) = Capture(() => Run(new Pizza(), file, stdoutFormat: "json", sortAsc: false));
        var arr = JArray.Parse(stdout);

        Assert.Equal("cheese", arr[0]["Toppings"]!.Value<string>());
    }

    [Fact]
    public void Sort_Ascending_LeastPopularFirst()
    {
        string file = TempJson(
            new { toppings = "bacon" },
            new { toppings = "cheese" },
            new { toppings = "cheese" }
        );

        var (stdout, _) = Capture(() => Run(new Pizza(), file, stdoutFormat: "json", sortAsc: true));
        var arr = JArray.Parse(stdout);

        Assert.Equal("bacon", arr[0]["Toppings"]!.Value<string>());
    }

    // ── output formats ────────────────────────────────────────────────────────

    [Fact]
    public void StdoutJson_HasCorrectStructure()
    {
        string file = TempJson(new { toppings = "cheese" });

        var (stdout, _) = Capture(() => Run(new Pizza(), file, stdoutFormat: "json"));
        var arr = JArray.Parse(stdout);   // throws if invalid JSON

        Assert.Single(arr);
        Assert.Equal(1,       arr[0]["Rank"]!.Value<int>());
        Assert.Equal("cheese", arr[0]["Toppings"]!.Value<string>());
        Assert.Equal(1,       arr[0]["Orders"]!.Value<int>());
    }

    [Fact]
    public void StdoutCsv_HasHeaderAndDataRow()
    {
        string file = TempJson(new { toppings = "cheese" });

        var (stdout, _) = Capture(() => Run(new Pizza(), file, stdoutFormat: "csv"));
        var lines = stdout.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("Rank,Toppings,Orders", lines[0]);
        Assert.StartsWith("1,", lines[1]);
        Assert.Contains("cheese", lines[1]);
    }

    [Fact]
    public void TableOutput_NoMatch_PrintsHelpfulMessage()
    {
        string file = TempJson(new { toppings = "bacon" });

        var (stdout, _) = Capture(() => Run(new Pizza(), file, toppingFilter: "zzz_nonexistent"));

        Assert.Contains("No combinations match", stdout);
    }

    // ── --stats ───────────────────────────────────────────────────────────────

    [Fact]
    public void Stats_PrintsSummaryToStdout()
    {
        string file = TempJson(
            new { toppings = "bacon,cheese" },
            new { toppings = "mushrooms" }
        );

        var (stdout, _) = Capture(() => Run(new Pizza(), file, showStats: true));

        Assert.Contains("Total pizzas loaded", stdout);
        Assert.Contains("Unique toppings",     stdout);
        Assert.Contains("Avg toppings/pizza",  stdout);
    }

    [Fact]
    public void Stats_WithStructuredOutput_GoesToStderr_NotStdout()
    {
        // When --stdout json is active, stats must go to stderr so the JSON remains pipe-safe.
        string file = TempJson(new { toppings = "bacon" });

        var (stdout, stderr) = Capture(() => Run(new Pizza(), file, showStats: true, stdoutFormat: "json"));

        JArray.Parse(stdout);                             // throws if stats leaked into JSON
        Assert.Contains("Dataset Summary", stderr);
    }

    // ── --chart ───────────────────────────────────────────────────────────────

    [Fact]
    public void Chart_AddsSolidBlockBarsToTable()
    {
        string file = TempJson(
            new { toppings = "bacon" },
            new { toppings = "bacon" }
        );

        var (stdout, _) = Capture(() => Run(new Pizza(), file, showChart: true));

        // The top result is 100% — its bar must contain at least one solid-block character.
        Assert.Contains("\u2588", stdout);
        Assert.Contains("Distribution", stdout);
    }

    [Fact]
    public void Chart_TopResultIsFullBar()
    {
        string file = TempJson(
            new { toppings = "bacon" },
            new { toppings = "bacon" }
        );

        var (stdout, _) = Capture(() => Run(new Pizza(), file, showChart: true));

        // The top result (100%) should produce a bar of exactly 20 solid blocks.
        Assert.Contains(new string('\u2588', 20), stdout);
    }

    // ── export ────────────────────────────────────────────────────────────────

    [Fact]
    public void Export_WritesValidJsonFile()
    {
        string dataFile   = TempJson(new { toppings = "bacon" });
        string exportFile = Path.GetTempFileName() + ".json";
        _tempFiles.Add(exportFile);

        Capture(() => Run(new Pizza(), dataFile, exportPath: exportFile));

        var arr = JArray.Parse(File.ReadAllText(exportFile));
        Assert.Single(arr);
        Assert.Equal("bacon", arr[0]["Toppings"]!.Value<string>());
        Assert.Equal(1,       arr[0]["Orders"]!.Value<int>());
    }

    [Fact]
    public void Export_WritesValidCsvFile()
    {
        string dataFile   = TempJson(new { toppings = "bacon" });
        string exportFile = Path.GetTempFileName() + ".csv";
        _tempFiles.Add(exportFile);

        Capture(() => Run(new Pizza(), dataFile, exportPath: exportFile));

        string[] lines = File.ReadAllLines(exportFile);
        Assert.Equal("Rank,Toppings,Orders", lines[0]);
        Assert.StartsWith("1,", lines[1]);
        Assert.Contains("bacon", lines[1]);
    }
}
