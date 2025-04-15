using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using Tesseract;

namespace SmartTalesAndTaskWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OCRController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly HttpClient _httpClient;
        public OCRController(IConfiguration configuration, IWebHostEnvironment env, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _env = env;
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost("extract-text")]
        public async Task<IActionResult> ExtractText([FromForm] IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest("No image uploaded.");

            string uploadFolder = _configuration["UploadSettings:UploadFolder"] ?? "Uploads";
            string filePath = Path.Combine(uploadFolder, image.FileName);

            try
            {
                Directory.CreateDirectory(uploadFolder);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                string extractedText = ProcessImageWithTesseract(filePath);
                return Ok(extractedText);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error processing image.");
            }

        }

        [HttpPost("image-to-speech")]
        public async Task<IActionResult> ImageToSpeech([FromForm] IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest("No image uploaded.");

            string uploadFolder = _configuration["UploadSettings:UploadFolder"] ?? "Uploads";
            string filePath = Path.Combine(uploadFolder, image.FileName);

            try
            {
                Directory.CreateDirectory(uploadFolder);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                string extractedText = ProcessImageWithTesseract(filePath);

                //--------------------------------------------------------------------//
                //----------- Now converting extracted text to audio-----------------//

                var fastApiUrl = _configuration.GetSection("Api:FastApi:Url").Value;
                var TTS_Endpoint = fastApiUrl + "tts/synthesize"; // Or service URL in Docker/K8s

                var json = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(new { text = extractedText }),
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
            catch (Exception ex)
            {
                return StatusCode(500, "Error processing image.");
            }


        }



        private string ProcessImageWithTesseract(string imagePath)
        {
            string _tessDataPath = Path.Combine(_env.WebRootPath, "tessdata");
            using var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);
            using var img = Pix.LoadFromFile(imagePath);
            using var page = engine.Process(img);
            return page.GetText();
        }
    }
}
