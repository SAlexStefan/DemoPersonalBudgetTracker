using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace PersonalBudgetTrackerApi.Controllers;

public static class ControllerExtensios
{
    public static Guid GetUserPublicId(this ControllerBase controller)
    {
        var claimId = controller.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (claimId is null || !Guid.TryParse(claimId, out var id))
        {
            throw new UnauthorizedAccessException("User is not authorised");
        }

        return id;
    }
}
