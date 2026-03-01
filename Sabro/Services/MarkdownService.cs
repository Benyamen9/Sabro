using Sabro.Services.Interfaces;
using Markdig;
using Ganss.Xss;

namespace Sabro.Services
{
    public class MarkdownService : IMarkdownService
    {
        private readonly MarkdownPipeline _pipeline;
        private readonly HtmlSanitizer _sanitizer;

        public MarkdownService()
        {
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();

            _sanitizer = new HtmlSanitizer();

            // Whitelist stricte
            _sanitizer.AllowedTags.Clear();
            _sanitizer.AllowedTags.Add("p");
            _sanitizer.AllowedTags.Add("br");
            _sanitizer.AllowedTags.Add("strong");
            _sanitizer.AllowedTags.Add("em");
            _sanitizer.AllowedTags.Add("u");
            _sanitizer.AllowedTags.Add("s");
            _sanitizer.AllowedTags.Add("sup");
            _sanitizer.AllowedTags.Add("sub");
            _sanitizer.AllowedTags.Add("h1");
            _sanitizer.AllowedTags.Add("h2");
            _sanitizer.AllowedTags.Add("h3");
            _sanitizer.AllowedTags.Add("h4");
            _sanitizer.AllowedTags.Add("ul");
            _sanitizer.AllowedTags.Add("ol");
            _sanitizer.AllowedTags.Add("li");
            _sanitizer.AllowedTags.Add("blockquote");
            _sanitizer.AllowedTags.Add("code");
            _sanitizer.AllowedTags.Add("pre");
            _sanitizer.AllowedTags.Add("a");

            _sanitizer.AllowedAttributes.Clear();
            _sanitizer.AllowedAttributes.Add("href");
            _sanitizer.AllowedAttributes.Add("title");

            _sanitizer.AllowedSchemes.Clear();
            _sanitizer.AllowedSchemes.Add("http");
            _sanitizer.AllowedSchemes.Add("https");
            _sanitizer.AllowedSchemes.Add("mailto");
        }

        public string ConvertToHtml(string markdown)
        {
            var html = Markdown.ToHtml(markdown, _pipeline);
            return Sanitize(html);
        }

        public string Sanitize(string html)
        {
            return _sanitizer.Sanitize(html);
        }
    }
}
