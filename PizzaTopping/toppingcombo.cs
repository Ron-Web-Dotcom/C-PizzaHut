namespace PizzaTopping;

/// <summary>
/// Represents an aggregated topping combination and the number of pizza orders that contained it.
/// </summary>
public class ToppingCombination
{
    /// <summary>
    /// Gets or sets the number of orders that contained this topping combination.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the comma-separated, alphabetically sorted, lower-cased topping names
    /// that make up this combination (e.g. <c>"bacon,pepperoni"</c>).
    /// </summary>
    public string Toppings { get; set; } = string.Empty;
}
