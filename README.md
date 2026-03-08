# PizzaTopping

A .NET 8 console application that reads pizza order data (from a local JSON file or a URL) and ranks topping combinations — or individual toppings — by order frequency.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Build

```bash
dotnet build PizzaTopping/PizzaTopping.csproj
```

## Usage

```bash
dotnet run --project PizzaTopping [options]
```

### Data source

| Option | Description |
|--------|-------------|
| `--file <path>` | Read pizza data from a local JSON file |
| `--url <url>` | Fetch pizza data from a custom URL |

### Analysis mode *(pick one; default: full exact combinations)*

| Option | Description |
|--------|-------------|
| `--singles` | Rank individual toppings by frequency |
| `--pairs` | Rank every 2-topping co-occurrence across all orders, regardless of other toppings on the pizza (market-basket style) |

### Filtering

| Option | Description | Default |
|--------|-------------|---------|
| `--top <n>` | Show top N results (must be > 0) | `15` |
| `--min-orders <n>` | Only show entries ordered at least N times | `1` |
| `--topping <name>` | Only show entries that contain a specific topping | — |
| `--combo-size <n>` | Only show combos with exactly N toppings | — |
| `--exclude <name>` | Remove all pizzas containing this topping before analysis | — |

### Output

| Option | Description | Default |
|--------|-------------|---------|
| `--sort asc\|desc` | Sort order (`asc` = least popular first) | `desc` |
| `--chart` | Add a visual `████░░░` frequency bar column to the table | — |
| `--stats` | Print dataset summary (totals, unique toppings, averages) before results | — |
| `--stdout json\|csv` | Print structured output to stdout instead of a table | — |
| `--export <file>` | Export results to a `.csv` or `.json` file | — |

## Examples

```bash
# Top 10 combos from a local file with a bar chart
dotnet run --project PizzaTopping -- --file pizzas.json --top 10 --chart

# Dataset summary + top 15 combos
dotnet run --project PizzaTopping -- --file pizzas.json --stats

# Which pairs of toppings appear together most often?
dotnet run --project PizzaTopping -- --file pizzas.json --pairs --top 10

# What do non-pepperoni orders look like?
dotnet run --project PizzaTopping -- --file pizzas.json --exclude pepperoni --singles

# Combos containing bacon, exported to CSV
dotnet run --project PizzaTopping -- --file pizzas.json --topping bacon --export results.csv

# Least-popular 5 two-topping combos
dotnet run --project PizzaTopping -- --file pizzas.json --combo-size 2 --top 5 --sort asc

# Individual topping frequency as JSON on stdout (stats go to stderr, safe to pipe)
dotnet run --project PizzaTopping -- --file pizzas.json --singles --stats --stdout json
```

## JSON Data Format

The input JSON must be an array of objects with a `toppings` field (comma-separated string):

```json
[
  { "toppings": "pepperoni,mushrooms,onions" },
  { "toppings": "bacon,cheese" }
]
```
