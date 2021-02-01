using System.Text;

namespace Cleipnir.Helpers
{
    public static class NetworkingExtensions
    {
        public static byte[] GetUtf8Bytes(this string s) => Encoding.UTF8.GetBytes(s);
        public static string ToUtf8String(this byte[] bytes) => Encoding.UTF8.GetString(bytes);
    }
}