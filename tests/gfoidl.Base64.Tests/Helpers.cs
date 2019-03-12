namespace gfoidl.Base64.Tests
{
    internal static class Helpers
    {
        public static string ToBase64Url(this string base64) => base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
