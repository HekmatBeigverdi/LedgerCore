using AutoMapper;
using LedgerCore.Core.Models.Documents;
using LedgerCore.Core.ViewModels.Cheques;
using LedgerCore.Core.ViewModels.Documents;
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
        
        // -------------------------
        // Receipt -> ReceiptDto
        // -------------------------
        CreateMap<Receipt, ReceiptDto>()
            // ساده‌ها رو AutoMapper خودش مپ می‌کند (Number, Date, Amount, FxRate, CashDeskCode, ReferenceNo, Description, Status)
            .ForMember(d => d.PartyCode,
                opt => opt.MapFrom(s => s.Party != null ? s.Party.Code : null))
            .ForMember(d => d.PartyName,
                opt => opt.MapFrom(s => s.Party != null ? s.Party.Name : null))

            .ForMember(d => d.BranchCode,
                opt => opt.MapFrom(s => s.Branch != null ? s.Branch.Code : null))
            .ForMember(d => d.BranchName,
                opt => opt.MapFrom(s => s.Branch != null ? s.Branch.Name : null))

            .ForMember(d => d.CurrencyCode,
                opt => opt.MapFrom(s => s.Currency != null ? s.Currency.Code : null))
            .ForMember(d => d.CurrencyName,
                opt => opt.MapFrom(s => s.Currency != null ? s.Currency.Name : null))

            .ForMember(d => d.BankAccountNumber,
                opt => opt.MapFrom(s => s.BankAccount != null ? s.BankAccount.AccountNumber : null))
            .ForMember(d => d.BankAccountTitle,
                opt => opt.MapFrom(s => s.BankAccount != null ? s.BankAccount.Title : null))
            .ForMember(d => d.BankId,
                opt => opt.MapFrom(s => s.BankAccount != null ? s.BankAccount.BankId : null))
            .ForMember(d => d.BankName,
                opt => opt.MapFrom(s => s.BankAccount != null && s.BankAccount.Bank != null
                    ? s.BankAccount.Bank.Name
                    : null))

            .ForMember(d => d.MethodName,
                opt => opt.MapFrom(s => s.Method.ToString()))
            .ForMember(d => d.StatusName,
                opt => opt.MapFrom(s => s.Status.ToString()))

            .ForMember(d => d.JournalVoucherNumber,
                opt => opt.MapFrom(s => s.JournalVoucher != null ? s.JournalVoucher.Number : null));

        // -------------------------
        // Payment -> PaymentDto
        // -------------------------
        CreateMap<Payment, PaymentDto>()
            .ForMember(d => d.PartyCode,
                opt => opt.MapFrom(s => s.Party != null ? s.Party.Code : null))
            .ForMember(d => d.PartyName,
                opt => opt.MapFrom(s => s.Party != null ? s.Party.Name : null))

            .ForMember(d => d.BranchCode,
                opt => opt.MapFrom(s => s.Branch != null ? s.Branch.Code : null))
            .ForMember(d => d.BranchName,
                opt => opt.MapFrom(s => s.Branch != null ? s.Branch.Name : null))

            .ForMember(d => d.CurrencyCode,
                opt => opt.MapFrom(s => s.Currency != null ? s.Currency.Code : null))
            .ForMember(d => d.CurrencyName,
                opt => opt.MapFrom(s => s.Currency != null ? s.Currency.Name : null))

            .ForMember(d => d.BankAccountNumber,
                opt => opt.MapFrom(s => s.BankAccount != null ? s.BankAccount.AccountNumber : null))
            .ForMember(d => d.BankAccountTitle,
                opt => opt.MapFrom(s => s.BankAccount != null ? s.BankAccount.Title : null))
            .ForMember(d => d.BankId,
                opt => opt.MapFrom(s => s.BankAccount != null ? s.BankAccount.BankId : null))
            .ForMember(d => d.BankName,
                opt => opt.MapFrom(s => s.BankAccount != null && s.BankAccount.Bank != null
                    ? s.BankAccount.Bank.Name
                    : null))

            .ForMember(d => d.MethodName,
                opt => opt.MapFrom(s => s.Method.ToString()))
            .ForMember(d => d.StatusName,
                opt => opt.MapFrom(s => s.Status.ToString()))

            .ForMember(d => d.JournalVoucherNumber,
                opt => opt.MapFrom(s => s.JournalVoucher != null ? s.JournalVoucher.Number : null));
        
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
    }
}