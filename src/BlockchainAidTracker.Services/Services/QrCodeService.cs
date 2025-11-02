using BlockchainAidTracker.Services.Interfaces;
using QRCoder;

namespace BlockchainAidTracker.Services.Services;

/// <summary>
/// Implementation of QR code generation service
/// </summary>
public class QrCodeService : IQrCodeService
{
    private readonly QRCodeGenerator _qrGenerator;

    public QrCodeService()
    {
        _qrGenerator = new QRCodeGenerator();
    }

    public string GenerateQrCode(string shipmentId)
    {
        if (string.IsNullOrWhiteSpace(shipmentId))
        {
            throw new ArgumentException("Shipment ID cannot be null or empty", nameof(shipmentId));
        }

        // Generate QR code data with shipment verification URL or ID
        var qrData = $"SHIPMENT:{shipmentId}";
        return GenerateQrCodeFromData(qrData);
    }

    public string GenerateQrCodeFromData(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            throw new ArgumentException("Data cannot be null or empty", nameof(data));
        }

        var qrCodeData = _qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20);

        return Convert.ToBase64String(qrCodeImage);
    }

    public byte[] GenerateQrCodeAsPng(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            throw new ArgumentException("Data cannot be null or empty", nameof(data));
        }

        var qrCodeData = _qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(20);
    }
}
