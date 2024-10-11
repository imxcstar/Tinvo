using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace TrustInnova.Provider.XunFei.API
{
    internal class XFUtils
    {
        public static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static string GetAuth(string hostUrl, string apiKey, string apiSecret, string method = "GET")
        {
            var url = new Uri(hostUrl);

            string dateString = DateTime.UtcNow.ToString("r");

            byte[] signatureBytes = Encoding.ASCII.GetBytes($"host: {url.Host}\ndate: {dateString}\n{method} {url.AbsolutePath} HTTP/1.1");

            using HMACSHA256 hmacsha256 = new(Encoding.ASCII.GetBytes(apiSecret));
            byte[] computedHash = hmacsha256.ComputeHash(signatureBytes);
            string signature = Convert.ToBase64String(computedHash);

            string authorizationString = $"api_key=\"{apiKey}\",algorithm=\"hmac-sha256\",headers=\"host date request-line\",signature=\"{signature}\"";
            string authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes(authorizationString));

            string query = $"authorization={authorization}&date={dateString}&host={url.Host}";

            return new UriBuilder(url) { Scheme = url.Scheme, Query = query }.ToString();
        }
    }
}
