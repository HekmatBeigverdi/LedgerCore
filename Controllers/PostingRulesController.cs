using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Interfaces.Services;
using LedgerCore.Core.Models.Accounting;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Master;
using LedgerCore.Core.Models.Security;
using LedgerCore.Core.ViewModels.Masters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PostingRulesController(IUnitOfWork uow, ISecurityActivityLogService activityLog) : ControllerBase
{
    [HttpGet]
    [HasPermission(PermissionCodes.Master_PostingRules_View)]
    public async Task<ActionResult<List<PostingRuleDto>>> GetAll(CancellationToken cancellationToken)
    {
        var ruleRepo = uow.Repository<PostingRule>();
        var lineRepo = uow.Repository<PostingRuleLine>();

        var rulesRaw = await ruleRepo.GetAllAsync(cancellationToken: cancellationToken);
        var linesRaw = await lineRepo.GetAllAsync(cancellationToken: cancellationToken);

        var rules = rulesRaw.Items
            .OrderBy(x => x.DocumentType)
            .ThenBy(x => x.Code)
            .ThenBy(x => x.BranchId.HasValue ? 1 : 0)
            .ThenBy(x => x.BranchId)
            .ThenByDescending(x => x.Priority)
            .ToList();

        var linesByRuleId = linesRaw.Items
            .GroupBy(x => x.PostingRuleId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.LineNumber).ToList());

        var result = rules.Select(rule => new PostingRuleDto
        {
            Id = rule.Id,
            Code = rule.Code,
            Name = rule.Name,
            DocumentType = rule.DocumentType,
            BranchId = rule.BranchId,
            IsActive = rule.IsActive,
            AutoPost = rule.AutoPost,
            Priority = rule.Priority,
            Lines = linesByRuleId.TryGetValue(rule.Id, out var ruleLines)
                ? ruleLines.Select(MapLine).ToList()
                : new List<PostingRuleLineDto>()
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission(PermissionCodes.Master_PostingRules_View)]
    public async Task<ActionResult<PostingRuleDto>> Get(int id, CancellationToken cancellationToken)
    {
        var ruleRepo = uow.Repository<PostingRule>();
        var lineRepo = uow.Repository<PostingRuleLine>();

        var rule = await ruleRepo.GetByIdAsync(id, cancellationToken);
        if (rule is null)
            return NotFound();

        var linesRaw = await lineRepo.FindAsync(
            x => x.PostingRuleId == id,
            cancellationToken: cancellationToken);

        var dto = new PostingRuleDto
        {
            Id = rule.Id,
            Code = rule.Code,
            Name = rule.Name,
            DocumentType = rule.DocumentType,
            BranchId = rule.BranchId,
            IsActive = rule.IsActive,
            AutoPost = rule.AutoPost,
            Priority = rule.Priority,
            Lines = linesRaw.Items
                .OrderBy(x => x.LineNumber)
                .Select(MapLine)
                .ToList()
        };

        return Ok(dto);
    }

    [HttpPost]
    [HasPermission(PermissionCodes.Master_PostingRules_Manage)]
    public async Task<IActionResult> Create(
        [FromBody] PostingRuleDto request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateRequestAsync(request, null, cancellationToken);
        if (validationError is not null)
            return validationError;

        var ruleRepo = uow.Repository<PostingRule>();
        var lineRepo = uow.Repository<PostingRuleLine>();

        var entity = new PostingRule
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            DocumentType = request.DocumentType.Trim(),
            BranchId = request.BranchId,
            IsActive = request.IsActive,
            AutoPost = request.AutoPost,
            Priority = request.Priority
        };

        await ruleRepo.AddAsync(entity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        foreach (var line in request.Lines.OrderBy(x => x.LineNumber))
        {
            var lineEntity = new PostingRuleLine
            {
                PostingRuleId = entity.Id,
                LineNumber = line.LineNumber,
                Side = line.Side,
                AmountSource = line.AmountSource,
                FixedAmount = line.AmountSource == PostingAmountSource.FixedAmount
                    ? line.FixedAmount
                    : null,
                AccountId = line.AccountId,
                UsePartyFromDocument = line.UsePartyFromDocument,
                IsActive = line.IsActive,
                DescriptionTemplate = string.IsNullOrWhiteSpace(line.DescriptionTemplate)
                    ? null
                    : line.DescriptionTemplate.Trim()
            };

            await lineRepo.AddAsync(lineEntity, cancellationToken);
        }

        await uow.SaveChangesAsync(cancellationToken);

        var response = new PostingRuleDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            DocumentType = entity.DocumentType,
            BranchId = entity.BranchId,
            IsActive = entity.IsActive,
            AutoPost = entity.AutoPost,
            Priority = entity.Priority,
            Lines = request.Lines
                .OrderBy(x => x.LineNumber)
                .Select(x => new PostingRuleLineDto
                {
                    Id = x.Id,
                    LineNumber = x.LineNumber,
                    Side = x.Side,
                    AmountSource = x.AmountSource,
                    FixedAmount = x.AmountSource == PostingAmountSource.FixedAmount ? x.FixedAmount : null,
                    AccountId = x.AccountId,
                    UsePartyFromDocument = x.UsePartyFromDocument,
                    IsActive = x.IsActive,
                    DescriptionTemplate = string.IsNullOrWhiteSpace(x.DescriptionTemplate)
                        ? null
                        : x.DescriptionTemplate.Trim()
                })
                .ToList()
        };

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, response);
    }

    [HttpPut("{id:int}")]
    [HasPermission(PermissionCodes.Master_PostingRules_Manage)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] PostingRuleDto request,
        CancellationToken cancellationToken)
    {
        var ruleRepo = uow.Repository<PostingRule>();
        var lineRepo = uow.Repository<PostingRuleLine>();

        var entity = await ruleRepo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        var validationError = await ValidateRequestAsync(request, id, cancellationToken);
        if (validationError is not null)
            return validationError;

        entity.Code = request.Code.Trim();
        entity.Name = request.Name.Trim();
        entity.DocumentType = request.DocumentType.Trim();
        entity.BranchId = request.BranchId;
        entity.IsActive = request.IsActive;
        entity.AutoPost = request.AutoPost;
        entity.Priority = request.Priority;

        ruleRepo.Update(entity);

        var existingLinesRaw = await lineRepo.FindAsync(
            x => x.PostingRuleId == id,
            cancellationToken: cancellationToken);

        var existingLines = existingLinesRaw.Items.ToList();
        if (existingLines.Count > 0)
            lineRepo.RemoveRange(existingLines);

        foreach (var line in request.Lines.OrderBy(x => x.LineNumber))
        {
            var lineEntity = new PostingRuleLine
            {
                PostingRuleId = entity.Id,
                LineNumber = line.LineNumber,
                Side = line.Side,
                AmountSource = line.AmountSource,
                FixedAmount = line.AmountSource == PostingAmountSource.FixedAmount
                    ? line.FixedAmount
                    : null,
                AccountId = line.AccountId,
                UsePartyFromDocument = line.UsePartyFromDocument,
                IsActive = line.IsActive,
                DescriptionTemplate = string.IsNullOrWhiteSpace(line.DescriptionTemplate)
                    ? null
                    : line.DescriptionTemplate.Trim()
            };

            await lineRepo.AddAsync(lineEntity, cancellationToken);
        }

        await uow.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [HasPermission(PermissionCodes.Master_PostingRules_Manage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ruleRepo = uow.Repository<PostingRule>();
        var lineRepo = uow.Repository<PostingRuleLine>();

        var entity = await ruleRepo.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return NotFound();

        entity.IsActive = false;
        ruleRepo.Update(entity);

        var linesRaw = await lineRepo.FindAsync(
            x => x.PostingRuleId == id,
            cancellationToken: cancellationToken);

        foreach (var line in linesRaw.Items)
        {
            line.IsActive = false;
            lineRepo.Update(line);
        }

        await uow.SaveChangesAsync(cancellationToken);
        await activityLog.LogAsync(
            action: "PostingRule.Deleted",
            entityType: nameof(PostingRule),
            entityId: entity.Id,
            actorUserId: null,
            actorUserName: User?.Identity?.Name,
            details: $"PostingRule '{entity.Code} - {entity.Name}' soft-deleted.",
            cancellationToken: cancellationToken);
        return NoContent();
    }

    private async Task<IActionResult?> ValidateRequestAsync(
        PostingRuleDto request,
        int? currentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("Code is required.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        if (string.IsNullOrWhiteSpace(request.DocumentType))
            return BadRequest("DocumentType is required.");

        if (request.Priority < 0)
            return BadRequest("Priority cannot be negative.");

        if (request.Lines is null || request.Lines.Count == 0)
            return BadRequest("At least one posting rule line is required.");

        if (request.BranchId.HasValue)
        {
            var branch = await uow.Repository<Branch>().GetByIdAsync(request.BranchId.Value, cancellationToken);
            if (branch is null)
                return BadRequest("BranchId is invalid.");
        }

        var ruleRepo = uow.Repository<PostingRule>();

        var duplicate = await ruleRepo.AnyAsync(
            x => x.DocumentType == request.DocumentType &&
                 x.Code == request.Code &&
                 (!currentId.HasValue || x.Id != currentId.Value),
            cancellationToken);

        if (duplicate)
            return BadRequest("A posting rule with this DocumentType and Code already exists.");

        var duplicateLineNumbers = request.Lines
            .GroupBy(x => x.LineNumber)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateLineNumbers.Count > 0)
            return BadRequest("Duplicate LineNumber values were found in posting rule lines.");

        var accountRepo = uow.Repository<Account>();

        foreach (var line in request.Lines)
        {
            if (line.LineNumber <= 0)
                return BadRequest("LineNumber must be greater than zero.");

            if (!Enum.IsDefined(typeof(PostingLineSide), line.Side))
                return BadRequest($"Invalid Side value at line {line.LineNumber}.");

            if (!Enum.IsDefined(typeof(PostingAmountSource), line.AmountSource))
                return BadRequest($"Invalid AmountSource value at line {line.LineNumber}.");

            if (line.AccountId <= 0)
                return BadRequest($"AccountId is required at line {line.LineNumber}.");

            var account = await accountRepo.GetByIdAsync(line.AccountId, cancellationToken);
            if (account is null)
                return BadRequest($"AccountId {line.AccountId} is invalid at line {line.LineNumber}.");

            if (!account.IsActive)
                return BadRequest($"AccountId {line.AccountId} is inactive at line {line.LineNumber}.");

            if (!account.IsPosting)
                return BadRequest($"AccountId {line.AccountId} must be a posting account at line {line.LineNumber}.");

            if (line.AmountSource == PostingAmountSource.FixedAmount)
            {
                if (!line.FixedAmount.HasValue || line.FixedAmount.Value <= 0)
                    return BadRequest($"FixedAmount must be greater than zero at line {line.LineNumber}.");
            }
            else
            {
                if (line.FixedAmount.HasValue && line.FixedAmount.Value < 0)
                    return BadRequest($"FixedAmount cannot be negative at line {line.LineNumber}.");
            }

            if (!string.IsNullOrWhiteSpace(line.DescriptionTemplate) && line.DescriptionTemplate.Length > 500)
                return BadRequest($"DescriptionTemplate length cannot exceed 500 characters at line {line.LineNumber}.");
        }

        return null;
    }

    private static PostingRuleLineDto MapLine(PostingRuleLine line)
    {
        return new PostingRuleLineDto
        {
            Id = line.Id,
            LineNumber = line.LineNumber,
            Side = line.Side,
            AmountSource = line.AmountSource,
            FixedAmount = line.FixedAmount,
            AccountId = line.AccountId,
            UsePartyFromDocument = line.UsePartyFromDocument,
            IsActive = line.IsActive,
            DescriptionTemplate = line.DescriptionTemplate
        };
    }
}