using System.Collections.Immutable;
using devSite.Components;
using MonorailCss.Theme;
using MyLittleContentEngine;
using MyLittleContentEngine.MonorailCss;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();

var locales = new Dictionary<string, LocaleInfo>();
builder
    .Services.AddContentEngineService(_ => new ContentEngineOptions
    {
        SiteTitle = "Dev Site",
        SiteDescription = "An Inflexible Content Engine for .NET",
        ContentRootPath = "Content",
        CanonicalBaseUrl = "https://jamesshenry.dev",
    })
    .WithMarkdownContentService(_ => new MarkdownContentOptions<BlogFrontMatter>()
    {
        ContentPath = "Content/blog",
        BasePageUrl = "/blog",
    });

var customApplies = new Dictionary<string, string>
            {
                {
                    "body",
                    "bg-white text-base-900 dark:bg-base-950 dark:text-base-100 font-sans transition-colors duration-300"
                },
                { "h1, h2, h3, h4", "focus:outline-none font-bold tracking-tight" },
                { ".prose", "dark:prose-invert max-w-none" },
                // Navigation & Layout
                {
                    ".nav-link",
                    "font-mono text-[11px] font-bold text-base-400 hover:text-base-900 dark:hover:text-base-100 transition-colors uppercase tracking-tight"
                },
                { ".border-dot", "border-b border-dotted border-base-200 dark:border-base-800" },
                // UI Components
                {
                    ".sys-label",
                    "inline-block bg-base-100 dark:bg-base-900 text-[10px] font-mono font-bold px-2 py-0.5 rounded text-base-500 mb-4 uppercase tracking-wider border border-base-200 dark:border-base-800"
                },
                {
                    ".sys-card",
                    "block p-6 border border-base-200 dark:border-base-800 rounded-lg hover:border-primary-500/30 transition-all bg-white/50 dark:bg-base-900/50 backdrop-blur-sm shadow-sm"
                },
                {
                    ".btn-sys",
                    "inline-block font-mono text-[11px] font-bold border-2 border-base-900 dark:border-base-100 px-4 py-2 hover:bg-base-900 hover:text-white dark:hover:bg-base-100 dark:hover:text-base-950 transition-colors tracking-tighter"
                },
            }.ToImmutableDictionary();
builder.Services.AddMonorailCss(_ => new MonorailCssOptions
{
    ColorScheme = new AlgorithmicColorScheme { PrimaryHue = 220, BaseColorName = ColorNames.Slate },
    CustomCssFrameworkSettings = defaultSettings =>
        defaultSettings with
        {
            Theme = defaultSettings
                .Theme.AddFontFamily("sans", "Geist, Inter, sans-serif")
                .AddFontFamily("mono", "JetBrains Mono, monospace"),

            IncludePreflight = true,
Applies = defaultSettings.Applies.AddRange(customApplies)
        },
});
var app = builder.Build();

app.UseHttpsRedirection();

app.UseAntiforgery();

#if DEBUG
app.MapStaticAssets();
#endif
app.MapRazorComponents<App>();
app.UseMonorailCss();
await app.RunOrBuildContent(args);
