using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace SoftZorg.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AiController : ControllerBase
	{
		private readonly IHttpClientFactory _httpClientFactory;

		private readonly string _apiKey = "AIzaSyAl2uC2_HqNPxGmPYIZAmhk9fJEgWfdMgc";

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
				// Endpoint for Gemini 3.1 Flash Lite (Preview) with Thinking support
				var geminiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-lite-preview:generateContent?key={_apiKey}";

				var client = _httpClientFactory.CreateClient();

				// Construct the payload with System Instructions and Thinking Config
				var requestBody = new
				{
					system_instruction = new
					{
						parts = new[] { new { text = GetSystemPrompt() } }
					},
					contents = new[]
					{
						new
						{
							role = "user",
							parts = new[] { new { text = model.Prompt } }
						}
					},
					generationConfig = new
					{
						thinking_config = new
						{
							include_thoughts = true // Enables the step-by-step reasoning
						}
					}
				};

				var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
				var content = new StringContent(JsonSerializer.Serialize(requestBody, jsonOptions), Encoding.UTF8, "application/json");

				var response = await client.PostAsync(geminiUrl, content);

				if (response.IsSuccessStatusCode)
				{
					var responseString = await response.Content.ReadAsStringAsync();
					// Returns the full JSON so frontend can parse 'text' and potentially show 'thoughts' in logs
					return Content(responseString, "application/json");
				}

				var errorDetails = await response.Content.ReadAsStringAsync();
				return StatusCode((int)response.StatusCode, new { message = "Google AI Studio fout.", details = errorDetails });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Fout in backend: " + ex.Message });
			}
		}

		private string GetSystemPrompt()
		{
			return "Je bent een intelligente zorg-assistent. Jouw doel is om een melding (Facilitair, MIC of MIM) volledig te krijgen met ZO MIN MOGELIJK dubbele vragen.\n\n" +
				   "STRENGSTE REGELS:\n" +
				   "1. ANALYSEER eerst de hele conversatie. Streep de vragen weg waar de gebruiker al (deels) antwoord op heeft gegeven.\n" +
				   "2. ONTHOUD namen, tijden en locaties die eerder in het gesprek zijn genoemd.\n" +
				   "3. STEL ALLEEN de vragen die nog echt ontbreken. Combineer ze in één natuurlijk bericht.\n" +
				   "4. BEVESTIG wat je al weet (bijv: 'Ik heb genoteerd dat meneer Jan de Vries is gevallen...').\n" +
				   "5. Als alle informatie er is, geef dan een gestructureerde samenvatting en vraag: 'Klopt dit zo?'\n\n" +
				   "VRAGENLIJSTEN:\n" +
				   "- Facilitair: Wat is er kapot? Welke ruimte? Wat is de storing? Spoed of standaard?\n" +
				   "- MIC: Datum/tijd? Locatie? Welke zorgvrager? Soort incident? Gedrag cliënt? Wat is er gebeurd? Letsel (en waar)? Wie is ingelicht? Hoe te voorkomen?\n" +
				   "- MIM: Datum/tijd? Locatie? Soort incident? Betrokkenen? Hoe gebeurd? Letsel? Behoefte aan nazorg/vertrouwenspersoon?";
		}
	}

	public class AiRequest
	{
		public string Prompt { get; set; } = string.Empty;
		public string? Type { get; set; }
		public object? Context { get; set; }
	}
}