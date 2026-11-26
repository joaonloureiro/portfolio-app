using JoaoLoureiro.Portfolio.Domain.Entities;
using Xunit;

namespace JoaoLoureiro.Portfolio.Api.Tests;

public sealed class PostDomainTests
{
    private static readonly DateTime FirstTimestamp = new(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("Admin")]
    [InlineData("bad slug")]
    [InlineData("bad--slug")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    [InlineData("posts")]
    public void InvalidOrReservedSlug_IsRejected(string slug)
    {
        var exception = Assert.Throws<PostDomainException>(() =>
            Post.CreateDraft(Guid.NewGuid(), slug, FirstTimestamp));

        Assert.Equal(PostErrorKeys.InvalidInput, exception.ErrorKey);
    }

    [Fact]
    public void PublishRequiresCompleteEnglishAndPortugueseContent()
    {
        var post = Post.CreateDraft(Guid.NewGuid(), "bilingual-post", FirstTimestamp);

        var exception = Assert.Throws<PostDomainException>(() => post.Publish(FirstTimestamp.AddMinutes(1)));

        Assert.Equal(PostErrorKeys.InvalidInput, exception.ErrorKey);
        Assert.Equal(PostStatus.Draft, post.Status);
        Assert.Equal(1, post.Revision);
    }

    [Fact]
    public void PublicationTimestampIsAssignedOnceAndStatusTransitionsAreBounded()
    {
        var post = Post.CreateDraft(
            Guid.NewGuid(),
            "bilingual-post",
            FirstTimestamp,
            CompleteContent());

        post.Publish(FirstTimestamp.AddMinutes(1));
        var firstPublishedAt = post.PublishedAt;
        post.Unpublish(FirstTimestamp.AddMinutes(2));
        post.Publish(FirstTimestamp.AddMinutes(3));

        Assert.Equal(PostStatus.Published, post.Status);
        Assert.Equal(firstPublishedAt, post.PublishedAt);
        Assert.Equal(4, post.Revision);
    }

    [Fact]
    public void PublishedPostCannotBeUpdatedWithIncompleteContent()
    {
        var post = Post.CreateDraft(
            Guid.NewGuid(),
            "bilingual-post",
            FirstTimestamp,
            CompleteContent());
        post.Publish(FirstTimestamp.AddMinutes(1));

        var incomplete = new Dictionary<PostLocale, PostLocalizedContent>
        {
            [PostLocale.En] = new("English", "Excerpt", "Body"),
            [PostLocale.Pt] = new("", "", "")
        };

        var exception = Assert.Throws<PostDomainException>(() =>
            post.UpdateContent(incomplete, FirstTimestamp.AddMinutes(2)));

        Assert.Equal(PostErrorKeys.InvalidInput, exception.ErrorKey);
        Assert.Equal(PostStatus.Published, post.Status);
        Assert.Equal(2, post.Revision);
    }

    private static IReadOnlyDictionary<PostLocale, PostLocalizedContent> CompleteContent()
    {
        return new Dictionary<PostLocale, PostLocalizedContent>
        {
            [PostLocale.En] = new("English title", "English excerpt", "English body"),
            [PostLocale.Pt] = new("Título em português", "Resumo em português", "Corpo em português")
        };
    }
}
