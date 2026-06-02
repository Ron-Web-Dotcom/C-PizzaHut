namespace PizzaTopping
{
    /// <summary>
    /// Represents an aggregated result row produced by one of the three analysis
    /// modes (full combo, singles, or pairs). Each instance captures a canonical
    /// topping key and the number of pizza orders that matched it.
    /// </summary>
    internal class ToppingCombination
    {
        /// <summary>
        /// Number of pizza orders that contain this topping entry.
        /// <para>
        /// In combo mode this is the exact-match order count; in singles mode it is
        /// the number of pizzas the individual topping appeared on; in pairs mode it
        /// is the number of pizzas on which both toppings co-occurred, regardless of
        /// whatever else was ordered.
        /// </para>
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Canonical, comma-separated topping key for this result row.
        /// <para>
        /// Always stored lower-case, trimmed, and sorted alphabetically so that
        /// "Pepperoni,Cheese" and "cheese, pepperoni" collapse to the same key:
        /// <c>"cheese,pepperoni"</c>.
        /// </para>
        /// </summary>
        public string Toppings { get; set; }
    }
}
