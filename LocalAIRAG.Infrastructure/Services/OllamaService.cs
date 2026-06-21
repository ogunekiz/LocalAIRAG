using LocalAIRAG.Application.Abstractions;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace LocalAIRAG.Infrastructure.Services
{
	public class OllamaService : IVariableLlmService, IEmbeddingService
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly ISecretService _secretService; // Vault için eklendi

		public OllamaService(IHttpClientFactory httpClientFactory, ISecretService secretService)
		{
			_httpClientFactory = httpClientFactory;
			_secretService = secretService;
		}

		// 🧠 YEREL EMBEDDING GÖREVİ (Docker'daki Ollama'ya gider)
		public async Task<List<float>> GetEmbeddingAsync(string text)
		{
			// Temiz bir istemci oluşturuyoruz
			var client = _httpClientFactory.CreateClient();

			var response = await client.PostAsJsonAsync("http://localhost:11434/api/embeddings", new
			{
				model = "mxbai-embed-large",
				prompt = text
			});

			response.EnsureSuccessStatusCode();

			var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();
			return result?.Embedding ?? new List<float>();
		}

		// 🌐 BULUT LLM GÖREVİ (Doğrudan Google Sunucularına gider)
		public async Task<string> GenerateResponseAsync(string prompt, string systemPrompt)
		{

			// 🔑 API Anahtarını canlı olarak HashiCorp Vault'tan çekiyoruz!
			string geminiApiKey = await _secretService.GetSecretAsync("gemini_key");

			// Modeli en güncel kararlı sürüm olan gemini-2.5-flash olarak güncelledik
			var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={geminiApiKey}";

			var requestBody = new
			{
				contents = new[]
					{
						new
						{
								role = "user",
								parts = new[]
								{
										new { text = $"Sistem Talimatı:\n{systemPrompt}\n\nKullanıcı İsteği:\n{prompt}" }
								}
						}
				},
				generationConfig = new
				{
					temperature = 0.2,
					maxOutputTokens = 1000
				}
			};

			var client = _httpClientFactory.CreateClient();
			var response = await client.PostAsJsonAsync(url, requestBody);

			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				throw new Exception($"Gemini API Hatası ({response.StatusCode}): {errorContent}");
			}

			var jsonResult = await response.Content.ReadFromJsonAsync<GeminiResponse>();
			return jsonResult?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "Cevap üretilemedi.";
		}

	}

	public class OllamaEmbeddingResponse { public List<float> Embedding { get; set; } }
}
public class GeminiResponse
{
	public Candidate[] Candidates { get; set; }
}
public class Candidate
{
	public Content Content { get; set; }
}
public class Content
{
	public Part[] Parts { get; set; }
}
public class Part
{
	public string Text { get; set; }
}
