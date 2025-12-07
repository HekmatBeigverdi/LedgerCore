using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Workflow;

namespace LedgerCore.Core.Services;

public class ApprovalService(IUnitOfWork uow) : IApprovalService
{
    public async Task<ApprovalRequest> CreateApprovalRequestAsync(
        string entityType,
        int entityId,
        CancellationToken cancellationToken = default)
    {
        var approvalRepo = uow.Repository<ApprovalRequest>();

        // اگر درخواست pending برای این سند وجود داشته باشد، همان را برگردان
        var existing = await approvalRepo.FindAsync(
            x => x.EntityType == entityType
                 && x.EntityId == entityId
                 && x.Status == ApprovalStatus.Pending,
            null,
            cancellationToken);

        var existingRequest = existing.Items.FirstOrDefault();
        if (existingRequest is not null)
            return existingRequest;

        // اگر قبلاً ApprovalRequest دیگری برای این سند هست (مثلاً Approved/Rejected)،
        // مشکلی نیست، فقط درخواست جدید می‌سازیم.

        var request = new ApprovalRequest
        {
            EntityType = entityType,
            EntityId = entityId,
            Status = ApprovalStatus.Pending,
            RequestedAt = DateTime.UtcNow,
            RequestedBy = null // بعداً اگر UserContext داشتی می‌توانی پرش کنی
        };

        await approvalRepo.AddAsync(request, cancellationToken);

        // سند اصلی را Pending کن
        await SetDocumentStatusAsync(entityType, entityId, DocumentStatus.Pending, cancellationToken);

        await uow.SaveChangesAsync(cancellationToken);

        return request;
    }

    public async Task ApproveAsync(
        int approvalRequestId,
        string performedBy,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var approvalRepo = uow.Repository<ApprovalRequest>();
            var stepRepo = uow.Repository<ApprovalStep>();

            var request = await approvalRepo.GetByIdAsync(approvalRequestId, cancellationToken)
                          ?? throw new InvalidOperationException(
                              $"ApprovalRequest with id={approvalRequestId} not found.");

            if (request.Status is ApprovalStatus.Approved or ApprovalStatus.Rejected or ApprovalStatus.Cancelled)
                throw new InvalidOperationException("ApprovalRequest is already completed.");

            // مرحله جدید به عنوان History
            var nextOrder = request.Steps.Any()
                ? request.Steps.Max(s => s.StepOrder) + 1
                : 1;

            var step = new ApprovalStep
            {
                ApprovalRequestId = request.Id,
                StepOrder = nextOrder,
                RoleName = "N/A",       // بعداً می‌توانی واقعی‌اش کنی
                UserName = performedBy,
                IsRequired = true,
                Status = ApprovalStatus.Approved,
                ActionAt = DateTime.UtcNow,
                ActionBy = performedBy,
                Comment = comment
            };

            await stepRepo.AddAsync(step, cancellationToken);

            request.Status = ApprovalStatus.Approved;
            request.LastActionBy = performedBy;
            request.LastActionAt = DateTime.UtcNow;
            request.LastActionComment = comment;

            approvalRepo.Update(request);

            // سند اصلی را Approved کن
            await SetDocumentStatusAsync(
                request.EntityType,
                request.EntityId,
                DocumentStatus.Approved,
                cancellationToken);

            await uow.SaveChangesAsync(cancellationToken);
            await uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task RejectAsync(
        int approvalRequestId,
        string performedBy,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        await uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var approvalRepo = uow.Repository<ApprovalRequest>();
            var stepRepo = uow.Repository<ApprovalStep>();

            var request = await approvalRepo.GetByIdAsync(approvalRequestId, cancellationToken)
                          ?? throw new InvalidOperationException(
                              $"ApprovalRequest with id={approvalRequestId} not found.");

            if (request.Status is ApprovalStatus.Approved or ApprovalStatus.Rejected or ApprovalStatus.Cancelled)
                throw new InvalidOperationException("ApprovalRequest is already completed.");

            var nextOrder = request.Steps.Any()
                ? request.Steps.Max(s => s.StepOrder) + 1
                : 1;

            var step = new ApprovalStep
            {
                ApprovalRequestId = request.Id,
                StepOrder = nextOrder,
                RoleName = "N/A",
                UserName = performedBy,
                IsRequired = true,
                Status = ApprovalStatus.Rejected,
                ActionAt = DateTime.UtcNow,
                ActionBy = performedBy,
                Comment = comment
            };

            await stepRepo.AddAsync(step, cancellationToken);

            request.Status = ApprovalStatus.Rejected;
            request.LastActionBy = performedBy;
            request.LastActionAt = DateTime.UtcNow;
            request.LastActionComment = comment;

            approvalRepo.Update(request);

            // سند اصلی را Rejected کن
            await SetDocumentStatusAsync(
                request.EntityType,
                request.EntityId,
                DocumentStatus.Cancelled,
                cancellationToken);

            await uow.SaveChangesAsync(cancellationToken);
            await uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    // ===================== Helper =====================

    private async Task SetDocumentStatusAsync(
        string entityType,
        int entityId,
        DocumentStatus newStatus,
        CancellationToken cancellationToken)
    {
        switch (entityType)
        {
            case "SalesInvoice":
            {
                var repo = uow.Repository<SalesInvoice>();
                var entity = await repo.GetByIdAsync(entityId, cancellationToken)
                             ?? throw new InvalidOperationException($"SalesInvoice with id={entityId} not found.");
                entity.Status = newStatus;
                repo.Update(entity);
                break;
            }

            case "PurchaseInvoice":
            {
                var repo = uow.Repository<PurchaseInvoice>();
                var entity = await repo.GetByIdAsync(entityId, cancellationToken)
                             ?? throw new InvalidOperationException($"PurchaseInvoice with id={entityId} not found.");
                entity.Status = newStatus;
                repo.Update(entity);
                break;
            }

            // می‌توانی بعداً کیس‌های دیگری مثل JournalVoucher, Payment, Receipt, PayrollDocument و ... را اضافه کنی

            default:
                // برای entityTypeهای ناشناخته فعلاً فقط ApprovalRequest را آپدیت می‌کنیم و به سند اصلی کاری نداریم
                break;
        }
    }
}