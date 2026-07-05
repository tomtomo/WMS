using FluentValidation;

namespace Wms.Outbound.Application.Features.CompletePickingTask;

public sealed class CompletePickingTaskValidator : AbstractValidator<CompletePickingTaskCommand>
{
    public CompletePickingTaskValidator()
    {
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.StagingLocationId).NotEmpty();
        RuleFor(command => command.ActualQty).GreaterThan(0);
    }
}
