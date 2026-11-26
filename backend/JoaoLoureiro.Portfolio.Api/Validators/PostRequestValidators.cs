using FluentValidation;
using JoaoLoureiro.Portfolio.Api.Dtos;
using JoaoLoureiro.Portfolio.Domain.Entities;

namespace JoaoLoureiro.Portfolio.Api.Validators;

public sealed class CreatePostRequestValidator : AbstractValidator<CreatePostRequest>
{
    public CreatePostRequestValidator()
    {
        RuleFor(request => request.Slug)
            .NotEmpty()
            .MaximumLength(Post.MaxSlugLength)
            .WithErrorCode(PostErrorKeys.InvalidInput);

        RuleFor(request => request.Content)
            .SetValidator(new PostContentRequestValidator())
            .When(request => request.Content is not null);
    }
}

public sealed class UpdatePostRequestValidator : AbstractValidator<UpdatePostRequest>
{
    public UpdatePostRequestValidator()
    {
        RuleFor(request => request.ExpectedRevision)
            .GreaterThan(0)
            .WithErrorCode(PostErrorKeys.InvalidInput);
        RuleFor(request => request.Content)
            .NotNull()
            .WithErrorCode(PostErrorKeys.InvalidInput)
            .SetValidator(new PostContentRequestValidator());
    }
}

public sealed class PostRevisionRequestValidator : AbstractValidator<PostRevisionRequest>
{
    public PostRevisionRequestValidator()
    {
        RuleFor(request => request.ExpectedRevision)
            .GreaterThan(0)
            .WithErrorCode(PostErrorKeys.InvalidInput);
    }
}

public sealed class DeletePostRequestValidator : AbstractValidator<DeletePostRequest>
{
    public DeletePostRequestValidator()
    {
        RuleFor(request => request.ExpectedRevision)
            .GreaterThan(0)
            .WithErrorCode(PostErrorKeys.InvalidInput);
        RuleFor(request => request.Confirmation)
            .NotEmpty()
            .WithErrorCode(PostErrorKeys.InvalidInput);
    }
}

public sealed class PostContentRequestValidator : AbstractValidator<PostContentRequest?>
{
    public PostContentRequestValidator()
    {
        RuleFor(request => request)
            .NotNull()
            .WithErrorCode(PostErrorKeys.InvalidInput);
        RuleFor(request => request!.En)
            .NotNull()
            .WithErrorCode(PostErrorKeys.InvalidInput);
        RuleFor(request => request!.Pt)
            .NotNull()
            .WithErrorCode(PostErrorKeys.InvalidInput);

        RuleFor(request => request!.En)
            .SetValidator(new PostLocalizedContentRequestValidator())
            .When(request => request?.En is not null);
        RuleFor(request => request!.Pt)
            .SetValidator(new PostLocalizedContentRequestValidator())
            .When(request => request?.Pt is not null);
    }
}

public sealed class PostLocalizedContentRequestValidator : AbstractValidator<PostLocalizedContentRequest?>
{
    public PostLocalizedContentRequestValidator()
    {
        RuleFor(content => content!.Title)
            .MaximumLength(PostLocalizedContent.MaxTitleLength)
            .WithErrorCode(PostErrorKeys.InvalidInput)
            .When(content => content is not null);
        RuleFor(content => content!.Excerpt)
            .MaximumLength(PostLocalizedContent.MaxExcerptLength)
            .WithErrorCode(PostErrorKeys.InvalidInput)
            .When(content => content is not null);
        RuleFor(content => content!.Body)
            .MaximumLength(PostLocalizedContent.MaxBodyLength)
            .WithErrorCode(PostErrorKeys.InvalidInput)
            .When(content => content is not null);
    }
}
