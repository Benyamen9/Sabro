using FluentValidation;
using Sabro.Shared.Results;
using Sabro.Translations.Domain;
using Sabro.Translations.Infrastructure;

namespace Sabro.Translations.Application.Authors;

internal sealed class AuthorService : IAuthorService
{
    private readonly TranslationsDbContext dbContext;
    private readonly IValidator<CreateAuthorRequest> validator;

    public AuthorService(TranslationsDbContext dbContext, IValidator<CreateAuthorRequest> validator)
    {
        this.dbContext = dbContext;
        this.validator = validator;
    }

    public async Task<Result<AuthorDto>> CreateAsync(CreateAuthorRequest request, CancellationToken cancellationToken)
    {
        var shapeResult = await validator.ValidateAsync(request, cancellationToken);
        if (!shapeResult.IsValid)
        {
            var message = string.Join("; ", shapeResult.Errors.Select(e => e.ErrorMessage));
            return Result<AuthorDto>.Failure(Error.Validation(message));
        }

        var domainResult = Author.Create(request.Name, request.SyriacName, request.Title);
        if (!domainResult.IsSuccess)
        {
            return Result<AuthorDto>.Failure(domainResult.Error!);
        }

        var author = domainResult.Value!;
        dbContext.Authors.Add(author);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AuthorDto>.Success(new AuthorDto(
            author.Id,
            author.Name,
            author.SyriacName,
            author.Title,
            author.CreatedAt,
            author.UpdatedAt));
    }
}
