using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Domain.UnitTests.TestDoubles;
using Xunit;

namespace Wms.BuildingBlocks.Domain.UnitTests;

// Test StronglyTypedId: factory valid by construction dengan penjagaan nilai kosong dan equality by value dan tipe.
public sealed class StronglyTypedIdTests
{
    [Fact]
    public void Create_succeeds_for_a_non_empty_guid()
    {
        var value = Guid.NewGuid();

        var result = SampleId.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Fact]
    public void Create_rejects_an_empty_guid_as_invalid()
    {
        var result = SampleId.Create(Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("id.invalid");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_blank_string_id_as_invalid(string value)
    {
        var result = SkuCode.Create(value);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
    }

    [Fact]
    public void Create_succeeds_for_a_non_blank_string_id()
    {
        SkuCode.Create("ABC-123").IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Ids_of_the_same_type_and_value_are_equal()
    {
        var value = Guid.NewGuid();

        var a = SampleId.Create(value).Value;
        var b = SampleId.Create(value).Value;

        a.Should().Be(b);
    }

    [Fact]
    public void Ids_of_the_same_type_with_different_values_are_not_equal()
    {
        var a = SampleId.Create(Guid.NewGuid()).Value;
        var b = SampleId.Create(Guid.NewGuid()).Value;

        a.Should().NotBe(b);
    }

    [Fact]
    public void Ids_of_different_types_are_not_equal()
    {
        var value = Guid.NewGuid();

        var sampleId = SampleId.Create(value).Value;
        var otherId = OtherId.Create(value).Value;

        sampleId.Equals(otherId).Should().BeFalse();
    }
}
