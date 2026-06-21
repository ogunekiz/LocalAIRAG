using LocalAIRAG.Application.Abstractions;
using LocalAIRAG.Domain.Entities;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace LocalAIRAG.Infrastructure.Services
{
	public class ChromaDbService : IVectorDbService
	{
		private readonly HttpClient _httpClient;
		private const string CollectionName = "dotnet_rag_collection";

		public ChromaDbService(HttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		// Koleksiyon yoksa oluşturan yardımcı metot
		private async Task EnsureCollectionExistsAsync()
		{
			var createResponse = await _httpClient.PostAsJsonAsync("/api/v1/collections", new { name = CollectionName });
			// Eğer zaten varsa 400 dönebilir, o yüzden EnsureSuccessStatusCode demiyoruz.
		}

		// 1. Chunk'ları ve Vektörleri ChromaDB'ye Kaydetme
		public async Task SaveChunksAsync(List<DocumentChunk> chunks)
		{
			// 1. Önce koleksiyonun var olduğundan EMİN OLALIM (Yoksa sıfırdan yaratacak)
			await EnsureCollectionExistsAsync();

			// 2. Koleksiyonun sistemdeki ID'sini almak için istek atıyoruz
			var collectionResponse = await _httpClient.GetFromJsonAsync<ChromaCollectionResponse>($"/api/v1/collections/{CollectionName}");

			if (collectionResponse != null)
			{
				var requestBody = new
				{
					ids = chunks.Select(c => c.Id).ToList(),
					embeddings = chunks.Select(c => c.Embedding).ToList(),
					documents = chunks.Select(c => c.Text).ToList(),
					metadatas = chunks.Select(c => new { documentId = c.DocumentId }).ToList()
				};

				// 3. Kararlı sürüm (0.4.15) için resmi döküman ekleme endpoint'i
				var response = await _httpClient.PostAsJsonAsync($"/api/v1/collections/{collectionResponse.Id}/add", requestBody);
				response.EnsureSuccessStatusCode();
			}
		}

		// 2. Vektör ile Benzerlik Araması (Query) Yapma
		public async Task<List<DocumentChunk>> SearchSimilarChunksAsync(List<float> queryEmbedding, int limit = 3)
		{
			await EnsureCollectionExistsAsync();

			var collectionResponse = await _httpClient.GetFromJsonAsync<ChromaCollectionResponse>($"/api/v1/collections/{CollectionName}");
			if (collectionResponse == null) return new List<DocumentChunk>();

			var requestBody = new
			{
				query_embeddings = new List<List<float>> { queryEmbedding },
				n_results = limit
			};

			var response = await _httpClient.PostAsJsonAsync($"/api/v1/collections/{collectionResponse.Id}/query", requestBody);
			response.EnsureSuccessStatusCode();

			var queryResult = await response.Content.ReadFromJsonAsync<ChromaQueryResult>();
			var results = new List<DocumentChunk>();

			if (queryResult != null && queryResult.Ids.Any() && queryResult.Ids[0].Any())
			{
				for (int i = 0; i < queryResult.Ids[0].Count; i++)
				{
					results.Add(new DocumentChunk
					{
						Id = queryResult.Ids[0][i],
						Text = queryResult.Documents[0][i],
						DocumentId = queryResult.Metadatas[0][i].DocumentId
					});
				}
			}

			return results;
		}
	}

	// ChromaDB API Modelleri
	public class ChromaCollectionResponse
	{
		[JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
	}

	public class ChromaQueryResult
	{
		[JsonPropertyName("ids")] public List<List<string>> Ids { get; set; } = new();
		[JsonPropertyName("documents")] public List<List<string>> Documents { get; set; } = new();
		[JsonPropertyName("metadatas")] public List<List<ChromaMetadata>> Metadatas { get; set; } = new();
	}

	public class ChromaMetadata
	{
		[JsonPropertyName("documentId")] public string DocumentId { get; set; } = string.Empty;
	}
}
