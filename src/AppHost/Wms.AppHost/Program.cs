// What : .NET Aspire AppHost — composition root / orchestrator inner-loop lokal.
// Why  : Satu entry point F5 yang menyalakan seluruh dependency lokal (Postgres,
//        RabbitMQ) + tiap service host dalam satu proses (strategi local-first).
//        Masih kosong di tahap skeleton — resource ditambah saat host-nya sudah ada.
// How  : model builder DistributedApplication — deklarasikan resource, lalu Build().Run().
var builder = DistributedApplication.CreateBuilder(args);

await builder.Build().RunAsync();
