using LocalAIRAG.Application.Abstractions;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace LocalAIRAG.Infrastructure.Services
{
	public class OllamaService : IVariableLlmService, IEmbeddingService
	{
		private readonly HttpClient _httpClient;

		public OllamaService(HttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		// 1. Metni Vektöre Çevirme (Embedding) Implemantasyonu
		public async Task<List<float>> GetEmbeddingAsync(string text)
		{
			var requestBody = new { model = "mxbai-embed-large", prompt = text };

			var response = await _httpClient.PostAsJsonAsync("/api/embeddings", requestBody);
			response.EnsureSuccessStatusCode();

			var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();
			return result?.Embedding ?? new List<float>();
		}

		// 2. LLM'den Cevap Üretme (Generate) Implemantasyonu
		public async Task<string> GenerateResponseAsync(string prompt, string systemPrompt = "")
		{
			var requestBody = new
			{
				model = "llama3",
				prompt = prompt,
				system = systemPrompt,
				stream = false // Cevabın parça parça değil, tek seferde bütün gelmesi için
			};

			var response = await _httpClient.PostAsJsonAsync("/api/generate", requestBody);
			response.EnsureSuccessStatusCode();

			var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>();
			return result?.Response ?? string.Empty;
		}
	}

	// Dışarıdan dönecek JSON modelleri:
	public class OllamaEmbeddingResponse
	{
		[JsonPropertyName("embedding")]
		public List<float> Embedding { get; set; } = new();
	}

	public class OllamaGenerateResponse
	{
		[JsonPropertyName("response")]
		public string Response { get; set; } = string.Empty;
	}
}
