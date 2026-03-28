using Microsoft.EntityFrameworkCore;
using StaqFinance.Modules.Identity.Application.Services;
using StaqFinance.Modules.Tenancy.Domain.Entities;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace StaqFinance.Modules.Identity.Infrastructure.Services;

internal sealed class SlugService : ISlugService
{
    private readonly DbContext _context;

    public SlugService(DbContext context)
    {
        _context = context;
    }

    public string Slugify(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark) continue;

            if (char.IsAsciiLetter(c)) sb.Append(char.ToLowerInvariant(c));
            else if (char.IsAsciiDigit(c)) sb.Append(c);
            else if (char.IsWhiteSpace(c) || c == '-') sb.Append('-');
        }

        var slug = Regex.Replace(sb.ToString(), @"-+", "-").Trim('-');

        if (slug.Length > 40)
            slug = slug[..40].TrimEnd('-');

        return slug;
    }

    public async Task<string> GenerateUniqueSlugAsync(string input, CancellationToken cancellationToken = default)
    {
        var baseSlug = Slugify(input);

        if (baseSlug.Length < 3)
            baseSlug = baseSlug.PadRight(3, '0');

        var candidate = baseSlug;
        var counter = 2;
        const int maxAttempts = 20;

        while (counter <= maxAttempts)
        {
            var exists = await _context.Set<Tenant>()
                .AnyAsync(t => t.Slug == candidate, cancellationToken);

            if (!exists) return candidate;

            var suffix = $"-{counter}";
            var baseLength = Math.Min(baseSlug.Length, 40 - suffix.Length);
            candidate = baseSlug[..baseLength] + suffix;
            counter++;
        }

        // Sufixo aleatório após esgotadas as tentativas numéricas
        var randomSuffix = "-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(3)).ToLowerInvariant();
        var finalBaseLength = Math.Min(baseSlug.Length, 40 - randomSuffix.Length);
        return baseSlug[..finalBaseLength] + randomSuffix;
    }
}
