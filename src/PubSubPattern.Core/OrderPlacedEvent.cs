public abstract class BaseEvent
{
    public abstract int Version { get; }
}

// Represents version 1 of the Order Placed event
public class OrderPlacedEvent : BaseEvent
{
    public string OrderId { get; }
    public string CustomerId { get; }
    public DateTime OrderDate { get; }
    public override int Version { get; }

    // Constructor for Version 1
    public OrderPlacedEvent(string orderId, string customerId, DateTime orderDate)
    {
        OrderId = orderId;
        CustomerId = customerId;
        OrderDate = orderDate;
        Version = 1;
    }
}

// Represents version 2 of the Order Placed event with ShippingAddress
public class OrderPlacedEventV2 : BaseEvent
{
    public string OrderId { get; }
    public string CustomerId { get; }
    public DateTime OrderDate { get; }
    public string ShippingAddress { get; }
    public override int Version { get; }

    // Constructor for Version 2
    public OrderPlacedEventV2(string orderId, string customerId, DateTime orderDate, string shippingAddress)
    {
        OrderId = orderId;
        CustomerId = customerId;
        OrderDate = orderDate;
        ShippingAddress = shippingAddress;
        Version = 2;
    }
}
