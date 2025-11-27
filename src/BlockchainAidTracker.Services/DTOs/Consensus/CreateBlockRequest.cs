using System.ComponentModel.DataAnnotations;

namespace BlockchainAidTracker.Services.DTOs.Consensus;

/// <summary>
/// Request to manually trigger block creation
/// </summary>
public class CreateBlockRequest
{
    /// <summary>
    /// Password for validator private key decryption.
    /// Required for signing the block.
    /// </summary>
    [Required(ErrorMessage = "Validator password is required")]
    public string ValidatorPassword { get; set; } = string.Empty;
}
