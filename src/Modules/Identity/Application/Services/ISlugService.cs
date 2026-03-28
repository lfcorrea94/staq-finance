namespace StaqFinance.Modules.Identity.Application.Services;

public interface ISlugService
{
    string Slugify(string input);
    Task<string> GenerateUniqueSlugAsync(string input, CancellationToken cancellationToken = default);
}
