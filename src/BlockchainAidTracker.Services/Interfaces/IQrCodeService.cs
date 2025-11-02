namespace BlockchainAidTracker.Services.Interfaces;

/// <summary>
/// Service for generating and validating QR codes
/// </summary>
public interface IQrCodeService
{
    /// <summary>
    /// Generates a QR code for a shipment
    /// </summary>
    /// <param name="shipmentId">Shipment ID</param>
    /// <returns>Base64 encoded QR code image</returns>
    string GenerateQrCode(string shipmentId);

    /// <summary>
    /// Generates a QR code with custom data
    /// </summary>
    /// <param name="data">Data to encode</param>
    /// <returns>Base64 encoded QR code image</returns>
    string GenerateQrCodeFromData(string data);

    /// <summary>
    /// Generates a QR code and returns as PNG byte array
    /// </summary>
    /// <param name="data">Data to encode</param>
    /// <returns>PNG image as byte array</returns>
    byte[] GenerateQrCodeAsPng(string data);
}
