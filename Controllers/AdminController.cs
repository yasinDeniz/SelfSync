using Microsoft.AspNetCore.Mvc;
using SelfSync.Attributes;

namespace SelfSync.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly ILogger<AdminController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;

        public AdminController(ILogger<AdminController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            
            // API anahtarını yapılandırmadan al (ortam değişkeni veya appsettings)
            _apiKey = GetApiKey();
            
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("API anahtarı bulunamadı! Yönetici fonksiyonları devre dışı.");
            }
        }

        private string GetApiKey()
        {
            // Önce yapılandırmadan API anahtarını almaya çalış
            string apiKey = _configuration["AdminSettings:ApiKey"];
            
            // Eğer bulunamazsa, ortam değişkeninden almaya çalış
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = Environment.GetEnvironmentVariable("SELFSYNC_ADMIN_API_KEY");
            }
            
            return apiKey;
        }

        [HttpPost("reset-rate-limit")]
        public IActionResult ResetRateLimit([FromHeader(Name = "X-API-Key")] string apiKey, [FromBody] ResetRateLimitRequest request)
        {
            // API anahtarını doğrula
            if (string.IsNullOrEmpty(_apiKey))
            {
                return StatusCode(500, "API anahtarı yapılandırılmamış. Yönetici işlemleri kullanılamaz.");
            }
            
            if (string.IsNullOrEmpty(apiKey) || apiKey != _apiKey)
            {
                _logger.LogWarning("Geçersiz API anahtarı ile rate limit sıfırlama denemesi");
                return Unauthorized("Geçersiz API anahtarı");
            }

            // IP adresi ve endpoint kontrol et
            if (string.IsNullOrEmpty(request.IpAddress) || string.IsNullOrEmpty(request.EndpointPath))
            {
                return BadRequest("IP adresi ve endpoint path gereklidir");
            }

            bool success = RequestRateLimitAttribute.ResetRateLimit(request.IpAddress, request.EndpointPath);
            
            if (success)
            {
                _logger.LogInformation("Rate limit başarıyla sıfırlandı: {IpAddress} - {Endpoint}", request.IpAddress, request.EndpointPath);
                return Ok(new { message = $"{request.IpAddress} IP adresi için {request.EndpointPath} endpoint'inin rate limiti başarıyla sıfırlandı" });
            }
            else
            {
                return NotFound(new { message = $"{request.IpAddress} IP adresi için {request.EndpointPath} endpoint'inde aktif rate limit kaydı bulunamadı" });
            }
        }

        [HttpPost("reset-all-rate-limits")]
        public IActionResult ResetAllRateLimits([FromHeader(Name = "X-API-Key")] string apiKey)
        {
            // API anahtarını doğrula
            if (string.IsNullOrEmpty(_apiKey))
            {
                return StatusCode(500, "API anahtarı yapılandırılmamış. Yönetici işlemleri kullanılamaz.");
            }
            
            if (string.IsNullOrEmpty(apiKey) || apiKey != _apiKey)
            {
                _logger.LogWarning("Geçersiz API anahtarı ile tüm rate limitleri sıfırlama denemesi");
                return Unauthorized("Geçersiz API anahtarı");
            }

            RequestRateLimitAttribute.ResetAllRateLimits();
            _logger.LogInformation("Tüm rate limitler başarıyla sıfırlandı");
            
            return Ok(new { message = "Tüm rate limitler başarıyla sıfırlandı" });
        }
    }

    public class ResetRateLimitRequest
    {
        public string IpAddress { get; set; }
        public string EndpointPath { get; set; }
    }
} 