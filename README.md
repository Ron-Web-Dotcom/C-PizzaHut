# PizzaTopping

A command-line tool for analysing pizza topping order data. Load a JSON dataset from a local file or remote URL, then rank combinations, compare datasets, filter results, and export in multiple formats.

---

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download)

---

## Build & Run

```bash
cd PizzaTopping
dotnet run -- [options]
```

---

## Data Format

The tool expects a JSON array where each element represents one pizza order:

```json
[
  { "toppings": ["pepperoni"] },
  { "toppings": ["bacon", "mushrooms"] },
  { "toppings": ["four cheese", "pepperoni", "sausage"] }
]
```

A local sample dataset (`pizzas.json`) is included in the project (12,761 orders, 36 unique toppings).

---

## All Options

### Data Source

| Flag | Description |
|------|-------------|
| `--file <path>` | Read pizza data from a local JSON file |
| `--url <url>` | Fetch pizza data from a custom HTTPS URL |

If neither is provided, the tool fetches the default dataset from `https://brightway.com/CodeTests/pizzas.json`.

---

### Analysis Modes *(pick one)*

| Flag | Description |
|------|-------------|
| *(default)* | Rank topping **combinations** by frequency |
| `--singles` | Rank **individual toppings** by frequency |
| `--co-occurrence <topping>` | Rank toppings that appear most often **alongside** a given topping |
| `--distribution` | Show a **histogram** of how many orders have 1, 2, 3 … toppings |

---

### Comparison

| Flag | Description |
|------|-------------|
| `--compare <source>` | Load a second dataset (file path or URL — auto-detected) and show **Current / Baseline / Delta** columns for the top results |

---

### Output Control

| Flag | Description |
|------|-------------|
| `--top <n>` | Show top N results (default: `15`) |
| `--offset <n>` | Skip first N results — use with `--top` for pagination |
| `--sort asc\|desc` | Sort order: `asc` = least popular first (default: `desc`) |
| `--percent` | Add a `%` column showing each result's share of total orders |
| `--group-by-size` | Insert section headers between combo-size groups in table output |
| `--no-header` | Suppress the header row and separator line |
| `--verbose` | Print data source, record count, and active filters before results |
| `--stdout json\|csv` | Write results as JSON or CSV to stdout instead of a table |
| `--export <file>` | Export results to a `.csv` or `.json` file |
| `--stats` | Print dataset statistics after the results |

---

### Filters

| Flag | Description |
|------|-------------|
| `--min-orders <n>` | Only show results ordered at least N times (default: `1`) |
| `--topping <name>` | Only show combos that contain this exact topping |
| `--exclude-topping <name>` | Exclude combos that contain a specific topping |
| `--search <text>` | Only show combos where any topping contains the search text as a substring |
| `--combo-size <n>` | Only show combos with exactly N toppings |
| `--min-combo-size <n>` | Only show combos with at least N toppings |
| `--max-combo-size <n>` | Only show combos with at most N toppings |

---

## Output Examples

All examples below use the included `pizzas.json` (12,761 orders).

---

### Default — top topping combinations

```
$ dotnet run -- --file pizzas.json

Rank  Toppings                      Orders
----  ----------------------------  ------
   1  pepperoni                       4616
   2  mozzarella cheese               1014
   3  four cheese                      956
   4  bacon                            732
   5  beef                             623
   6  sausage                          402
   7  italian sausage                  361
   8  chicken                          229
   9  four cheese,pepperoni            203
  10  ham                              165
  11  mushrooms                        159
  12  mozzarella cheese,pepperoni      155
  13  beef,pepperoni                   122
  14  bacon,pepperoni                  121
  15  black olives                     117
```

---

### `--singles` — rank individual toppings

```
$ dotnet run -- --file pizzas.json --singles

Rank  Toppings           Orders
----  -----------------  ------
   1  pepperoni            6335
   2  four cheese          1611
   3  mozzarella cheese    1461
   4  bacon                1447
   5  beef                 1140
   6  sausage               831
   7  mushrooms             733
   8  italian sausage       672
   9  black olives          456
  10  chicken               401
  11  pineapple             360
  12  ham                   341
  13  jalapenos             258
  14  green peppers         206
  15  canadian bacon        174
```

---

### `--percent` — add share-of-total column

```
$ dotnet run -- --file pizzas.json --percent --top 10

Rank  Toppings                      Orders       %
----  ----------------------------  ------  ------
   1  pepperoni                       4616   36.2%
   2  mozzarella cheese               1014    7.9%
   3  four cheese                      956    7.5%
   4  bacon                            732    5.7%
   5  beef                             623    4.9%
   6  sausage                          402    3.2%
   7  italian sausage                  361    2.8%
   8  chicken                          229    1.8%
   9  four cheese,pepperoni            203    1.6%
  10  ham                              165    1.3%
```

---

### `--co-occurrence` — what do pepperoni lovers also order?

```
$ dotnet run -- --file pizzas.json --co-occurrence pepperoni --top 10

Rank  Toppings           Orders
----  -----------------  ------
   1  four cheese           389
   2  bacon                 316
   3  mushrooms             281
   4  mozzarella cheese     256
   5  beef                  247
   6  sausage               238
   7  italian sausage       204
   8  black olives          160
   9  jalapenos             140
  10  green peppers          97
```

---

### `--distribution` — topping-count histogram

Shows how many orders have 1, 2, 3 … toppings. Supports `--percent`, `--no-header`, and `--export`.

```
$ dotnet run -- --file pizzas.json --distribution --percent

Toppings  Orders  % of Total
--------  ------  ----------
       1    9943       77.9%
       2    1764       13.8%
       3     533        4.2%
       4     303        2.4%
       5     130        1.0%
       6      51        0.4%
       7      21        0.2%
       8       8        0.1%
       9       5        0.0%
      10       2        0.0%
      11       1        0.0%
--------  ------  ----------
   Total   12761      100.0%
```

Export the distribution to a file:

```
$ dotnet run -- --file pizzas.json --distribution --export dist.json
```

`dist.json`:
```json
[
  { "Size": 1, "Orders": 9943, "Percent": "77.9%" },
  { "Size": 2, "Orders": 1764, "Percent": "13.8%" },
  { "Size": 3, "Orders":  533, "Percent":  "4.2%" }
]
```

---

### `--group-by-size` — section headers by topping count

Inserts a labelled section header whenever the topping count changes. Works best combined with `--min-combo-size` so results are naturally contiguous within each group.

```
$ dotnet run -- --file pizzas.json --min-combo-size 2 --top 12 --group-by-size

Rank  Toppings                              Orders
----  ------------------------------------  ------

── 2 toppings ──
   1  four cheese,pepperoni                   203
   2  mozzarella cheese,pepperoni             155
   3  beef,pepperoni                          122
   4  bacon,pepperoni                         121
   5  pepperoni,sausage                        96
   6  italian sausage,pepperoni                85
   7  jalapenos,pepperoni                      67
   8  mushrooms,pepperoni                      60
   9  bacon,pineapple                          39
  10  bacon,mushrooms                          30

── 3 toppings ──
  11  bacon,mushrooms,pepperoni                28
  12  jalapenos,mushrooms,pepperoni            21
```

---

### `--compare` — compare two datasets

Loads a second dataset (file path or URL, auto-detected) and shows a **Current / Baseline / Delta** table. Respects `--top`, `--offset`, `--sort`, `--min-orders`, `--percent`, `--verbose`, and `--export`. The analysis mode (`--singles`, `--co-occurrence`, or default combos) is applied to both datasets.

```
$ dotnet run -- --file pizzas.json --compare pizzas_last_month.json --top 5

Rank  Toppings               Current  Baseline   Delta
----  ---------------------  -------  --------  ------
   1  pepperoni                 4616      4062    +554
   2  mozzarella cheese         1014       892    +122
   3  four cheese                956       841    +115
   4  bacon                      732       644     +88
   5  beef                       623       548     +75
```

With `--percent` (delta shown in percentage points):

```
$ dotnet run -- --file pizzas.json --compare pizzas_last_month.json --top 5 --percent

Rank  Toppings               Current  Baseline    Delta
----  ---------------------  -------  --------  -------
   1  pepperoni               36.2%     36.3%    -0.1%
   2  mozzarella cheese        7.9%      7.9%    +0.0%
   3  four cheese              7.5%      7.5%    +0.0%
   4  bacon                    5.7%      5.7%    +0.0%
   5  beef                     4.9%      4.9%    +0.0%
```

Combos absent from the baseline are labelled `(new)`:

```
Rank  Toppings               Current  Baseline   Delta
----  ---------------------  -------  --------  ------
   9  bbq chicken,pepperoni       28       N/A    (new)
```

With `--verbose`:

```
$ dotnet run -- --file pizzas.json --compare pizzas_last_month.json --verbose --top 3

Current:  file: pizzas.json (12761 orders)
Baseline: pizzas_last_month.json (11200 orders)

Rank  Toppings           Current  Baseline  Delta
----  -----------------  -------  --------  -----
   1  pepperoni             4616      4062   +554
   2  mozzarella cheese     1014       892   +122
   3  four cheese            956       841   +115
```

Export the comparison:

```
$ dotnet run -- --file pizzas.json --compare pizzas_last_month.json --export compare.csv
```

`compare.csv`:
```
Rank,Toppings,Current,Baseline,Delta
1,"pepperoni",4616,4062,+554
2,"mozzarella cheese",1014,892,+122
3,"four cheese",956,841,+115
```

---

### `--verbose` — print source, record count, and active filters

```
$ dotnet run -- --file pizzas.json --topping bacon --min-orders 5 --verbose

Source:  file: pizzas.json
Loaded:  12761 orders
Mode:    combos
Filters: topping=bacon, min-orders=5

Rank  Toppings                   Orders
----  -------------------------  ------
   1  bacon                         732
   2  bacon,pepperoni               121
   3  bacon,pineapple                39
   4  bacon,mushrooms                30
   5  bacon,mushrooms,pepperoni      28
```

---

### `--stats` — dataset summary after results

```
$ dotnet run -- --file pizzas.json --stats --top 3

Rank  Toppings           Orders
----  -----------------  ------
   1  pepperoni            4616
   2  mozzarella cheese    1014
   3  four cheese           956

--- Dataset Statistics ---
Total orders:     12761
Unique toppings:  36
Avg combo size:   1.38
Most popular:     pepperoni
```

---

### `--combo-size` — filter to exact topping count

```
$ dotnet run -- --file pizzas.json --combo-size 2 --top 10 --percent

Rank  Toppings                      Orders       %
----  ----------------------------  ------  ------
   1  four cheese,pepperoni            203    1.6%
   2  mozzarella cheese,pepperoni      155    1.2%
   3  beef,pepperoni                   122    1.0%
   4  bacon,pepperoni                  121    0.9%
   5  pepperoni,sausage                 96    0.8%
   6  italian sausage,pepperoni         85    0.7%
   7  jalapenos,pepperoni               67    0.5%
   8  mushrooms,pepperoni               60    0.5%
   9  bacon,pineapple                   39    0.3%
  10  bacon,mushrooms                   30    0.2%
```

---

### `--stdout json` — machine-readable JSON output

```
$ dotnet run -- --file pizzas.json --stdout json --top 3

[
  {
    "Rank": 1,
    "Toppings": "pepperoni",
    "Orders": 4616
  },
  {
    "Rank": 2,
    "Toppings": "mozzarella cheese",
    "Orders": 1014
  },
  {
    "Rank": 3,
    "Toppings": "four cheese",
    "Orders": 956
  }
]
```

---

### `--stdout csv` + `--no-header` — pipe-friendly CSV

```
$ dotnet run -- --file pizzas.json --stdout csv --no-header --top 5

1,"pepperoni",4616
2,"mozzarella cheese",1014
3,"four cheese",956
4,"bacon",732
5,"beef",623
```

---

### `--sort asc` + `--offset` — pagination

```
$ dotnet run -- --file pizzas.json --sort asc --top 5 --offset 5

Rank  Toppings          Orders
----  ----------------  ------
   6  pineapple,sausage      1
   7  beef,ham               1
   8  feta cheese,ham        1
   9  ham,mushrooms          1
  10  chicken,ham            1
```

---

### `--export` — save results to a file

```
$ dotnet run -- --file pizzas.json --top 5 --export results.json
Results exported to: results.json
```

`results.json`:
```json
[
  { "Rank": 1, "Toppings": "pepperoni",          "Orders": 4616 },
  { "Rank": 2, "Toppings": "mozzarella cheese",  "Orders": 1014 },
  { "Rank": 3, "Toppings": "four cheese",         "Orders":  956 },
  { "Rank": 4, "Toppings": "bacon",               "Orders":  732 },
  { "Rank": 5, "Toppings": "beef",                "Orders":  623 }
]
```

---

## Export Formats

All three export-capable modes write the format based on the file extension:

| Extension | Format |
|-----------|--------|
| `.json` | Indented JSON array |
| anything else | CSV with a header row |

| Mode | Exported columns |
|------|-----------------|
| Default / singles / co-occurrence | `Rank`, `Toppings`, `Orders` |
| `--distribution` | `Size`, `Orders`, `Percent` (always included) |
| `--compare` (CSV) | `Rank`, `Toppings`, `Current`, `Baseline`, `Delta` (+ `Current%`, `Baseline%`, `Delta%` when `--percent`) |
| `--compare` (JSON) | All columns always included for self-documenting output |

---

## Project Structure

```
PizzaTopping/
├── pizza.cs           # Pizza data class + all analysis, output, and export logic
├── toppingcombo.cs    # ToppingCombination result model
├── Program.cs         # CLI argument parsing and entry point
├── PizzaTopping.csproj
└── pizzas.json        # Sample dataset (12,761 orders)
```
