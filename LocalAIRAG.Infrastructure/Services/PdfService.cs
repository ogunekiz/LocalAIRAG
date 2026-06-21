using System.Text;
using System.Data;
using ExcelDataReader;
using LocalAIRAG.Application.Abstractions;

namespace LocalAIRAG.Infrastructure.Services;

public class PdfService : IPdfService
{
	// Arayüze (Interface) tam uyum sağlamak için parametreyi Stream ve string fileName olarak ayırıyoruz veya güncelliyoruz
	// Eğer interface sadece "Stream stream" alıyorsa, uzantıyı metot parametresi yerine dışarıdan beslemek gerekir.
	// Varsayıyorum ki IPdfService imzanız şu şekilde: Task<string> ExtractTextAsync(Stream stream, string fileName);
	public async Task<string> ExtractTextAsync(Stream fileStream, string fileName)
	{
		var extension = Path.GetExtension(fileName).ToLower();

		if (extension == ".xlsx" || extension == ".xls")
		{
			return ExtractTextFromExcel(fileStream);
		}

		if (extension == ".pdf")
		{
			return await ExtractTextFromPdfAsync(fileStream);
		}

		throw new NotSupportedException($"'{extension}' dosya formatı henüz desteklenmemektedir.");
	}

	// 📊 EXCEL DOSYALARINI MARKDOWN TABLOSUNA ÇEVİREN METOT
	private string ExtractTextFromExcel(Stream stream)
	{
		// ExcelDataReader için Encoding kaydı (Türkçe karakterler için şart)
		System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

		var markdownBuilder = new StringBuilder();

		// Stream'in pozisyonunu başa çekiyoruz (Garanti olması için)
		if (stream.CanSeek)
		{
			stream.Position = 0;
		}

		using var reader = ExcelReaderFactory.CreateReader(stream);

		// Excel içindeki tüm sayfaları DataSet olarak okuyoruz
		var result = reader.AsDataSet(new ExcelDataSetConfiguration()
		{
			ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true } // İlk satırı başlık yap
		});

		foreach (DataTable table in result.Tables)
		{
			markdownBuilder.AppendLine($"## Sayfa: {table.TableName}");
			markdownBuilder.AppendLine();

			// Markdown Tablo Başlıklarını Oluştur
			string headers = "| " + string.Join(" | ", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName)) + " |";
			string separators = "| " + string.Join(" | ", table.Columns.Cast<DataColumn>().Select(_ => "---")) + " |";

			markdownBuilder.AppendLine(headers);
			markdownBuilder.AppendLine(separators);

			// Satırları Markdown Tablo Hücrelerine Çevir
			foreach (DataRow row in table.Rows)
			{
				string rowText = "| " + string.Join(" | ", row.ItemArray.Select(item => item?.ToString()?.Replace("\n", " ").Trim() ?? "")) + " |";
				markdownBuilder.AppendLine(rowText);
			}

			markdownBuilder.AppendLine();
		}

		return markdownBuilder.ToString();
	}

	// 📄 PDF OKUMA METODU
	private async Task<string> ExtractTextFromPdfAsync(Stream stream)
	{
		// Mevcut eski PDF okuma mantığınızı buraya entegre edebilirsiniz
		if (stream.CanSeek)
		{
			stream.Position = 0;
		}

		// TODO: Mevcut PDF text extraction kodunuz...
		await Task.CompletedTask;
		return "PDF İçeriği";
	}
}