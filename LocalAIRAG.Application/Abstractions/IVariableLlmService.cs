namespace LocalAIRAG.Application.Abstractions
{
	public interface IVariableLlmService
	{
		Task<string> GenerateResponseAsync(string prompt, string systemPrompt = "");
	}
}
