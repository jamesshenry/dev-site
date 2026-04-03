using devSite.Components;
using MyLittleContentEngine;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.MonorailCss;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents();

builder
    .Services.AddContentEngineService(_ => new ContentEngineOptions
    {
        SiteTitle = "My Little Content Engine",
        SiteDescription = "An Inflexible Content Engine for .NET",
        ContentRootPath = "Content",
    })
    .WithMarkdownContentService(_ => new MarkdownContentOptions<BlogFrontMatter>()
    {
        ContentPath = "Content/blog",
        BasePageUrl = "blog",
    });

builder.Services.AddMonorailCss();
var app = builder.Build();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>();
app.UseMonorailCss();
await app.RunOrBuildContent(args);

public class BlogFrontMatter : IFrontMatter
{
    public string Title { get; init; } = "Empty title";
    public string Description { get; init; } = string.Empty;
    public string? Uid { get; init; } = null;

    public DateTime Date { get; init; } = DateTime.Now;
    public bool IsDraft { get; init; } = false;
    public string[] Tags { get; init; } = [];
    public string? RedirectUrl { get; init; }
    public string? Section { get; init; }

    public Metadata AsMetadata()
    {
        return new Metadata()
        {
            Title = Title,
            Description = Description,
            LastMod = Date,
            RssItem = true,
        };
    }
}
