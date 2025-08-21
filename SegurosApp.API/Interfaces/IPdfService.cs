using SegurosApp.API.DTOs;

namespace SegurosApp.API.Interfaces
{
    public interface IPdfService
    {
        Task<byte[]> GenerateInvoicePdfAsync(BillDetailDto billDetail);
    }
}