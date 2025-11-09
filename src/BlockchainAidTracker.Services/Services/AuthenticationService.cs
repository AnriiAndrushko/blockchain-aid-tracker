using BlockchainAidTracker.Core.Interfaces;
using BlockchainAidTracker.Core.Models;
using BlockchainAidTracker.DataAccess.Repositories;
using BlockchainAidTracker.Services.DTOs.Authentication;
using BlockchainAidTracker.Services.Exceptions;
using BlockchainAidTracker.Services.Interfaces;

namespace BlockchainAidTracker.Services.Services;

/// <summary>
/// Implementation of authentication service
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly IDigitalSignatureService _digitalSignatureService;
    private readonly IKeyManagementService _keyManagementService;
    private readonly TransactionSigningContext _signingContext;

    public AuthenticationService(
        IUserRepository userRepository,
        IPasswordService passwordService,
        ITokenService tokenService,
        IDigitalSignatureService digitalSignatureService,
        IKeyManagementService keyManagementService,
        TransactionSigningContext signingContext)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _digitalSignatureService = digitalSignatureService ?? throw new ArgumentNullException(nameof(digitalSignatureService));
        _keyManagementService = keyManagementService ?? throw new ArgumentNullException(nameof(keyManagementService));
        _signingContext = signingContext ?? throw new ArgumentNullException(nameof(signingContext));
    }

    public async Task<AuthenticationResponse> RegisterAsync(RegisterRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // Validate request
        ValidateRegisterRequest(request);

        // Check if username already exists
        if (await _userRepository.UsernameExistsAsync(request.Username))
        {
            throw new BusinessException($"Username '{request.Username}' is already taken");
        }

        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(request.Email))
        {
            throw new BusinessException($"Email '{request.Email}' is already registered");
        }

        // Generate cryptographic key pair for the user
        var (publicKey, privateKey) = _digitalSignatureService.GenerateKeyPair();

        // Hash the password
        var passwordHash = _passwordService.HashPassword(request.Password);

        // Encrypt the private key with the user's password
        var encryptedPrivateKey = _keyManagementService.EncryptPrivateKey(privateKey, request.Password);

        // Parse the role from request
        if (!Enum.TryParse<UserRole>(request.Role, out var userRole))
        {
            throw new BusinessException($"Invalid role: {request.Role}");
        }

        // Create user entity
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Username = request.Username,
            Email = request.Email,
            PasswordHash = passwordHash,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Organization = request.Organization,
            Role = userRole,
            PublicKey = publicKey,
            EncryptedPrivateKey = encryptedPrivateKey,
            RefreshToken = null,
            RefreshTokenExpiresAt = null,
            IsActive = true,
            CreatedTimestamp = DateTime.UtcNow,
            UpdatedTimestamp = DateTime.UtcNow
        };

        // Save user to database (AddAsync saves automatically)
        await _userRepository.AddAsync(user);

        // Store the decrypted private key for immediate transaction signing
        _signingContext.StorePrivateKey(user.Id, privateKey);

        // Generate tokens
        var (accessToken, accessTokenExpiresAt) = _tokenService.GenerateAccessToken(
            user.Id, user.Username, user.Email, user.Role.ToString(), user.FirstName, user.LastName);
        var (refreshToken, refreshTokenExpiresAt) = _tokenService.GenerateRefreshToken();

        // Update user with refresh token
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = refreshTokenExpiresAt;
        _userRepository.Update(user);

        return new AuthenticationResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = accessTokenExpiresAt,
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role.ToString()
        };
    }

    public async Task<AuthenticationResponse> LoginAsync(LoginRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.UsernameOrEmail))
        {
            throw new ArgumentException("Username or email is required", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Password is required", nameof(request));
        }

        // Find user by username or email
        var user = await _userRepository.GetByUsernameOrEmailAsync(request.UsernameOrEmail);

        if (user == null)
        {
            throw new UnauthorizedException("Invalid username/email or password");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("User account is inactive");
        }

        // Verify password
        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid username/email or password");
        }

        // Decrypt and store the private key for transaction signing
        try
        {
            var privateKey = _keyManagementService.DecryptPrivateKey(user.EncryptedPrivateKey, request.Password);
            _signingContext.StorePrivateKey(user.Id, privateKey);
        }
        catch (UnauthorizedAccessException)
        {
            // If decryption fails, log but don't block login (allows backward compatibility)
            // In production, you might want to handle this differently
        }

        // Generate tokens
        var (accessToken, accessTokenExpiresAt) = _tokenService.GenerateAccessToken(
            user.Id, user.Username, user.Email, user.Role.ToString(), user.FirstName, user.LastName);
        var (refreshToken, refreshTokenExpiresAt) = _tokenService.GenerateRefreshToken();

        // Update user with refresh token
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = refreshTokenExpiresAt;
        user.UpdatedTimestamp = DateTime.UtcNow;
        _userRepository.Update(user);

        return new AuthenticationResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = accessTokenExpiresAt,
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role.ToString()
        };
    }

    public async Task<AuthenticationResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new ArgumentException("Refresh token is required", nameof(request));
        }

        var userId = await ValidateRefreshTokenAsync(request.RefreshToken);

        if (userId == null)
        {
            throw new UnauthorizedException("Invalid or expired refresh token");
        }

        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedException("User not found or inactive");
        }

        // Generate new tokens
        var (accessToken, accessTokenExpiresAt) = _tokenService.GenerateAccessToken(
            user.Id, user.Username, user.Email, user.Role.ToString());
        var (refreshToken, refreshTokenExpiresAt) = _tokenService.GenerateRefreshToken();

        // Update user with new refresh token
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = refreshTokenExpiresAt;
        user.UpdatedTimestamp = DateTime.UtcNow;
        _userRepository.Update(user);

        return new AuthenticationResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = accessTokenExpiresAt,
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role.ToString()
        };
    }

    public async Task<string?> ValidateRefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var user = await _userRepository.GetByRefreshTokenAsync(refreshToken);

        if (user == null)
        {
            return null;
        }

        if (user.RefreshTokenExpiresAt == null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
        {
            return null;
        }

        return user.Id;
    }

    private void ValidateRegisterRequest(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            throw new BusinessException("First name is required");
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new BusinessException("Last name is required");
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new BusinessException("Username is required");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new BusinessException("Email is required");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new BusinessException("Password is required");
        }

        if (request.Password.Length < 8)
        {
            throw new BusinessException("Password must be at least 8 characters long");
        }

        if (string.IsNullOrWhiteSpace(request.Role))
        {
            throw new BusinessException("Role is required");
        }

        // Validate role
        if (!Enum.TryParse<UserRole>(request.Role, out _))
        {
            throw new BusinessException($"Invalid role: {request.Role}. Valid roles are: {string.Join(", ", Enum.GetNames(typeof(UserRole)))}");
        }

        // Basic email validation
        if (!request.Email.Contains('@'))
        {
            throw new BusinessException("Invalid email format");
        }
    }
}
