using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Services.Configuration;
using BlockchainAidTracker.Services.Consensus;
using BlockchainAidTracker.Services.DTOs.Blockchain;
using BlockchainAidTracker.Services.DTOs.Consensus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlockchainAidTracker.Api.Controllers;

/// <summary>
/// Controller for consensus operations and automated block creation
/// </summary>
[ApiController]
[Route("api/consensus")]
[Produces("application/json")]
public class ConsensusController : ControllerBase
{
    private readonly Blockchain.Blockchain _blockchain;
    private readonly IConsensusEngine _consensusEngine;
    private readonly IValidatorRepository _validatorRepository;
    private readonly ConsensusSettings _consensusSettings;
    private readonly ILogger<ConsensusController> _logger;

    public ConsensusController(
        Blockchain.Blockchain blockchain,
        IConsensusEngine consensusEngine,
        IValidatorRepository validatorRepository,
        ConsensusSettings consensusSettings,
        ILogger<ConsensusController> logger)
    {
        _blockchain = blockchain ?? throw new ArgumentNullException(nameof(blockchain));
        _consensusEngine = consensusEngine ?? throw new ArgumentNullException(nameof(consensusEngine));
        _validatorRepository = validatorRepository ?? throw new ArgumentNullException(nameof(validatorRepository));
        _consensusSettings = consensusSettings ?? throw new ArgumentNullException(nameof(consensusSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the current consensus status
    /// </summary>
    /// <returns>Consensus status information</returns>
    /// <response code="200">Consensus status retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ConsensusStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ConsensusStatusDto>> GetConsensusStatus()
    {
        try
        {
            _logger.LogInformation("Retrieving consensus status");

            var nextValidator = await _validatorRepository.GetNextValidatorForBlockCreationAsync();
            var activeValidators = await _validatorRepository.GetActiveValidatorsAsync();
            var lastBlock = _blockchain.GetLatestBlock();

            var status = new ConsensusStatusDto
            {
                ChainHeight = _blockchain.Chain.Count,
                PendingTransactionCount = _blockchain.PendingTransactions.Count,
                NextValidatorId = nextValidator?.Id,
                NextValidatorName = nextValidator?.Name,
                ActiveValidatorCount = activeValidators.Count,
                LastBlockTimestamp = lastBlock.Timestamp,
                LastBlockHash = lastBlock.Hash,
                AutomatedBlockCreationEnabled = _consensusSettings.EnableAutomatedBlockCreation,
                BlockCreationIntervalSeconds = _consensusSettings.BlockCreationIntervalSeconds
            };

            _logger.LogInformation(
                "Consensus status: Chain height {Height}, Pending transactions {Pending}, Next validator {Validator}",
                status.ChainHeight,
                status.PendingTransactionCount,
                status.NextValidatorName ?? "None");

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving consensus status");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Manually creates a new block from pending transactions (Admin or Validator only)
    /// </summary>
    /// <param name="request">Block creation request with validator password</param>
    /// <returns>Result of block creation operation</returns>
    /// <response code="200">Block created successfully</response>
    /// <response code="400">Bad request (no pending transactions, invalid password, etc.)</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden (insufficient permissions)</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("create-block")]
    [Authorize(Roles = "Administrator,Validator")]
    [ProducesResponseType(typeof(BlockCreationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BlockCreationResultDto>> CreateBlock([FromBody] CreateBlockRequest request)
    {
        try
        {
            _logger.LogInformation("Manual block creation requested");

            // Check if there are pending transactions
            if (_blockchain.PendingTransactions.Count == 0)
            {
                _logger.LogWarning("Cannot create block: no pending transactions");
                return BadRequest(new ProblemDetails
                {
                    Title = "No Pending Transactions",
                    Detail = "Cannot create a block without pending transactions",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Get the validator
            var validator = await _validatorRepository.GetNextValidatorForBlockCreationAsync();
            if (validator == null)
            {
                _logger.LogWarning("Cannot create block: no active validators");
                return BadRequest(new ProblemDetails
                {
                    Title = "No Active Validators",
                    Detail = "No active validators available to create a block",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Create the block
            var newBlock = await _consensusEngine.CreateBlockAsync(_blockchain, request.ValidatorPassword);

            // Add the block to the blockchain
            _blockchain.AddBlock(newBlock);

            // Save blockchain to persistence if configured
            await _blockchain.SaveToPersistenceAsync();

            // Note: Validator statistics are already saved by the consensusEngine.CreateBlockAsync
            // which calls validatorRepository.Update() that automatically saves changes

            _logger.LogInformation(
                "Block #{Index} created manually. Hash: {Hash}, Validator: {Validator}, Transactions: {TxCount}",
                newBlock.Index,
                newBlock.Hash,
                validator.Name,
                newBlock.Transactions.Count);

            var result = new BlockCreationResultDto
            {
                Success = true,
                Message = "Block created successfully",
                Block = BlockDto.FromBlock(newBlock),
                ValidatorId = validator.Id,
                ValidatorName = validator.Name,
                TransactionCount = newBlock.Transactions.Count
            };

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Block creation failed");
            return BadRequest(new ProblemDetails
            {
                Title = "Block Creation Failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during block creation");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while creating the block",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Validates a specific block against consensus rules (Admin or Validator only)
    /// </summary>
    /// <param name="index">Index of the block to validate</param>
    /// <returns>Validation result</returns>
    /// <response code="200">Block validation completed</response>
    /// <response code="404">Block not found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden (insufficient permissions)</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("validate-block/{index}")]
    [Authorize(Roles = "Administrator,Validator")]
    [ProducesResponseType(typeof(ValidationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<ValidationResultDto> ValidateBlock(int index)
    {
        try
        {
            _logger.LogInformation("Validating block at index {Index}", index);

            var block = _blockchain.GetBlockByIndex(index);
            if (block == null)
            {
                _logger.LogWarning("Block at index {Index} not found", index);
                return NotFound(new ProblemDetails
                {
                    Title = "Block Not Found",
                    Detail = $"Block at index {index} does not exist",
                    Status = StatusCodes.Status404NotFound
                });
            }

            if (index == 0)
            {
                // Genesis block is always valid
                return Ok(new ValidationResultDto
                {
                    IsValid = true,
                    BlockCount = _blockchain.Chain.Count,
                    ValidatedAt = DateTime.UtcNow
                });
            }

            var previousBlock = _blockchain.GetBlockByIndex(index - 1);
            if (previousBlock == null)
            {
                return Ok(new ValidationResultDto
                {
                    IsValid = false,
                    BlockCount = _blockchain.Chain.Count,
                    Errors = new List<string> { "Previous block not found" },
                    ValidatedAt = DateTime.UtcNow
                });
            }

            var isValid = _consensusEngine.ValidateBlock(block, previousBlock);

            var result = new ValidationResultDto
            {
                IsValid = isValid,
                BlockCount = _blockchain.Chain.Count,
                Errors = isValid ? new List<string>() : new List<string> { "Block validation failed according to consensus rules" },
                ValidatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Block {Index} validation result: {IsValid}", index, isValid);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating block at index {Index}", index);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while validating the block",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets information about all active validators
    /// </summary>
    /// <returns>List of active validators</returns>
    /// <response code="200">Validators retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("validators")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetActiveValidators()
    {
        try
        {
            _logger.LogInformation("Retrieving active validators");

            var validators = await _validatorRepository.GetActiveValidatorsAsync();

            var validatorList = validators.Select(v => new
            {
                v.Id,
                v.Name,
                v.PublicKey,
                v.Priority,
                v.TotalBlocksCreated,
                v.LastBlockCreatedTimestamp
            }).ToList();

            _logger.LogInformation("Retrieved {Count} active validators", validatorList.Count);

            return Ok(validatorList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active validators");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }
}
