namespace BlockchainAidTracker.Core.Models;

/// <summary>
/// Represents a user in the blockchain aid tracking system
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Username for authentication
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// Hashed password (using BCrypt or PBKDF2)
    /// </summary>
    public string PasswordHash { get; set; }

    /// <summary>
    /// User's public key for blockchain operations (ECDSA)
    /// </summary>
    public string PublicKey { get; set; }

    /// <summary>
    /// User's encrypted private key (encrypted with user password)
    /// </summary>
    public string EncryptedPrivateKey { get; set; }

    /// <summary>
    /// User's role in the system
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// First name
    /// </summary>
    public string FirstName { get; set; }

    /// <summary>
    /// Last name
    /// </summary>
    public string LastName { get; set; }

    /// <summary>
    /// Organization name (e.g., NGO, Logistics Company)
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// Phone number (optional)
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Whether the user account is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Timestamp when the user was created (UTC)
    /// </summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>
    /// Timestamp when the user was last updated (UTC)
    /// </summary>
    public DateTime UpdatedTimestamp { get; set; }

    /// <summary>
    /// Last login timestamp (UTC)
    /// </summary>
    public DateTime? LastLoginTimestamp { get; set; }

    /// <summary>
    /// Refresh token for JWT authentication
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Expiration timestamp for the refresh token (UTC)
    /// </summary>
    public DateTime? RefreshTokenExpiresAt { get; set; }

    /// <summary>
    /// Default constructor - initializes a new user with default values
    /// </summary>
    public User()
    {
        Id = Guid.NewGuid().ToString();
        Username = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
        PublicKey = string.Empty;
        EncryptedPrivateKey = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        Role = UserRole.Recipient;
        IsActive = true;
        CreatedTimestamp = DateTime.UtcNow;
        UpdatedTimestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Parameterized constructor for creating a user with specific details
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="email">Email address</param>
    /// <param name="passwordHash">Hashed password</param>
    /// <param name="publicKey">Public key</param>
    /// <param name="encryptedPrivateKey">Encrypted private key</param>
    /// <param name="role">User role</param>
    /// <param name="firstName">First name</param>
    /// <param name="lastName">Last name</param>
    /// <param name="organization">Optional organization</param>
    /// <param name="phoneNumber">Optional phone number</param>
    public User(
        string username,
        string email,
        string passwordHash,
        string publicKey,
        string encryptedPrivateKey,
        UserRole role,
        string firstName,
        string lastName,
        string? organization = null,
        string? phoneNumber = null)
    {
        Id = Guid.NewGuid().ToString();
        Username = username ?? throw new ArgumentNullException(nameof(username));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        PublicKey = publicKey ?? throw new ArgumentNullException(nameof(publicKey));
        EncryptedPrivateKey = encryptedPrivateKey ?? throw new ArgumentNullException(nameof(encryptedPrivateKey));
        Role = role;
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
        Organization = organization;
        PhoneNumber = phoneNumber;
        IsActive = true;
        CreatedTimestamp = DateTime.UtcNow;
        UpdatedTimestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the full name of the user
    /// </summary>
    /// <returns>Full name</returns>
    public string GetFullName()
    {
        return $"{FirstName} {LastName}";
    }

    /// <summary>
    /// Updates the last login timestamp
    /// </summary>
    public void UpdateLastLogin()
    {
        LastLoginTimestamp = DateTime.UtcNow;
        UpdatedTimestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the user account
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedTimestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the user account
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedTimestamp = DateTime.UtcNow;
    }
}
