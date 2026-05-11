using Sabro.Biblical.Domain;

namespace Sabro.Biblical.Application.Books;

public sealed record CreateBiblicalBookRequest(
    string Code,
    string EnglishName,
    Testament Testament,
    int Order,
    string? SyriacName = null);
