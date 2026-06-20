using LocalAIRAG.Application.Abstractions;
using UglyToad.PdfPig;

namespace LocalAIRAG.Infrastructure.Services
{
	public class PdfService : IPdfService
	{
		public async Task<string> ExtractTextAsync(Stream pdfStream)
		{
			return await Task.Run(() =>
			{
				using var document = PdfDocument.Open(pdfStream);
				var text = string.Empty;

				foreach (var page in document.GetPages())
				{
					text += page.Text + " ";
				}

				return text;
			});
		}
	}
}
