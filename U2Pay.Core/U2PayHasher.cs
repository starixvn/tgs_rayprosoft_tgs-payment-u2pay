using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Tgs.Payment.U2Pay;

public class U2PayHasher
{
    private readonly ILogger<U2PayHasher> _logger;

    public U2PayHasher(ILogger<U2PayHasher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ComputeHash(string privateKey, IEnumerable<KeyValuePair<string, string?>> dict)
    {
        var content = string.Join(
            "&",
            dict.OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .Select(p => $"{p.Key}={p.Value}"));

        var fullText = $"{content}{privateKey}";

        _logger.LogTrace("FullText: {fullText}", fullText);

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(fullText));

        var sha256Hash = Convert.ToHexString(digest).ToLower();

        var hash = MD5.HashData(Encoding.UTF8.GetBytes(sha256Hash));

        return Convert.ToHexString(hash).ToUpper();
    }
}
