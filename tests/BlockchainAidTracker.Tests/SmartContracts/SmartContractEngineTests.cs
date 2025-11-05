using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.SmartContracts;
using BlockchainAidTracker.SmartContracts.Engine;
using BlockchainAidTracker.SmartContracts.Interfaces;
using BlockchainAidTracker.SmartContracts.Models;
using FluentAssertions;

namespace BlockchainAidTracker.Tests.SmartContracts;

public class SmartContractEngineTests
{
    private class TestContract : SmartContract
    {
        public override string Name => "Test Contract";
        public override string Description => "A test contract";

        public TestContract(string id = "test-contract-1") : base(id) { }

        public override bool CanExecute(ContractExecutionContext context)
        {
            return context.Transaction.Type == TransactionType.ShipmentCreated;
        }

        public override async Task<ContractExecutionResult> ExecuteAsync(ContractExecutionContext context)
        {
            await Task.CompletedTask;
            var output = new Dictionary<string, object> { { "executed", true } };
            var stateChanges = new Dictionary<string, object> { { "executionCount", 1 } };
            return ContractExecutionResult.SuccessResult(output, stateChanges);
        }
    }

    [Fact]
    public void Constructor_ShouldInitializeEngine()
    {
        // Act
        var engine = new SmartContractEngine();

        // Assert
        engine.Should().NotBeNull();
        engine.GetAllContracts().Should().BeEmpty();
    }

    [Fact]
    public void DeployContract_ShouldAddContractToEngine()
    {
        // Arrange
        var engine = new SmartContractEngine();
        var contract = new TestContract();

        // Act
        var result = engine.DeployContract(contract);

        // Assert
        result.Should().BeTrue();
        engine.GetAllContracts().Should().ContainSingle();
        engine.GetContract(contract.ContractId).Should().Be(contract);
    }

    [Fact]
    public void DeployContract_WithNullContract_ShouldThrowException()
    {
        // Arrange
        var engine = new SmartContractEngine();

        // Act
        var act = () => engine.DeployContract(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DeployContract_WithDuplicateId_ShouldReturnFalse()
    {
        // Arrange
        var engine = new SmartContractEngine();
        var contract1 = new TestContract("contract-1");
        var contract2 = new TestContract("contract-1");
        engine.DeployContract(contract1);

        // Act
        var result = engine.DeployContract(contract2);

        // Assert
        result.Should().BeFalse();
        engine.GetAllContracts().Should().ContainSingle();
    }

    [Fact]
    public void UndeployContract_ShouldRemoveContract()
    {
        // Arrange
        var engine = new SmartContractEngine();
        var contract = new TestContract();
        engine.DeployContract(contract);

        // Act
        var result = engine.UndeployContract(contract.ContractId);

        // Assert
        result.Should().BeTrue();
        engine.GetAllContracts().Should().BeEmpty();
        engine.GetContract(contract.ContractId).Should().BeNull();
    }

    [Fact]
    public void UndeployContract_WithNonExistentId_ShouldReturnFalse()
    {
        // Arrange
        var engine = new SmartContractEngine();

        // Act
        var result = engine.UndeployContract("non-existent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void UndeployContract_WithNullOrEmptyId_ShouldThrowException()
    {
        // Arrange
        var engine = new SmartContractEngine();

        // Act & Assert
        engine.Invoking(e => e.UndeployContract(null!)).Should().Throw<ArgumentException>();
        engine.Invoking(e => e.UndeployContract("")).Should().Throw<ArgumentException>();
        engine.Invoking(e => e.UndeployContract("   ")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetContract_ShouldReturnDeployedContract()
    {
        // Arrange
        var engine = new SmartContractEngine();
        var contract = new TestContract("my-contract");
        engine.DeployContract(contract);

        // Act
        var retrieved = engine.GetContract("my-contract");

        // Assert
        retrieved.Should().Be(contract);
    }

    [Fact]
    public void GetContract_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var engine = new SmartContractEngine();

        // Act
        var result = engine.GetContract("non-existent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetContract_WithNullOrEmptyId_ShouldThrowException()
    {
        // Arrange
        var engine = new SmartContractEngine();

        // Act & Assert
        engine.Invoking(e => e.GetContract(null!)).Should().Throw<ArgumentException>();
        engine.Invoking(e => e.GetContract("")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetAllContracts_ShouldReturnAllDeployedContracts()
    {
        // Arrange
        var engine = new SmartContractEngine();
        var contract1 = new TestContract("contract-1");
        var contract2 = new TestContract("contract-2");
        engine.DeployContract(contract1);
        engine.DeployContract(contract2);

        // Act
        var contracts = engine.GetAllContracts();

        // Assert
        contracts.Should().HaveCount(2);
        contracts.Should().Contain(contract1);
        contracts.Should().Contain(contract2);
    }

    [Fact]
    public async Task ExecuteContractAsync_ShouldExecuteContract()
    {
        // Arrange
        var engine = new SmartContractEngine();
        var contract = new TestContract();
        engine.DeployContract(contract);

        var transaction = new Transaction(TransactionType.ShipmentCreated, "sender-key", "{}");
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await engine.ExecuteContractAsync(contract.ContractId, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Output.Should().ContainKey("executed");
        result.Output["executed"].Should().Be(true);
    }

    [Fact]
    public async Task ExecuteContractAsync_ShouldUpdateContractState()
    {
        // Arrange
        var engine = new SmartContractEngine();
        var contract = new TestContract();
        engine.DeployContract(contract);

        var transaction = new Transaction(TransactionType.ShipmentCreated, "sender-key", "{}");
        var context = new ContractExecutionContext(transaction);

        // Act
        await engine.ExecuteContractAsync(contract.ContractId, context);
        var state = engine.GetContractState(contract.ContractId);

        // Assert
        state.Should().NotBeNull();
        state.Should().ContainKey("executionCount");
        state!["executionCount"].Should().Be(1);
    }

    [Fact]
    public async Task ExecuteContractAsync_WithNonExistentContract_ShouldReturnFailure()
    {
        // Arrange
        var engine = new SmartContractEngine();
        var transaction = new Transaction(TransactionType.ShipmentCreated, "sender-key", "{}");
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await engine.ExecuteContractAsync("non-existent", context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ExecuteContractAsync_WhenCannotExecute_ShouldReturnFailure()
    {
        // Arrange
        var engine = new SmartContractEngine();
        var contract = new TestContract();
        engine.DeployContract(contract);

        var transaction = new Transaction(TransactionType.StatusUpdated, "sender-key", "{}");
        var context = new ContractExecutionContext(transaction);

        // Act
        var result = await engine.ExecuteContractAsync(contract.ContractId, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cannot execute");
    }

    [Fact]
    public async Task ExecuteContractAsync_WithNullOrEmptyId_ShouldThrowException()
    {
        // Arrange
        var engine = new SmartContractEngine();
        var transaction = new Transaction(TransactionType.ShipmentCreated, "sender-key", "{}");
        var context = new ContractExecutionContext(transaction);

        // Act & Assert
        await engine.Awaiting(e => e.ExecuteContractAsync(null!, context))
            .Should().ThrowAsync<ArgumentException>();
        await engine.Awaiting(e => e.ExecuteContractAsync("", context))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteContractAsync_WithNullContext_ShouldThrowException()
    {
        // Arrange
        var engine = new SmartContractEngine();
        var contract = new TestContract();
        engine.DeployContract(contract);

        // Act & Assert
        await engine.Awaiting(e => e.ExecuteContractAsync(contract.ContractId, null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteApplicableContractsAsync_ShouldExecuteAllApplicableContracts()
    {
        // Arrange
        var engine = new SmartContractEngine();
        var contract1 = new TestContract("contract-1");
        var contract2 = new TestContract("contract-2");
        engine.DeployContract(contract1);
        engine.DeployContract(contract2);

        var transaction = new Transaction(TransactionType.ShipmentCreated, "sender-key", "{}");
        var context = new ContractExecutionContext(transaction);

        // Act
        var results = await engine.ExecuteApplicableContractsAsync(context);

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
    }

    [Fact]
    public async Task ExecuteApplicableContractsAsync_ShouldOnlyExecuteApplicableContracts()
    {
        // Arrange
        var engine = new SmartContractEngine();
        var contract1 = new TestContract("contract-1"); // Can execute ShipmentCreated
        var contract2 = new TestContract("contract-2"); // Can execute ShipmentCreated
        engine.DeployContract(contract1);
        engine.DeployContract(contract2);

        var transaction = new Transaction(TransactionType.StatusUpdated, "sender-key", "{}");
        var context = new ContractExecutionContext(transaction);

        // Act
        var results = await engine.ExecuteApplicableContractsAsync(context);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteApplicableContractsAsync_WithNullContext_ShouldThrowException()
    {
        // Arrange
        var engine = new SmartContractEngine();

        // Act & Assert
        await engine.Awaiting(e => e.ExecuteApplicableContractsAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void GetContractState_ShouldReturnContractState()
    {
        // Arrange
        var engine = new SmartContractEngine();
        var contract = new TestContract();
        contract.UpdateState(new Dictionary<string, object> { { "key", "value" } });
        engine.DeployContract(contract);

        // Act
        var state = engine.GetContractState(contract.ContractId);

        // Assert
        state.Should().NotBeNull();
        state.Should().ContainKey("key");
        state!["key"].Should().Be("value");
    }

    [Fact]
    public void GetContractState_WithNonExistentContract_ShouldReturnNull()
    {
        // Arrange
        var engine = new SmartContractEngine();

        // Act
        var state = engine.GetContractState("non-existent");

        // Assert
        state.Should().BeNull();
    }
}
