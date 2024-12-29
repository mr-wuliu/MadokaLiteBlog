using Microsoft.AspNetCore.Mvc;

public abstract class BaseController : ControllerBase
{
    protected string? CurrentUser => User.Identity?.Name;
    
    protected bool TryGetCurrentUser(out string username)
    {
        username = User.Identity?.Name ?? string.Empty;
        return !string.IsNullOrEmpty(username);
    }
}