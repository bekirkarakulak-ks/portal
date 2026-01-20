using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.API.Extensions;
using Portal.Core.DTOs;
using Portal.Core.Interfaces;

namespace Portal.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ipAddress = GetIpAddress();
        var result = await _authService.LoginAsync(request, ipAddress);

        if (result == null)
            return Unauthorized(new { message = "Kullanici adi veya sifre hatali" });

        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var ipAddress = GetIpAddress();
        var result = await _authService.RegisterWithVerificationAsync(request, ipAddress);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result);
    }

    /// <summary>
    /// Email adresinin kayit icin uygun olup olmadigini kontrol eder
    /// Organizasyon sorgusundan ad/soyad bilgisi doner
    /// </summary>
    [HttpGet("check-email")]
    public async Task<IActionResult> CheckEmail([FromQuery] string email)
    {
        if (string.IsNullOrEmpty(email))
            return BadRequest(new { message = "Email adresi gerekli" });

        var result = await _authService.CheckEmailEligibilityAsync(email);
        return Ok(result);
    }

    /// <summary>
    /// Email dogrulama tokenini dogrular ve hesabi aktif eder
    /// </summary>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var ipAddress = GetIpAddress();
        var result = await _authService.VerifyEmailAsync(request.Token, ipAddress);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result);
    }

    /// <summary>
    /// Dogrulama emailini tekrar gonderir
    /// </summary>
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        var result = await _authService.ResendVerificationEmailAsync(request.Email);

        if (!result)
            return BadRequest(new { message = "Dogrulama emaili gonderilemedi" });

        return Ok(new { message = "Dogrulama emaili tekrar gonderildi" });
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var ipAddress = GetIpAddress();
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress);

        if (result == null)
            return Unauthorized(new { message = "Gecersiz veya suresi dolmus token" });

        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var ipAddress = GetIpAddress();
        await _authService.RevokeTokenAsync(request.RefreshToken, ipAddress);
        return Ok(new { message = "Basariyla cikis yapildi" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var username = User.GetUsername();
        var result = await _authService.GetUserInfoByUsernameAsync(username);

        if (result == null)
            return NotFound(new { message = "Kullanici bulunamadi" });

        return Ok(result);
    }

    /// <summary>
    /// Giriş yapmış kullanıcının şifresini değiştirir
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var username = User.GetUsername();
        var result = await _authService.ChangePasswordAsync(username, request);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result);
    }

    /// <summary>
    /// Şifremi unuttum - email ile sıfırlama linki gönderir
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
            return BadRequest(new { message = "Email adresi gerekli" });

        var result = await _authService.ForgotPasswordAsync(request);

        // Güvenlik için her zaman başarılı dön
        return Ok(result);
    }

    /// <summary>
    /// Şifre sıfırlama tokenı ile yeni şifre belirleme
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.NewPassword))
            return BadRequest(new { message = "Token ve yeni sifre gerekli" });

        var result = await _authService.ResetPasswordAsync(request);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result);
    }

    private string? GetIpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
            return Request.Headers["X-Forwarded-For"].FirstOrDefault();

        return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
    }
}
