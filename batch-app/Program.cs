Console.WriteLine($"batch-app start: {DateTime.UtcNow:o}");
// Simulate some work
await Task.Delay(500);
Console.WriteLine("Doing batch work...");
await Task.Delay(500);
Console.WriteLine("batch-app complete.");

