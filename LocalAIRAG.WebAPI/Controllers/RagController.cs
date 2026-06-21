using LocalAIRAG.Application.Features.Chat;
using LocalAIRAG.Application.Features.Documents;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LocalAIRAG.WebAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class RagController : ControllerBase
	{
		private readonly IMediator _mediator;

		public RagController(IMediator mediator)
		{
			_mediator = mediator;
		}

		// 📥 1. Endpoint: PDF Dökümanı Yükleme ve Hafızaya Alma
		[HttpPost("upload")]
		public async Task<IActionResult> UploadDocument(IFormFile file)
		{
			if (file == null || file.Length == 0)
				return BadRequest("Lütfen geçerli bir PDF dosyası yükleyin.");

			// Dosyayı belleğe (stream) alıp MediatR Command'ine gönderiyoruz
			using var stream = file.OpenReadStream();
			var command = new UploadDocumentCommand(stream, file.FileName);

			var response = await _mediator.Send(command);

			if (!response.IsSuccess)
				return BadRequest(response.Message);

			return Ok(response);
		}

		// 💬 2. Endpoint: Hafızadaki Veriye Göre Soru Sorma
		[HttpPost("ask")]
		public async Task<IActionResult> AskQuestion([FromBody] string question)
		{
			if (string.IsNullOrWhiteSpace(question))
				return BadRequest("Soru boş olamaz.");

			// Mevcut tırnakları (varsa) tamamen temizleyip, kesin olarak tırnak içine alıyoruz
			var formattedQuestion = $"\"{question.Trim().Trim('"')}\"";

			var query = new AskQuestionQuery(formattedQuestion);
			var response = await _mediator.Send(query);

			return Ok(response);
		}
	}
}
