using Shuttle.Core.Contract;

namespace Shuttle.Recall.Testing.Order;

public class Order(Guid id)
{
    private readonly List<OrderItem> _items = [];

    public Guid Id { get; } = id;

    public ItemAdded AddItem(string product, double quantity, double cost)
    {
        var result = new ItemAdded
        {
            Product = product,
            Quantity = quantity,
            Cost = cost
        };

        On(result);

        return result;
    }

    private void On(ItemAdded itemAdded)
    {
        Guard.AgainstNull(itemAdded);

        _items.Add(new()
        {
            Product = itemAdded.Product,
            Quantity = itemAdded.Quantity,
            Cost = itemAdded.Cost
        });
    }

    public double Total()
    {
        return _items.Sum(item => item.Total());
    }
}