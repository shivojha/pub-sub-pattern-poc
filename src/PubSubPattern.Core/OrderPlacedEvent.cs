public class OrderPlacedEvent
{
    public string OrderId { get; }
    public string CustomerId { get; }
    public DateTime OrderDate { get; }

    public OrderPlacedEvent(string orderId, string customerId, DateTime orderDate)
    {
        OrderId = orderId;
        CustomerId = customerId;
        OrderDate = orderDate;
    }
}
