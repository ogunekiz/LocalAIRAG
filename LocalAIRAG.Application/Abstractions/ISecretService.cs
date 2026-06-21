namespace LocalAIRAG.Application.Abstractions
{
	public interface ISecretService
	{
		Task<string> GetSecretAsync(string key);
	}
}
