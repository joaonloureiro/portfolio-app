using JoaoLoureiro.Portfolio.Core.Posts;
using JoaoLoureiro.Portfolio.Domain.Entities;
using Xunit;

namespace JoaoLoureiro.Portfolio.Api.Tests;

public sealed class PostApplicationTests
{
    [Fact]
    public async Task ApplicationUsesControlledUtcClockAndPreservesFirstPublicationTime()
    {
        var clock = new MutableClock(new DateTime(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc));
        var store = new InMemoryPostStore();
        var service = new PostApplicationService(store, clock);
        var content = CompleteContent();

        var created = await service.CreateDraftAsync(new CreatePostCommand("controlled-clock", content), CancellationToken.None);
        Assert.Equal(PostOperationOutcome.Succeeded, created.Outcome);

        clock.UtcNow = new DateTime(2026, 9, 9, 13, 0, 0, DateTimeKind.Utc);
        var published = await service.PublishAsync(created.Post!.Id, new LifecyclePostCommand(created.Post.Revision), CancellationToken.None);
        Assert.Equal(PostOperationOutcome.Succeeded, published.Outcome);
        Assert.Equal(clock.UtcNow, published.Post!.PublishedAt);

        var firstPublication = published.Post.PublishedAt;
        clock.UtcNow = clock.UtcNow.AddHours(1);
        var unpublished = await service.UnpublishAsync(published.Post.Id, new LifecyclePostCommand(published.Post.Revision), CancellationToken.None);
        var republished = await service.PublishAsync(unpublished.Post!.Id, new LifecyclePostCommand(unpublished.Post.Revision), CancellationToken.None);

        Assert.Equal(PostOperationOutcome.Succeeded, republished.Outcome);
        Assert.Equal(firstPublication, republished.Post!.PublishedAt);
    }

    private static IReadOnlyDictionary<PostLocale, PostLocalizedContent> CompleteContent()
    {
        return new Dictionary<PostLocale, PostLocalizedContent>
        {
            [PostLocale.En] = new("English title", "English excerpt", "English body"),
            [PostLocale.Pt] = new("Título em português", "Resumo em português", "Corpo em português")
        };
    }

    private sealed class MutableClock(DateTime initial) : IClock
    {
        public DateTime UtcNow { get; set; } = initial;
    }

    private sealed class InMemoryPostStore : IPostStore
    {
        private readonly Dictionary<Guid, Post> posts = new();

        public Task<PostPage<Post>> ListAdminAsync(int page, int pageSize, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PostPage<Post>(posts.Values.Skip((page - 1) * pageSize).Take(pageSize).ToArray(), page, pageSize, false));
        }

        public Task<Post?> GetAdminAsync(Guid id, CancellationToken cancellationToken)
        {
            posts.TryGetValue(id, out var post);
            return Task.FromResult(post is null ? null : Clone(post));
        }

        public Task<Post?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            return Task.FromResult(posts.Values.SingleOrDefault(post => post.Slug == slug && post.Status == PostStatus.Published));
        }

        public Task<PostPage<Post>> ListPublishedAsync(int page, int pageSize, CancellationToken cancellationToken)
        {
            var published = posts.Values.Where(post => post.Status == PostStatus.Published).ToArray();
            return Task.FromResult(new PostPage<Post>(published, page, pageSize, false));
        }

        public Task<PostStoreMutationResult> AddAsync(Post post, CancellationToken cancellationToken)
        {
            posts[post.Id] = Clone(post);
            return Task.FromResult(PostStoreMutationResult.Succeeded);
        }

        public Task<PostStoreMutationResult> UpdateAsync(Post post, long expectedRevision, CancellationToken cancellationToken)
        {
            if (!posts.TryGetValue(post.Id, out var current)) return Task.FromResult(PostStoreMutationResult.NotFound);
            if (current.Revision != expectedRevision) return Task.FromResult(PostStoreMutationResult.Conflict);
            posts[post.Id] = Clone(post);
            return Task.FromResult(PostStoreMutationResult.Succeeded);
        }

        public Task<PostStoreMutationResult> DeleteAsync(Guid id, long expectedRevision, CancellationToken cancellationToken)
        {
            if (!posts.TryGetValue(id, out var current)) return Task.FromResult(PostStoreMutationResult.NotFound);
            return Task.FromResult(current.Revision == expectedRevision && posts.Remove(id)
                ? PostStoreMutationResult.Succeeded
                : PostStoreMutationResult.Conflict);
        }

        private static Post Clone(Post source)
        {
            var clone = Post.CreateDraft(source.Id, source.Slug, source.UpdatedAt, source.LocalizedContent);
            if (source.PublishedAt is not null)
            {
                clone.Publish(source.PublishedAt.Value);
            }

            if (source.Status == PostStatus.Unpublished)
            {
                clone.Unpublish(source.UpdatedAt);
            }

            while (clone.Revision < source.Revision)
            {
                clone.UpdateContent(source.LocalizedContent, source.UpdatedAt);
            }

            return clone;
        }
    }
}
