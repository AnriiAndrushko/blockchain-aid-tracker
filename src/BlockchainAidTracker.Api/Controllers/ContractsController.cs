using BlockchainAidTracker.Blockchain;
using BlockchainAidTracker.DataAccess;
using BlockchainAidTracker.Services.DTOs.SmartContract;
using BlockchainAidTracker.SmartContracts.Engine;
using BlockchainAidTracker.SmartContracts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlockchainAidTracker.Api.Controllers;

/// <summary>
/// Controller for smart contract operations
/// </summary>
[ApiController]
[Route("api/contracts")]
[Produces("application/json")]
public class ContractsController : ControllerBase
{
    private readonly SmartContractEngine _contractEngine;
    private readonly Blockchain.Blockchain _blockchain;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ContractsController> _logger;

    public ContractsController(
        SmartContractEngine contractEngine,
        Blockchain.Blockchain blockchain,
        ApplicationDbContext dbContext,
        ILogger<ContractsController> logger)
    {
        _contractEngine = contractEngine ?? throw new ArgumentNullException(nameof(contractEngine));
        _blockchain = blockchain ?? throw new ArgumentNullException(nameof(blockchain));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all deployed smart contracts
    /// </summary>
    /// <returns>List of all deployed contracts</returns>
    /// <response code="200">Contracts retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<ContractDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<List<ContractDto>> GetAllContracts()
    {
        try
        {
            _logger.LogInformation("Retrieving all deployed smart contracts");

            var contracts = _contractEngine.GetAllContracts();
            var contractDtos = new List<ContractDto>();

            foreach (var contract in contracts)
            {
                // Try to load from database
                var dbEntity = _dbContext.SmartContracts.Find(contract.ContractId);
                var dto = ContractDto.FromContractWithEntity(contract, dbEntity);
                contractDtos.Add(dto);
            }

            _logger.LogInformation("Retrieved {ContractCount} deployed contracts", contractDtos.Count);
            return Ok(contractDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contracts");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets a specific smart contract by ID
    /// </summary>
    /// <param name="contractId">Contract ID</param>
    /// <returns>Contract details</returns>
    /// <response code="200">Contract retrieved successfully</response>
    /// <response code="404">Contract not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{contractId}")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<ContractDto> GetContract(string contractId)
    {
        try
        {
            _logger.LogInformation("Retrieving smart contract {ContractId}", contractId);

            var contract = _contractEngine.GetContract(contractId);

            if (contract == null)
            {
                _logger.LogWarning("Contract {ContractId} not found", contractId);
                return NotFound(new ProblemDetails
                {
                    Title = "Contract Not Found",
                    Detail = $"Contract with ID '{contractId}' not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Try to load from database
            var dbEntity = _dbContext.SmartContracts.Find(contractId);
            var dto = ContractDto.FromContractWithEntity(contract, dbEntity);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contract {ContractId}", contractId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets the state of a specific smart contract
    /// </summary>
    /// <param name="contractId">Contract ID</param>
    /// <returns>Contract state</returns>
    /// <response code="200">Contract state retrieved successfully</response>
    /// <response code="404">Contract not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{contractId}/state")]
    [ProducesResponseType(typeof(Dictionary<string, object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public ActionResult<Dictionary<string, object>> GetContractState(string contractId)
    {
        try
        {
            _logger.LogInformation("Retrieving state for contract {ContractId}", contractId);

            var state = _contractEngine.GetContractState(contractId);

            if (state == null)
            {
                _logger.LogWarning("Contract {ContractId} not found", contractId);
                return NotFound(new ProblemDetails
                {
                    Title = "Contract Not Found",
                    Detail = $"Contract with ID '{contractId}' not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contract state for {ContractId}", contractId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Executes a smart contract for a specific transaction
    /// </summary>
    /// <param name="request">Contract execution request</param>
    /// <returns>Contract execution result</returns>
    /// <response code="200">Contract executed successfully</response>
    /// <response code="404">Contract or transaction not found</response>
    /// <response code="400">Invalid request</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("execute")]
    [Authorize]
    [ProducesResponseType(typeof(ContractExecutionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ContractExecutionResultDto>> ExecuteContract([FromBody] ExecuteContractRequest request)
    {
        try
        {
            _logger.LogInformation("Executing contract {ContractId} for transaction {TransactionId}",
                request.ContractId, request.TransactionId);

            if (string.IsNullOrWhiteSpace(request.ContractId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "Contract ID is required",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (string.IsNullOrWhiteSpace(request.TransactionId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "Transaction ID is required",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Get the transaction from the blockchain
            var transaction = _blockchain.GetTransactionById(request.TransactionId);
            if (transaction == null)
            {
                _logger.LogWarning("Transaction {TransactionId} not found", request.TransactionId);
                return NotFound(new ProblemDetails
                {
                    Title = "Transaction Not Found",
                    Detail = $"Transaction with ID '{request.TransactionId}' not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Create execution context
            var context = new ContractExecutionContext(transaction, null, request.AdditionalData);

            // Execute the contract
            var result = await _contractEngine.ExecuteContractAsync(request.ContractId, context);

            _logger.LogInformation("Contract {ContractId} execution {Result} for transaction {TransactionId}",
                request.ContractId, result.Success ? "succeeded" : "failed", request.TransactionId);

            return Ok(ContractExecutionResultDto.FromResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing contract {ContractId} for transaction {TransactionId}",
                request.ContractId, request.TransactionId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }
}
