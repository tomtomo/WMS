using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Wms.BuildingBlocks.Application.UnitTests.TestDoubles;
using Xunit;

namespace Wms.BuildingBlocks.Application.UnitTests;

// Test AddValidatedOptions.
public sealed class ValidatedOptionsTests
{
    [Fact]
    public void Invalid_configuration_fails_validation()
    {
        var provider = BuildProvider(maxItems: "0");
        var options = provider.GetRequiredService<IOptions<SampleOptions>>();

        var act = () => _ = options.Value;

        act.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void Valid_configuration_binds_the_options()
    {
        var provider = BuildProvider(maxItems: "42");
        var options = provider.GetRequiredService<IOptions<SampleOptions>>();

        options.Value.MaxItems.Should().Be(42);
    }

    private static ServiceProvider BuildProvider(string maxItems)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Sample:MaxItems"] = maxItems })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddValidatedOptions<SampleOptions>("Sample");
        return services.BuildServiceProvider();
    }
}
