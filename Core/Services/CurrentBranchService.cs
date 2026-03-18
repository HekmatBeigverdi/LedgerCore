using System.Security.Claims;
using LedgerCore.Core.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace LedgerCore.Core.Services;

public class CurrentBranchService(IHttpContextAccessor accessor) : ICurrentBranchService
{
    public int? GetCurrentBranchId()
    {
        var http = accessor.HttpContext;
        if (http == null) return null;

        // 1) اول Claim داخل JWT (برای کاربران عادی، مبنا همین است)
        var claimVal = http.User?.FindFirstValue("branchId");
        var hasClaim = int.TryParse(claimVal, out var claimBranchId);

        // 2) اگر Header هست، فقط Admin اجازه override دارد
        if (http.Request.Headers.TryGetValue("X-Branch-Id", out var headerVal) &&
            int.TryParse(headerVal.ToString(), out var headerBranchId))
        {
            // فقط Admin می‌تواند شعبه را با Header عوض کند
            if (http.User?.IsInRole("Admin") == true)
                return headerBranchId;

            // کاربر عادی: Header را نادیده می‌گیریم (جلوگیری از جعل)
            if (hasClaim) return claimBranchId;

            // اگر claim هم ندارد، Header را قبول نمی‌کنیم
            return null;
        }

        // 3) اگر Header نبود، از Claim استفاده کن
        if (hasClaim) return claimBranchId;

        return null;
    }

    public int GetRequiredBranchId()
    {
        var id = GetCurrentBranchId();
        if (!id.HasValue)
            throw new InvalidOperationException("Branch scope is not set. Ensure JWT contains branchId (DefaultBranchId).");
        return id.Value;
    }
}