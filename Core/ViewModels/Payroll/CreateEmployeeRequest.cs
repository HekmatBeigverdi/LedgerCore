using LedgerCore.Core.Models.Enums;

namespace LedgerCore.Core.ViewModels.Payroll;

public class CreateEmployeeRequest
{
    public string PersonnelCode { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public Gender Gender { get; set; } = Gender.Unknown;
    public DateTime? BirthDate { get; set; }
    public string? NationalId { get; set; }
    public string? InsuranceCode { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;
    public int? BranchId { get; set; }
    public int? CostCenterId { get; set; }
    public bool IsActive { get; set; } = true;
}