using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PersonalBudgetTrackerApi;

public class ValidateCsrfAttribute : ActionFilterAttribute
{
    private const string CookieName = "XSRF-TOKEN";
    private const string HeaderName = "X-XSRF-TOKEN";

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var cookieValue = context.HttpContext.Request.Cookies[CookieName];
        var headerValue = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (string.IsNullOrEmpty(cookieValue) || string.IsNullOrEmpty(headerValue) || !FixedTimeEquals(cookieValue, headerValue))
        {
            context.Result = new ObjectResult("Missing or invalid CSRF token.")
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        base.OnActionExecuting(context);
    }

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
