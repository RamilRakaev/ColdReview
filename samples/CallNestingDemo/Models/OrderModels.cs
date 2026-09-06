namespace CallNestingDemo.Models;

public sealed class CreateOrderRequest
{
    public string Customer { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public sealed class CreateOrderCommand
{
    public string Customer { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public sealed class Order
{
    public int Id { get; set; }
    public string Customer { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
