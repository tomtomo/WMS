using FluentValidation;

namespace Wms.Notifications.Deliveries.MarkDeliveryRead;

public sealed class MarkDeliveryReadValidator : AbstractValidator<MarkDeliveryReadCommand>
{
    public MarkDeliveryReadValidator() => RuleFor(command => command.DeliveryId).NotEmpty();
}
