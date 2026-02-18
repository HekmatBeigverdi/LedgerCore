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

        // اولویت 1: Header (برای حالتی که بعداً خواستی شعبه را سوییچ کنی)
        if (http.Request.Headers.TryGetValue("X-Branch-Id", out var headerVal) &&
            int.TryParse(headerVal.ToString(), out var headerBranchId))
        {
            return headerBranchId;
        }

        // اولویت 2: Claim داخل JWT
        var claimVal = http.User?.FindFirstValue("branchId");
        if (int.TryParse(claimVal, out var claimBranchId))
            return claimBranchId;

        return null;
    }

    public int GetRequiredBranchId()
    {
        var id = GetCurrentBranchId();
        if (!id.HasValue)
            throw new InvalidOperationException("Branch scope is not set. Provide X-Branch-Id header or ensure JWT contains branchId.");
        return id.Value;
    }
}