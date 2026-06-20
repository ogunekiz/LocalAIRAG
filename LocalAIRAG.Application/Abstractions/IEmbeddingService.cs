namespace LocalAIRAG.Application.Abstractions
{
	public interface IEmbeddingService
	{
		Task<List<float>> GetEmbeddingAsync(string text);
	}
}
