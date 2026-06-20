using LocalAIRAG.Domain.Entities;

namespace LocalAIRAG.Application.Abstractions
{
	public interface IVectorDbService
	{
		Task SaveChunksAsync(List<DocumentChunk> chunks);
		Task<List<DocumentChunk>> SearchSimilarChunksAsync(List<float> queryEmbedding, int limit = 3);
	}
}
