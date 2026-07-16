using Xunit;

// Semua E2E memakai aplikasi, database, dan browser yang sama. Jalankan berurutan agar tidak flaky.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
