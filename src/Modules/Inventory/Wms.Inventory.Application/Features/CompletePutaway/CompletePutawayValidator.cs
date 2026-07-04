using FluentValidation;

namespace Wms.Inventory.Application.Features.CompletePutaway;

public sealed class CompletePutawayValidator : AbstractValidator<CompletePutawayCommand>
{
    public CompletePutawayValidator()
    {
        RuleFor(command => command.PutawayTaskId).NotEmpty();
        RuleFor(command => command.ActualDestinationId).NotEmpty();
    }
}
