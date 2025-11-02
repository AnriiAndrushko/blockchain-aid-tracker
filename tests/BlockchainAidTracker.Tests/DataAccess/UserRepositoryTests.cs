using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Tests.Infrastructure;
using Xunit;

namespace BlockchainAidTracker.Tests.DataAccess;

public class UserRepositoryTests : DatabaseTestBase
{
    private readonly UserRepository _userRepository;

    public UserRepositoryTests()
    {
        _userRepository = new UserRepository(Context);
    }

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ShouldAddUserToDatabase()
    {
        // Arrange
        var user = TestData.CreateUser()
            .WithUsername("newuser")
            .WithEmail("newuser@test.com")
            .Build();

        // Act
        var result = await _userRepository.AddAsync(user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Username, result.Username);

        // Verify it's in database by querying with new context
        using var newContext = CreateNewContext();
        var dbUser = await newContext.Users.FindAsync(user.Id);
        Assert.NotNull(dbUser);
        Assert.Equal("newuser", dbUser.Username);
    }

    [Fact]
    public async Task AddAsync_ShouldSetTimestamps()
    {
        // Arrange
        var user = TestData.CreateUser().Build();

        // Act
        await _userRepository.AddAsync(user);

        // Assert
        Assert.NotEqual(default, user.CreatedTimestamp);
        Assert.NotEqual(default, user.UpdatedTimestamp);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ShouldReturnUser()
    {
        // Arrange
        var user = TestData.CreateUser().Build();
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _userRepository.GetByIdAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Username, result.Username);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingUser_ShouldReturnNull()
    {
        // Act
        var result = await _userRepository.GetByIdAsync("non-existing-id");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByUsernameAsync Tests

    [Fact]
    public async Task GetByUsernameAsync_ExistingUsername_ShouldReturnUser()
    {
        // Arrange
        var user = TestData.CreateUser()
            .WithUsername("uniqueuser")
            .Build();
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _userRepository.GetByUsernameAsync("uniqueuser");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("uniqueuser", result.Username);
    }

    [Fact]
    public async Task GetByUsernameAsync_NonExistingUsername_ShouldReturnNull()
    {
        // Act
        var result = await _userRepository.GetByUsernameAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByEmailAsync Tests

    [Fact]
    public async Task GetByEmailAsync_ExistingEmail_ShouldReturnUser()
    {
        // Arrange
        var user = TestData.CreateUser()
            .WithEmail("unique@test.com")
            .Build();
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _userRepository.GetByEmailAsync("unique@test.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("unique@test.com", result.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_NonExistingEmail_ShouldReturnNull()
    {
        // Act
        var result = await _userRepository.GetByEmailAsync("nonexistent@test.com");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByPublicKeyAsync Tests

    [Fact]
    public async Task GetByPublicKeyAsync_ExistingKey_ShouldReturnUser()
    {
        // Arrange
        var publicKey = "unique-public-key-123";
        var user = TestData.CreateUser()
            .WithPublicKey(publicKey)
            .Build();
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var result = await _userRepository.GetByPublicKeyAsync(publicKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(publicKey, result.PublicKey);
    }

    [Fact]
    public async Task GetByPublicKeyAsync_NonExistingKey_ShouldReturnNull()
    {
        // Act
        var result = await _userRepository.GetByPublicKeyAsync("nonexistent-key");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByRoleAsync Tests

    [Fact]
    public async Task GetByRoleAsync_ShouldReturnUsersWithSpecificRole()
    {
        // Arrange
        var coordinator1 = TestData.CreateUser()
            .WithUsername("coord1")
            .WithEmail("coord1@test.com")
            .AsCoordinator()
            .Build();

        var coordinator2 = TestData.CreateUser()
            .WithUsername("coord2")
            .WithEmail("coord2@test.com")
            .AsCoordinator()
            .Build();

        var donor = TestData.CreateUser()
            .WithUsername("donor1")
            .WithEmail("donor1@test.com")
            .AsDonor()
            .Build();

        await Context.Users.AddRangeAsync(coordinator1, coordinator2, donor);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var coordinators = await _userRepository.GetByRoleAsync(UserRole.Coordinator);

        // Assert
        Assert.Equal(2, coordinators.Count);
        Assert.All(coordinators, u => Assert.Equal(UserRole.Coordinator, u.Role));
    }

    [Fact]
    public async Task GetByRoleAsync_NoUsersWithRole_ShouldReturnEmptyList()
    {
        // Act
        var validators = await _userRepository.GetByRoleAsync(UserRole.Validator);

        // Assert
        Assert.Empty(validators);
    }

    #endregion

    #region GetActiveUsersAsync Tests

    [Fact]
    public async Task GetActiveUsersAsync_ShouldReturnOnlyActiveUsers()
    {
        // Arrange
        var activeUser1 = TestData.CreateUser()
            .WithUsername("active1")
            .WithEmail("active1@test.com")
            .Build();

        var activeUser2 = TestData.CreateUser()
            .WithUsername("active2")
            .WithEmail("active2@test.com")
            .Build();

        var inactiveUser = TestData.CreateUser()
            .WithUsername("inactive1")
            .WithEmail("inactive1@test.com")
            .AsInactive()
            .Build();

        await Context.Users.AddRangeAsync(activeUser1, activeUser2, inactiveUser);
        await Context.SaveChangesAsync();
        DetachAllEntities();

        // Act
        var activeUsers = await _userRepository.GetActiveUsersAsync();

        // Assert
        Assert.Equal(2, activeUsers.Count);
        Assert.All(activeUsers, u => Assert.True(u.IsActive));
    }

    #endregion

    #region UsernameExistsAsync Tests

    [Fact]
    public async Task UsernameExistsAsync_ExistingUsername_ShouldReturnTrue()
    {
        // Arrange
        var user = TestData.CreateUser()
            .WithUsername("existinguser")
            .Build();
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();

        // Act
        var exists = await _userRepository.UsernameExistsAsync("existinguser");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task UsernameExistsAsync_NonExistingUsername_ShouldReturnFalse()
    {
        // Act
        var exists = await _userRepository.UsernameExistsAsync("nonexistent");

        // Assert
        Assert.False(exists);
    }

    #endregion

    #region EmailExistsAsync Tests

    [Fact]
    public async Task EmailExistsAsync_ExistingEmail_ShouldReturnTrue()
    {
        // Arrange
        var user = TestData.CreateUser()
            .WithEmail("existing@test.com")
            .Build();
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();

        // Act
        var exists = await _userRepository.EmailExistsAsync("existing@test.com");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task EmailExistsAsync_NonExistingEmail_ShouldReturnFalse()
    {
        // Act
        var exists = await _userRepository.EmailExistsAsync("nonexistent@test.com");

        // Assert
        Assert.False(exists);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldUpdateUserInDatabase()
    {
        // Arrange
        var user = TestData.CreateUser().Build();
        Context.Users.Add(user);
        Context.SaveChanges();

        var originalLastLogin = user.LastLoginTimestamp;

        // Act
        user.UpdateLastLogin();
        _userRepository.Update(user);

        // Assert
        DetachAllEntities();
        var updatedUser = Context.Users.Find(user.Id);
        Assert.NotNull(updatedUser);
        Assert.NotEqual(originalLastLogin, updatedUser.LastLoginTimestamp);
    }

    #endregion

    #region Remove Tests

    [Fact]
    public void Remove_ShouldRemoveUserFromDatabase()
    {
        // Arrange
        var user = TestData.CreateUser().Build();
        Context.Users.Add(user);
        Context.SaveChanges();
        var userId = user.Id;

        // Act
        _userRepository.Remove(user);

        // Assert
        DetachAllEntities();
        var removedUser = Context.Users.Find(userId);
        Assert.Null(removedUser);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllUsers()
    {
        // Arrange
        var user1 = TestData.CreateUser().WithUsername("user1").WithEmail("user1@test.com").Build();
        var user2 = TestData.CreateUser().WithUsername("user2").WithEmail("user2@test.com").Build();
        var user3 = TestData.CreateUser().WithUsername("user3").WithEmail("user3@test.com").Build();

        await Context.Users.AddRangeAsync(user1, user2, user3);
        await Context.SaveChangesAsync();

        // Act
        var users = await _userRepository.GetAllAsync();

        // Assert
        Assert.Equal(3, users.Count);
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ShouldReturnEmptyList()
    {
        // Act
        var users = await _userRepository.GetAllAsync();

        // Assert
        Assert.Empty(users);
    }

    #endregion

    #region Database Cleanup Tests

    [Fact]
    public async Task MultipleConcurrentOperations_ShouldMaintainDataIntegrity()
    {
        // Arrange
        var users = Enumerable.Range(1, 10)
            .Select(i => TestData.CreateUser()
                .WithUsername($"user{i}")
                .WithEmail($"user{i}@test.com")
                .Build())
            .ToList();

        // Act - Add multiple users
        await _userRepository.AddRangeAsync(users);

        // Assert
        var allUsers = await _userRepository.GetAllAsync();
        Assert.Equal(10, allUsers.Count);

        // Cleanup verification
        ClearDatabase();
        var usersAfterCleanup = await _userRepository.GetAllAsync();
        Assert.Empty(usersAfterCleanup);
    }

    #endregion
}
