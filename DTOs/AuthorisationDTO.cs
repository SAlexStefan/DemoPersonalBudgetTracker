using System.ComponentModel.DataAnnotations;

namespace PersonalBudgetTrackerApi.DTOs;

public record AuthorisationResponse(string Token, string Email, string CsrfToken);
public record RegistrationRequest(
    [Required, EmailAddress, MaxLength(255)] string Email,
    [Required, MinLength(8), MaxLength(72)] string Password);
public record LoginRequest(string Email, string Password);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, [Required, MinLength(8), MaxLength(72)] string NewPassword);



