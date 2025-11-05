using BlockchainAidTracker.Blockchain;
using BlockchainAidTracker.Services.DTOs.Blockchain;
using Microsoft.AspNetCore.Mvc;

namespace BlockchainAidTracker.Api.Controllers;

/// <summary>
/// Controller for blockchain query operations
/// </summary>
[ApiController]
[Route("api/blockchain")]
[Produces("application/json")]
public class BlockchainController : ControllerBase
{
    private readonly Blockchain.Blockchain _blockchain;
    private readonly ILogger<BlockchainController> _logger;

    public BlockchainController(
        Blockchain.Blockchain blockchain,
        ILogger<BlockchainController> logger)
    {
        _blockchain = blockchain ?? throw new ArgumentNullException(nameof(blockchain));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the complete blockchain
    /// </summary>
    /// <returns>List of all blocks in the chain</returns>
    /// <response code="200">Blockchain retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("chain")]
    [ProducesResponseType(typeof(List<BlockDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<List<BlockDto>> GetChain()
    {
        try
        {
            _logger.LogInformation("Retrieving complete blockchain");

            var blocks = _blockchain.Chain.Select(BlockDto.FromBlock).ToList();

            _logger.LogInformation("Retrieved blockchain with {BlockCount} blocks", blocks.Count);
            return Ok(blocks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blockchain");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets a specific block by its index
    /// </summary>
    /// <param name="index">Block index</param>
    /// <returns>Block details</returns>
    /// <response code="200">Block retrieved successfully</response>
    /// <response code="404">Block not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("blocks/{index}")]
    [ProducesResponseType(typeof(BlockDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<BlockDto> GetBlockByIndex(int index)
    {
        try
        {
            _logger.LogInformation("Retrieving block at index {Index}", index);

            var block = _blockchain.GetBlockByIndex(index);

            if (block == null)
            {
                _logger.LogWarning("Block at index {Index} not found", index);
                return NotFound(new ProblemDetails
                {
                    Title = "Block Not Found",
                    Detail = $"Block at index {index} was not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            _logger.LogInformation("Block at index {Index} retrieved successfully", index);
            return Ok(BlockDto.FromBlock(block));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving block at index {Index}", index);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets a specific transaction by its ID
    /// </summary>
    /// <param name="id">Transaction ID</param>
    /// <returns>Transaction details</returns>
    /// <response code="200">Transaction retrieved successfully</response>
    /// <response code="404">Transaction not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("transactions/{id}")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<TransactionDto> GetTransactionById(string id)
    {
        try
        {
            _logger.LogInformation("Retrieving transaction {TransactionId}", id);

            var transaction = _blockchain.GetTransactionById(id);

            if (transaction == null)
            {
                _logger.LogWarning("Transaction {TransactionId} not found", id);
                return NotFound(new ProblemDetails
                {
                    Title = "Transaction Not Found",
                    Detail = $"Transaction with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            _logger.LogInformation("Transaction {TransactionId} retrieved successfully", id);
            return Ok(TransactionDto.FromTransaction(transaction));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transaction {TransactionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Validates the entire blockchain
    /// </summary>
    /// <returns>Validation result</returns>
    /// <response code="200">Validation completed successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<ValidationResultDto> ValidateChain()
    {
        try
        {
            _logger.LogInformation("Validating blockchain");

            var isValid = _blockchain.IsValidChain();
            var result = new ValidationResultDto
            {
                IsValid = isValid,
                BlockCount = _blockchain.Chain.Count,
                ValidatedAt = DateTime.UtcNow,
                Errors = isValid ? new List<string>() : new List<string> { "Blockchain validation failed" }
            };

            _logger.LogInformation("Blockchain validation result: {IsValid}", isValid);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating blockchain");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets all pending transactions that haven't been added to a block yet
    /// </summary>
    /// <returns>List of pending transactions</returns>
    /// <response code="200">Pending transactions retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<List<TransactionDto>> GetPendingTransactions()
    {
        try
        {
            _logger.LogInformation("Retrieving pending transactions");

            var pendingTransactions = _blockchain.PendingTransactions
                .Select(TransactionDto.FromTransaction)
                .ToList();

            _logger.LogInformation("Retrieved {Count} pending transactions", pendingTransactions.Count);
            return Ok(pendingTransactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending transactions");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }
}
