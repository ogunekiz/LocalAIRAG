using LocalAIRAG.Application.Abstractions;
using MediatR;

namespace LocalAIRAG.Application.Features.Chat
{
	// 1. Kullanıcının sorusu - Opsiyonel olarak belirli bir dosyada arama desteği ekledik
	public record AskQuestionQuery(string Question, string? FilterFileName = null) : IRequest<AskQuestionResponse>;

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

			// Adım 2: ChromaDB'de en yakın döküman parçalarını bul (Eğer filtre varsa sadece o dosyaya bak)
			// Not: IVectorDbService'deki Search yönteminizi genişleterek filtre parametresini ChromaDB query'sinin 'where' alanına geçirebilirsiniz.
			var similarChunks = await _vectorDbService.SearchSimilarChunksAsync(queryEmbedding, limit: 4);

			// Eğer kodunuzda filtreleme aktifse benzeri bir eşleşme süzgeci uygulanabilir:
			if (!string.IsNullOrEmpty(request.FilterFileName))
			{
				similarChunks = similarChunks.Where(c => c.FileName.Equals(request.FilterFileName, StringComparison.OrdinalIgnoreCase)).ToList();
			}

			if (!similarChunks.Any())
			{
				return new AskQuestionResponse("Hafızada bu soruyla ilgili veya kriterlere uygun hiçbir döküman bulunamadı.", new());
			}

			// Adım 3: Bulunan döküman parçalarını birleştirerek bir "Context" (Bağlam) oluştur
			var contextText = string.Join("\n\n--- CHUNK ---\n\n", similarChunks.Select(c => c.Text));

			// Adım 4: Prompt Engineering - Kurumsal kural setini ve özellikle TABLO analiz yeteneğini güçlendiriyoruz
			var systemPrompt = "Sen kurumsal dökümanları ve ham Excel verilerini analiz eden profesyonel bir yapay zeka asistanısın.\n\n" +
												 "SANA VERİLEN KURALLAR:\n" +
												 "1. Sana sağlanan [Bağlam Verileri] alanında Markdown formatında tablolar (| satır | sütun | yapısı) yer alabilir. Satır ve sütunların kesişimindeki bilgileri birbirleriyle doğru eşleştir. Bir personelin verisini altındaki veya üstündeki satırla karıştırma.\n" +
												 "2. Sadece ve sadece sana iletilen bağlam verilerini kaynak al. Kesinlikle dışarıdan bilgi veya varsayım ekleme.\n" +
												 "3. Eğer sorulan sorunun cevabı sağlanan bağlam verilerinde veya tablonun hücrelerinde net olarak yoksa dürüstçe 'Bu bilgi dökümanlarda bulunmuyor.' şeklinde yanıt ver.\n" +
												 "4. Yanıtlarını profesyonel, net ve akıcı bir Türkçe ile hazırla.";

			var finalPrompt = $"[Bağlam Verileri]:\n{contextText}\n\n[Kullanıcı Sorusu]:\n{request.Question}";

			// Adım 5: Gemini / Yerel LLM modeline gönder ve dökümana dayalı cevabı al
			string aiAnswer = await _llmService.GenerateResponseAsync(finalPrompt, systemPrompt);

			var sources = similarChunks.Select(c => c.Text).ToList();
			return new AskQuestionResponse(aiAnswer, sources);
		}
	}
}