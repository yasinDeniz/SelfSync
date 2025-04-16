# SelfSync API

Bu API, uzak klasörleri ZIP olarak indirme ve klasörlerin son değiştirilme tarihlerini kontrol etme işlevselliği sağlar.

## Kurulum

1. Projeyi klonlayın:
```bash
git clone https://github.com/yasinDeniz/SelfSync.git
cd SelfSync
```

2. Bağımlılıkları yükleyin:
```bash
dotnet restore
```

3. Geliştirme ortamı için yapılandırma:
```bash
# Appsettings.Development.json dosyası oluşturun (bu dosya .gitignore'a eklenmiştir)
cp appsettings.Example.json appsettings.Development.json
```

4. Güvenlik için API anahtarını yapılandırın (aşağıdaki yöntemlerden birini seçin):

### A) User Secrets Kullanımı (Geliştirme Ortamı)

```bash
dotnet user-secrets init
dotnet user-secrets set "AdminSettings:ApiKey" "sizin_gizli_anahtariniz"
```

### B) Ortam Değişkeni Kullanımı (Üretim Ortamı)

Linux/macOS:
```bash
export SELFSYNC_ADMIN_API_KEY="sizin_gizli_anahtariniz"
```

Windows CMD:
```cmd
set SELFSYNC_ADMIN_API_KEY=sizin_gizli_anahtariniz
```

Windows PowerShell:
```powershell
$env:SELFSYNC_ADMIN_API_KEY="sizin_gizli_anahtariniz"
```

### C) appsettings.json (Önerilmez, .gitignore'a eklenmiştir)

```json
{
  "AdminSettings": {
    "ApiKey": "sizin_gizli_anahtariniz"
  }
}
```

## Kullanım

API temel olarak iki işlev sunar:

### 1. Klasörleri ZIP olarak indirme

Endpoint: `GET /Projects/{path}`

Örnek:
```
GET /Projects/netileti.queries
```

Not: Path parametresindeki "." karakterleri "/" ile değiştirilir. Örneğin, "netileti.queries" ifadesi "netileti/queries" klasörüne erişir.

### 2. Klasörün Son Değiştirilme Tarihini Alma

Endpoint: `GET /Projects/lastmodified/{path}`

Örnek:
```
GET /Projects/lastmodified/test.queries
```

### 3. Rate Limit Yönetimi (Sadece Yöneticiler)

Rate limiti sıfırlama (belirli bir IP ve endpoint için):
```
GET /Admin/reset-rate-limit?ipAddress=192.168.1.1&endpointPath=/Projects/test.queries
X-API-Key: sizin_gizli_anahtariniz
```

Tüm rate limitleri sıfırlama:
```
GET /Admin/reset-all-rate-limits
X-API-Key: sizin_gizli_anahtariniz
```

## Rate Limiting

Her IP adresi, saat başına her endpoint için maksimum 10 istek yapabilir. Bu limit aşıldığında, API 429 Too Many Requests yanıtı döndürür.

## Güvenlik

- API anahtarı GIT'e eklenmemelidir
- Üretim ortamında, ortam değişkenleri veya güvenli bir yapılandırma yönetimi kullanın
- API anahtarı sadece yetkili personel tarafından bilinmeli ve kullanılmalıdır 