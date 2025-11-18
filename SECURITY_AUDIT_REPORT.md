# COMPREHENSIVE SECURITY AUDIT REPORT
## Blockchain Aid Tracker - .NET 9.0 Application

**Audit Date:** November 18, 2025
**Scope:** Full codebase security review
**Thoroughness Level:** Very Thorough

---

## EXECUTIVE SUMMARY

The security audit identified **13 critical/high severity vulnerabilities** requiring immediate remediation before production deployment. The most critical issues involve disabled cryptographic signature validation, hardcoded secrets, weak session management, and incomplete input validation. Authentication and authorization controls are partially implemented but have gaps.

---

## VULNERABILITIES BY CATEGORY

### 1. CRYPTOGRAPHY & SIGNATURE VALIDATION ISSUES

#### 🔴 CRITICAL: Disabled Transaction Signature Validation
**File:** `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Api/Program.cs`
**Lines:** 190-191
**Severity:** CRITICAL
**Description:** Transaction signature validation is disabled globally, allowing unsigned or forged transactions to be added to the blockchain without verification. This completely undermines the integrity of the blockchain.
```csharp
blockchain.ValidateTransactionSignatures = false; // TODO: Enable when private keys are properly managed
blockchain.ValidateBlockSignatures = false; // Block validator signatures not yet implemented
```
**Impact:** 
- Transactions can be tampered with and added to blockchain
- No way to verify transaction authenticity
- Complete loss of blockchain immutability guarantees
- Violates core blockchain security principles

**Recommended Fix:**
- Enable signature validation: `blockchain.ValidateTransactionSignatures = true;`
- Implement proper private key management using secure key vault services
- Ensure all users have valid key pairs in the database
- Add unit tests to verify signature validation is enforced

---

#### 🔴 CRITICAL: Hardcoded Validator Password
**File:** `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Api/Program.cs`
**Line:** 38
**Severity:** CRITICAL
**Description:** Validator private key decryption password is hardcoded with a default weak password in the configuration.
```csharp
ValidatorPassword = builder.Configuration["ConsensusSettings:ValidatorPassword"] ?? "ValidatorPassword123!",
```

**Also appears in:**
- `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Services/Configuration/ConsensusSettings.cs` (Line 31)
- `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Api/appsettings.json` (Line 39)
- `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Api/appsettings.Testing.json` (Line 32)

**Impact:**
- Anyone with access to code/configuration can decrypt validator private keys
- Validators can be impersonated
- Blocks can be forged with validator signatures
- Consensus mechanism is completely compromised

**Recommended Fix:**
- Remove hardcoded password from configuration files and code
- Store validator passwords in secure key management system (Azure Key Vault, AWS KMS, HashiCorp Vault)
- Use per-validator unique passwords
- Never commit secrets to version control
- Add .gitignore rules for secrets files

---

#### 🟡 HIGH: Weak Hardcoded JWT Secret
**File:** `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Api/appsettings.json`
**Line:** 14
**Severity:** HIGH
**Description:** JWT secret key is hardcoded and insufficient in strength. Current key "YourSecretKeyHere-ChangeInProduction-MustBe32CharsMinimum" is only 60 characters and explicitly marked as placeholder.
```json
"SecretKey": "YourSecretKeyHere-ChangeInProduction-MustBe32CharsMinimum",
```

**Also appears in:**
- `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Api/appsettings.Testing.json` (Line 10) with "TestSecretKey..."

**Impact:**
- If JWT secret is known/compromised, attackers can forge valid tokens
- Tokens could have arbitrary claims injected
- Complete authentication bypass possible
- All user authorization is compromised

**Recommended Fix:**
- Generate strong random secret (minimum 256 bits for HMAC-SHA256)
- Store in secure configuration management system
- Use different secrets for dev/test/prod environments
- Rotate secrets regularly
- Never commit secrets to version control
- Use environment variables or secure vaults for secret storage

---

### 2. AUTHENTICATION & AUTHORIZATION ISSUES

#### 🔴 CRITICAL: Missing Authentication on Consensus Status Endpoint
**File:** `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Api/Controllers/ConsensusController.cs`
**Lines:** 45-88 (GetConsensusStatus method)
**Severity:** CRITICAL
**Description:** The `/api/consensus/status` endpoint has no `[Authorize]` attribute, exposing sensitive blockchain and validator information to unauthenticated users.
```csharp
[HttpGet("status")]  // No [Authorize] attribute
public async Task<ActionResult<ConsensusStatusDto>> GetConsensusStatus()
```

**Impact:**
- Unauthenticated users can query blockchain state
- Next validator information disclosed
- Pending transaction count revealed
- Enables reconnaissance attacks
- May violate compliance requirements

**Recommended Fix:**
- Add `[Authorize]` attribute to the method
- Consider role-based access (Admin/Validator only)
- Audit all public endpoints to ensure authentication

---

#### 🔴 CRITICAL: Missing Authentication on Active Validators Endpoint
**File:** `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Api/Controllers/ConsensusController.cs`
**Lines:** 282-317 (GetActiveValidators method)
**Severity:** CRITICAL
**Description:** The `/api/consensus/validators` endpoint has no authentication requirements, exposing active validator list.
```csharp
[HttpGet("validators")]  // No [Authorize] attribute
public async Task<ActionResult> GetActiveValidators()
```

**Impact:**
- Validators can be targeted for attacks
- Network topology exposed
- Enables network-level reconnaissance
- DoS targets identified

**Recommended Fix:**
- Add `[Authorize]` or `[Authorize(Roles = "Administrator,Validator")]` attribute

---

#### 🟡 HIGH: Session Management Without Token Blacklist
**File:** Multiple authentication files
**Severity:** HIGH
**Description:** JWT tokens are not invalidated on logout. The logout endpoint only returns a message telling clients to remove tokens; server-side revocation is not implemented.

**File:** `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Api/Controllers/AuthenticationController.cs`
**Lines:** 169-177
```csharp
[HttpPost("logout")]
[Authorize]
public ActionResult Logout()
{
    var username = User.Identity?.Name;
    _logger.LogInformation("User {Username} logged out", username ?? "Unknown");
    return Ok(new { message = "Logged out successfully. Please remove tokens from client storage." });
}
```

**Impact:**
- Compromised tokens remain valid until expiration
- Logout is ineffective against token replay attacks
- Users cannot revoke access if device is stolen
- Violates security best practices

**Recommended Fix:**
- Implement token blacklist/revocation mechanism
- Store revoked tokens in Redis or database with expiration
- Check blacklist on every token validation
- Add token revocation timestamp to claims

---

#### 🟡 HIGH: Weak Password Validation
**File:** `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Services/Services/AuthenticationService.cs`
**Lines:** 292-295
**Severity:** HIGH
**Description:** Password complexity requirements are minimal (only 8 characters, no special characters required).
```csharp
if (request.Password.Length < 8)
{
    throw new BusinessException("Password must be at least 8 characters long");
}
```

**Also appears in:** `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Services/DTOs/Validator/CreateValidatorRequest.cs` (Line 21)

**Impact:**
- Users can set weak passwords (e.g., "password1")
- Vulnerable to brute force attacks
- Dictionary attacks more likely to succeed
- Doesn't meet modern password policy standards (NIST, OWASP)

**Recommended Fix:**
- Require minimum 12-16 characters
- Require uppercase, lowercase, digits, and special characters
- Implement password strength meter
- Check against common password lists
- Consider passphrase requirements

---

#### 🟡 HIGH: Overly Permissive CORS Configuration
**File:** `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Api/Program.cs`
**Lines:** 95-108
**Severity:** HIGH
**Description:** CORS allows all HTTP methods (`AllowAnyMethod()`) and all headers. Combined with `AllowCredentials()`, this enables cross-origin requests to make authenticated calls.
```csharp
policy.WithOrigins(allowedOrigins)
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials()
```

**Impact:**
- Enables CSRF attacks if credentials are in cookies
- Broad method allowance could enable unintended operations
- Custom header validation bypassed
- Violates least privilege principle

**Recommended Fix:**
- Restrict methods to required ones: `.AllowAnyMethod()` → `.WithMethods("GET", "POST", "PUT")`
- Specify allowed headers explicitly: `.AllowAnyHeader()` → `.WithHeaders("Content-Type", "Authorization")`
- Only allow credentials if necessary
- Implement CSRF tokens for state-changing operations

---

### 3. INJECTION VULNERABILITIES

#### 🟡 HIGH: Insufficient Input Validation on Shipment Data
**File:** `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Services/DTOs/Shipment/CreateShipmentRequest.cs`
**Lines:** 10-13
**Severity:** HIGH
**Description:** Request DTOs lack validation attributes. Origin, Destination, and RecipientId have no length limits or required validations.
```csharp
public string Origin { get; set; } = string.Empty;
public string Destination { get; set; } = string.Empty;
public string RecipientId { get; set; } = string.Empty;
public DateTime ExpectedDeliveryDate { get; set; }
public List<ShipmentItemDto> Items { get; set; } = new();
```

**Impact:**
- Oversized inputs could cause buffer overflows or DoS
- Arbitrary string injection possible
- No validation prevents malformed data entry
- Invalid recipient IDs not caught until database layer

**Recommended Fix:**
- Add `[Required]` and `[StringLength]` attributes
- Validate dates (not in past, reasonable future limit)
- Validate recipient ID format/existence
- Implement input sanitization on service layer

---

#### 🟡 MEDIUM: Missing Validation on User Request DTOs
**File:** `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Services/DTOs/Authentication/RegisterRequest.cs`
**Lines:** 8-14
**Severity:** MEDIUM
**Description:** RegisterRequest has no validation attributes for email format, password strength, or field lengths.
```csharp
public class RegisterRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Organization { get; set; }
    public string Role { get; set; } = string.Empty;
}
```

**Basic validation in service only:**
- Email validation just checks for '@' character (Line 309 in AuthenticationService)
- No minimum length checks on most fields

**Impact:**
- Invalid data reaches database
- Email validation is insufficient
- Username length not limited
- No XSS protection on name fields

**Recommended Fix:**
- Add `[Required]`, `[EmailAddress]`, `[StringLength]` attributes
- Use `[RegularExpression]` for username validation
- Move common validation to DTOs using Data Annotations

---

### 4. DATA EXPOSURE & LOGGING ISSUES

#### 🔴 CRITICAL: Hardcoded Database Credentials
**File:** `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Api/appsettings.json`
**Line:** 11
**Severity:** CRITICAL
**Description:** PostgreSQL connection string contains hardcoded credentials in plaintext.
```json
"PostgreSQL": "Host=localhost;Database=blockchain_aid_tracker;Username=postgres;Password=postgres"
```

**Impact:**
- Database accessible to anyone with config file access
- Production database compromise likely
- Default credentials used ("postgres"/"postgres")
- GDPR/HIPAA/PCI-DSS violations

**Recommended Fix:**
- Remove credentials from config files
- Use environment variables or secure credential stores
- Implement certificate-based database authentication
- Use Azure Managed Identity or similar
- Never commit credentials to source control

---

#### 🟡 HIGH: Console.WriteLine Used for Error Logging
**File:** `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Web/Services/ApiClientService.cs`
**Lines:** 55, 71, 87, 102
**Severity:** HIGH
**Description:** Exception messages are logged directly to console, potentially exposing sensitive information.
```csharp
Console.WriteLine($"GET request failed: {ex.Message}");
Console.WriteLine($"POST request failed: {ex.Message}");
Console.WriteLine($"PUT request failed: {ex.Message}");
Console.WriteLine($"DELETE request failed: {ex.Message}");
```

**Impact:**
- Error details visible in console/server logs
- Stack traces could expose system information
- API URLs and parameters may be logged
- Information available to attackers with server access

**Recommended Fix:**
- Replace with `ILogger<>` dependency injection
- Use structured logging (Serilog, NLog)
- Log sensitive details to secure, restricted logs
- Return generic error messages to clients

---

#### 🟡 MEDIUM: AllowedHosts Wildcard Configuration
**File:** `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Api/appsettings.json`
**Line:** 8
**Severity:** MEDIUM
**Description:** AllowedHosts set to "*" allows requests from any host, disabling Host header validation.
```json
"AllowedHosts": "*",
```

**Also in:** `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Api/appsettings.Testing.json` (Line 8)

**Impact:**
- Host header injection attacks possible
- Cache poisoning attacks enabled
- Bypasses host-based access controls
- Session fixation attacks possible

**Recommended Fix:**
- Specify exact allowed hosts: `"AllowedHosts": "localhost,blockchain-api.example.com,api.example.com"`
- Match production domain names exactly
- Use different config for each environment

---

### 5. API SECURITY

#### 🟡 MEDIUM: Missing Input Validation on Query Parameters
**File:** `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Api/Controllers/ShipmentController.cs`
**Lines:** 117-126
**Severity:** MEDIUM
**Description:** Query parameters for filtering have no validation. RecipientId could be any string.
```csharp
public async Task<ActionResult<List<ShipmentDto>>> GetShipments(
    [FromQuery] ShipmentStatus? status = null,
    [FromQuery] string? recipientId = null)
```

**Impact:**
- Invalid IDs cause unhandled exceptions
- Potential information disclosure through error messages
- Performance impact from invalid queries
- May enable injection attacks on downstream systems

**Recommended Fix:**
- Validate recipientId is valid GUID format
- Add maximum query string length validation
- Implement query parameter allowlisting
- Return specific error messages for invalid input

---

#### 🟡 MEDIUM: Insufficient Authorization Check on Shipment List
**File:** `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Api/Controllers/ShipmentController.cs`
**Lines:** 113-141
**Severity:** MEDIUM
**Description:** `GetShipments` endpoint returns all shipments regardless of user role. No filtering by user's role or ownership.

**Impact:**
- Users can see all shipments in system
- Violates role-based access control
- Donors could see other donors' shipments
- Information disclosure about all operations

**Recommended Fix:**
- Filter shipments by user role:
  - Coordinators/Admins: see all
  - Recipients: see only assigned shipments
  - Donors: see only funded shipments
  - Logistics Partners: see assigned routes
- Add authorization check in ShipmentService
- Return 403 Forbidden if user lacks access

---

### 6. CONFIGURATION & DEPLOYMENT ISSUES

#### 🟡 MEDIUM: PostgreSQL Credentials Hardcoded in Dev/Test
**File:** Multiple appsettings files
**Severity:** MEDIUM
**Description:** Even development and testing configurations contain hardcoded credentials using default postgres user.

**Impact:**
- Credentials could leak through version control history
- Test data exposed if credentials shared
- Violates principle of least privilege

**Recommended Fix:**
- Use Docker Compose with environment variable injection
- Create database with restricted credentials for tests
- Use in-memory databases for testing
- Never commit real credentials even in test configs

---

#### 🟡 MEDIUM: JWT Token Expiration Configuration Weak
**File:** `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Api/appsettings.json`
**Lines:** 17-18
**Severity:** MEDIUM
**Description:** Access tokens expire after 60 minutes, which is acceptable, but refresh tokens expire after 7 days, which is excessive.
```json
"ExpirationMinutes": 60,
"RefreshTokenExpirationDays": 7
```

**Impact:**
- 7-day compromise window if refresh token is stolen
- Tokens could be used for extended periods
- Doesn't meet zero-trust security principles

**Recommended Fix:**
- Reduce refresh token expiration to 24-72 hours
- Implement token rotation on refresh
- Implement sliding window for refresh token expiration
- Add refresh token rotation and revocation

---

### 7. MISSING SECURITY CONTROLS

#### 🟡 HIGH: No Rate Limiting Implementation
**File:** Application-wide
**Severity:** HIGH
**Description:** No rate limiting configured on any endpoints. Enables brute force attacks on authentication and DoS.

**Impact:**
- Brute force attacks on login endpoint
- Password reset abuse
- Token refresh spam
- Consensus endpoint DoS possible
- Resource exhaustion attacks

**Recommended Fix:**
- Implement rate limiting middleware
- Limit login attempts (5 failed attempts = 15 minute lockout)
- Limit refresh token requests (10 per hour)
- Implement sliding window rate limiting
- Consider using AspNetCoreRateLimit or similar NuGet package

---

#### 🟡 MEDIUM: No Input Sanitization for XSS Prevention
**File:** Blazor Web UI components
**Severity:** MEDIUM
**Description:** While Blazor has built-in protections, user input fields display data without explicit sanitization markup.

**Recommended Fix:**
- Use `@((MarkupString)sanitizedHtml)` only for trusted HTML
- Implement server-side input sanitization
- Use HTML encoder for all user input display
- Consider AntiXSS library for complex scenarios

---

#### 🟡 MEDIUM: No CSRF Token Implementation
**File:** API Controllers
**Severity:** MEDIUM
**Description:** No CSRF tokens visible in Blazor forms. RESTful API + SPA may be vulnerable.

**Impact:**
- Cross-site request forgery attacks possible
- State-changing operations could be triggered from external sites
- User session hijacking for form submissions

**Recommended Fix:**
- Implement RequestVerificationToken in Blazor forms
- Validate on API side with AntiForgeryToken attributes
- Use SameSite cookie attribute
- Implement custom CSRF middleware if needed

---

#### 🟡 MEDIUM: No Security Headers Configured
**File:** `/home/user/blockchain-aid-tracker/src/BlockchainAidTracker.Api/Program.cs`
**Severity:** MEDIUM
**Description:** No security headers (HSTS, CSP, X-Frame-Options, etc.) configured.

**Impact:**
- Clickjacking attacks possible
- XSS attacks have broader impact
- No HTTPS enforcement
- Browser security features not leveraged

**Recommended Fix:**
- Add HSTS header middleware
- Configure Content Security Policy
- Set X-Frame-Options: DENY
- Set X-Content-Type-Options: nosniff
- Set X-XSS-Protection: 1; mode=block

---

## SUMMARY TABLE

| # | Category | Severity | Issue | File | Line(s) |
|---|----------|----------|-------|------|---------|
| 1 | Cryptography | CRITICAL | Disabled Signature Validation | Program.cs | 190-191 |
| 2 | Cryptography | CRITICAL | Hardcoded Validator Password | Program.cs, appsettings.json | 38, 39 |
| 3 | Cryptography | HIGH | Weak JWT Secret | appsettings.json | 14 |
| 4 | Auth | CRITICAL | Missing Auth on Consensus Status | ConsensusController.cs | 45-88 |
| 5 | Auth | CRITICAL | Missing Auth on Validators Endpoint | ConsensusController.cs | 282-317 |
| 6 | Auth | HIGH | No Token Blacklist/Revocation | AuthenticationController.cs | 169-177 |
| 7 | Auth | HIGH | Weak Password Validation | AuthenticationService.cs | 292-295 |
| 8 | CORS | HIGH | Overly Permissive CORS | Program.cs | 95-108 |
| 9 | Input | HIGH | Insufficient Input Validation | CreateShipmentRequest.cs | 10-13 |
| 10 | Input | MEDIUM | Missing DTO Validation | RegisterRequest.cs | 8-14 |
| 11 | Data | CRITICAL | Hardcoded DB Credentials | appsettings.json | 11 |
| 12 | Logging | HIGH | Console.WriteLine for Errors | ApiClientService.cs | 55,71,87,102 |
| 13 | Config | MEDIUM | AllowedHosts Wildcard | appsettings.json | 8 |
| 14 | API | MEDIUM | Missing Query Parameter Validation | ShipmentController.cs | 117-126 |
| 15 | API | MEDIUM | Missing RBAC on Shipment List | ShipmentController.cs | 113-141 |
| 16 | Config | MEDIUM | Hardcoded Test Credentials | appsettings files | Multiple |
| 17 | Auth | MEDIUM | Excessive Refresh Token TTL | appsettings.json | 18 |
| 18 | Controls | HIGH | No Rate Limiting | N/A | N/A |
| 19 | Controls | MEDIUM | No CSRF Token Implementation | N/A | N/A |
| 20 | Controls | MEDIUM | No Security Headers | Program.cs | N/A |

---

## REMEDIATION ROADMAP (Priority Order)

### Phase 1: CRITICAL (Fix Immediately)
1. **Enable Transaction Signature Validation** (Program.cs:190)
   - Estimated effort: 2 hours
   - Risk of delay: CRITICAL

2. **Remove Hardcoded Passwords from Config** (appsettings.json:39, Program.cs:38)
   - Estimated effort: 4 hours
   - Risk of delay: CRITICAL

3. **Remove Database Credentials from Config** (appsettings.json:11)
   - Estimated effort: 3 hours
   - Risk of delay: CRITICAL

4. **Add Missing Authorization Endpoints** (ConsensusController.cs:45,282)
   - Estimated effort: 1 hour
   - Risk of delay: CRITICAL

### Phase 2: HIGH PRIORITY (Fix within 1 week)
5. **Implement Rate Limiting** 
   - Estimated effort: 8 hours
   - Packages: AspNetCoreRateLimit

6. **Implement Token Revocation/Blacklist**
   - Estimated effort: 6 hours
   - Consider Redis integration

7. **Fix CORS Configuration** (Program.cs:103)
   - Estimated effort: 1 hour

8. **Replace Console.WriteLine Logging** (ApiClientService.cs)
   - Estimated effort: 2 hours

9. **Implement Strong Password Policy**
   - Estimated effort: 3 hours

### Phase 3: MEDIUM PRIORITY (Fix within 2 weeks)
10. **Add Input Validation to DTOs**
    - Estimated effort: 4 hours
    - Add [Required], [StringLength], [EmailAddress] attributes

11. **Implement Security Headers Middleware**
    - Estimated effort: 2 hours

12. **Implement Row-Level Security/RBAC**
    - Estimated effort: 6 hours
    - Filter shipments and resources by user role

13. **Fix AllowedHosts Configuration**
    - Estimated effort: 1 hour

14. **Migrate Secrets to Key Vault**
    - Estimated effort: 8 hours
    - Use Azure Key Vault, AWS Secrets Manager, or HashiCorp Vault

### Phase 4: ONGOING
15. **Implement CSRF Protection**
    - Estimated effort: 4 hours

16. **Add Unit Tests for Security Controls**
    - Estimated effort: 10 hours

17. **Security Code Review Process**
    - Implement pre-commit security scanning
    - Add SonarQube or similar tool

18. **Vulnerability Management**
    - Regular dependency updates
    - Security scanning in CI/CD pipeline

---

## SECURITY TESTING RECOMMENDATIONS

1. **Dependency Scanning**
   - Use: OWASP Dependency-Check, Snyk, or WhiteSource
   - Frequency: Every build

2. **Static Analysis**
   - Use: SonarQube, Fortify, or Roslyn analyzers
   - Frequency: Every commit

3. **Dynamic Testing**
   - Use: OWASP ZAP, Burp Suite
   - Frequency: Before release

4. **Penetration Testing**
   - Hire professional firm before production
   - Frequency: At least annually

5. **Threat Modeling**
   - Implement: STRIDE or PASTA methodology
   - Review with security team

---

## COMPLIANCE CONSIDERATIONS

- **GDPR**: Database credentials exposure, insufficient data protection
- **PCI-DSS**: Hardcoded credentials, insufficient access controls
- **HIPAA**: Encryption controls, audit logging gaps
- **SOC 2**: Control environment, monitoring inadequate

---

