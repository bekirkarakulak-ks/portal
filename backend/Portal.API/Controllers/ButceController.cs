using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.API.Attributes;
using Portal.API.Extensions;

namespace Portal.API.Controllers;

[ApiController]
[Route("api/butce")]
[Authorize]
public class ButceController : ControllerBase
{
    [HttpGet("me")]
    [RequirePermission("BUTCE.Kendi.Goruntule")]
    public IActionResult GetMyButce([FromQuery] int yil)
    {
        var userId = User.GetUserId();
        var username = User.GetUsername();
        var aktifYil = yil > 0 ? yil : DateTime.Now.Year;

        var butce = new
        {
            UserId = userId,
            Username = username,
            Yil = aktifYil,
            ToplamButce = 100000m,
            KullanilanButce = 45000m,
            KalanButce = 55000m,
            Harcamalar = new[]
            {
                new { Id = 1, Kategori = "Egitim", Tutar = 15000m, Tarih = "2024-02-15", Aciklama = "Online kurs" },
                new { Id = 2, Kategori = "Seyahat", Tutar = 20000m, Tarih = "2024-03-10", Aciklama = "Konferans katilimi" },
                new { Id = 3, Kategori = "Ekipman", Tutar = 10000m, Tarih = "2024-04-01", Aciklama = "Laptop yenileme" }
            }
        };

        return Ok(butce);
    }

    [HttpGet("departman/{departmanId}")]
    [RequirePermission("BUTCE.Tum.Goruntule")]
    public IActionResult GetDepartmanButce(int departmanId, [FromQuery] int yil)
    {
        var aktifYil = yil > 0 ? yil : DateTime.Now.Year;

        var butce = new
        {
            DepartmanId = departmanId,
            DepartmanAdi = "Yazilim Gelistirme",
            Yil = aktifYil,
            ToplamButce = 500000m,
            KullanilanButce = 320000m,
            KalanButce = 180000m,
            CalisanButceleri = new[]
            {
                new { CalisanId = 1, AdSoyad = "Ahmet Yilmaz", ToplamButce = 100000m, Kullanilan = 45000m },
                new { CalisanId = 2, AdSoyad = "Mehmet Demir", ToplamButce = 80000m, Kullanilan = 60000m },
                new { CalisanId = 3, AdSoyad = "Ayse Kaya", ToplamButce = 120000m, Kullanilan = 95000m }
            }
        };

        return Ok(butce);
    }

    [HttpGet("tum")]
    [RequirePermission("BUTCE.Tum.Goruntule")]
    public IActionResult GetAllButceler([FromQuery] int yil)
    {
        var aktifYil = yil > 0 ? yil : DateTime.Now.Year;

        var butceler = new
        {
            Yil = aktifYil,
            ToplamSirketButcesi = 2000000m,
            ToplamKullanilan = 1200000m,
            Departmanlar = new[]
            {
                new { DepartmanId = 1, DepartmanAdi = "Yazilim Gelistirme", Butce = 500000m, Kullanilan = 320000m },
                new { DepartmanId = 2, DepartmanAdi = "Pazarlama", Butce = 400000m, Kullanilan = 280000m },
                new { DepartmanId = 3, DepartmanAdi = "Insan Kaynaklari", Butce = 200000m, Kullanilan = 150000m }
            }
        };

        return Ok(butceler);
    }

    [HttpPost("harcama")]
    [RequirePermission("BUTCE.Kendi.Goruntule")]
    public IActionResult CreateHarcama([FromBody] HarcamaRequest request)
    {
        var userId = User.GetUserId();

        var yeniHarcama = new
        {
            Id = 100,
            UserId = userId,
            Kategori = request.Kategori,
            Tutar = request.Tutar,
            Aciklama = request.Aciklama,
            Tarih = DateTime.Now,
            Durum = "Bekliyor"
        };

        return Ok(yeniHarcama);
    }

    [HttpPut("guncelle/{butceId}")]
    [RequirePermission("BUTCE.Duzenle")]
    public IActionResult UpdateButce(int butceId, [FromBody] ButceGuncelleRequest request)
    {
        var guncelleyenId = User.GetUserId();

        return Ok(new
        {
            ButceId = butceId,
            YeniTutar = request.YeniTutar,
            GuncelleyenId = guncelleyenId,
            GuncellemeTarihi = DateTime.Now
        });
    }
}

public record HarcamaRequest(string Kategori, decimal Tutar, string Aciklama);
public record ButceGuncelleRequest(decimal YeniTutar, string? Aciklama);
