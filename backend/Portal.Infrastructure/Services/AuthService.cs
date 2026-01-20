using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Portal.Core.DTOs;
using Portal.Core.Entities;
using Portal.Core.Interfaces;
using Portal.Infrastructure.Data;

namespace Portal.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly BigQueryRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly string _passwordPepper;
    private readonly string _verificationUrl;

    // Departman -> Rol eslestirmesi
    private static readonly Dictionary<string, int> DepartmentRoleMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "İnsan Kaynakları", 2 },    // IK_KULLANICI
        { "Insan Kaynaklari", 2 },    // IK_KULLANICI (Turkish chars)
        { "Finans", 4 },              // BUTCE_KULLANICI
        { "Muhasebe", 4 },            // BUTCE_KULLANICI
        { "Üst Yönetim", 7 },         // UST_YONETICI
        { "Ust Yonetim", 7 },         // UST_YONETICI (Turkish chars)
        { "Genel Müdürlük", 7 },      // UST_YONETICI
        { "Genel Mudurluk", 7 },      // UST_YONETICI
    };

    public AuthService(
        BigQueryRepository repository,
        IConfiguration configuration,
        IEmailService emailService)
    {
        _repository = repository;
        _configuration = configuration;
        _emailService = emailService;

        _passwordPepper = Environment.GetEnvironmentVariable("PASSWORD_PEPPER")
            ?? _configuration["Security:PasswordPepper"]
            ?? "DEFAULT_PEPPER_CHANGE_IN_PRODUCTION";

        _verificationUrl = Environment.GetEnvironmentVariable("EMAIL_VERIFICATION_URL")
            ?? _configuration["Email:VerificationUrl"]
            ?? "https://portal.konyalisaat.com.tr/verify-email";

        if (_passwordPepper == "DEFAULT_PEPPER_CHANGE_IN_PRODUCTION" ||
            _passwordPepper.Contains("DEV_ONLY"))
        {
            Console.WriteLine("WARNING: Using default PASSWORD_PEPPER. Set PASSWORD_PEPPER env var in production!");
        }
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, string? ipAddress)
    {
        var user = await _repository.GetUserByUsernameOnlyAsync(request.Username);

        if (user == null)
            return null;

        if (!VerifyPassword(request.Password, user.PasswordHash))
            return null;

        await _repository.UpdateUserLastLoginAsync(user.Username, user.Client);

        var permissions = await _repository.GetUserPermissionsAsync(user.Username, user.Client);
        var roles = await _repository.GetUserRolesAsync(user.Username, user.Client);

        var (accessToken, expiration) = GenerateAccessToken(user, permissions, roles);
        var refreshToken = await GenerateRefreshTokenAsync(user.Username, user.Client, ipAddress);

        return new AuthResponse(
            0,
            user.Username,
            user.Email,
            user.FirstName,
            user.LastName,
            accessToken,
            refreshToken,
            expiration,
            permissions,
            roles
        );
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, string? ipAddress)
    {
        var exists = await _repository.UserExistsAsync(request.Username, request.Email);
        if (exists)
            return null;

        var passwordHash = HashPassword(request.Password);

        var user = await _repository.CreateUserAsync(
            username: request.Username,
            passwordHash: passwordHash,
            firstName: request.FirstName,
            lastName: request.LastName,
            email: request.Email,
            client: "00"
        );

        // Email pattern'e gore organizasyon kuralini bul ve varsayilan rol ata
        await AssignDefaultRoleByEmailAsync(user.Username, request.Email);

        var permissions = await _repository.GetUserPermissionsAsync(user.Username, user.Client);
        var roles = await _repository.GetUserRolesAsync(user.Username, user.Client);

        var (accessToken, expiration) = GenerateAccessToken(user, permissions, roles);
        var refreshToken = await GenerateRefreshTokenAsync(user.Username, user.Client, ipAddress);

        return new AuthResponse(
            0,
            user.Username,
            user.Email,
            user.FirstName,
            user.LastName,
            accessToken,
            refreshToken,
            expiration,
            permissions,
            roles
        );
    }

    /// <summary>
    /// Email uygunlugunu kontrol eder (organizasyon sorgusunda var mi veya @konyalisaat.com.tr mi)
    /// </summary>
    public async Task<EmailLookupResponse> CheckEmailEligibilityAsync(string email)
    {
        // Organizasyon sorgusundan calisan bilgisi ara
        var employee = await _repository.GetEmployeeByEmailAsync(email);

        if (employee != null)
        {
            return new EmailLookupResponse(
                Found: true,
                FirstName: employee.Name,
                LastName: employee.Surname,
                Department: employee.Department,
                Position: employee.Position,
                Title: employee.Title,
                Phone: employee.Phone
            );
        }

        // @konyalisaat.com.tr ile biten emaillere izin ver
        if (email.EndsWith("@konyalisaat.com.tr", StringComparison.OrdinalIgnoreCase))
        {
            return new EmailLookupResponse(
                Found: true,
                FirstName: null,
                LastName: null,
                Department: null,
                Position: null,
                Title: null,
                Phone: null
            );
        }

        return new EmailLookupResponse(
            Found: false,
            FirstName: null,
            LastName: null,
            Department: null,
            Position: null,
            Title: null,
            Phone: null
        );
    }

    /// <summary>
    /// Email dogrulama ile kayit islemi
    /// </summary>
    public async Task<RegisterResponse> RegisterWithVerificationAsync(RegisterRequest request, string? ipAddress)
    {
        // Email uygunlugunu kontrol et (PersonelListesi veya @konyalisaat.com.tr)
        EmailLookupResponse? eligibility = null;
        try
        {
            eligibility = await CheckEmailEligibilityAsync(request.Email);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Email eligibility check failed: {ex.Message}");
            // PersonelListesi tablosu yoksa veya hata olursa, @konyalisaat.com.tr kontrolu yap
            if (request.Email.EndsWith("@konyalisaat.com.tr", StringComparison.OrdinalIgnoreCase))
            {
                eligibility = new EmailLookupResponse(true, null, null, null, null, null, null);
            }
        }

        // Email kontrolu - simdilik sadece domain kontrolu (PersonelListesi bossa bile calissin)
        if (eligibility == null || !eligibility.Found)
        {
            // Fallback: @konyalisaat.com.tr ile biten emaillere izin ver
            if (!request.Email.EndsWith("@konyalisaat.com.tr", StringComparison.OrdinalIgnoreCase))
            {
                return new RegisterResponse(
                    Success: false,
                    Message: "Bu email adresi ile kayit olamazsiniz. Sadece kurum email adresleri (@konyalisaat.com.tr) kabul edilmektedir.",
                    RequiresEmailVerification: false,
                    Username: null,
                    Email: null,
                    FirstName: null,
                    LastName: null
                );
            }
            eligibility = new EmailLookupResponse(true, null, null, null, null, null, null);
        }

        // Kullanici zaten var mi kontrol et
        bool exists = false;
        try
        {
            exists = await _repository.UserExistsIncludingPendingAsync(request.Username, request.Email);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: User exists check failed: {ex.Message}");
            // Tablo yoksa devam et
        }

        if (exists)
        {
            return new RegisterResponse(
                Success: false,
                Message: "Bu kullanici adi veya email adresi zaten kayitli.",
                RequiresEmailVerification: false,
                Username: null,
                Email: null,
                FirstName: null,
                LastName: null
            );
        }

        // Ad/soyad organizasyon sorgusundan doldur (eger varsa)
        var firstName = !string.IsNullOrEmpty(eligibility?.FirstName) ? eligibility.FirstName : request.FirstName;
        var lastName = !string.IsNullOrEmpty(eligibility?.LastName) ? eligibility.LastName : request.LastName;

        var passwordHash = HashPassword(request.Password);

        // Kullaniciyi olustur
        User user;
        bool emailVerificationEnabled = true;

        try
        {
            // Oncelikle dogrulama bekler durumda olusturmayi dene
            user = await _repository.CreateUserPendingVerificationAsync(
                username: request.Username,
                passwordHash: passwordHash,
                firstName: firstName,
                lastName: lastName,
                email: request.Email,
                phone: eligibility?.Phone,
                client: "00"
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: CreateUserPendingVerificationAsync failed: {ex.Message}");
            // Fallback: Normal kullanici olustur (isblocked = 0)
            try
            {
                user = await _repository.CreateUserAsync(
                    username: request.Username,
                    passwordHash: passwordHash,
                    firstName: firstName,
                    lastName: lastName,
                    email: request.Email,
                    phone: eligibility?.Phone,
                    client: "00"
                );
                emailVerificationEnabled = false; // Email dogrulama atla
            }
            catch (Exception ex2)
            {
                Console.WriteLine($"Error: CreateUserAsync also failed: {ex2.Message}");
                return new RegisterResponse(
                    Success: false,
                    Message: $"Kullanici olusturulamadi: {ex2.Message}",
                    RequiresEmailVerification: false,
                    Username: null,
                    Email: null,
                    FirstName: null,
                    LastName: null
                );
            }
        }

        // Dogrulama tokeni olustur (eger email dogrulama aktifse)
        string? token = null;
        if (emailVerificationEnabled)
        {
            try
            {
                token = GenerateVerificationToken();
                var expiresAt = DateTime.UtcNow.AddHours(24);
                await _repository.CreateEmailVerificationTokenAsync(request.Username, request.Email, token, expiresAt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create verification token: {ex.Message}");
                emailVerificationEnabled = false;
            }
        }

        // Dogrulama emaili gonder
        if (emailVerificationEnabled && token != null)
        {
            try
            {
                await _emailService.SendVerificationEmailAsync(request.Email, firstName, token, _verificationUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not send verification email: {ex.Message}");
                // Email gonderimi basarisiz olsa bile kayit devam etsin
            }
        }

        // Email dogrulama devre disiysa, rol atamasini simdi yap
        if (!emailVerificationEnabled)
        {
            try
            {
                await AssignRoleByDepartmentAsync(user.Username, request.Email);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not assign role: {ex.Message}");
            }
        }

        var message = emailVerificationEnabled
            ? "Kayit basarili! Email adresinize gonderilen dogrulama linkine tiklayarak hesabinizi aktif edin."
            : "Kayit basarili! Giris yapabilirsiniz.";

        return new RegisterResponse(
            Success: true,
            Message: message,
            RequiresEmailVerification: emailVerificationEnabled,
            Username: user.Username,
            Email: user.Email,
            FirstName: firstName,
            LastName: lastName
        );
    }

    /// <summary>
    /// Email dogrulama tokenini dogrula ve hesabi aktif et
    /// </summary>
    public async Task<VerifyEmailResponse> VerifyEmailAsync(string token, string? ipAddress)
    {
        var verificationToken = await _repository.GetEmailVerificationTokenAsync(token);

        if (verificationToken == null)
        {
            return new VerifyEmailResponse(
                Success: false,
                Message: "Gecersiz dogrulama linki.",
                AuthData: null
            );
        }

        if (verificationToken.IsUsed)
        {
            return new VerifyEmailResponse(
                Success: false,
                Message: "Bu dogrulama linki daha once kullanilmis.",
                AuthData: null
            );
        }

        if (verificationToken.ExpiresAt < DateTime.UtcNow)
        {
            return new VerifyEmailResponse(
                Success: false,
                Message: "Dogrulama linkinin suresi dolmus. Lutfen yeni bir link talep edin.",
                AuthData: null
            );
        }

        // Tokeni kullanildi olarak isaretle
        await _repository.MarkEmailVerificationTokenUsedAsync(token);

        // Kullaniciyi aktif et
        await _repository.VerifyUserEmailAsync(verificationToken.Username, "00");

        // Departmana gore rol ata
        await AssignRoleByDepartmentAsync(verificationToken.Username, verificationToken.Email);

        // Kullanici bilgilerini getir ve token olustur
        var user = await _repository.GetUserByUsernameOnlyAsync(verificationToken.Username);
        if (user == null)
        {
            return new VerifyEmailResponse(
                Success: false,
                Message: "Kullanici bulunamadi.",
                AuthData: null
            );
        }

        var permissions = await _repository.GetUserPermissionsAsync(user.Username, user.Client);
        var roles = await _repository.GetUserRolesAsync(user.Username, user.Client);

        var (accessToken, expiration) = GenerateAccessToken(user, permissions, roles);
        var refreshToken = await GenerateRefreshTokenAsync(user.Username, user.Client, ipAddress);

        return new VerifyEmailResponse(
            Success: true,
            Message: "Email adresiniz dogrulandi! Hesabiniz aktif edildi.",
            AuthData: new AuthResponse(
                0,
                user.Username,
                user.Email,
                user.FirstName,
                user.LastName,
                accessToken,
                refreshToken,
                expiration,
                permissions,
                roles
            )
        );
    }

    /// <summary>
    /// Dogrulama emailini tekrar gonder
    /// </summary>
    public async Task<bool> ResendVerificationEmailAsync(string email)
    {
        // Email ile kullanici bul
        var eligibility = await CheckEmailEligibilityAsync(email);
        if (!eligibility.Found)
        {
            return false;
        }

        // Yeni token olustur
        var token = GenerateVerificationToken();
        var expiresAt = DateTime.UtcNow.AddHours(24);

        // Kullanici adini bulmak icin email ile arama yap
        // (Bu kisim gelistirilebilir - simdiilik email'i username olarak kabul ediyoruz)
        var firstName = eligibility.FirstName ?? "Kullanici";

        try
        {
            await _emailService.SendVerificationEmailAsync(email, firstName, token, _verificationUrl);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Departmana gore rol ata (Insan Kaynaklari, Finans, Ust Yonetim)
    /// </summary>
    private async Task AssignRoleByDepartmentAsync(string username, string email)
    {
        try
        {
            // Organizasyon sorgusundan departman bilgisi al
            var employee = await _repository.GetEmployeeByEmailAsync(email);

            if (employee != null && !string.IsNullOrEmpty(employee.Department))
            {
                // Departmana gore rol belirle
                if (DepartmentRoleMap.TryGetValue(employee.Department, out var roleId))
                {
                    await _repository.AssignRoleToUserAsync(username, roleId);
                    Console.WriteLine($"User {username} assigned role {roleId} based on department {employee.Department}");
                    return;
                }
            }

            // Eslesen departman yoksa varsayilan CALISAN rolu ata
            await _repository.AssignRoleToUserAsync(username, 1); // CALISAN
            Console.WriteLine($"User {username} assigned default CALISAN role (department: {employee?.Department ?? "unknown"})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not assign role to user {username}: {ex.Message}");
            // Varsayilan CALISAN rolu ata
            try
            {
                await _repository.AssignRoleToUserAsync(username, 1);
            }
            catch { }
        }
    }

    /// <summary>
    /// Email adresine gore organizasyon kuralini bulur ve varsayilan rol atar (eski metod - uyumluluk icin)
    /// </summary>
    private async Task AssignDefaultRoleByEmailAsync(string username, string email)
    {
        await AssignRoleByDepartmentAsync(username, email);
    }

    /// <summary>
    /// Dogrulama tokeni olustur
    /// </summary>
    private static string GenerateVerificationToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    public async Task<AuthResponse?> RefreshTokenAsync(string refreshToken, string? ipAddress)
    {
        var token = await _repository.GetRefreshTokenAsync(refreshToken);

        if (token == null || !token.IsActive)
            return null;

        var user = token.User;
        var permissions = await _repository.GetUserPermissionsAsync(user.Username, user.Client);
        var roles = await _repository.GetUserRolesAsync(user.Username, user.Client);

        var (accessToken, expiration) = GenerateAccessToken(user, permissions, roles);
        var newRefreshToken = await GenerateRefreshTokenAsync(user.Username, user.Client, ipAddress);

        await _repository.RevokeRefreshTokenAsync(refreshToken, ipAddress, newRefreshToken);

        return new AuthResponse(
            0,
            user.Username,
            user.Email,
            user.FirstName,
            user.LastName,
            accessToken,
            newRefreshToken,
            expiration,
            permissions,
            roles
        );
    }

    public async Task RevokeTokenAsync(string refreshToken, string? ipAddress)
    {
        var token = await _repository.GetRefreshTokenAsync(refreshToken);

        if (token != null && token.IsActive)
        {
            await _repository.RevokeRefreshTokenAsync(refreshToken, ipAddress);
        }
    }

    public async Task<UserInfoResponse?> GetUserInfoAsync(int userId)
    {
        await Task.CompletedTask;
        return null;
    }

    public async Task<UserInfoResponse?> GetUserInfoByUsernameAsync(string username)
    {
        var user = await _repository.GetUserByUsernameOnlyAsync(username);
        if (user == null) return null;

        var permissions = await _repository.GetUserPermissionsAsync(user.Username, user.Client);
        var roles = await _repository.GetUserRolesAsync(user.Username, user.Client);

        var modules = new List<ModuleInfo>();

        return new UserInfoResponse(
            0,
            user.Username,
            user.Email,
            user.FirstName,
            user.LastName,
            permissions,
            roles,
            modules
        );
    }

    private (string token, DateTime expiration) GenerateAccessToken(User user, List<string> permissions, List<string> roles)
    {
        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? _configuration["JwtSettings:Secret"]!;
        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
            ?? _configuration["JwtSettings:Issuer"]!;
        var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
            ?? _configuration["JwtSettings:Audience"]!;
        var expirationMinutes = int.Parse(
            Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES")
            ?? _configuration["JwtSettings:AccessTokenExpirationMinutes"]
            ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email ?? ""),
            new("firstName", user.FirstName),
            new("lastName", user.LastName),
            new("client", user.Client)
        };

        // Add permissions as claims
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        // Add roles as claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var expiration = DateTime.UtcNow.AddMinutes(expirationMinutes);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expiration,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiration);
    }

    private async Task<string> GenerateRefreshTokenAsync(string username, string client, string? ipAddress)
    {
        var expirationDays = int.Parse(
            Environment.GetEnvironmentVariable("JWT_REFRESH_EXPIRATION_DAYS")
            ?? _configuration["JwtSettings:RefreshTokenExpirationDays"]
            ?? "7");

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expiresAt = DateTime.UtcNow.AddDays(expirationDays);

        await _repository.CreateRefreshTokenAsync(username, client, token, expiresAt, ipAddress);

        return token;
    }

    private bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
            return false;

        if (storedHash.StartsWith("$2"))
        {
            return VerifyBcryptPassword(password, storedHash);
        }

        if (storedHash.Length == 64 && IsHexString(storedHash))
        {
            return VerifySha256Password(password, storedHash);
        }

        return false;
    }

    private bool VerifyBcryptPassword(string password, string storedHash)
    {
        try
        {
            var pepperedPassword = password + _passwordPepper;
            return BCrypt.Net.BCrypt.Verify(pepperedPassword, storedHash);
        }
        catch
        {
            return false;
        }
    }

    public string HashPassword(string password)
    {
        var pepperedPassword = password + _passwordPepper;
        return BCrypt.Net.BCrypt.HashPassword(pepperedPassword, workFactor: 12);
    }

    private static bool VerifySha256Password(string password, string storedHash)
    {
        var inputHash = ComputeSha256Hash(password);
        return string.Equals(inputHash, storedHash, StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }

    private static bool IsHexString(string str)
    {
        foreach (var c in str)
        {
            if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                return false;
        }
        return true;
    }

    // ===== ŞİFRE İŞLEMLERİ =====

    /// <summary>
    /// Giriş yapmış kullanıcının şifresini değiştirir
    /// </summary>
    public async Task<ChangePasswordResponse> ChangePasswordAsync(string username, ChangePasswordRequest request)
    {
        try
        {
            // Kullanıcıyı getir
            var user = await _repository.GetUserByUsernameOnlyAsync(username);
            if (user == null)
            {
                return new ChangePasswordResponse(false, "Kullanici bulunamadi.");
            }

            // Mevcut şifreyi doğrula
            if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                return new ChangePasswordResponse(false, "Mevcut sifre hatali.");
            }

            // Yeni şifre validasyonu
            if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 6)
            {
                return new ChangePasswordResponse(false, "Yeni sifre en az 6 karakter olmalidir.");
            }

            // Yeni şifreyi hashle ve kaydet
            var newPasswordHash = HashPassword(request.NewPassword);
            await _repository.UpdateUserPasswordAsync(username, user.Client, newPasswordHash);

            return new ChangePasswordResponse(true, "Sifreniz basariyla degistirildi.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error changing password: {ex.Message}");
            return new ChangePasswordResponse(false, "Sifre degistirilirken bir hata olustu.");
        }
    }

    /// <summary>
    /// Şifremi unuttum - email ile sıfırlama linki gönderir
    /// </summary>
    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        try
        {
            // Email ile kullanıcı bul
            var user = await _repository.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                // Güvenlik için kullanıcı bulunamasa bile başarılı mesajı dön
                return new ForgotPasswordResponse(true, "Eger bu email adresi sistemde kayitliysa, sifre sifirlama linki gonderildi.");
            }

            // Önceki kullanılmamış tokenleri iptal et
            await _repository.InvalidateUserPasswordResetTokensAsync(user.Username);

            // Yeni token oluştur
            var token = GenerateVerificationToken();
            var expiresAt = DateTime.UtcNow.AddHours(1); // 1 saat geçerli

            await _repository.CreatePasswordResetTokenAsync(user.Username, user.Email, token, expiresAt);

            // Şifre sıfırlama emaili gönder
            var resetUrl = _verificationUrl.Replace("verify-email", "reset-password");
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.FirstName, token, resetUrl);

            return new ForgotPasswordResponse(true, "Sifre sifirlama linki email adresinize gonderildi.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in forgot password: {ex.Message}");
            return new ForgotPasswordResponse(false, "Sifre sifirlama islemi sirasinda bir hata olustu.");
        }
    }

    /// <summary>
    /// Şifre sıfırlama tokenı ile yeni şifre belirleme
    /// </summary>
    public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        try
        {
            // Tokeni doğrula
            var resetToken = await _repository.GetPasswordResetTokenAsync(request.Token);

            if (resetToken == null)
            {
                return new ResetPasswordResponse(false, "Gecersiz sifre sifirlama linki.");
            }

            if (resetToken.IsUsed)
            {
                return new ResetPasswordResponse(false, "Bu sifre sifirlama linki daha once kullanilmis.");
            }

            if (resetToken.ExpiresAt < DateTime.UtcNow)
            {
                return new ResetPasswordResponse(false, "Sifre sifirlama linkinin suresi dolmus. Lutfen yeni bir link talep edin.");
            }

            // Yeni şifre validasyonu
            if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 6)
            {
                return new ResetPasswordResponse(false, "Yeni sifre en az 6 karakter olmalidir.");
            }

            // Kullanıcıyı getir
            var user = await _repository.GetUserByUsernameOnlyAsync(resetToken.Username);
            if (user == null)
            {
                return new ResetPasswordResponse(false, "Kullanici bulunamadi.");
            }

            // Tokeni kullanıldı olarak işaretle
            await _repository.MarkPasswordResetTokenUsedAsync(request.Token);

            // Yeni şifreyi hashle ve kaydet
            var newPasswordHash = HashPassword(request.NewPassword);
            await _repository.UpdateUserPasswordAsync(user.Username, user.Client, newPasswordHash);

            return new ResetPasswordResponse(true, "Sifreniz basariyla sifirlandi. Giris yapabilirsiniz.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error resetting password: {ex.Message}");
            return new ResetPasswordResponse(false, "Sifre sifirlama sirasinda bir hata olustu.");
        }
    }
}
