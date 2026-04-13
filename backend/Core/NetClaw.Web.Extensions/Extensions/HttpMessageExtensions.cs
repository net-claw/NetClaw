using System.Text;
using System.Text.Json;


namespace NetClaw.AspNetCore.Extensions.Extensions
{
    public static class HttpMessageExtensions
    {
        public static async Task<T> GetContentAsync<T>(this HttpResponseMessage httpResponse)
        {
            var content = await httpResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, JsonExtensions.SerializerOptions());
        }

        public static async Task<T> NewtonsoftGetContentAsync<T>(this HttpResponseMessage httpResponse)
        {
            var content = await httpResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content);
        }

        public static void SetContent<T>(this HttpRequestMessage httpRequestMessage, T content)
        {
            httpRequestMessage.Content =
                new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, "application/json");
        }

        public static async Task AssertIfBadRequestAsync(this HttpResponseMessage httpResponseMessage)
        {
            if (httpResponseMessage.StatusCode != System.Net.HttpStatusCode.BadRequest)
            {
                return;
            }

            var content = await httpResponseMessage.Content.ReadAsStringAsync();
            //using Xunit if use this
            //Assert.False(true, content);
        }

    }
}
