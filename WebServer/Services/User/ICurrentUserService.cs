using System.Security.Claims;

namespace WebServer.Services.User
{
    public interface ICurrentUserService
    {
        /// <summary>
        /// Gets the current user's ID from the ClaimsPrincipal
        /// </summary>
        string? GetUserId(ClaimsPrincipal user);

        /// <summary>
        /// Gets the current user's name from the ClaimsPrincipal
        /// </summary>
        string? GetUserName(ClaimsPrincipal user);

        /// <summary>
        /// Gets the current user's roles
        /// </summary>
        Task<IList<string>> GetUserRolesAsync(ClaimsPrincipal user);

        /// <summary>
        /// Checks if user is in admin or manager role
        /// </summary>
        Task<bool> IsAdminOrManagerAsync(ClaimsPrincipal user);

        /// <summary>
        /// Checks if user is admin
        /// </summary>
        Task<bool> IsAdminAsync(ClaimsPrincipal user);
    }
}
