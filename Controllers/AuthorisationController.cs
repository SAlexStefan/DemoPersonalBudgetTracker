using Microsoft.AspNetCore.Mvc;
using PersonalBudgetTrackerApi.DTOs;
using PersonalBudgetTrackerApi.Models;
using PersonalBudgetTrackerApi.Repositories.Interfaces;
using PersonalBudgetTrackerApi.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using PersonalBudgetTrackerApi.Services.Interfaces;
using System.Security.Cryptography;

namespace PersonalBudgetTrackerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthorisationController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordResetRepository _passwordResetRepository;
    private readonly TokenService _tokenService;
    private readonly IEmailSender _emailSender;
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(14);

    private const string RefreshCookieName = "refreshToken";
    private const string CsrfCookieName = "XSRF-TOKEN";
    private const string AuthCookiePath = "/api/Authorisation";
    private const string CsrfCookiePath = "/";

    private readonly string _frontendBaseUrl;


    public AuthorisationController(IUserRepository userRepository, IRefreshTokenRepository refreshTokenRepository, IPasswordResetRepository passwordResetRepository, TokenService tokenService, IEmailSender emailSender, IConfiguration config)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _passwordResetRepository = passwordResetRepository;
        _emailSender = emailSender;
        _frontendBaseUrl = config["Frontend:BaseUrl"]
            ?? throw new InvalidOperationException("Frontend:BaseUrl is not configured");
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register(RegistrationRequest request)
    {
        var normalizeEmail = request.Email.Trim().ToLowerInvariant();
        var existingUser = await _userRepository.GetUserByEmailAsync(normalizeEmail);
        if (existingUser is not null)
        {
            return Conflict("Account already exists");
        }

        var user = new User
        {
            Email = normalizeEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        user = await _userRepository.CreateUserasync(user);

        return Ok(await BuildAuthResponseAsync(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthorisationResponse>> Login(LoginRequest request)
    {
        var normalizeEmail = request.Email.Trim().ToLowerInvariant();

        var existingUser = await _userRepository.GetUserByEmailAsync(normalizeEmail);

        if (existingUser is not null && existingUser.LockedUntil > DateTime.UtcNow)
        {
            return Unauthorized("Account temporarily locked due to repeated failed logins. Try again later.");
        }

        var isUserAnPasswordCorrect = existingUser is not null && BCrypt.Net.BCrypt.Verify(request.Password, existingUser.PasswordHash);

        if (existingUser is not null && !isUserAnPasswordCorrect)
        {
            var attempts = existingUser.FailedLoginAttempts + 1;
            DateTime? lockedUntil = attempts >= 5 ? DateTime.UtcNow.AddMinutes(15) : null;
            await _userRepository.RecordFailedLoginAsync(existingUser.UserId, attempts, lockedUntil);
        }

        if (existingUser is null || !isUserAnPasswordCorrect)
        {
            return Unauthorized("Invalid email or password.");
        }

        await _userRepository.ResetFailedLoginAsync(existingUser.UserId);
        return Ok(await BuildAuthResponseAsync(existingUser));
    }

    [HttpPost("refresh")]
    [ValidateCsrf]
    public async Task<ActionResult<AuthorisationResponse>> RefreshToken()
    {

        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var rawToken) || !TryParseRawToken(rawToken, out var hash))
        {
            return Unauthorized("Invalid refresh token.");
        }

        var existing = await _refreshTokenRepository.GetTokenByHashAsync(hash);
        if (existing is null || !existing.IsActive)
        {
            return Unauthorized("Invalid or expired refresh token.");
        }

        var user = await _userRepository.GetUserByIdAsync(existing.UserId);
        if (user is null)
        {
            return Unauthorized("Invalid refresh token.");
        }

        var response = await BuildAuthResponseAsync(user);

        await _refreshTokenRepository.RevokeTokenAsync(existing.RefreshTokenId, null);

        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var publicUserId = this.GetUserPublicId();
        var userId = await _userRepository.GetUserIdByPublic(publicUserId);
        if (userId is not null)
        {
            await _refreshTokenRepository.RevokeAllTokensForUserAsync((int)userId);
            Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = AuthCookiePath });
            Response.Cookies.Delete(CsrfCookieName, new CookieOptions { Path = CsrfCookiePath });
            Response.Cookies.Delete(CsrfCookieName, new CookieOptions { Path = AuthCookiePath });
        }

        return NoContent();
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetUserByEmailAsync(email);

        if (user is not null)
        {
            var (rawToken, hash) = _tokenService.GenerateRefreshToken();
            await _passwordResetRepository.CreatePasswordResetTokenAsync(new PasswordResetToken
            {
                TokenHash = hash,
                UserId = user.UserId,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });
            var resetLink = $"{_frontendBaseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";
            await _emailSender.SendPasswordResetAsync(user.Email, resetLink);
        }
        return Ok("A reset link has been sent to your email.");
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var hash = _tokenService.HashRefreshToken(request.Token);
        var resetToken = await _passwordResetRepository.GetPasswordResetTokenByHashAsync(hash);
        if (resetToken is null || resetToken.UsedAt is not null || resetToken.ExpiresAt < DateTime.UtcNow)
        {
            return BadRequest("Invalid or expired reset token.");
        }

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepository.UpdatePasswordAsync(resetToken.UserId, newHash);
        await _passwordResetRepository.MarkedUsedPasswordResetTokenAsync(resetToken.PasswordResetTokenId);
        await _refreshTokenRepository.RevokeAllTokensForUserAsync(resetToken.UserId);

        return Ok("Password updated.");
    }
    #region Private methods

    private async Task<AuthorisationResponse> BuildAuthResponseAsync(User user)
    {
        var accessToken = _tokenService.CreateToken(user);
        var csrfToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var (rawRefreshToken, hash) = _tokenService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime);

        await _refreshTokenRepository.CreateTokenAsync(new RefreshToken
        {
            TokenHash = hash,
            UserId = user.UserId,
            ExpiresAt = expiresAt
        });

        Response.Cookies.Append(RefreshCookieName, rawRefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expiresAt,
            Path = AuthCookiePath
        });

        Response.Cookies.Delete(CsrfCookieName, new CookieOptions { Path = AuthCookiePath });
        Response.Cookies.Append(CsrfCookieName, csrfToken, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expiresAt,
            Path = CsrfCookiePath
        });

        return new AuthorisationResponse(accessToken, user.Email, csrfToken);
    }

    private static bool TryParseRawToken(string? raw, out string hash)
    {
        hash = string.Empty;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        try
        {
            var bytes = Convert.FromBase64String(raw);
            hash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(bytes));
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    #endregion

}
