using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.API.Attributes;
using Portal.API.Extensions;

namespace Portal.API.Controllers;

[ApiController]
[Route("api/ik")]
[Authorize]
public class IKController : ControllerBase
{
    // ========== BORDRO ==========

    [HttpGet("bordro/me")]
    [RequirePermission("IK.Bordro.KendiGoruntule")]
    public IActionResult GetMyBordro([FromQuery] int ay, [FromQuery] int yil)
    {
        var userId = User.GetUserId();
        var username = User.GetUsername();

        // Ornek veri - gercek uygulamada veritabanindan gelecek
        var bordro = new
        {
            UserId = userId,
            Username = username,
            Ay = ay > 0 ? ay : DateTime.Now.Month,
            Yil = yil > 0 ? yil : DateTime.Now.Year,
            BrutMaas = 50000m,
            NetMaas = 38500m,
            SgkIsci = 7000m,
            GelirVergisi = 4500m,
            Kesintiler = new[]
            {
                new { Ad = "SGK Isci Payi", Tutar = 7000m },
                new { Ad = "Gelir Vergisi", Tutar = 4500m }
            }
        };

        return Ok(bordro);
    }

    [HttpGet("bordro/calisan/{calisanId}")]
    [RequirePermission("IK.Bordro.TumGoruntule")]
    public IActionResult GetCalisanBordro(int calisanId, [FromQuery] int ay, [FromQuery] int yil)
    {
        // Yonetici yetkisi ile baska calisanin bordrosunu goruntuleme
        var bordro = new
        {
            UserId = calisanId,
            Ay = ay > 0 ? ay : DateTime.Now.Month,
            Yil = yil > 0 ? yil : DateTime.Now.Year,
            BrutMaas = 45000m,
            NetMaas = 34650m
        };

        return Ok(bordro);
    }

    [HttpGet("bordro/tum")]
    [RequirePermission("IK.Bordro.TumGoruntule")]
    public IActionResult GetAllBordrolar([FromQuery] int ay, [FromQuery] int yil)
    {
        // Tum calisanlarin bordro ozeti
        var bordrolar = new[]
        {
            new { CalisanId = 1, AdSoyad = "Ahmet Yilmaz", NetMaas = 38500m },
            new { CalisanId = 2, AdSoyad = "Mehmet Demir", NetMaas = 34650m },
            new { CalisanId = 3, AdSoyad = "Ayse Kaya", NetMaas = 42000m }
        };

        return Ok(bordrolar);
    }

    // ========== IZIN ==========

    [HttpGet("izin/me")]
    [RequirePermission("IK.Izin.KendiGoruntule")]
    public IActionResult GetMyIzinler()
    {
        var userId = User.GetUserId();

        var izinler = new
        {
            UserId = userId,
            YillikIzinHakki = 14,
            KullanilanIzin = 5,
            KalanIzin = 9,
            IzinListesi = new[]
            {
                new { Id = 1, BaslangicTarihi = "2024-03-15", BitisTarihi = "2024-03-17", Gun = 3, Durum = "Onaylandi", Tur = "Yillik Izin" },
                new { Id = 2, BaslangicTarihi = "2024-06-10", BitisTarihi = "2024-06-11", Gun = 2, Durum = "Onaylandi", Tur = "Yillik Izin" }
            }
        };

        return Ok(izinler);
    }

    [HttpGet("izin/tum")]
    [RequirePermission("IK.Izin.TumGoruntule")]
    public IActionResult GetAllIzinler([FromQuery] string? durum)
    {
        var izinler = new[]
        {
            new { Id = 1, CalisanId = 1, AdSoyad = "Ahmet Yilmaz", BaslangicTarihi = "2024-03-15", BitisTarihi = "2024-03-17", Durum = "Onaylandi" },
            new { Id = 2, CalisanId = 2, AdSoyad = "Mehmet Demir", BaslangicTarihi = "2024-04-01", BitisTarihi = "2024-04-05", Durum = "Bekliyor" },
            new { Id = 3, CalisanId = 3, AdSoyad = "Ayse Kaya", BaslangicTarihi = "2024-04-10", BitisTarihi = "2024-04-12", Durum = "Bekliyor" }
        };

        if (!string.IsNullOrEmpty(durum))
        {
            izinler = izinler.Where(i => i.Durum == durum).ToArray();
        }

        return Ok(izinler);
    }

    [HttpPost("izin/talep")]
    [RequirePermission("IK.Izin.KendiGoruntule")]
    public IActionResult CreateIzinTalebi([FromBody] IzinTalebiRequest request)
    {
        var userId = User.GetUserId();

        // Yeni izin talebi olustur
        var yeniTalep = new
        {
            Id = 100,
            UserId = userId,
            BaslangicTarihi = request.BaslangicTarihi,
            BitisTarihi = request.BitisTarihi,
            Tur = request.Tur,
            Aciklama = request.Aciklama,
            Durum = "Bekliyor",
            OlusturmaTarihi = DateTime.Now
        };

        return Ok(yeniTalep);
    }

    [HttpPost("izin/{izinId}/onayla")]
    [RequirePermission("IK.Izin.Onayla")]
    public IActionResult OnaylaIzin(int izinId, [FromBody] IzinOnayRequest request)
    {
        var onaylayanId = User.GetUserId();

        return Ok(new
        {
            IzinId = izinId,
            Durum = request.Onay ? "Onaylandi" : "Reddedildi",
            OnaylayanId = onaylayanId,
            Aciklama = request.Aciklama,
            OnayTarihi = DateTime.Now
        });
    }
}

public record IzinTalebiRequest(
    string BaslangicTarihi,
    string BitisTarihi,
    string Tur,
    string? Aciklama
);

public record IzinOnayRequest(bool Onay, string? Aciklama);
