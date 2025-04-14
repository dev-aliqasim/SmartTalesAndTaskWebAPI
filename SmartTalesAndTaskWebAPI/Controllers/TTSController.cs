using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SmartTalesAndTaskWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TTSController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public TTSController(IHttpClientFactory httpClientFactory,IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
        }

        [HttpPost("synthesize")]
        public async Task<IActionResult> SynthesizeText([FromBody] TextRequest request)
        {
            var fastApiUrl = _configuration.GetSection("Api:FastApi:Url").Value;
            var TTS_Endpoint = fastApiUrl+"tts/synthesize"; // Or service URL in Docker/K8s

            var json = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(TTS_Endpoint, json);

            if (response.IsSuccessStatusCode)
            {
                var audioBytes = await response.Content.ReadAsByteArrayAsync();
                return File(audioBytes, "audio/wav", "tts_output.wav");
            }

            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
        }
    }
    public class TextRequest
    {
        public string text { get; set; }
    }
}
