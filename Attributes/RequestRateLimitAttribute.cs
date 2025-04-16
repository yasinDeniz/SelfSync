using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace SelfSync.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RequestRateLimitAttribute : ActionFilterAttribute
    {
        private readonly int _maxRequests;
        private readonly int _timeWindowInHours;
        private static MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

        public RequestRateLimitAttribute(int maxRequests = 10, int timeWindowInHours = 1)
        {
            _maxRequests = maxRequests;
            _timeWindowInHours = timeWindowInHours;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var ipAddress = GetClientIpAddress(context);
            var endpoint = context.HttpContext.Request.Path.ToString();
            var cacheKey = $"{ipAddress}_{endpoint}";

            // Cache'de bu IP için kayıt var mı kontrol et
            if (!_cache.TryGetValue(cacheKey, out RequestData requestData))
            {
                // Yeni kayıt oluştur
                requestData = new RequestData
                {
                    RequestCount = 1,
                    FirstRequestTime = DateTime.UtcNow
                };

                // Cache'e kaydet (1 saatlik süre için)
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(_timeWindowInHours));

                _cache.Set(cacheKey, requestData, cacheEntryOptions);
            }
            else
            {
                // Süre dolmuş mu kontrol et
                if ((DateTime.UtcNow - requestData.FirstRequestTime) > TimeSpan.FromHours(_timeWindowInHours))
                {
                    // Süre dolmuşsa sayaç sıfırla
                    requestData.RequestCount = 1;
                    requestData.FirstRequestTime = DateTime.UtcNow;
                }
                else
                {
                    // Limit aşıldı mı kontrol et
                    if (requestData.RequestCount >= _maxRequests)
                    {
                        // 429 Too Many Requests yanıtı döndür
                        context.Result = new ContentResult
                        {
                            Content = $"Saat başına izin verilen istek sayısını ({_maxRequests}) aştınız. Lütfen {_timeWindowInHours} saat sonra tekrar deneyin.",
                            StatusCode = (int)HttpStatusCode.TooManyRequests
                        };
                        return;
                    }

                    // İstek sayısını artır
                    requestData.RequestCount++;
                }

                // Cache'i güncelle
                _cache.Set(cacheKey, requestData, new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(_timeWindowInHours)));
            }

            base.OnActionExecuting(context);
        }

        private string GetClientIpAddress(ActionExecutingContext context)
        {
            // X-Forwarded-For veya gerçek IP adresini al
            var forwardedIp = context.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedIp))
            {
                return forwardedIp.Split(',')[0].Trim();
            }

            // Gerçek IP adresini al
            return context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        }

        // Rate limit verilerini sıfırlamak için statik yöntem
        public static bool ResetRateLimit(string ipAddress, string endpoint)
        {
            var cacheKey = $"{ipAddress}_{endpoint}";
            if (_cache.TryGetValue(cacheKey, out _))
            {
                _cache.Remove(cacheKey);
                return true;
            }
            return false;
        }

        // Tüm rate limitleri sıfırlamak için statik yöntem
        public static void ResetAllRateLimits()
        {
            // MemoryCache doğrudan tümünü temizlemeyi desteklemez
            // Yeni bir boş cache oluştur
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        private class RequestData
        {
            public int RequestCount { get; set; }
            public DateTime FirstRequestTime { get; set; }
        }
    }
} 