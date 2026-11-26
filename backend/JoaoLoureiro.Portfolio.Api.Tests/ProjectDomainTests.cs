using JoaoLoureiro.Portfolio.Domain.Entities;
using Xunit;

namespace JoaoLoureiro.Portfolio.Api.Tests;

public sealed class ProjectDomainTests
{
    private static readonly DateTime Now = new(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void PublishRequiresCompleteEnglishAndPortugueseContent()
    {
        var project = Project.CreateDraft(4, 4, "/project.png", null, null, [".NET"], Now, CompleteContent());

        project.Publish(Now.AddMinutes(1));

        Assert.Equal(ProjectStatus.Published, project.Status);
        Assert.Equal(2, project.Revision);
        Assert.Equal(Now.AddMinutes(1), project.PublishedAt);
    }

    [Fact]
    public void IncompleteProjectCannotBePublished()
    {
        var project = Project.CreateDraft(4, 4, "/project.png", null, null, [".NET"], Now);

        var exception = Assert.Throws<ProjectDomainException>(() => project.Publish(Now.AddMinutes(1)));

        Assert.Equal(ProjectErrorKeys.InvalidInput, exception.ErrorKey);
    }

    [Fact]
    public void PublishedProjectMustBeUnpublishedBeforeDeletion()
    {
        var project = Project.CreateDraft(4, 4, "/project.png", null, null, [".NET"], Now, CompleteContent());
        project.Publish(Now.AddMinutes(1));
        Assert.False(project.CanDelete);

        project.Unpublish(Now.AddMinutes(2));

        Assert.True(project.CanDelete);
    }

    [Fact]
    public void RejectsUnapprovedUrlsAndImageTraversal()
    {
        Assert.Throws<ProjectDomainException>(() => Project.CreateDraft(4, 4, "/../secret.png", null, null, [".NET"], Now));
        Assert.Throws<ProjectDomainException>(() => Project.CreateDraft(4, 4, "/project.png", "https://evil.example/project", null, [".NET"], Now));
        Assert.Throws<ProjectDomainException>(() => Project.CreateDraft(4, 4, "/project.png", "http://github.com/project", null, [".NET"], Now));
    }

    [Fact]
    public void UpdateIncrementsRevisionAndKeepsOrderIndependentOfIdentifier()
    {
        var project = Project.CreateDraft(12, 3, "/project.png", null, null, [".NET"], Now, CompleteContent());

        project.SetSortOrder(1, Now.AddMinutes(1));

        Assert.Equal(12, project.Id);
        Assert.Equal(1, project.SortOrder);
        Assert.Equal(2, project.Revision);
    }

    private static IReadOnlyDictionary<ProjectLocale, ProjectLocalizedContent> CompleteContent()
    {
        return new Dictionary<ProjectLocale, ProjectLocalizedContent>
        {
            [ProjectLocale.En] = new("Title", "Summary", "Details", "Features", ["Feature one"]),
            [ProjectLocale.Pt] = new("Título", "Resumo", "Detalhes", "Recursos", ["Recurso um"])
        };
    }
}
