using JoaoLoureiro.Portfolio.Core.Projects;
using JoaoLoureiro.Portfolio.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JoaoLoureiro.Portfolio.Api.Tests;

public sealed class ProjectPersistenceTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private static readonly DateTime FirstTimestamp = new(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc);
    private static int nextProjectId = 100;

    [Fact]
    public async Task SeededProjects_PreservePublishedContentAndOrder()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IProjectStore>();

        var page = await store.ListPublishedAsync(1, 20, CancellationToken.None);

        Assert.Equal(new[] { 1, 2, 3 }, page.Items.Select(project => project.Id));
        Assert.Equal("Personal Portfolio", page.Items[0].LocalizedContent[ProjectLocale.En].Title);
        Assert.Equal("Portfólio Pessoal", page.Items[0].LocalizedContent[ProjectLocale.Pt].Title);
        Assert.Equal(
            "**Automation & Messaging:** **N8N** for workflow automation and **RabbitMQ** for message queuing between services.",
            page.Items[1].LocalizedContent[ProjectLocale.En].Features[5]);
        Assert.Equal("https://happy.joaoloureiro.dev.br/", page.Items[2].LiveUrl);
    }

    [Fact]
    public async Task ProjectStore_RoundTripsAndReplacesOrderedChildren()
    {
        var id = Interlocked.Increment(ref nextProjectId);
        var project = Project.CreateDraft(id, 10, "/project.png", null, null, [".NET", "SQLite"], FirstTimestamp, CompleteContent());

        await using var scope = factory.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IProjectStore>();

        Assert.Equal(ProjectStoreMutationResult.Succeeded, await store.AddAsync(project, CancellationToken.None));

        var stored = await store.GetAdminAsync(id, CancellationToken.None);
        Assert.NotNull(stored);
        stored!.Update(9, "/updated.png", null, null, ["Next.js"], CompleteContent("Updated", ["Only feature"]), FirstTimestamp.AddMinutes(1));

        Assert.Equal(ProjectStoreMutationResult.Succeeded, await store.UpdateAsync(stored, 1, CancellationToken.None));

        var updated = await store.GetAdminAsync(id, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(9, updated!.SortOrder);
        Assert.Equal(new[] { "Next.js" }, updated.Technologies);
        Assert.Equal(new[] { "Only feature" }, updated.LocalizedContent[ProjectLocale.En].Features);
        Assert.Equal(2, updated.Revision);
    }

    [Fact]
    public async Task ProjectStore_RejectsStaleUpdateWithoutOverwritingNewerContent()
    {
        var id = Interlocked.Increment(ref nextProjectId);
        var project = Project.CreateDraft(id, 11, "/project.png", null, null, [".NET"], FirstTimestamp, CompleteContent());

        await using var scope = factory.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IProjectStore>();
        Assert.Equal(ProjectStoreMutationResult.Succeeded, await store.AddAsync(project, CancellationToken.None));

        var current = await store.GetAdminAsync(id, CancellationToken.None);
        var stale = await store.GetAdminAsync(id, CancellationToken.None);
        Assert.NotNull(current);
        Assert.NotNull(stale);

        current!.Update(12, "/current.png", null, null, [".NET"], CompleteContent("Current"), FirstTimestamp.AddMinutes(1));
        stale!.Update(13, "/stale.png", null, null, [".NET"], CompleteContent("Stale"), FirstTimestamp.AddMinutes(2));

        Assert.Equal(ProjectStoreMutationResult.Succeeded, await store.UpdateAsync(current, 1, CancellationToken.None));
        Assert.Equal(ProjectStoreMutationResult.Conflict, await store.UpdateAsync(stale, 1, CancellationToken.None));

        var stored = await store.GetAdminAsync(id, CancellationToken.None);
        Assert.Equal("Current", stored!.LocalizedContent[ProjectLocale.En].Title);
        Assert.Equal(2, stored.Revision);
    }

    [Fact]
    public async Task ProjectStore_ReordersWithoutChangingIdentifiers()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IProjectStore>();
        var projects = (await store.ListAdminAsync(1, 20, CancellationToken.None)).Items;
        var original = projects.ToDictionary(project => project.Id);

        try
        {
            var result = await store.ReorderAsync(
                projects.Select((project, index) => new ProjectOrderChange(project.Id, projects.Count - index, project.Revision)).ToArray(),
                CancellationToken.None);

            Assert.Equal(ProjectStoreMutationResult.Succeeded, result);
            var reordered = (await store.ListAdminAsync(1, 20, CancellationToken.None)).Items;
            Assert.Equal(original.Keys.OrderByDescending(id => id), reordered.Select(project => project.Id));
            Assert.Equal(original.Keys, reordered.Select(project => project.Id).OrderBy(id => id));
            Assert.All(reordered, project => Assert.Equal(original[project.Id].Revision + 1, project.Revision));
        }
        finally
        {
            var current = (await store.ListAdminAsync(1, 20, CancellationToken.None)).Items;
            await store.ReorderAsync(
                current.Select(project => new ProjectOrderChange(project.Id, original[project.Id].SortOrder, project.Revision)).ToArray(),
                CancellationToken.None);
        }
    }

    private static IReadOnlyDictionary<ProjectLocale, ProjectLocalizedContent> CompleteContent(
        string englishTitle = "English title",
        IReadOnlyList<string>? englishFeatures = null)
    {
        return new Dictionary<ProjectLocale, ProjectLocalizedContent>
        {
            [ProjectLocale.En] = new(englishTitle, "English summary", "English details", "English features", englishFeatures ?? ["English feature"]),
            [ProjectLocale.Pt] = new("Título em português", "Resumo em português", "Detalhes em português", "Recursos", ["Recurso em português"])
        };
    }
}
