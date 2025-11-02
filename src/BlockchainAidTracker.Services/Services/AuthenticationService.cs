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

    public AuthenticationService(
        IUserRepository userRepository,
        IPasswordService passwordService,
        ITokenService tokenService,
        IDigitalSignatureService digitalSignatureService)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _digitalSignatureService = digitalSignatureService ?? throw new ArgumentNullException(nameof(digitalSignatureService));
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
            EncryptedPrivateKey = privateKey, // TODO: Encrypt with user password
            RefreshToken = null,
            RefreshTokenExpiresAt = null,
            IsActive = true,
            CreatedTimestamp = DateTime.UtcNow,
            UpdatedTimestamp = DateTime.UtcNow
        };

        // Save user to database (AddAsync saves automatically)
        await _userRepository.AddAsync(user);

        // Generate tokens
        var (accessToken, accessTokenExpiresAt) = _tokenService.GenerateAccessToken(
            user.Id, user.Username, user.Email, user.Role.ToString());
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

        // Generate tokens
        var (accessToken, accessTokenExpiresAt) = _tokenService.GenerateAccessToken(
            user.Id, user.Username, user.Email, user.Role.ToString());
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
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new ArgumentException("Username is required", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("Email is required", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Password is required", nameof(request));
        }

        if (request.Password.Length < 8)
        {
            throw new ArgumentException("Password must be at least 8 characters long", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw new ArgumentException("Full name is required", nameof(request));
        }

        // Basic email validation
        if (!request.Email.Contains('@'))
        {
            throw new ArgumentException("Invalid email format", nameof(request));
        }
    }
}
