using Sabro.Shared.Results;

namespace Sabro.Shared.Pagination;

public static class PageRequest
{
    public const int DefaultPageSize = 50;

    public const int MaxPageSize = 200;

    public static Error? Validate(int page, int pageSize)
    {
        var fields = new Dictionary<string, IReadOnlyList<string>>();
        if (page < 1)
        {
            fields["page"] = new[] { "Page must be 1 or greater." };
        }

        if (pageSize < 1)
        {
            fields["pageSize"] = new[] { "PageSize must be 1 or greater." };
        }
        else if (pageSize > MaxPageSize)
        {
            fields["pageSize"] = new[] { $"PageSize must not exceed {MaxPageSize}." };
        }

        return fields.Count == 0 ? null : Error.Validation(fields);
    }
}
