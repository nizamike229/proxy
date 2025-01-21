using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace MyProxy.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProxyController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public ProxyController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return BadRequest("URL parameter is required.");
            }

            try
            {
                Console.WriteLine($"[INFO] Request started for URL: {url}");
                
                var proxyAddress = "http://191.102.123.196:999";
                var handler = new HttpClientHandler
                {
                    Proxy = new WebProxy(proxyAddress),
                    UseProxy = false
                };
                
                using var proxyClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };
                
                var stopwatch = Stopwatch.StartNew();

                Console.WriteLine($"[INFO] Sending request to {url} through proxy {proxyAddress}...");
                
                var response = await proxyClient.GetAsync(url);
                
                stopwatch.Stop();
                Console.WriteLine(
                    $"[INFO] Request completed in {stopwatch.ElapsedMilliseconds} ms with status code: {response.StatusCode}");
                
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStreamAsync();
                
                Console.WriteLine($"[INFO] Response content type: {response.Content.Headers.ContentType}");
                
                return File(content, response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream");
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"[ERROR] Request timed out: {ex.Message}");
                return StatusCode(408, "Request timed out.");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[ERROR] HTTP request error: {ex.Message}");
                return StatusCode(502, $"HTTP request error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] General error: {ex.Message}");
                return StatusCode(500, $"Unexpected error: {ex.Message}");
            }
        }
    }
}