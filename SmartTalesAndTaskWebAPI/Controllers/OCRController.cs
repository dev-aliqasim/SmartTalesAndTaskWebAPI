﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tesseract;

namespace SmartTalesAndTaskWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OCRController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public OCRController(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
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
