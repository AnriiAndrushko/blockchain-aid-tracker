using BlockchainAidTracker.Services.Services;
using Xunit;

namespace BlockchainAidTracker.Tests.Services;

/// <summary>
/// Unit tests for QrCodeService
/// </summary>
public class QrCodeServiceTests
{
    private readonly QrCodeService _qrCodeService;

    public QrCodeServiceTests()
    {
        _qrCodeService = new QrCodeService();
    }

    [Fact]
    public void GenerateQrCode_ValidShipmentId_ReturnsBase64String()
    {
        // Arrange
        var shipmentId = "shipment123";

        // Act
        var result = _qrCodeService.GenerateQrCode(shipmentId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        // Check if it's valid base64
        Assert.True(IsValidBase64(result));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateQrCode_InvalidShipmentId_ThrowsArgumentException(string? shipmentId)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _qrCodeService.GenerateQrCode(shipmentId!));
    }

    [Fact]
    public void GenerateQrCode_DifferentShipmentIds_GeneratesDifferentQrCodes()
    {
        // Arrange
        var shipmentId1 = "shipment123";
        var shipmentId2 = "shipment456";

        // Act
        var qrCode1 = _qrCodeService.GenerateQrCode(shipmentId1);
        var qrCode2 = _qrCodeService.GenerateQrCode(shipmentId2);

        // Assert
        Assert.NotEqual(qrCode1, qrCode2);
    }

    [Fact]
    public void GenerateQrCode_SameShipmentId_GeneratesSameQrCode()
    {
        // Arrange
        var shipmentId = "shipment123";

        // Act
        var qrCode1 = _qrCodeService.GenerateQrCode(shipmentId);
        var qrCode2 = _qrCodeService.GenerateQrCode(shipmentId);

        // Assert
        Assert.Equal(qrCode1, qrCode2);
    }

    [Fact]
    public void GenerateQrCodeFromData_ValidData_ReturnsBase64String()
    {
        // Arrange
        var data = "Test data for QR code";

        // Act
        var result = _qrCodeService.GenerateQrCodeFromData(data);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(IsValidBase64(result));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateQrCodeFromData_InvalidData_ThrowsArgumentException(string? data)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _qrCodeService.GenerateQrCodeFromData(data!));
    }

    [Fact]
    public void GenerateQrCodeFromData_LongData_SuccessfullyGeneratesQrCode()
    {
        // Arrange
        var longData = new string('A', 1000);

        // Act
        var result = _qrCodeService.GenerateQrCodeFromData(longData);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(IsValidBase64(result));
    }

    [Fact]
    public void GenerateQrCodeFromData_SpecialCharacters_SuccessfullyGeneratesQrCode()
    {
        // Arrange
        var data = "Special characters: @#$%^&*()_+-=[]{}|;:',.<>?/~`";

        // Act
        var result = _qrCodeService.GenerateQrCodeFromData(data);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(IsValidBase64(result));
    }

    [Fact]
    public void GenerateQrCodeFromData_UnicodeCharacters_SuccessfullyGeneratesQrCode()
    {
        // Arrange
        var data = "Unicode: こんにちは 世界 🌍";

        // Act
        var result = _qrCodeService.GenerateQrCodeFromData(data);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(IsValidBase64(result));
    }

    [Fact]
    public void GenerateQrCodeAsPng_ValidData_ReturnsByteArray()
    {
        // Arrange
        var data = "Test data";

        // Act
        var result = _qrCodeService.GenerateQrCodeAsPng(data);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        // Check if it starts with PNG header
        Assert.Equal(0x89, result[0]); // PNG magic number
        Assert.Equal(0x50, result[1]); // 'P'
        Assert.Equal(0x4E, result[2]); // 'N'
        Assert.Equal(0x47, result[3]); // 'G'
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateQrCodeAsPng_InvalidData_ThrowsArgumentException(string? data)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _qrCodeService.GenerateQrCodeAsPng(data!));
    }

    [Fact]
    public void GenerateQrCodeAsPng_SameData_GeneratesSamePng()
    {
        // Arrange
        var data = "Test data";

        // Act
        var png1 = _qrCodeService.GenerateQrCodeAsPng(data);
        var png2 = _qrCodeService.GenerateQrCodeAsPng(data);

        // Assert
        Assert.Equal(png1, png2);
    }

    [Fact]
    public void GenerateQrCode_ContainsShipmentIdInData()
    {
        // Arrange
        var shipmentId = "shipment123";

        // Act
        var qrCodeBase64 = _qrCodeService.GenerateQrCode(shipmentId);
        var qrCodeWithShipmentPrefix = _qrCodeService.GenerateQrCodeFromData($"SHIPMENT:{shipmentId}");

        // Assert
        // Both should generate the same QR code since GenerateQrCode adds "SHIPMENT:" prefix
        Assert.Equal(qrCodeWithShipmentPrefix, qrCodeBase64);
    }

    /// <summary>
    /// Helper method to validate base64 string
    /// </summary>
    private bool IsValidBase64(string base64String)
    {
        if (string.IsNullOrEmpty(base64String))
            return false;

        try
        {
            Convert.FromBase64String(base64String);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
