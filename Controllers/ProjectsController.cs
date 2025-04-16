using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Text.RegularExpressions;
using SelfSync.Attributes;

namespace SelfSync.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly ILogger<ProjectsController> _logger;
        private readonly IWebHostEnvironment _environment;

        public ProjectsController(ILogger<ProjectsController> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        [HttpGet("{path}")]
        [RequestRateLimit(maxRequests: 10, timeWindowInHours: 1)]
        public IActionResult DownloadFolder(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return BadRequest("Lütfen geçerli bir klasör yolu belirtin");
            }

            // "." karakterini "/" ile değiştir
            string normalizedPath = path.Replace('.', '/');
            
            // Yolu normalize et (büyük/küçük harf duyarlılığını kaldır)
            normalizedPath = normalizedPath.ToLower();
            normalizedPath = normalizedPath.Replace('\\', '/');

            // Projeler klasörünün tam yolunu al
            string projectsBasePath = Path.Combine(_environment.ContentRootPath, "Projects");
            string folderPath = Path.Combine(projectsBasePath, normalizedPath);

            // Klasörün var olup olmadığını kontrol et
            if (!Directory.Exists(folderPath))
            {
                return NotFound($"'{normalizedPath}' klasörü bulunamadı");
            }

            // ZIP dosyası için geçici bir ad oluştur
            string zipFileName = $"{Path.GetFileName(folderPath)}.zip";
            string tempZipPath = Path.Combine(Path.GetTempPath(), zipFileName);

            try
            {
                // Eğer dosya önceden varsa sil
                if (System.IO.File.Exists(tempZipPath))
                {
                    System.IO.File.Delete(tempZipPath);
                }

                // Klasörü ZIP olarak sıkıştır
                ZipFile.CreateFromDirectory(folderPath, tempZipPath);

                // ZIP dosyasını istemciye gönder
                byte[] fileBytes = System.IO.File.ReadAllBytes(tempZipPath);
                return File(fileBytes, "application/zip", zipFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Klasörü ZIP olarak indirirken hata oluştu: {Path}", normalizedPath);
                return StatusCode(500, "Klasörü indirirken bir hata oluştu");
            }
            finally
            {
                // Geçici ZIP dosyasını temizle
                if (System.IO.File.Exists(tempZipPath))
                {
                    try
                    {
                        System.IO.File.Delete(tempZipPath);
                    }
                    catch
                    {
                        // Silme başarısız olsa bile devam et
                    }
                }
            }
        }

        [HttpGet("lastmodified/{path}")]
        [RequestRateLimit(maxRequests: 10, timeWindowInHours: 1)]
        public IActionResult GetLastModifiedDate(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return BadRequest("Lütfen geçerli bir klasör yolu belirtin");
            }

            // "." karakterini "/" ile değiştir
            string normalizedPath = path.Replace('.', '/');
            
            // Yolu normalize et (büyük/küçük harf duyarlılığını kaldır)
            normalizedPath = normalizedPath.ToLower();
            normalizedPath = normalizedPath.Replace('\\', '/');

            // Projeler klasörünün tam yolunu al
            string projectsBasePath = Path.Combine(_environment.ContentRootPath, "Projects");
            string folderPath = Path.Combine(projectsBasePath, normalizedPath);

            // Klasörün var olup olmadığını kontrol et
            if (!Directory.Exists(folderPath))
            {
                return NotFound($"'{normalizedPath}' klasörü bulunamadı");
            }

            try
            {
                // Klasörün son değiştirilme tarihini al
                DirectoryInfo dirInfo = new DirectoryInfo(folderPath);
                DateTime lastModified = dirInfo.LastWriteTime;

                // İçerideki dosya ve klasörlerin en son değiştirilme tarihini kontrol et
                foreach (var file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime > lastModified)
                    {
                        lastModified = fileInfo.LastWriteTime;
                    }
                }

                foreach (var dir in Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories))
                {
                    var subDirInfo = new DirectoryInfo(dir);
                    if (subDirInfo.LastWriteTime > lastModified)
                    {
                        lastModified = subDirInfo.LastWriteTime;
                    }
                }

                // Sonucu JSON olarak döndür
                var result = new 
                {
                    path = normalizedPath,
                    lastModified = lastModified,
                    lastModifiedUtc = lastModified.ToUniversalTime(),
                    formattedDate = lastModified.ToString("yyyy-MM-dd HH:mm:ss")
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Klasörün son değiştirilme tarihini alırken hata oluştu: {Path}", normalizedPath);
                return StatusCode(500, "Klasörün bilgilerini alırken bir hata oluştu");
            }
        }
    }
} 