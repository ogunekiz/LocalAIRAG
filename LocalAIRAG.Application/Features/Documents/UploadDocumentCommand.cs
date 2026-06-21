using LocalAIRAG.Application.Abstractions;
using LocalAIRAG.Domain.Entities;
using MediatR;

namespace LocalAIRAG.Application.Features.Documents
{
	// 1. Dışarıdan istenecek parametreler (WebAPI'den gelecek)
	public record UploadDocumentCommand(Stream FileStream, string FileName) : IRequest<UploadDocumentResponse>;

	// 2. İşlem bittiğinde dönülecek cevap
	public record UploadDocumentResponse(bool IsSuccess, string Message, int TotalChunks);

	// 3. İş mantığının döndüğü yer (Handler)
	public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, UploadDocumentResponse>
	{
		private readonly IPdfService _pdfService;
		private readonly IEmbeddingService _embeddingService;
		private readonly IVectorDbService _vectorDbService;

		public UploadDocumentCommandHandler(
				IPdfService pdfService,
				IEmbeddingService embeddingService,
				IVectorDbService vectorDbService)
		{
			_pdfService = pdfService;
			_embeddingService = embeddingService;
			_vectorDbService = vectorDbService;
		}

		public async Task<UploadDocumentResponse> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
		{
			try
			{
				// 🛠️ ÇÖZÜM: request'ten gelen FileName bilgisini de metoda argüman olarak geçiyoruz
				string fullText = await _pdfService.ExtractTextAsync(request.FileStream, request.FileName);

				if (string.IsNullOrWhiteSpace(fullText))
					return new UploadDocumentResponse(false, "Dosya içeriği boş veya okunamadı.", 0);

				// Adım 2: Metni 500'er karakterlik parçalara (Chunk) böl
				var textChunks = SplitIntoChunks(fullText, chunkSize: 500, overlap: 50);
				var documentId = Guid.NewGuid().ToString();
				var chunksToSave = new List<DocumentChunk>();

				// Adım 3: Her bir parçayı sırayla vektörleştir
				foreach (var textChunk in textChunks)
				{
					var embedding = await _embeddingService.GetEmbeddingAsync(textChunk);

					chunksToSave.Add(new DocumentChunk
					{
						DocumentId = documentId,
						Text = textChunk,
						Embedding = embedding,
						// 💡 İLERİYE YATIRIM: ChromaDB için metadata alanlarını dolduruyoruz
						FileName = request.FileName,
						Extension = Path.GetExtension(request.FileName).ToLower()
					});
				}

				// Adım 4: Vektörleri ve metinleri topluca ChromaDB'ye kaydet
				await _vectorDbService.SaveChunksAsync(chunksToSave);

				return new UploadDocumentResponse(true, $"{request.FileName} başarıyla işlendi ve hafızaya alındı.", chunksToSave.Count);
			}
			catch (Exception ex)
			{
				return new UploadDocumentResponse(false, $"Hata oluştu: {ex.Message}", 0);
			}
		}

		// Basit Chunking Yardımcı Metodu
		private List<string> SplitIntoChunks(string text, int chunkSize, int overlap)
		{
			var chunks = new List<string>();
			if (string.IsNullOrEmpty(text)) return chunks;

			int i = 0;
			while (i < text.Length)
			{
				int length = Math.Min(chunkSize, text.Length - i);
				chunks.Add(text.Substring(i, length));
				i += (chunkSize - overlap); // Örtüşme (Overlap) sağlayarak anlam bütünlüğünü koruyoruz
			}
			return chunks;
		}
	}
}
