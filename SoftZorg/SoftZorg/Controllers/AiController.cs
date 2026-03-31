using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace SoftZorg.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AiController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> ChatWithAi([FromBody] AiRequest model)
        {
            if (string.IsNullOrEmpty(model.Prompt))
                return BadRequest(new { message = "Geen tekst ontvangen." });

            try
            {
                // Verbinding met lokale LM Studio
                var lmStudioUrl = "http://localhost:1234/v1/chat/completions";
                var client = _httpClientFactory.CreateClient();

                var requestBody = new
                {
                    model = "local-model",
                    messages = new[] {
                        new { role = "system", content = "Je bent een behulpzame zorg-assistent." },
                        new { role = "user", content = model.Prompt }
                    },
                    temperature = 0.7
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(lmStudioUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    return Content(responseString, "application/json");
                }

                return StatusCode(500, new { message = "LM Studio reageert niet. Staat de server aan?" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Fout in backend: " + ex.Message });
            }
        }
    }

    public class AiRequest { public string Prompt { get; set; } = string.Empty; }
}