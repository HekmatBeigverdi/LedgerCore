using AutoMapper;
using LedgerCore.Core.Models.Assets;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.Models.Payroll;
using LedgerCore.Core.ViewModels.Assets;
using LedgerCore.Core.ViewModels.Cheques;
using LedgerCore.Core.ViewModels.Documents;
using LedgerCore.Core.ViewModels.Payroll;
using LedgerCore.Core.ViewModels.ReceiptsPayments;

namespace LedgerCore.Mapping;

public class DomainMappingProfile : Profile
{
    public DomainMappingProfile()
    {
        // SalesInvoice → SalesInvoiceDto
        CreateMap<SalesInvoice, SalesInvoiceDto>()
            .ForMember(d => d.CustomerCode, m => m.MapFrom(s => s.Customer!.Code))
            .ForMember(d => d.CustomerName, m => m.MapFrom(s => s.Customer!.Name))
            .ForMember(d => d.BranchName, m => m.MapFrom(s => s.Branch!.Name))
            .ForMember(d => d.WarehouseName, m => m.MapFrom(s => s.Warehouse!.Name))
            .ForMember(d => d.CurrencyCode, m => m.MapFrom(s => s.Currency!.Code));

        // InvoiceLine → InvoiceLineDto
        CreateMap<InvoiceLine, InvoiceLineDto>()
            .ForMember(d => d.ProductCode, m => m.MapFrom(s => s.Product!.Code))
            .ForMember(d => d.ProductName, m => m.MapFrom(s => s.Product!.Name))
            .ForMember(d => d.TaxRateName, m => m.MapFrom(s => s.TaxRate!.Name));

        // CreateSalesInvoiceRequest → SalesInvoice (ساخت Entity از Request)
        CreateMap<CreateSalesInvoiceRequest, SalesInvoice>()
            .ForMember(d => d.Number, m => m.Ignore())   // بعداً از NumberSeries می‌گیریم
            .ForMember(d => d.Lines, m => m.Ignore());   // جداگانه مپ می‌کنیم

        CreateMap<CreateSalesInvoiceLineRequest, InvoiceLine>()
            .ForMember(d => d.Id, m => m.Ignore())
            .ForMember(d => d.SalesInvoiceId, m => m.Ignore())
            .ForMember(d => d.PurchaseInvoiceId, m => m.Ignore());

        // UpdateSalesInvoiceRequest → SalesInvoice
        CreateMap<UpdateSalesInvoiceRequest, SalesInvoice>()
            .ForMember(d => d.Number, m => m.Ignore())
            .ForMember(d => d.Lines, m => m.Ignore());

        CreateMap<UpdateSalesInvoiceLineRequest, InvoiceLine>()
            .ForMember(d => d.SalesInvoiceId, m => m.Ignore())
            .ForMember(d => d.PurchaseInvoiceId, m => m.Ignore());
        
        // ===== PurchaseInvoice =====
        CreateMap<PurchaseInvoice, PurchaseInvoiceDto>()
            .ForMember(d => d.SupplierCode, m => m.MapFrom(s => s.Supplier!.Code))
            .ForMember(d => d.SupplierName, m => m.MapFrom(s => s.Supplier!.Name))
            .ForMember(d => d.BranchName, m => m.MapFrom(s => s.Branch!.Name))
            .ForMember(d => d.WarehouseName, m => m.MapFrom(s => s.Warehouse!.Name))
            .ForMember(d => d.CurrencyCode, m => m.MapFrom(s => s.Currency!.Code));

        CreateMap<CreatePurchaseInvoiceRequest, PurchaseInvoice>()
            .ForMember(d => d.Number, m => m.Ignore())
            .ForMember(d => d.Lines, m => m.Ignore());

        CreateMap<CreatePurchaseInvoiceLineRequest, InvoiceLine>()
            .ForMember(d => d.Id, m => m.Ignore())
            .ForMember(d => d.SalesInvoiceId, m => m.Ignore())
            .ForMember(d => d.PurchaseInvoiceId, m => m.Ignore());

        CreateMap<UpdatePurchaseInvoiceRequest, PurchaseInvoice>()
            .ForMember(d => d.Number, m => m.Ignore())
            .ForMember(d => d.Lines, m => m.Ignore());

        CreateMap<UpdatePurchaseInvoiceLineRequest, InvoiceLine>()
            .ForMember(d => d.SalesInvoiceId, m => m.Ignore())
            .ForMember(d => d.PurchaseInvoiceId, m => m.Ignore());
        
        // ===== Receipt =====
        CreateMap<Receipt, ReceiptDto>()
            .ForMember(d => d.PartyCode, m => m.MapFrom(s => s.Party!.Code))
            .ForMember(d => d.PartyName, m => m.MapFrom(s => s.Party!.Name))
            .ForMember(d => d.BranchName, m => m.MapFrom(s => s.Branch!.Name))
            .ForMember(d => d.CurrencyCode, m => m.MapFrom(s => s.Currency!.Code))
            .ForMember(d => d.BankAccountTitle, m => m.MapFrom(s => s.BankAccount!.Title));

        CreateMap<CreateReceiptRequest, Receipt>()
            .ForMember(d => d.Id, m => m.Ignore())
            .ForMember(d => d.Number, m => m.Ignore())
            .ForMember(d => d.Status, m => m.Ignore())
            .ForMember(d => d.JournalVoucherId, m => m.Ignore())
            .ForMember(d => d.JournalVoucher, m => m.Ignore());

        CreateMap<UpdateReceiptRequest, Receipt>()
            .ForMember(d => d.Id, m => m.Ignore())
            .ForMember(d => d.Number, m => m.Ignore())
            .ForMember(d => d.Status, m => m.Ignore())
            .ForMember(d => d.JournalVoucherId, m => m.Ignore())
            .ForMember(d => d.JournalVoucher, m => m.Ignore());

        // ===== Payment =====
        CreateMap<Payment, PaymentDto>()
            .ForMember(d => d.PartyCode, m => m.MapFrom(s => s.Party!.Code))
            .ForMember(d => d.PartyName, m => m.MapFrom(s => s.Party!.Name))
            .ForMember(d => d.BranchName, m => m.MapFrom(s => s.Branch!.Name))
            .ForMember(d => d.CurrencyCode, m => m.MapFrom(s => s.Currency!.Code))
            .ForMember(d => d.BankAccountTitle, m => m.MapFrom(s => s.BankAccount!.Title));

        CreateMap<CreatePaymentRequest, Payment>()
            .ForMember(d => d.Id, m => m.Ignore())
            .ForMember(d => d.Number, m => m.Ignore())
            .ForMember(d => d.Status, m => m.Ignore())
            .ForMember(d => d.JournalVoucherId, m => m.Ignore())
            .ForMember(d => d.JournalVoucher, m => m.Ignore());

        CreateMap<UpdatePaymentRequest, Payment>()
            .ForMember(d => d.Id, m => m.Ignore())
            .ForMember(d => d.Number, m => m.Ignore())
            .ForMember(d => d.Status, m => m.Ignore())
            .ForMember(d => d.JournalVoucherId, m => m.Ignore())
            .ForMember(d => d.JournalVoucher, m => m.Ignore());        

        // -------------------------
        // Cheque -> ChequeDto
        // -------------------------
        CreateMap<Cheque, ChequeDto>()
            .ForMember(d => d.PartyName, m => m.MapFrom(s => s.Party!.Name))
            .ForMember(d => d.BankAccountTitle, m => m.MapFrom(s => s.BankAccount!.Title))
            .ForMember(d => d.CurrencyCode, m => m.MapFrom(s => s.Currency!.Code));

        CreateMap<ChequeHistory, ChequeHistoryDto>();

        CreateMap<RegisterChequeRequest, Cheque>()
            .ForMember(d => d.Id, m => m.Ignore())
            .ForMember(d => d.Status, m => m.Ignore())
            .ForMember(d => d.History, m => m.Ignore());  
        
        // ===== FixedAsset =====
        CreateMap<FixedAsset, FixedAssetDto>()
            .ForMember(d => d.CategoryName, m => m.MapFrom(s => s.Category!.Name))
            .ForMember(d => d.DepreciationMethodName, m => m.MapFrom(s => s.DepreciationMethod!.Name))
            .ForMember(d => d.BranchName, m => m.MapFrom(s => s.Branch!.Name))
            .ForMember(d => d.CostCenterName, m => m.MapFrom(s => s.CostCenter!.Name))
            .ForMember(d => d.ProjectName, m => m.MapFrom(s => s.Project!.Name));

        CreateMap<CreateFixedAssetRequest, FixedAsset>()
            .ForMember(d => d.Id, m => m.Ignore())
            .ForMember(d => d.Status, m => m.Ignore())
            .ForMember(d => d.AccumulatedDepreciation, m => m.Ignore())
            .ForMember(d => d.DepreciationSchedules, m => m.Ignore())
            .ForMember(d => d.Transactions, m => m.Ignore());

        CreateMap<UpdateFixedAssetRequest, FixedAsset>()
            .ForMember(d => d.Id, m => m.Ignore())
            .ForMember(d => d.Status, m => m.Ignore())
            .ForMember(d => d.AccumulatedDepreciation, m => m.Ignore())
            .ForMember(d => d.DepreciationSchedules, m => m.Ignore())
            .ForMember(d => d.Transactions, m => m.Ignore());

        // ===== DepreciationSchedule =====
        CreateMap<DepreciationSchedule, DepreciationScheduleDto>();

        // ===== AssetTransaction =====
        CreateMap<AssetTransaction, AssetTransactionDto>();
        
        // ===== Payroll =====
        
        // PayrollLine -> PayrollLineDto
        CreateMap<PayrollLine, PayrollLineDto>()
            .ForMember(d => d.EmployeePersonnelCode, m => m.MapFrom(s => s.Employee!.PersonnelCode))
            .ForMember(d => d.EmployeeFullName, m => m.MapFrom(s => s.Employee!.FullName));
        
        // PayrollDocument -> PayrollDocumentDto
        CreateMap<PayrollDocument, PayrollDocumentDto>()
            .ForMember(d => d.PayrollPeriodCode, m => m.MapFrom(s => s.PayrollPeriod!.Code))
            .ForMember(d => d.PayrollPeriodName, m => m.MapFrom(s => s.PayrollPeriod!.Name))
            .ForMember(d => d.BranchName, m => m.MapFrom(s => s.Branch!.Name))
            .ForMember(d => d.Lines, m => m.MapFrom(s => s.Lines));

        // CreatePayrollRequest -> PayrollDocument
        CreateMap<CreatePayrollRequest, PayrollDocument>()
            .ForMember(d => d.Id, m => m.Ignore())
            .ForMember(d => d.Number, m => m.Ignore())
            .ForMember(d => d.Status, m => m.Ignore())
            .ForMember(d => d.TotalGross, m => m.Ignore())
            .ForMember(d => d.TotalDeductions, m => m.Ignore())
            .ForMember(d => d.TotalNet, m => m.Ignore())
            .ForMember(d => d.JournalVoucherId, m => m.Ignore())
            .ForMember(d => d.JournalVoucher, m => m.Ignore())
            .ForMember(d => d.Lines, m => m.MapFrom(s => s.Lines));

        // CreatePayrollLineRequest -> PayrollLine
        CreateMap<CreatePayrollLineRequest, PayrollLine>()
            .ForMember(d => d.Id, m => m.Ignore())
            .ForMember(d => d.PayrollDocumentId, m => m.Ignore())
            .ForMember(d => d.PayrollDocument, m => m.Ignore())
            .ForMember(d => d.NetAmount, m => m.Ignore());
    }
}