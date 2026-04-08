using System.Collections.Immutable;
using devSite.Components;
using MonorailCss.Theme;
using MyLittleContentEngine;
using MyLittleContentEngine.MonorailCss;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents();

var locales = new Dictionary<string, LocaleInfo>();
builder
    .Services.AddContentEngineService(_ => new ContentEngineOptions
    {
        SiteTitle = "Dev Site",
        SiteDescription = "An Inflexible Content Engine for .NET",
        ContentRootPath = "Content",
        CanonicalBaseUrl = "https://jamesshenry.dev",
        // Localization = new LocalizationOptions()
        // {
        //     DefaultLocale = "en-GB",
        //     Locales = ImmutableDictionary<string, LocaleInfo>.Empty.Add(
        //         "en-GB",
        //         new LocaleInfo("English")
        //     ),
        // },
    })
    .WithMarkdownContentService(_ => new MarkdownContentOptions<BlogFrontMatter>()
    {
        ContentPath = "Content/blog",
        BasePageUrl = "blog",
    });

builder.Services.AddMonorailCss(_ => new MonorailCssOptions
{
    ColorScheme = new AlgorithmicColorScheme { PrimaryHue = 220, BaseColorName = ColorNames.Slate },
    CustomCssFrameworkSettings = defaultSettings =>
        defaultSettings with
        {
            Theme = defaultSettings
                .Theme.AddFontFamily("sans", "Geist, Inter, sans-serif")
                .AddFontFamily("mono", "JetBrains Mono, monospace"),

            Applies = defaultSettings
                .Applies.Add("body", "bg-white text-slate-900 selection:bg-blue-100 font-sans")
                .Add("h1, h2, h3, h4", "focus:outline-none font-bold tracking-tight")
                .Add(
                    ".nav-link",
                    "font-mono text-[11px] uppercase tracking-wider text-slate-500 hover:text-slate-950 transition-colors relative"
                )
                .Add(".nav-link.active", "text-slate-950 font-bold")
                .Add(".nav-link.active::before", "content-['>'] absolute -left-4 text-blue-600")
                .Add(
                    ".sys-card",
                    "bg-white border border-slate-200 p-6 hover:border-slate-400 transition-all"
                )
                .Add(
                    ".sys-label",
                    "font-mono text-[10px] uppercase tracking-tighter text-slate-400 mb-1 block"
                )
                .Add(
                    ".btn-sys",
                    "font-mono text-xs border border-slate-950 px-4 py-2 hover:bg-slate-950 hover:text-white transition-all inline-block cursor-pointer"
                )
                // 4. Utilities
                .Add(".border-dot", "border-b border-dotted border-slate-300"),
        },
});
var app = builder.Build();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>();
app.UseMonorailCss();
await app.RunOrBuildContent(args);
