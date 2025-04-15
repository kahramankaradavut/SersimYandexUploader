using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace YandexUploader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YandexController : ControllerBase
    {
        private readonly HttpClient _client;
        private readonly IConfiguration _configuration;
        private const string ClientId = "ed7f464694ba455a8c6d2b1725978140";
        private const string ClientSecret = "7777965ce9034b62ac6b20df0b39393d";
        private const string RedirectUri = "https://localhost:5135/api/Yandex/callback";

        public YandexController(IConfiguration configuration)
        {
            _client = new HttpClient();
                _configuration = configuration;

        }

        [HttpGet("authorize")]
        public IActionResult Authorize()
        {
            var authorizationUrl = $"https://oauth.yandex.com/authorize?response_type=code&client_id={ClientId}&redirect_uri={RedirectUri}";
            
            return Redirect(authorizationUrl);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code)
        {
            var tokenUrl = "https://oauth.yandex.com/token";
            var parameters = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("client_id", ClientId),
                new KeyValuePair<string, string>("client_secret", ClientSecret),
                new KeyValuePair<string, string>("redirect_uri", RedirectUri)
            });

            var response = await _client.PostAsync(tokenUrl, parameters);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return BadRequest("Token alma hatası: " + responseBody);

            var token = JsonConvert.DeserializeObject<TokenResponse>(responseBody);
            return Ok(token);
        }

     [HttpPost("upload")]
// public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
public async Task<ActionResult> AddOne([FromForm] DocumentAddRequest docAddRequest, IFormFile file) 

{
    if (file == null || file.Length == 0)
        return BadRequest("Dosya seçilmedi.");

    var nameSuffix = string.IsNullOrWhiteSpace(docAddRequest?.Name) ? "" : $"_{docAddRequest.Name}";
    var fileName = $"{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}{nameSuffix}_{file.FileName}";
    var filePath = $"/MyAppFolder/{fileName}";

    var uploadLinkUrl = $"https://cloud-api.yandex.net/v1/disk/resources/upload?path=disk:{filePath}&overwrite=true";
    string accessToken = _configuration["Yandex:AccessToken"] ?? throw new InvalidOperationException("AccessToken is not configured.");
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", accessToken);

    // Upload link al
    var linkResponse = await _client.GetAsync(uploadLinkUrl);
    if (!linkResponse.IsSuccessStatusCode)
        return StatusCode((int)linkResponse.StatusCode, "Yükleme bağlantısı alınamadı.");

    var linkJson = await linkResponse.Content.ReadAsStringAsync();
    var uploadUrl = JsonConvert.DeserializeObject<UploadLinkResponse>(linkJson)?.Href;

    if (string.IsNullOrEmpty(uploadUrl))
        return StatusCode(500, "Yükleme bağlantısı çözümlenemedi.");

    using var fileStream = file.OpenReadStream();
    var streamContent = new StreamContent(fileStream);
    streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

    var uploadResponse = await _client.PutAsync(uploadUrl, streamContent);

    if (!uploadResponse.IsSuccessStatusCode)
        return StatusCode((int)uploadResponse.StatusCode, "Dosya yüklenemedi.");

    // Dosyayı public yap
    var publishUri = $"https://cloud-api.yandex.net/v1/disk/resources/publish?path=disk:{filePath}";
    var publishResponse = await _client.PutAsync(publishUri, null);
    if (!publishResponse.IsSuccessStatusCode)
        return StatusCode((int)publishResponse.StatusCode, "Dosya public yapılamadı.");

    // Public link al
    var infoUri = $"https://cloud-api.yandex.net/v1/disk/resources?path=disk:{filePath}";
    var infoResponse = await _client.GetAsync(infoUri);
    var infoJson = await infoResponse.Content.ReadAsStringAsync();
    var publicLink = JsonConvert.DeserializeObject<ResourceInfo>(infoJson)?.PublicUrl;

    return Ok(new { message = "Dosya başarıyla yüklendi", publicUrl = publicLink });
}

        
        
        public class DocumentAddRequest{
            public string? Name { get; set; }
        }

        public class TokenResponse
        {
            [JsonProperty("access_token")]
            public string? AccessToken { get; set; }

            [JsonProperty("token_type")]
            public string? TokenType { get; set; }

            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }
        }

        public class UploadLinkResponse
        {
            [JsonProperty("href")]
            public string? Href { get; set; }
        }

        public class ResourceInfo
        {
            [JsonProperty("public_url")]
            public string? PublicUrl { get; set; }
        }
    }

    
}
