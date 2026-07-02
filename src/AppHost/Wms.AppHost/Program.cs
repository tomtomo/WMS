// Aspire AppHost - untuk composition root inner loop lokal.
var builder = DistributedApplication.CreateBuilder(args);

await builder.Build().RunAsync();
