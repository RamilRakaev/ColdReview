using CallNestingDemo.Handlers;
using CallNestingDemo.Models;

namespace CallNestingDemo.Controllers;

public sealed class OrdersController
{
    private readonly CreateOrderHandler _handler;

    public OrdersController(CreateOrderHandler handler)
    {
        _handler = handler;
    }

    public Order Create(CreateOrderRequest request)
    {
        var command = new CreateOrderCommand
        {
            Customer = request.Customer,
            Product = request.Product,
            Quantity = request.Quantity
        };

        return _handler.Handle(command);
    }
}
