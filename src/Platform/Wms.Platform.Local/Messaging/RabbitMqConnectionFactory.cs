using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Wms.Platform.Local.Messaging;

// Satu koneksi AMQP per proses (Singleton), lazy.
// Channel (IModel) tidak thread-safe: pemakai membuat channel sendiri per operasi/subscription.
public sealed class RabbitMqConnectionFactory(
    IConfiguration configuration,
    IOptions<RabbitMqOptions> options) : IDisposable
{
    private readonly Lazy<IConnection> _connection = new(
        () => Connect(configuration, options.Value),
        LazyThreadSafetyMode.ExecutionAndPublication);

    public IModel CreateChannel() => _connection.Value.CreateModel();

    public void Dispose()
    {
        if (_connection.IsValueCreated)
        {
            _connection.Value.Dispose();
        }
    }

    private static IConnection Connect(IConfiguration configuration, RabbitMqOptions options)
    {
        var connectionString = configuration.GetConnectionString(options.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{options.ConnectionStringName}' untuk RabbitMQ tidak ditemukan di konfigurasi.");
        }

        // DispatchConsumersAsync: consumer async (AsyncEventingBasicConsumer) wajib dispatcher async.
        var factory = new ConnectionFactory
        {
            Uri = new Uri(connectionString),
            DispatchConsumersAsync = true,
        };
        return factory.CreateConnection();
    }
}
