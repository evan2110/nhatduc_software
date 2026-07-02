using System.Security.Claims;
using NhatDucSoftware.Core.Helpers;
using NhatDucSoftware.Core.Models;

namespace NhatDucSoftware.Web.Services;

public class UserSession
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private AuthenticatedUser? _cachedUser;

    public UserSession(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public AuthenticatedUser? CurrentUser
    {
        get
        {
            if (_cachedUser is not null)
            {
                return _cachedUser;
            }

            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            int? teacherId = null;
            var teacherIdClaim = principal.FindFirst("TeacherId")?.Value;
            if (int.TryParse(teacherIdClaim, out var tid))
            {
                teacherId = tid;
            }

            _cachedUser = new AuthenticatedUser
            {
                Id = userId,
                Username = principal.Identity.Name ?? string.Empty,
                Role = principal.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty,
                TeacherId = teacherId
            };

            return _cachedUser;
        }
    }

    public bool IsAuthenticated => CurrentUser is not null;

    public bool IsAdmin => CurrentUser?.Role == "Admin";

    public bool IsTeacher => CurrentUser?.Role == "Teacher";

    public bool CanDeleteStudent => AdminPermissions.CanDeleteStudent(CurrentUser);

    public bool CanDeleteClass => AdminPermissions.CanDeleteClass(CurrentUser);

    public bool CanRemoveStudentFromClass => AdminPermissions.CanRemoveStudentFromClass(CurrentUser);

    public bool CanTransferClass => AdminPermissions.CanTransferClass(CurrentUser);

    public bool CanDeleteTeacher => AdminPermissions.CanDeleteTeacher(CurrentUser);

    public bool CanManagePaySettings => AdminPermissions.CanManagePaySettings(CurrentUser);

    public bool CanAdjustMonthlyPay => AdminPermissions.CanAdjustMonthlyPay(CurrentUser);

    public bool CanDeletePaymentHistory => AdminPermissions.CanDeletePaymentHistory(CurrentUser);

    public bool CanManageWeeklySchedule => AdminPermissions.CanManageWeeklySchedule(CurrentUser);

    public bool CanManageTuitionDiscount => AdminPermissions.CanManageTuitionDiscount(CurrentUser);

    public void SetCachedUser(AuthenticatedUser user) => _cachedUser = user;

    public void ClearCache() => _cachedUser = null;
}
