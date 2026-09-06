using CallNestingDemo.Controllers;
using CallNestingDemo.Handlers;
using CallNestingDemo.Models;
using CallNestingDemo.Repositories;

namespace CallNestingDemo;

internal static class Program
{
    private static void Main()
    {
        var repository = new OrderRepository();
        var handler = new CreateOrderHandler(repository);
        var controller = new OrdersController(handler);

        Order created = controller.Create(new CreateOrderRequest
        {
            Customer = "Biba",
            Product = "Boba",
            Quantity = 3
        });

        Console.WriteLine($"Создан заказ #{created.Id} для {created.Customer}: {created.Product} x {created.Quantity}");
    }
}
