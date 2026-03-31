using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Payroll;

public class EmployeeDto
{
    public int Id { get; set; }
    public string PersonnelCode { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public Gender Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? NationalId { get; set; }
    public string? InsuranceCode { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public EmploymentType EmploymentType { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int? CostCenterId { get; set; }
    public string? CostCenterName { get; set; }
    public bool IsActive { get; set; }
}