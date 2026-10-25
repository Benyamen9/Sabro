using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Sabro.Play.Application.Shmo;

/// <summary>
/// Whether finding a figure on a given day made its card illuminated: a 1 in
/// <see cref="Odds"/> chance per find (Owner, 2026-10-25), independent of the
/// figure.
/// </summary>
/// <remarks>
/// Derived, never stored: the same player, figure and day always give the same
/// answer, on every device, and there is nothing to reroll. A figure found again
/// on a later day is a fresh chance.
/// </remarks>
internal static class ShmoIllumination
{
    public const int Odds = 20;

    public static bool IsIlluminated(string logtoUserId, Guid figureId, DateOnly foundOn)
    {
        var key = string.Create(
            CultureInfo.InvariantCulture,
            $"{logtoUserId}|{figureId:D}|{foundOn:yyyy-MM-dd}");
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return BinaryPrimitives.ReadUInt32BigEndian(hash) % Odds == 0;
    }
}
