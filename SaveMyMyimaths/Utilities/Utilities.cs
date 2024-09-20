using System.Security.Cryptography;
using System.Web;

namespace SaveMyMyimaths.Utilities;

public abstract class Utilities
{
    //yeah you can see here i use a default value for host
    //hope that i could remember this setting nah
    public static void UpdateCookie(HttpResponseMessage response, string host = "https://app.myimaths.com/")
    {
        foreach (string cookie in response.Headers.GetValues("Set-Cookie"))
            Registry.Registry.Container.SetCookies(new Uri(host), cookie);
    }

    public static void AddHeaders(ref HttpRequestMessage request)
    {
        request.Headers.Add("Accept-Encoding", "gzip, deflate, br, zstd");
        request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9," +
                                      "image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                                          "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 " +
                                          "Safari/537.36 Edg/128.0.0.0");
    }

    public static string ToQueryString(Dictionary<string, string?> parameters)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        foreach (KeyValuePair<string, string?> key in parameters) query[key.Key] = key.Value;
        return query.ToString()!;
    }

    public static string Encrypt(string plain)
    {
        using var aes = Aes.Create();
        aes.Key =
            "satori is an angel and i mean it"u8
                .ToArray();
        aes.IV = new byte[16];

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plain);
            }
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public static string Decrypt(string cipher)
    {
        using var aes = Aes.Create();
        aes.Key =
            "satori is an angel and i mean it"u8
                .ToArray();
        aes.IV = new byte[16];

        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        using var ms = new MemoryStream(Convert.FromBase64String(cipher));
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }
}