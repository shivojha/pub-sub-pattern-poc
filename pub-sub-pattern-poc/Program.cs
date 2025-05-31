// See https://aka.ms/new-console-template for more information


var eventAggregator = new EventAggregator();


eventAggregator.Subscribe<OrderPlacedEvent>(new OrderPlacedEventHandler().Handle);
// Simulating an order placed event
eventAggregator.Subscribe<OrderPlacedEvent>( new OrderPlacedEventHandler2().Handle);

eventAggregator.Publish(new OrderPlacedEvent("12345", "C001", DateTime.Now));

Console.WriteLine("End of program execution. Press any key to exit.");

Console.ReadLine();
