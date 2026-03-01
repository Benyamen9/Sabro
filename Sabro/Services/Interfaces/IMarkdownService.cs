namespace Sabro.Services.Interfaces
{
    public interface IMarkdownService
    {
        string ConvertToHtml(string markdown);
        string Sanitize(string html);
    }
}
