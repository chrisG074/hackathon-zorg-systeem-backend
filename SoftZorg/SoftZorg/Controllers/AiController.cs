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
		private readonly string? _apiKey;

		public AiController(IHttpClientFactory httpClientFactory)
		{
			_httpClientFactory = httpClientFactory;
          _apiKey = Environment.GetEnvironmentVariable("GOOGLEAIKEY");
		}

		[HttpPost("chat")]
		public async Task<IActionResult> ChatWithAi([FromBody] AiRequest model)
		{
			if (string.IsNullOrEmpty(model.Prompt))
				return BadRequest(new { message = "Geen tekst ontvangen." });

			if (string.IsNullOrWhiteSpace(_apiKey))
                return StatusCode(500, new { message = "API key ontbreekt. Zet GOOGLEAIKEY als environment variable." });

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
				   "5. Als alle informatie er is, geef dan een korte samenvatting en vraag: 'Ik heb alle gegevens verzameld. Zal ik de melding voor je klaarzetten?'\n" +
				   "6. Zodra de gebruiker bevestigt (bijv: 'ja', 'is goed', 'doe maar'), antwoord je ALTIJD met exact het woord [COMPLEET] gevolgd door een JSON object met de verzamelde data.\n\n" +
				   "VRAGENLIJSTEN & JSON KEYS:\n\n" +
				   "--- FACILITAIR ---\n" +
				   "- Wat is er kapot? -> key: 'wat_kapot'\n" +
				   "- Welke ruimte? -> key: 'ruimte'\n" +
				   "- Wat is de storing? -> key: 'storing_omschrijving'\n" +
				   "- Spoed (true) of standaard (false)? -> key: 'is_spoed'\n" +
				   "JSON formaat: { \"wat_kapot\": \"\", \"ruimte\": \"\", \"storing_omschrijving\": \"\", \"is_spoed\": false }\n\n" +
				   "--- MIC ---\n" +
				   "- Datum/tijd? -> key: 'datum_tijd'\n" +
				   "- Waar? -> key: 'locatie'\n" +
				   "- Welke zorgvrager? -> key: 'zorgvrager'\n" +
				   "- Soort incident (kies uit: Medicatie, probleemgedrag, seksueel misbruik, vermissing bewoner, stoot/knel/bots, verbranding, verslikking, inname schadelijke stoffen, beleidsfout, overig)? -> key: 'soort_incident'\n" +
				   "- Gedrag cliënt? -> key: 'gedrag_client'\n" +
				   "- Wat is er gebeurd? -> key: 'gebeurtenis'\n" +
				   "- Letsel en waar? -> key: 'letsel'\n" +
				   "- Ingelicht & Hoe voorkomen? -> key: 'opvolging'\n" +
				   "JSON formaat: { \"datum_tijd\": \"\", \"locatie\": \"\", \"zorgvrager\": \"\", \"soort_incident\": \"\", \"gedrag_client\": \"\", \"gebeurtenis\": \"\", \"letsel\": \"\", \"opvolging\": \"\" }\n\n" +
				   "--- MIM ---\n" +
				   "- Datum/tijd? -> key: 'datum_tijd'\n" +
				   "- Waar? -> key: 'locatie'\n" +
				   "- Soort incident (kies uit: Agressie fysiek, agressie verbaal, gevaarlijke situatie, ongewenst gedrag, overig, psychisch letsel, prik/spat/snij incident, valpartijen)? -> key: 'soort_incident'\n" +
				   "- Andere mensen betrokken? -> key: 'betrokkenen'\n" +
				   "- Hoe gebeurd? -> key: 'hoe_gebeurd'\n" +
				   "- Letsel? -> key: 'letsel'\n" +
				   "- Behoefte aan opvang (true/false)? -> key: 'behoefte_opvang'\n" +
				   "JSON formaat: { \"datum_tijd\": \"\", \"locatie\": \"\", \"soort_incident\": \"\", \"betrokkenen\": \"\", \"hoe_gebeurd\": \"\", \"letsel\": \"\", \"behoefte_opvang\": false }";
		}
	}

	public class AiRequest
	{
		public string Prompt { get; set; } = string.Empty;
		public string? Type { get; set; }
		public object? Context { get; set; }
	}
}