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
    private readonly IAuditLogService _auditLogService;

    public AuthenticationService(
        IUserRepository userRepository,
        IPasswordService passwordService,
        ITokenService tokenService,
        IDigitalSignatureService digitalSignatureService,
        IKeyManagementService keyManagementService,
        TransactionSigningContext signingContext,
        IAuditLogService auditLogService)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _digitalSignatureService = digitalSignatureService ?? throw new ArgumentNullException(nameof(digitalSignatureService));
        _keyManagementService = keyManagementService ?? throw new ArgumentNullException(nameof(keyManagementService));
        _signingContext = signingContext ?? throw new ArgumentNullException(nameof(signingContext));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    public async Task<AuthenticationResponse> RegisterAsync(RegisterRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        try
        {
            // Validate request
            ValidateRegisterRequest(request);

            // Check if username already exists
            if (await _userRepository.UsernameExistsAsync(request.Username))
            {
                await _auditLogService.LogFailureAsync(
                    AuditLogCategory.Authentication,
                    AuditLogAction.UserRegistered,
                    $"Registration failed: Username '{request.Username}' already exists",
                    "Username already taken",
                    username: request.Username);
                throw new BusinessException($"Username '{request.Username}' is already taken");
            }

            // Check if email already exists
            if (await _userRepository.EmailExistsAsync(request.Email))
            {
                await _auditLogService.LogFailureAsync(
                    AuditLogCategory.Authentication,
                    AuditLogAction.UserRegistered,
                    $"Registration failed: Email '{request.Email}' already registered",
                    "Email already registered",
                    username: request.Username);
                throw new BusinessException($"Email '{request.Email}' is already registered");
            }

            // Generate cryptographic key pair for the user
            var (publicKey, privateKey) = _digitalSignatureService.GenerateKeyPair();

            // Hash the password
            var passwordHash = _passwordService.HashPassword(request.Password);

            // Encrypt the private key with the user's password
            var encryptedPrivateKey = _keyManagementService.EncryptPrivateKey(privateKey, request.Password);

            // Parse full name into first and last name
            var nameParts = request.FullName.Trim().Split(' ', 2);
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

            // Create user entity
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                FirstName = firstName,
                LastName = lastName,
                Organization = request.Organization,
                Role = UserRole.Recipient, // Default role
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
                user.Id, user.Username, user.Email, user.Role.ToString());
            var (refreshToken, refreshTokenExpiresAt) = _tokenService.GenerateRefreshToken();

            // Update user with refresh token
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiresAt = refreshTokenExpiresAt;
            _userRepository.Update(user);

            // Log successful registration
            await _auditLogService.LogAsync(
                AuditLogCategory.Authentication,
                AuditLogAction.UserRegistered,
                $"User '{user.Username}' registered successfully",
                user.Id,
                user.Username,
                user.Id,
                "User");

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
        catch (BusinessException)
        {
            throw; // Re-throw business exceptions (already logged)
        }
        catch (Exception ex)
        {
            await _auditLogService.LogFailureAsync(
                AuditLogCategory.Authentication,
                AuditLogAction.UserRegistered,
                $"Registration failed for user '{request.Username}'",
                ex.Message,
                username: request.Username);
            throw;
        }
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

        try
        {
            // Find user by username or email
            var user = await _userRepository.GetByUsernameOrEmailAsync(request.UsernameOrEmail);

            if (user == null)
            {
                await _auditLogService.LogFailureAsync(
                    AuditLogCategory.Authentication,
                    AuditLogAction.UserLoggedIn,
                    $"Login failed: User '{request.UsernameOrEmail}' not found",
                    "Invalid credentials",
                    username: request.UsernameOrEmail);
                throw new UnauthorizedException("Invalid username/email or password");
            }

            if (!user.IsActive)
            {
                await _auditLogService.LogFailureAsync(
                    AuditLogCategory.Authentication,
                    AuditLogAction.UserLoggedIn,
                    $"Login failed: User '{user.Username}' account is inactive",
                    "Account inactive",
                    user.Id,
                    user.Username);
                throw new UnauthorizedException("User account is inactive");
            }

            // Verify password
            if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            {
                await _auditLogService.LogFailureAsync(
                    AuditLogCategory.Authentication,
                    AuditLogAction.UserLoggedIn,
                    $"Login failed: Invalid password for user '{user.Username}'",
                    "Invalid credentials",
                    user.Id,
                    user.Username);
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
                user.Id, user.Username, user.Email, user.Role.ToString());
            var (refreshToken, refreshTokenExpiresAt) = _tokenService.GenerateRefreshToken();

            // Update user with refresh token
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiresAt = refreshTokenExpiresAt;
            user.UpdatedTimestamp = DateTime.UtcNow;
            _userRepository.Update(user);

            // Log successful login
            await _auditLogService.LogAsync(
                AuditLogCategory.Authentication,
                AuditLogAction.UserLoggedIn,
                $"User '{user.Username}' logged in successfully",
                user.Id,
                user.Username,
                user.Id,
                "User");

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
        catch (UnauthorizedException)
        {
            throw; // Re-throw unauthorized exceptions (already logged)
        }
        catch (Exception ex)
        {
            await _auditLogService.LogFailureAsync(
                AuditLogCategory.Authentication,
                AuditLogAction.UserLoggedIn,
                $"Login failed for user '{request.UsernameOrEmail}'",
                ex.Message,
                username: request.UsernameOrEmail);
            throw;
        }
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

        try
        {
            var userId = await ValidateRefreshTokenAsync(request.RefreshToken);

            if (userId == null)
            {
                await _auditLogService.LogFailureAsync(
                    AuditLogCategory.Authentication,
                    AuditLogAction.TokenRefreshed,
                    "Token refresh failed: Invalid or expired refresh token",
                    "Invalid refresh token");
                throw new UnauthorizedException("Invalid or expired refresh token");
            }

            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null || !user.IsActive)
            {
                await _auditLogService.LogFailureAsync(
                    AuditLogCategory.Authentication,
                    AuditLogAction.TokenRefreshed,
                    "Token refresh failed: User not found or inactive",
                    "User not found or inactive",
                    userId);
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

            // Log successful token refresh
            await _auditLogService.LogAsync(
                AuditLogCategory.Authentication,
                AuditLogAction.TokenRefreshed,
                $"Token refreshed for user '{user.Username}'",
                user.Id,
                user.Username,
                user.Id,
                "User");

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
        catch (UnauthorizedException)
        {
            throw; // Re-throw unauthorized exceptions (already logged)
        }
        catch (Exception ex)
        {
            await _auditLogService.LogFailureAsync(
                AuditLogCategory.Authentication,
                AuditLogAction.TokenRefreshed,
                "Token refresh failed",
                ex.Message);
            throw;
        }
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

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw new BusinessException("Full name is required");
        }

        // Basic email validation
        if (!request.Email.Contains('@'))
        {
            throw new BusinessException("Invalid email format");
        }
    }
}
