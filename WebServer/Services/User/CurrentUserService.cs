using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using WebServer.Models.Identity;

namespace WebServer.Services.User
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly UserManager<AppUser> _userManager;

        public CurrentUserService(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public string? GetUserId(ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public string? GetUserName(ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return user.Identity?.Name;
        }

        public async Task<IList<string>> GetUserRolesAsync(ClaimsPrincipal user)
        {
            var userId = GetUserId(user);
            if (string.IsNullOrEmpty(userId))
                return new List<string>();

            var appUser = await _userManager.FindByIdAsync(userId);
            if (appUser == null)
                return new List<string>();

            return await _userManager.GetRolesAsync(appUser);
        }

        public async Task<bool> IsAdminOrManagerAsync(ClaimsPrincipal user)
        {
            var roles = await GetUserRolesAsync(user);
            return roles.Contains("admin") || roles.Contains("manager");
        }

        public async Task<bool> IsAdminAsync(ClaimsPrincipal user)
        {
            var roles = await GetUserRolesAsync(user);
            return roles.Contains("admin");
        }
    }
}
