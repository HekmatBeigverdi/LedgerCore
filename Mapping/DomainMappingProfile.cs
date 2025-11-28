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
    }
}