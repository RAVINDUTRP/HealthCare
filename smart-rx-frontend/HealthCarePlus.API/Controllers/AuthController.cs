using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;
using HealthCarePlus.API.DTOs;
using HealthCarePlus.API.Models;
using HealthCarePlus.API.Services;

namespace HealthCarePlus.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly DoctorService _doctorService;
    private readonly PharmacyService _pharmacyService;
    private readonly PatientService _patientService;
    private readonly JwtService _jwtService;
    private readonly AvatarService _avatarService;

    public AuthController(UserService userService, DoctorService doctorService, PharmacyService pharmacyService, PatientService patientService, JwtService jwtService, AvatarService avatarService)
    {
        _userService = userService;
        _doctorService = doctorService;
        _pharmacyService = pharmacyService;
        _patientService = patientService;
        _jwtService = jwtService;
        _avatarService = avatarService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (dto == null)
            return BadRequest("Invalid request data");

        if (await _userService.GetByUsernameAsync(dto.Username) != null)
            return BadRequest("Username already taken");

        if (await _userService.GetByEmailAsync(dto.Email) != null)
            return BadRequest("Email already registered");

        // Generate avatar
        var (color, emoji) = _avatarService.GenerateAvatar(dto.Username);
        var avatarUrl = _avatarService.GenerateAvatarUrl(color, emoji);

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = string.IsNullOrWhiteSpace(dto.Role) ? "User" : dto.Role,
            FirstName = dto.Username, // Default to username
            LastName = "", // Empty last name
            Phone = dto.Phone,
            DateOfBirth = dto.DateOfBirth,
            AvatarColor = color,
            AvatarEmoji = emoji,
            ProfileImageUrl = avatarUrl
        };

        var userId = await _userService.CreateAsync(user);
        if (userId == null)
            return BadRequest("Failed to create user");
        
        var token = _jwtService.GenerateToken(user.Username, user.Role, userId);
        return Ok(new AuthResponse(token, user.Username, user.Role, user.AvatarColor, user.AvatarEmoji, user.ProfileImageUrl));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _userService.GetByUsernameAsync(dto.UsernameOrEmail) ?? await _userService.GetByEmailAsync(dto.UsernameOrEmail);
        if (user == null) return Unauthorized("Invalid credentials");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)) return Unauthorized("Invalid credentials");

        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        await _userService.UpdateAsync(user.Id!, user);

        var token = _jwtService.GenerateToken(user.Username, user.Role, user.Id);
        return Ok(new AuthResponse(token, user.Username, user.Role, user.AvatarColor, user.AvatarEmoji, user.ProfileImageUrl));
    }

    // Doctor-specific registration
    [HttpPost("register-doctor")]
    public async Task<IActionResult> RegisterDoctor([FromBody] DoctorRegisterDto dto)
    {
        if (dto == null)
            return BadRequest("Invalid request data");

        // Check if license number already exists
        var existingDoctor = await _doctorService.GetDoctorByLicenseNumberAsync(dto.LicenseNumber);
        if (existingDoctor != null)
            return BadRequest("License number already registered");

        // Generate avatar
        var (color, emoji) = _avatarService.GenerateAvatar(dto.Username);
        var avatarUrl = _avatarService.GenerateAvatarUrl(color, emoji);

        // Register the doctor
        var doctorId = await _doctorService.RegisterDoctorAsync(dto);
        if (doctorId == null)
            return BadRequest("Failed to register doctor");

        // Get the created user for token generation
        var user = await _userService.GetByUsernameAsync(dto.Username);
        if (user == null)
            return BadRequest("Failed to retrieve user data");

        // Generate JWT token
        var token = _jwtService.GenerateToken(user.Username, user.Role, user.Id);
        
        // Get doctor details for response
        var doctor = await _doctorService.GetDoctorByIdAsync(doctorId);
        if (doctor == null)
            return BadRequest("Failed to retrieve doctor data");

        var response = new AuthResponse(token, user.Username, user.Role, user.AvatarColor, user.AvatarEmoji, user.ProfileImageUrl);
        response.DoctorId = doctorId;
        response.LicenseNumber = doctor.LicenseNumber;
        response.Specialization = doctor.Specialization;

        return Ok(response);
    }

    // Doctor login
    [HttpPost("login-doctor")]
    public async Task<IActionResult> LoginDoctor([FromBody] DoctorLoginDto dto)
    {
        // First authenticate as regular user
        var user = await _userService.GetByUsernameAsync(dto.UsernameOrEmail) ?? await _userService.GetByEmailAsync(dto.UsernameOrEmail);
        if (user == null) return Unauthorized("Invalid credentials");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)) return Unauthorized("Invalid credentials");

        // Check if user is a doctor
        if (user.Role != "Doctor")
            return Unauthorized("User is not registered as a doctor");

        // Get doctor details
        var doctor = await _doctorService.GetDoctorByUserIdAsync(user.Id);
        if (doctor == null)
            return BadRequest("Doctor profile not found");

        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        await _userService.UpdateAsync(user.Id!, user);

        var token = _jwtService.GenerateToken(user.Username, user.Role, user.Id);
        
        var response = new AuthResponse(token, user.Username, user.Role, user.AvatarColor, user.AvatarEmoji, user.ProfileImageUrl);
        response.DoctorId = doctor.Id;
        response.LicenseNumber = doctor.LicenseNumber;
        response.Specialization = doctor.Specialization;

        return Ok(response);
    }

    // Pharmacist-specific registration
    [HttpPost("register-pharmacist")]
    public async Task<IActionResult> RegisterPharmacist([FromBody] PharmacistRegisterDto dto)
    {
        if (dto == null)
            return BadRequest("Invalid request data");

        // Register the pharmacist
        var pharmacistId = await _pharmacyService.RegisterPharmacyAsync(dto);
        if (pharmacistId == null)
            return BadRequest("Failed to register pharmacist or user already exists");

        // Get the created user for token generation
        var user = await _userService.GetByUsernameAsync(dto.Username);
        if (user == null)
            return BadRequest("Failed to retrieve user data");

        // Generate JWT token
        var token = _jwtService.GenerateToken(user.Username, user.Role, user.Id);
        
        // Get pharmacy details for response
        var pharmacy = await _pharmacyService.GetPharmacyByIdAsync(pharmacistId);
        if (pharmacy == null)
            return BadRequest("Failed to retrieve pharmacy data");

        var response = new AuthResponse(token, user.Username, user.Role, user.AvatarColor, user.AvatarEmoji, user.ProfileImageUrl);
        response.PharmacyId = pharmacistId;
        response.PharmacyName = pharmacy.PharmacyName;
        response.PharmacyLicenseNumber = pharmacy.LicenseNumber;

        return Ok(response);
    }

    // Pharmacist login
    [HttpPost("login-pharmacist")]
    public async Task<IActionResult> LoginPharmacist([FromBody] PharmacistLoginDto dto)
    {
        // First authenticate as regular user
        var user = await _userService.GetByUsernameAsync(dto.UsernameOrEmail) ?? await _userService.GetByEmailAsync(dto.UsernameOrEmail);
        if (user == null) return Unauthorized("Invalid credentials");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)) return Unauthorized("Invalid credentials");

        // Check if user is a pharmacist
        if (user.Role != "Pharmacist")
            return Unauthorized("User is not registered as a pharmacist");

        // Get pharmacy details
        var pharmacy = await _pharmacyService.GetPharmacyByUserIdAsync(user.Id);
        if (pharmacy == null)
            return BadRequest("Pharmacy profile not found");

        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        await _userService.UpdateAsync(user.Id!, user);

        var token = _jwtService.GenerateToken(user.Username, user.Role, user.Id);
        
        var response = new AuthResponse(token, user.Username, user.Role, user.AvatarColor, user.AvatarEmoji, user.ProfileImageUrl);
        response.PharmacyId = pharmacy.Id;
        response.PharmacyName = pharmacy.PharmacyName;
        response.PharmacyLicenseNumber = pharmacy.LicenseNumber;

        return Ok(response);
    }

    // Patient-specific registration
    [HttpPost("register-patient")]
    public async Task<IActionResult> RegisterPatient([FromBody] PatientRegisterDto dto)
    {
        if (dto == null)
            return BadRequest("Invalid request data");

        // Register the patient
        var patientId = await _patientService.RegisterPatientAsync(dto);
        if (patientId == null)
            return BadRequest("Failed to register patient or user already exists");

        // Get the created user for token generation
        var user = await _userService.GetByUsernameAsync(dto.Username);
        if (user == null)
            return BadRequest("Failed to retrieve user data");

        // Generate JWT token
        var token = _jwtService.GenerateToken(user.Username, user.Role, user.Id);
        
        // Get patient details for response
        var patient = await _patientService.GetPatientByIdAsync(patientId);
        if (patient == null)
            return BadRequest("Failed to retrieve patient data");

        var response = new AuthResponse(token, user.Username, user.Role, user.AvatarColor, user.AvatarEmoji, user.ProfileImageUrl);
        response.PatientId = patientId;
        response.MedicalRecordNumber = patient.MedicalRecordNumber;
        response.BloodType = patient.BloodType;

        return Ok(response);
    }

    // Patient login
    [HttpPost("login-patient")]
    public async Task<IActionResult> LoginPatient([FromBody] PatientLoginDto dto)
    {
        // First authenticate as regular user
        var user = await _userService.GetByUsernameAsync(dto.UsernameOrEmail) ?? await _userService.GetByEmailAsync(dto.UsernameOrEmail);
        if (user == null) return Unauthorized("Invalid credentials");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)) return Unauthorized("Invalid credentials");

        // Check if user is a patient
        if (user.Role != "Patient")
            return Unauthorized("User is not registered as a patient");

        // Get patient details
        var patient = await _patientService.GetPatientByUserIdAsync(user.Id);
        if (patient == null)
            return BadRequest("Patient profile not found");

        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        await _userService.UpdateAsync(user.Id!, user);

        var token = _jwtService.GenerateToken(user.Username, user.Role, user.Id);
        
        var response = new AuthResponse(token, user.Username, user.Role, user.AvatarColor, user.AvatarEmoji, user.ProfileImageUrl);
        response.PatientId = patient.Id;
        response.MedicalRecordNumber = patient.MedicalRecordNumber;
        response.BloodType = patient.BloodType;

        return Ok(response);
    }
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        if (string.IsNullOrEmpty(dto.Email))
            return BadRequest("Email is required");

        var user = await _userService.GetByEmailAsync(dto.Email);
        if (user == null)
        {
            // Return success even if user doesn't exist for security reasons
            return Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
        }

        // Generate reset token
        var resetToken = Guid.NewGuid().ToString();
        var resetTokenExpiry = DateTime.UtcNow.AddHours(1); // Token expires in 1 hour

        // Save reset token to user
        user.PasswordResetToken = resetToken;
        user.PasswordResetTokenExpiry = resetTokenExpiry;
        await _userService.UpdateAsync(user.Id!, user);

        // In a real application, you would send an email here
        // For now, we'll return the token in the response for testing purposes
        // TODO: Implement email service to send reset link
        
        return Ok(new { 
            message = "If an account with that email exists, a password reset link has been sent.",
            resetToken = resetToken, // Remove this in production
            resetLink = $"/reset-password?token={resetToken}&email={user.Email}"
        });
    }

    // Reset Password
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Token) || string.IsNullOrEmpty(dto.NewPassword))
            return BadRequest("Email, token, and new password are required");

        if (dto.NewPassword.Length < 6)
            return BadRequest("Password must be at least 6 characters long");

        var user = await _userService.GetByEmailAsync(dto.Email);
        if (user == null)
            return BadRequest("Invalid reset request");

        // Validate reset token
        if (user.PasswordResetToken != dto.Token || user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            return BadRequest("Invalid or expired reset token");

        // Update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetToken = null; // Clear the reset token
        user.PasswordResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        
        await _userService.UpdateAsync(user.Id!, user);

        return Ok(new { message = "Password has been reset successfully" });
    }
}
