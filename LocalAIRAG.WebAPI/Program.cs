using LocalAIRAG.Application.Abstractions;
using LocalAIRAG.Infrastructure.Services;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MediatR Kaydı
builder.Services.AddMediatR(typeof(IEmbeddingService).Assembly);

// PDF Okuma Servisi
builder.Services.AddScoped<IPdfService, PdfService>();

// 📦 1. IHttpClientFactory Kaydı (Gemini ve Yerel Embedding İstemcileri İçin)
builder.Services.AddHttpClient();

// 🧠 2. Yapay Zeka ve Vektörleştirme Servisleri (Gemini + Yerel Ollama Entegreli)
builder.Services.AddScoped<IEmbeddingService, OllamaService>();
builder.Services.AddScoped<IVariableLlmService, OllamaService>();

builder.Services.AddSingleton<ISecretService, VaultService>();

// 🗄️ 3. ÇÖZÜM: Vektör Veritabanı (ChromaDB) HttpClient Kaydı (Yorum satırını kaldırdık ve temizledik)
builder.Services.AddHttpClient<IVectorDbService, ChromaDbService>(client =>
{
	client.BaseAddress = new Uri("http://localhost:8000");
});

var app = builder.Build(); // Artık burası hatasız geçecek!

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();