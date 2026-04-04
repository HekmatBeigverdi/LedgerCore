using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Inventory;
using LedgerCore.Core.Models.Workflow;

namespace LedgerCore.Core.Services;

public class ApprovalService(
    IUnitOfWork uow,
    ICurrentBranchService currentBranch) : IApprovalService
{
    public async Task<ApprovalRequest> CreateApprovalRequestAsync(
        string entityType,
        int entityId,
        CancellationToken cancellationToken = default)
    {
        var approvalRepo = uow.Repository<ApprovalRequest>();

        // اگر درخواست pending برای این سند وجود داشته باشد، همان را برگردان
        var branchId = GetBranchIdOrThrow();

        // چک اینکه سند واقعاً در همین شعبه وجود دارد
        await EnsureDocumentInCurrentBranchAsync(
            entityType,
            entityId,
            cancellationToken);
        
        var documentStatus = await GetDocumentStatusAsync(
            entityType,
            entityId,
            cancellationToken);

        if (documentStatus != DocumentStatus.Draft)
            throw new InvalidOperationException("Only draft documents can be submitted for approval.");

        // اگر درخواست pending برای این سند وجود داشته باشد
        var existing = await approvalRepo.FindAsync(
            x => x.BranchId == branchId
                 && x.EntityType == entityType
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
            BranchId = branchId,
            EntityType = entityType,
            EntityId = entityId,
            Status = ApprovalStatus.Pending,
            RequestedAt = DateTime.UtcNow,
            RequestedBy = null // بعداً اگر UserContext داشتی می‌توانی پرش کنی
        };

        await approvalRepo.AddAsync(request, cancellationToken);

        // سند اصلی را Pending کن
        await SetDocumentStatusAsync(
            entityType,
            entityId,
            branchId,
            DocumentStatus.Pending,
            cancellationToken);
        
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

            var request = await GetApprovalRequestScopedOrThrowAsync(
                approvalRequestId,
                cancellationToken);

            if (request.Status is ApprovalStatus.Approved or ApprovalStatus.Rejected or ApprovalStatus.Cancelled)
                throw new InvalidOperationException("ApprovalRequest is already completed.");

            // مرحله جدید به عنوان History
            var existingSteps = await stepRepo.FindAsync(
                x => x.ApprovalRequestId == request.Id,
                null,
                cancellationToken);

            var nextOrder = existingSteps.Items.Any()
                ? existingSteps.Items.Max(s => s.StepOrder) + 1
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
                request.BranchId,
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

            var request = await GetApprovalRequestScopedOrThrowAsync(
                approvalRequestId,
                cancellationToken);

            if (request.Status is ApprovalStatus.Approved or ApprovalStatus.Rejected or ApprovalStatus.Cancelled)
                throw new InvalidOperationException("ApprovalRequest is already completed.");

            var existingSteps = await stepRepo.FindAsync(
                x => x.ApprovalRequestId == request.Id,
                null,
                cancellationToken);

            var nextOrder = existingSteps.Items.Any()
                ? existingSteps.Items.Max(s => s.StepOrder) + 1
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
                request.BranchId,
                DocumentStatus.Draft,
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
    private async Task<ApprovalRequest?> GetApprovalRequestScopedAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var branchId = GetBranchIdOrThrow();
        var approvalRepo = uow.Repository<ApprovalRequest>();

        var page = await approvalRepo.FindAsync(
            x => x.Id == id && x.BranchId == branchId,
            null,
            cancellationToken);

        return page.Items.FirstOrDefault();
    }

    private async Task<ApprovalRequest> GetApprovalRequestScopedOrThrowAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var request = await GetApprovalRequestScopedAsync(id, cancellationToken);
        if (request is null)
            throw new InvalidOperationException($"ApprovalRequest with id={id} not found.");

        return request;
    }
    private async Task EnsureDocumentInCurrentBranchAsync(
        string entityType,
        int entityId,
        CancellationToken cancellationToken)
    {
        var branchId = GetBranchIdOrThrow();

        switch (entityType)
        {
            case "SalesInvoice":
            {
                var entity = await uow.Invoices
                    .GetSalesInvoiceWithLinesAsync(entityId, branchId, cancellationToken);

                if (entity is null)
                    throw new InvalidOperationException(
                        $"SalesInvoice with id={entityId} not found in current branch.");

                break;
            }

            case "PurchaseInvoice":
            {
                var entity = await uow.Invoices
                    .GetPurchaseInvoiceWithLinesAsync(entityId, branchId, cancellationToken);

                if (entity is null)
                    throw new InvalidOperationException(
                        $"PurchaseInvoice with id={entityId} not found in current branch.");

                break;
            }

            case "Receipt":
            {
                var entity = await uow.Repository<Receipt>()
                    .GetByIdAsync(entityId, cancellationToken);

                if (entity is null || entity.BranchId != branchId)
                    throw new InvalidOperationException(
                        $"Receipt with id={entityId} not found in current branch.");

                break;
            }

            case "Payment":
            {
                var entity = await uow.Repository<Payment>()
                    .GetByIdAsync(entityId, cancellationToken);

                if (entity is null || entity.BranchId != branchId)
                    throw new InvalidOperationException(
                        $"Payment with id={entityId} not found in current branch.");

                break;
            }

            case "JournalVoucher":
            {
                var entity = await uow.Repository<JournalVoucher>()
                    .GetByIdAsync(entityId, cancellationToken);

                if (entity is null || entity.BranchId != branchId)
                    throw new InvalidOperationException(
                        $"JournalVoucher with id={entityId} not found in current branch.");

                break;
            }
            
            case "CashTransfer":
            {
                var entity = await uow.Repository<CashTransfer>()
                    .GetByIdAsync(entityId, cancellationToken);

                if (entity is null || entity.BranchId != branchId)
                    throw new InvalidOperationException(
                        $"CashTransfer with id={entityId} not found in current branch.");

                break;
            }
            case "InventoryAdjustment":
            {
                var entity = await uow.Repository<InventoryAdjustment>()
                    .GetByIdAsync(entityId, cancellationToken);

                if (entity is null || entity.BranchId != branchId)
                    throw new InvalidOperationException(
                        $"InventoryAdjustment with id={entityId} not found in current branch.");

                break;
            }
            case "SalesReturn":
            {
                var entity = await uow.Invoices
                    .GetSalesReturnWithLinesAsync(entityId, cancellationToken);

                if (entity is null || entity.BranchId != branchId)
                    throw new InvalidOperationException(
                        $"SalesReturn with id={entityId} not found in current branch.");

                break;
            }

            default:
                throw new InvalidOperationException(
                    $"Approval is not supported for entityType '{entityType}'.");
        }
    }
    
    private int GetBranchIdOrThrow()
        => currentBranch.GetRequiredBranchId();

    private async Task<DocumentStatus> GetDocumentStatusAsync(
        string entityType,
        int entityId,
        CancellationToken cancellationToken)
    {
        var branchId = GetBranchIdOrThrow();

        switch (entityType)
        {
            case "SalesInvoice":
            {
                var entity = await uow.Invoices.GetSalesInvoiceWithLinesAsync(
                    entityId,
                    branchId,
                    cancellationToken);

                if (entity is null)
                    throw new InvalidOperationException(
                        $"SalesInvoice with id={entityId} not found in current branch.");

                return entity.Status;
            }

            case "PurchaseInvoice":
            {
                var entity = await uow.Invoices.GetPurchaseInvoiceWithLinesAsync(
                    entityId,
                    branchId,
                    cancellationToken);

                if (entity is null)
                    throw new InvalidOperationException(
                        $"PurchaseInvoice with id={entityId} not found in current branch.");

                return entity.Status;
            }

            case "Receipt":
            {
                var entity = await uow.Repository<Receipt>()
                    .GetByIdAsync(entityId, cancellationToken);

                if (entity is null || entity.BranchId != branchId)
                    throw new InvalidOperationException(
                        $"Receipt with id={entityId} not found in current branch.");

                return entity.Status;
            }

            case "Payment":
            {
                var entity = await uow.Repository<Payment>()
                    .GetByIdAsync(entityId, cancellationToken);

                if (entity is null || entity.BranchId != branchId)
                    throw new InvalidOperationException(
                        $"Payment with id={entityId} not found in current branch.");

                return entity.Status;
            }

            case "JournalVoucher":
            {
                var entity = await uow.Repository<JournalVoucher>()
                    .GetByIdAsync(entityId, cancellationToken);

                if (entity is null || entity.BranchId != branchId)
                    throw new InvalidOperationException(
                        $"JournalVoucher with id={entityId} not found in current branch.");

                return entity.Status;
            }
            
            case "CashTransfer":
            {
                var entity = await uow.Repository<CashTransfer>()
                    .GetByIdAsync(entityId, cancellationToken);

                if (entity is null || entity.BranchId != branchId)
                    throw new InvalidOperationException(
                        $"CashTransfer with id={entityId} not found in current branch.");

                return entity.Status;
            }
            case "InventoryAdjustment":
            {
                var entity = await uow.Repository<InventoryAdjustment>()
                    .GetByIdAsync(entityId, cancellationToken);

                if (entity is null || entity.BranchId != branchId)
                    throw new InvalidOperationException(
                        $"InventoryAdjustment with id={entityId} not found in current branch.");

                return entity.Status;
            }
            case "SalesReturn":
            {
                var entity = await uow.Invoices
                    .GetSalesReturnWithLinesAsync(entityId, cancellationToken);

                if (entity is null || entity.BranchId != branchId)
                    throw new InvalidOperationException(
                        $"SalesReturn with id={entityId} not found in current branch.");

                return entity.Status;
            }

            default:
                throw new InvalidOperationException(
                    $"Approval is not supported for entityType '{entityType}'.");
        }
    }
    private async Task SetDocumentStatusAsync(
        string entityType,
        int entityId,
        int branchId,
        DocumentStatus newStatus,
        CancellationToken cancellationToken)
    {
        switch (entityType)
        {
            case "SalesInvoice":
            {
                var entity = await uow.Invoices.GetSalesInvoiceWithLinesAsync(
                                 entityId,
                                 branchId,
                                 cancellationToken)
                             ?? throw new InvalidOperationException(
                                 $"SalesInvoice with id={entityId} not found in current branch.");

                entity.Status = newStatus;
                uow.Invoices.UpdateSalesInvoice(entity);
                break;
            }

            case "PurchaseInvoice":
            {
                var entity = await uow.Invoices.GetPurchaseInvoiceWithLinesAsync(
                                 entityId,
                                 branchId,
                                 cancellationToken)
                             ?? throw new InvalidOperationException(
                                 $"PurchaseInvoice with id={entityId} not found in current branch.");

                entity.Status = newStatus;
                uow.Invoices.UpdatePurchaseInvoice(entity);
                break;
            }

            case "Receipt":
            {
                var entity = await uow.Repository<Receipt>()
                    .GetByIdAsync(entityId, cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"Receipt with id={entityId} not found in current branch.");

                if (entity.BranchId != branchId)
                    throw new InvalidOperationException(
                        $"Receipt with id={entityId} not found in current branch.");

                entity.Status = newStatus;
                uow.Repository<Receipt>().Update(entity);
                break;
            }

            case "Payment":
            {
                var entity = await uow.Repository<Payment>()
                    .GetByIdAsync(entityId, cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"Payment with id={entityId} not found in current branch.");

                if (entity.BranchId != branchId)
                    throw new InvalidOperationException(
                        $"Payment with id={entityId} not found in current branch.");

                entity.Status = newStatus;
                uow.Repository<Payment>().Update(entity);
                break;
            }

            case "JournalVoucher":
            {
                var entity = await uow.Repository<JournalVoucher>()
                    .GetByIdAsync(entityId, cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"JournalVoucher with id={entityId} not found in current branch.");

                if (entity.BranchId != branchId)
                    throw new InvalidOperationException(
                        $"JournalVoucher with id={entityId} not found in current branch.");

                entity.Status = newStatus;
                uow.Repository<JournalVoucher>().Update(entity);
                break;
            }
            
            case "CashTransfer":
            {
                var entity = await uow.Repository<CashTransfer>()
                                 .GetByIdAsync(entityId, cancellationToken)
                             ?? throw new InvalidOperationException(
                                 $"CashTransfer with id={entityId} not found in current branch.");

                if (entity.BranchId != branchId)
                    throw new InvalidOperationException(
                        $"CashTransfer with id={entityId} not found in current branch.");

                entity.Status = newStatus;
                uow.Repository<CashTransfer>().Update(entity);
                break;
            }
            case "InventoryAdjustment":
            {
                var entity = await uow.Repository<InventoryAdjustment>()
                                 .GetByIdAsync(entityId, cancellationToken)
                             ?? throw new InvalidOperationException(
                                 $"InventoryAdjustment with id={entityId} not found in current branch.");

                if (entity.BranchId != branchId)
                    throw new InvalidOperationException(
                        $"InventoryAdjustment with id={entityId} not found in current branch.");

                entity.Status = newStatus;
                uow.Repository<InventoryAdjustment>().Update(entity);
                break;
            }
            case "SalesReturn":
            {
                var entity = await uow.Invoices
                                 .GetSalesReturnWithLinesAsync(entityId, cancellationToken)
                             ?? throw new InvalidOperationException(
                                 $"SalesReturn with id={entityId} not found in current branch.");

                if (entity.BranchId != branchId)
                    throw new InvalidOperationException(
                        $"SalesReturn with id={entityId} not found in current branch.");

                entity.Status = newStatus;
                uow.Invoices.UpdateSalesReturn(entity);
                break;
            }

            default:
                throw new InvalidOperationException(
                    $"Approval is not supported for entityType '{entityType}'.");
        }
    }
}