namespace LocalAIRAG.Domain.Entities
{
	public class DocumentChunk
	{
		public string Id { get; set; } = Guid.NewGuid().ToString();
		public string DocumentId { get; set; } = string.Empty;
		public string Text { get; set; } = string.Empty;
		public List<float> Embedding { get; set; } = new();

		// 💡 Filtreleme ve yönetim için eklenen yeni alanlar:
		public string FileName { get; set; } = string.Empty;
		public string Extension { get; set; } = string.Empty;
	}
}