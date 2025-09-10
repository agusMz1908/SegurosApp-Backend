using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SegurosApp.API.DTOs;
using SegurosApp.API.Interfaces;

namespace SegurosApp.API.Services
{
    public class PdfService : IPdfService
    {
        private readonly ILogger<PdfService> _logger;

        public PdfService(ILogger<PdfService> logger)
        {
            _logger = logger;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(BillDetailDto billDetail)
        {
            try
            {
                _logger.LogInformation("Generando PDF para factura {BillId}", billDetail.Id);

                var pdfBytes = await Task.Run(() =>
                {
                    return Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(2, Unit.Centimetre);
                            page.PageColor(Colors.White);
                            page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                            page.Header()
                                .Text($"Factura #{billDetail.Id:D6} - Sistema de Gestión de Pólizas")
                                .SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);

                            page.Content().Column(column =>
                            {
                                BuildInvoiceContent(column, billDetail);
                            });

                            page.Footer()
                                .AlignCenter()
                                .Text($"Generado el {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC")
                                .FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                    }).GeneratePdf();
                });

                _logger.LogInformation("PDF generado exitosamente para factura {BillId}, tamaño: {Size} bytes",
                    billDetail.Id, pdfBytes.Length);

                return pdfBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando PDF para factura {BillId}", billDetail.Id);
                throw;
            }
        }

        private void BuildInvoiceContent(ColumnDescriptor column, BillDetailDto billDetail)
        {
            column.Item().PaddingBottom(20).Row(row =>
            {
                row.ConstantItem(150).Column(col =>
                {
                    col.Item().Text("FACTURA DE SERVICIOS").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                    col.Item().Text("Sistema de Escaneo de Pólizas").FontSize(12).FontColor(Colors.Grey.Darken1);
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().AlignRight().Text($"Factura #{billDetail.Id:D6}").FontSize(16).SemiBold();
                    col.Item().AlignRight().Text($"Período: {billDetail.BillingPeriod}").FontSize(10);
                    col.Item().AlignRight().Text($"Estado: {billDetail.Status}").FontSize(10);
                });
            });

            column.Item().PaddingBottom(15).Background(Colors.Grey.Lighten4).Padding(10).Column(col =>
            {
                col.Item().Text("Información de la Empresa").SemiBold().FontSize(12).FontColor(Colors.Blue.Medium);
                col.Item().Text($"Empresa: {billDetail.CompanyName}").FontSize(10);

                if (!string.IsNullOrEmpty(billDetail.CompanyAddress))
                    col.Item().Text($"Dirección: {billDetail.CompanyAddress}").FontSize(10);

                if (!string.IsNullOrEmpty(billDetail.CompanyRUC))
                    col.Item().Text($"RUC: {billDetail.CompanyRUC}").FontSize(10);
            });

            column.Item().PaddingBottom(15).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Detalles de Facturación").SemiBold().FontSize(12).FontColor(Colors.Blue.Medium);
                    col.Item().Text($"Fecha de Emisión: {billDetail.GeneratedAt:dd/MM/yyyy}").FontSize(10);
                    col.Item().Text($"Fecha de Vencimiento: {billDetail.DueDate:dd/MM/yyyy}").FontSize(10);
                    col.Item().Text($"Tier Aplicado: {billDetail.AppliedTierName}").FontSize(10);
                    col.Item().Text($"Precio por Póliza: ${billDetail.PricePerPoliza:N2}").FontSize(10);
                });

                if (billDetail.PaidAt.HasValue)
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Información de Pago").SemiBold().FontSize(12).FontColor(Colors.Green.Medium);
                        col.Item().Text($"Fecha de Pago: {billDetail.PaidAt.Value:dd/MM/yyyy}").FontSize(10);

                        if (!string.IsNullOrEmpty(billDetail.PaymentMethod))
                            col.Item().Text($"Método: {billDetail.PaymentMethod}").FontSize(10);

                        if (!string.IsNullOrEmpty(billDetail.PaymentReference))
                            col.Item().Text($"Referencia: {billDetail.PaymentReference}").FontSize(10);
                    });
                }
            });

            column.Item().PaddingBottom(15).Column(col =>
            {
                col.Item().Text("Detalle de Pólizas Escaneadas").SemiBold().FontSize(12).FontColor(Colors.Blue.Medium);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(80);   
                        columns.RelativeColumn(3);    
                        columns.RelativeColumn(2);    
                        columns.ConstantColumn(70);   
                        columns.ConstantColumn(70);   
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Fecha").FontColor(Colors.White).FontSize(9).SemiBold();
                        header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Archivo").FontColor(Colors.White).FontSize(9).SemiBold();
                        header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Número Póliza").FontColor(Colors.White).FontSize(9).SemiBold();
                        header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Precio").FontColor(Colors.White).FontSize(9).SemiBold().AlignRight();
                        header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Total").FontColor(Colors.White).FontSize(9).SemiBold().AlignRight();
                    });

                    foreach (var item in billDetail.BillingItems)
                    {
                        table.Cell().Padding(5).Text(item.ScanDate.ToString("dd/MM/yyyy")).FontSize(9);
                        table.Cell().Padding(5).Text(item.FileName).FontSize(9);
                        table.Cell().Padding(5).Text(item.VelneoPolizaNumber ?? "N/A").FontSize(9).AlignCenter();
                        table.Cell().Padding(5).Text($"${item.PricePerPoliza:N2}").FontSize(9).AlignRight();
                        table.Cell().Padding(5).Text($"${item.Amount:N2}").FontSize(9).AlignRight();
                    }
                });
            });

            column.Item().PaddingTop(15).AlignRight().Width(250).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.ConstantColumn(80);
                });

                table.Cell().Padding(5).Text("Total Pólizas:").SemiBold().FontSize(10);
                table.Cell().Padding(5).Text(billDetail.TotalPolizasEscaneadas.ToString()).FontSize(10).AlignRight();

                table.Cell().Padding(5).Text("Subtotal:").SemiBold().FontSize(10);
                table.Cell().Padding(5).Text($"${billDetail.SubTotal:N2}").FontSize(10).AlignRight();

                if (billDetail.TaxAmount > 0)
                {
                    table.Cell().Padding(5).Text("Impuestos:").SemiBold().FontSize(10);
                    table.Cell().Padding(5).Text($"${billDetail.TaxAmount:N2}").FontSize(10).AlignRight();
                }

                table.Cell().Background(Colors.Blue.Medium).Padding(5).Text("TOTAL:").FontColor(Colors.White).SemiBold().FontSize(12);
                table.Cell().Background(Colors.Blue.Medium).Padding(5).Text($"${billDetail.TotalAmount:N2}").FontColor(Colors.White).SemiBold().FontSize(12).AlignRight();
            });
        }
    }
}