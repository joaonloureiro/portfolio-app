using JoaoLoureiro.Portfolio.Core.Posts;
using JoaoLoureiro.Portfolio.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JoaoLoureiro.Portfolio.Api.Tests;

public sealed class PostPersistenceTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private static readonly DateTime FirstTimestamp = new(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task PostStore_RoundTripsBothLocalesAndMarkdownExactly()
    {
        var post = Post.CreateDraft(
            Guid.NewGuid(),
            $"round-trip-{Guid.NewGuid():N}",
            FirstTimestamp,
            CompleteContent());

        await using var scope = factory.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IPostStore>();

        Assert.Equal(PostStoreMutationResult.Succeeded, await store.AddAsync(post, CancellationToken.None));
        var stored = await store.GetAdminAsync(post.Id, CancellationToken.None);

        Assert.NotNull(stored);
        Assert.Equal("# Exact markdown\n\n[link](https://example.com)", stored.LocalizedContent[PostLocale.En].Body);
        Assert.Equal("Corpo em português", stored.LocalizedContent[PostLocale.Pt].Body);
        Assert.Equal(1, stored.Revision);
    }

    [Fact]
    public async Task PostStore_RejectsDuplicateSlugDeterministically()
    {
        var slug = $"duplicate-{Guid.NewGuid():N}";
        var first = Post.CreateDraft(Guid.NewGuid(), slug, FirstTimestamp);
        var second = Post.CreateDraft(Guid.NewGuid(), slug, FirstTimestamp.AddMinutes(1));

        await using var scope = factory.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IPostStore>();

        Assert.Equal(PostStoreMutationResult.Succeeded, await store.AddAsync(first, CancellationToken.None));
        Assert.Equal(PostStoreMutationResult.SlugConflict, await store.AddAsync(second, CancellationToken.None));
    }

    [Fact]
    public async Task PostStore_StaleUpdateDoesNotOverwriteNewerContent()
    {
        var post = Post.CreateDraft(
            Guid.NewGuid(),
            $"concurrency-{Guid.NewGuid():N}",
            FirstTimestamp,
            CompleteContent());

        await using var scope = factory.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IPostStore>();
        Assert.Equal(PostStoreMutationResult.Succeeded, await store.AddAsync(post, CancellationToken.None));

        var firstRead = await store.GetAdminAsync(post.Id, CancellationToken.None);
        var staleRead = await store.GetAdminAsync(post.Id, CancellationToken.None);
        Assert.NotNull(firstRead);
        Assert.NotNull(staleRead);

        firstRead!.UpdateContent(CompleteContent("First update"), FirstTimestamp.AddMinutes(1));
        staleRead!.UpdateContent(CompleteContent("Stale update"), FirstTimestamp.AddMinutes(2));

        Assert.Equal(
            PostStoreMutationResult.Succeeded,
            await store.UpdateAsync(firstRead, expectedRevision: 1, CancellationToken.None));
        Assert.Equal(
            PostStoreMutationResult.Conflict,
            await store.UpdateAsync(staleRead, expectedRevision: 1, CancellationToken.None));

        var stored = await store.GetAdminAsync(post.Id, CancellationToken.None);
        Assert.Equal("First update", stored!.LocalizedContent[PostLocale.En].Title);
        Assert.Equal(2, stored.Revision);
    }

    [Fact]
    public async Task PublishedOrderingUsesPublicationTimeThenIdentifier()
    {
        var older = Post.CreateDraft(
            Guid.NewGuid(),
            $"older-{Guid.NewGuid():N}",
            FirstTimestamp,
            CompleteContent());
        older.Publish(FirstTimestamp.AddMinutes(1));

        var newer = Post.CreateDraft(
            Guid.NewGuid(),
            $"newer-{Guid.NewGuid():N}",
            FirstTimestamp,
            CompleteContent());
        newer.Publish(FirstTimestamp.AddMinutes(2));

        var draft = Post.CreateDraft(
            Guid.NewGuid(),
            $"draft-{Guid.NewGuid():N}",
            FirstTimestamp,
            CompleteContent());

        await using var scope = factory.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IPostStore>();
        Assert.Equal(PostStoreMutationResult.Succeeded, await store.AddAsync(older, CancellationToken.None));
        Assert.Equal(PostStoreMutationResult.Succeeded, await store.AddAsync(newer, CancellationToken.None));
        Assert.Equal(PostStoreMutationResult.Succeeded, await store.AddAsync(draft, CancellationToken.None));

        var result = await store.ListPublishedAsync(1, 20, CancellationToken.None);

        var publishedIds = result.Items
            .Where(post => post.Id is var id && (id == newer.Id || id == older.Id))
            .Select(post => post.Id)
            .ToArray();
        Assert.Equal(new[] { newer.Id, older.Id }, publishedIds);
        Assert.DoesNotContain(result.Items, post => post.Id == draft.Id);
    }

    private static IReadOnlyDictionary<PostLocale, PostLocalizedContent> CompleteContent(string title = "English title")
    {
        return new Dictionary<PostLocale, PostLocalizedContent>
        {
            [PostLocale.En] = new(title, "English excerpt", "# Exact markdown\n\n[link](https://example.com)"),
            [PostLocale.Pt] = new("Título em português", "Resumo em português", "Corpo em português")
        };
    }
}
