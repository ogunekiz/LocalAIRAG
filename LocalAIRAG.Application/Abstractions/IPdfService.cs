namespace LocalAIRAG.Application.Abstractions
{
	public interface IPdfService
	{
		Task<string> ExtractTextAsync(Stream fileStream, string fileName);
	}
}
