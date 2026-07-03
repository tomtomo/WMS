using FluentValidation;

namespace Wms.BuildingBlocks.Application.UnitTests.TestDoubles;

// Validator yang mencatat saat ValidationBehavior menjalankannya.
public sealed class RecordingCommandValidator : AbstractValidator<RecordingCommand>
{
    public RecordingCommandValidator(PipelineRecorder recorder)
        => RuleFor(command => command.Value).Custom((_, _) => recorder.Add("Validation"));
}
