using CallNestingDemo.Models;
using CallNestingDemo.Repositories;

namespace CallNestingDemo.Handlers;

public sealed class CreateOrderHandler
{
    private readonly OrderRepository _repository;

    public CreateOrderHandler(OrderRepository repository)
    {
        _repository = repository;
    }

    public Order Handle(CreateOrderCommand command)
    {
        var order = new Order
        {
            Customer = command.Customer,
            Product = command.Product,
            Quantity = command.Quantity
        };

        return _repository.Create(order);
    }
}
