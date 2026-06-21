using LocalAIRAG.Application.Abstractions;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;

namespace LocalAIRAG.Infrastructure.Services;

public class VaultService : ISecretService
{
	private readonly IVaultClient _vaultClient;

	public VaultService()
	{
		// Vault bağlantı ayarları
		var vaultClientSettings = new VaultClientSettings(
				"http://localhost:8200",
				new TokenAuthMethodInfo("my-root-token")
		);

		_vaultClient = new VaultClient(vaultClientSettings);
	}

	public async Task<string> GetSecretAsync(string key)
	{
		try
		{
			// secret/data/localairag yolundaki verileri kv-v2 motoru standardıyla okuyoruz
			Secret<SecretData> secret = await _vaultClient.V1.Secrets.KeyValue.V2
					.ReadSecretAsync(path: "localairag", mountPoint: "secret");

			if (secret?.Data?.Data != null && secret.Data.Data.TryGetValue(key, out var value))
			{
				return value?.ToString() ?? string.Empty;
			}

			throw new Exception($"Vault içinde '{key}' anahtarı bulunamadı.");
		}
		catch (Exception ex)
		{
			throw new Exception($"Vault'tan veri okunurken hata oluştu: {ex.Message}");
		}
	}
}