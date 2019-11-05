using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using WebAssembly;

namespace Jazz2.Wasm
{
    public static class WasmResourceLoader
    {
        public static string GetBaseAddress()
        {
            using (JSObject window = (JSObject)Runtime.GetGlobalObject("window"))
            using (JSObject location = (JSObject)window.GetObjectProperty("location")) {
                string address = (string)location.GetObjectProperty("href");

                int idx = address.LastIndexOf('/');
                if (idx != -1) {
                    address = address.Substring(0, idx + 1);
                }

                return address;
            }
        }

        public static async Task<(Stream, int)> LoadAsync(string relativePath, string baseAddress)
        {
            Stream content;
            int size;

            var httpClient = new HttpClient { BaseAddress = new Uri(baseAddress), Timeout = new TimeSpan(0, 20, 0) };

#if DEBUG
            Console.WriteLine($"Requesting '{relativePath}' at '{baseAddress}'...");
#endif

            try {
                var response = await httpClient.GetAsync(relativePath);
                response.EnsureSuccessStatusCode();
                content = await response.Content.ReadAsStreamAsync();
                size = (int)(response.Content.Headers.ContentLength ?? 0);
            } catch (Exception ex) {
                content = null;
                size = 0;

                Console.WriteLine($"[Error] {nameof(WasmResourceLoader)}.{nameof(LoadAsync)}(): {ex}");
            }

            return (content, size);
        }
    }
}