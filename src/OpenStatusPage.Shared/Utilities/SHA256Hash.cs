using System.Security.Cryptography;
using System.Text;

namespace OpenStatusPage.Shared.Utilities
{
    public class SHA256Hash
    {
        public static string Create(string text)
        {
            using var sha256 = SHA256.Create();

            return BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "");
        }
    }
}
