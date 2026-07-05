using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Outbound.Domain.ValueObjects;

// Alamat tujuan pengiriman
public sealed record ShipTo
{
    private ShipTo(string recipient, string addressLine, string city)
    {
        Recipient = recipient;
        AddressLine = addressLine;
        City = city;
    }

    public string Recipient { get; }

    public string AddressLine { get; }

    public string City { get; }

    public static Result<ShipTo> Create(string recipient, string addressLine, string city)
    {
        if (string.IsNullOrWhiteSpace(recipient))
        {
            return Result.Invalid<ShipTo>(new Error("ship_to.recipient_required", "Recipient wajib diisi."));
        }

        if (string.IsNullOrWhiteSpace(addressLine))
        {
            return Result.Invalid<ShipTo>(new Error("ship_to.address_required", "AddressLine wajib diisi."));
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            return Result.Invalid<ShipTo>(new Error("ship_to.city_required", "City wajib diisi."));
        }

        return Result.Success(new ShipTo(recipient.Trim(), addressLine.Trim(), city.Trim()));
    }
}
