using CallNestingDemo.Models;

namespace CallNestingDemo.Repositories;

public sealed class OrderRepository
{
    private int _nextId = 1;

    public Order Create(Order order)
    {
        // Поставьте breakpoint на следующей строке.
        // Cold Review покажет цепочку:
        // OrderRepository.Create ← CreateOrderHandler.Handle ← OrdersController.Create ← Program.Main
        order.Id = _nextId++;
        return order;
    }
}
