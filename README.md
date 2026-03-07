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

| Option | Description | Default |
|--------|-------------|---------|
| `--file <path>` | Read pizza data from a local JSON file | — |
| `--url <url>` | Fetch pizza data from a custom URL | built-in URL |
| `--top <n>` | Show top N combinations (must be > 0) | `15` |
| `--min-orders <n>` | Only show combos ordered at least N times | `1` |
| `--export <file>` | Export results to a `.csv` or `.json` file | — |
| `--topping <name>` | Only show combos containing a specific topping | — |
| `--combo-size <n>` | Only show combos with exactly N toppings | — |
| `--sort asc\|desc` | Sort order (`asc` = least popular first) | `desc` |
| `--stdout json\|csv` | Print structured output to stdout instead of a table | — |
| `--singles` | Rank individual toppings by frequency instead of combos | — |
| `--help` | Show help and exit | — |

## Examples

```bash
# Top 10 combos from a local file
dotnet run --project PizzaTopping -- --file pizzas.json --top 10

# Combos containing pepperoni, exported to CSV
dotnet run --project PizzaTopping -- --file pizzas.json --topping pepperoni --export results.csv

# Individual topping frequency as JSON on stdout
dotnet run --project PizzaTopping -- --file pizzas.json --singles --stdout json

# Least-popular 5 two-topping combos
dotnet run --project PizzaTopping -- --file pizzas.json --combo-size 2 --top 5 --sort asc
```

## JSON Data Format

The input JSON must be an array of objects with a `toppings` field (comma-separated string):

```json
[
  { "toppings": "pepperoni,mushrooms,onions" },
  { "toppings": "bacon,cheese" }
]
```
