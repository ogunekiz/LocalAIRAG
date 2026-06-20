using LocalAIRAG.Application.Abstractions;
using MediatR;

namespace LocalAIRAG.Application.Features.Chat
{
	// 1. Kullanıcının sorusu
	public record AskQuestionQuery(string Question) : IRequest<AskQuestionResponse>;

	// 2. Yapay zekanın yerel veriye bakarak verdiği cevap
	public record AskQuestionResponse(string Answer, List<string> RetrievedSources);

	// 3. RAG Akışını yöneten Handler
	public class AskQuestionQueryHandler : IRequestHandler<AskQuestionQuery, AskQuestionResponse>
	{
		private readonly IEmbeddingService _embeddingService;
		private readonly IVectorDbService _vectorDbService;
		private readonly IVariableLlmService _llmService;

		public AskQuestionQueryHandler(
				IEmbeddingService embeddingService,
				IVectorDbService vectorDbService,
				IVariableLlmService llmService)
		{
			_embeddingService = embeddingService;
			_vectorDbService = vectorDbService;
			_llmService = llmService;
		}

		public async Task<AskQuestionResponse> Handle(AskQuestionQuery request, CancellationToken cancellationToken)
		{
			// Adım 1: Kullanıcının sorduğu soruyu vektör haline getir
			var queryEmbedding = await _embeddingService.GetEmbeddingAsync(request.Question);

			// Adım 2: Bu vektörle ChromaDB'de en yakın (en alakalı) 3 döküman parçasını bul
			var similarChunks = await _vectorDbService.SearchSimilarChunksAsync(queryEmbedding, limit: 3);

			if (!similarChunks.Any())
			{
				return new AskQuestionResponse("Hafızada bu soruyla ilgili hiçbir döküman bulunamadı. Lütfen önce döküman yükleyin.", new());
			}

			// Adım 3: Bulunan döküman parçalarını birleştirerek bir "Context" (Bağlam) oluştur
			var contextText = string.Join("\n\n--- CHUNK ---\n\n", similarChunks.Select(c => c.Text));

			// Adım 4: Prompt Engineering - Kurumsal kural setini ve bağlamı hazırla
			var systemPrompt = "Sen şirkete ait dökümanları analiz eden kurumsal bir yapay zeka asistanısın. " +
												 "Sana aşağıda sağlanan 'Bağlam' (Context) verilerini kullanarak kullanıcının sorusunu yanıtla. " +
												 "Eğer sorunun cevabı sağlanan bağlam içerisinde yoksa, bilgiyi dışarıdan uydurma, dürüstçe 'Bu bilgi dökümanlarda bulunmuyor' de.";

			var finalPrompt = $"[Bağlam Verileri]:\n{contextText}\n\n[Kullanıcı Sorusu]:\n{request.Question}";

			// Adım 5: Yerel Llama3 modeline gönder ve dökümana dayalı cevabı al
			string aiAnswer = await _llmService.GenerateResponseAsync(finalPrompt, systemPrompt);

			var sources = similarChunks.Select(c => c.Text).ToList();
			return new AskQuestionResponse(aiAnswer, sources);
		}
	}
}
