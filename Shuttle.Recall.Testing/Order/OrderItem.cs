namespace Shuttle.Recall.Testing.Order;

public class OrderItem
{
    public double Cost { get; set; }
    public string Product { get; set; } = string.Empty;
    public double Quantity { get; set; }

    public double Total()
    {
        return Quantity * Cost;
    }
}