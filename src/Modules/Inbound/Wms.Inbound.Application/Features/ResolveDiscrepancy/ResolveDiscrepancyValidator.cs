using FluentValidation;

namespace Wms.Inbound.Application.Features.ResolveDiscrepancy;

public sealed class ResolveDiscrepancyValidator : AbstractValidator<ResolveDiscrepancyCommand>
{
    public ResolveDiscrepancyValidator()
    {
        RuleFor(command => command.GoodsReceiptId).NotEmpty();
        RuleFor(command => command.DiscrepancyId).NotEmpty();
        RuleFor(command => command.Action).IsInEnum();
    }
}
