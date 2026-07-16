using FluentValidation;

namespace Wms.Outbound.Application.Features.CreateOutboundOrder;

public sealed class CreateOutboundOrderValidator : AbstractValidator<CreateOutboundOrderCommand>
{
    public CreateOutboundOrderValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.Recipient).NotEmpty();
        RuleFor(command => command.AddressLine).NotEmpty();
        RuleFor(command => command.City).NotEmpty();
        RuleFor(command => command.Lines).NotEmpty();
        RuleForEach(command => command.Lines).ChildRules(line =>
        {
            line.RuleFor(item => item.Sku).NotEmpty();
            line.RuleFor(item => item.Qty).GreaterThan(0m);
            line.RuleFor(item => item.Uom).NotEmpty();
        });
    }
}
