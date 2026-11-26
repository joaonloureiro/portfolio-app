using FluentValidation;
using JoaoLoureiro.Portfolio.Api.Dtos;
using JoaoLoureiro.Portfolio.Core.Projects;
using JoaoLoureiro.Portfolio.Domain.Entities;

namespace JoaoLoureiro.Portfolio.Api.Validators;

public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(request => request.SortOrder)
            .InclusiveBetween(1, Project.MaxSortOrder)
            .WithErrorCode(ProjectErrorKeys.InvalidInput);
        RuleFor(request => request.ImageReference)
            .NotEmpty()
            .MaximumLength(Project.MaxImageReferenceLength)
            .WithErrorCode(ProjectErrorKeys.InvalidInput);
        RuleFor(request => request.Technologies)
            .NotNull()
            .WithErrorCode(ProjectErrorKeys.InvalidInput)
            .Must(values => values is { Count: > 0 and <= Project.MaxTechnologyCount })
            .WithErrorCode(ProjectErrorKeys.InvalidInput);
        RuleForEach(request => request.Technologies)
            .NotEmpty()
            .MaximumLength(Project.MaxTechnologyLength)
            .WithErrorCode(ProjectErrorKeys.InvalidInput);
        RuleFor(request => request.Content)
            .SetValidator(new ProjectContentRequestValidator())
            .When(request => request.Content is not null);
    }
}

public sealed class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(request => request.ExpectedRevision)
            .GreaterThan(0)
            .WithErrorCode(ProjectErrorKeys.InvalidInput);
        RuleFor(request => request.SortOrder)
            .InclusiveBetween(1, Project.MaxSortOrder)
            .WithErrorCode(ProjectErrorKeys.InvalidInput);
        RuleFor(request => request.ImageReference)
            .NotEmpty()
            .MaximumLength(Project.MaxImageReferenceLength)
            .WithErrorCode(ProjectErrorKeys.InvalidInput);
        RuleFor(request => request.Technologies)
            .NotNull()
            .WithErrorCode(ProjectErrorKeys.InvalidInput)
            .Must(values => values is { Count: > 0 and <= Project.MaxTechnologyCount })
            .WithErrorCode(ProjectErrorKeys.InvalidInput);
        RuleForEach(request => request.Technologies)
            .NotEmpty()
            .MaximumLength(Project.MaxTechnologyLength)
            .WithErrorCode(ProjectErrorKeys.InvalidInput);
        RuleFor(request => request.Content)
            .NotNull()
            .WithErrorCode(ProjectErrorKeys.InvalidInput)
            .SetValidator(new ProjectContentRequestValidator());
    }
}

public sealed class ProjectRevisionRequestValidator : AbstractValidator<ProjectRevisionRequest>
{
    public ProjectRevisionRequestValidator()
    {
        RuleFor(request => request.ExpectedRevision)
            .GreaterThan(0)
            .WithErrorCode(ProjectErrorKeys.InvalidInput);
    }
}

public sealed class DeleteProjectRequestValidator : AbstractValidator<DeleteProjectRequest>
{
    public DeleteProjectRequestValidator()
    {
        RuleFor(request => request.ExpectedRevision)
            .GreaterThan(0)
            .WithErrorCode(ProjectErrorKeys.InvalidInput);
        RuleFor(request => request.Confirmation)
            .NotEmpty()
            .WithErrorCode(ProjectErrorKeys.InvalidInput);
    }
}

public sealed class ReorderProjectsRequestValidator : AbstractValidator<ReorderProjectsRequest>
{
    public ReorderProjectsRequestValidator()
    {
        RuleFor(request => request.Items)
            .NotNull()
            .WithErrorCode(ProjectErrorKeys.InvalidInput)
            .Must(items => items is { Count: > 0 and <= 50 })
            .WithErrorCode(ProjectErrorKeys.InvalidInput);
        RuleForEach(request => request.Items)
            .SetValidator(new ReorderProjectItemRequestValidator());
    }
}

public sealed class ReorderProjectItemRequestValidator : AbstractValidator<ReorderProjectItemRequest>
{
    public ReorderProjectItemRequestValidator()
    {
        RuleFor(item => item.Id)
            .GreaterThan(0)
            .WithErrorCode(ProjectErrorKeys.InvalidInput);
        RuleFor(item => item.ExpectedRevision)
            .GreaterThan(0)
            .WithErrorCode(ProjectErrorKeys.InvalidInput);
    }
}

public sealed class ProjectContentRequestValidator : AbstractValidator<ProjectContentRequest?>
{
    public ProjectContentRequestValidator()
    {
        RuleFor(request => request!.En)
            .NotNull()
            .WithErrorCode(ProjectErrorKeys.InvalidInput);
        RuleFor(request => request!.Pt)
            .NotNull()
            .WithErrorCode(ProjectErrorKeys.InvalidInput);
        RuleFor(request => request!.En)
            .SetValidator(new ProjectLocalizedContentRequestValidator())
            .When(request => request?.En is not null);
        RuleFor(request => request!.Pt)
            .SetValidator(new ProjectLocalizedContentRequestValidator())
            .When(request => request?.Pt is not null);
    }
}

public sealed class ProjectLocalizedContentRequestValidator : AbstractValidator<ProjectLocalizedContentRequest?>
{
    public ProjectLocalizedContentRequestValidator()
    {
        RuleFor(content => content!.Title)
            .MaximumLength(ProjectLocalizedContent.MaxTitleLength)
            .WithErrorCode(ProjectErrorKeys.InvalidInput)
            .When(content => content is not null);
        RuleFor(content => content!.Summary)
            .MaximumLength(ProjectLocalizedContent.MaxSummaryLength)
            .WithErrorCode(ProjectErrorKeys.InvalidInput)
            .When(content => content is not null);
        RuleFor(content => content!.Details)
            .MaximumLength(ProjectLocalizedContent.MaxDetailsLength)
            .WithErrorCode(ProjectErrorKeys.InvalidInput)
            .When(content => content is not null);
        RuleFor(content => content!.FeatureHeading)
            .MaximumLength(ProjectLocalizedContent.MaxFeatureHeadingLength)
            .WithErrorCode(ProjectErrorKeys.InvalidInput)
            .When(content => content is not null);
        RuleFor(content => content!.Features)
            .NotNull()
            .WithErrorCode(ProjectErrorKeys.InvalidInput)
            .Must(values => values is { Count: > 0 and <= ProjectLocalizedContent.MaxFeatureCount })
            .WithErrorCode(ProjectErrorKeys.InvalidInput)
            .When(content => content is not null);
        RuleForEach(content => content!.Features)
            .NotEmpty()
            .MaximumLength(ProjectLocalizedContent.MaxFeatureLength)
            .WithErrorCode(ProjectErrorKeys.InvalidInput)
            .When(content => content?.Features is not null);
    }
}
