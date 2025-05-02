using DriveFlow_CRM_API.Controllers;          
using System.Text.Json.Serialization;

namespace DriveFlow_CRM_API.Json
{
    /// <summary>
    /// Source-generated System.Text.Json metadata for all API DTOs.
    /// Whenever you add a new request/response type, append a
    /// [JsonSerializable] line in the matching controller section.
    /// </summary>

    // ──────────────────────────── AUTH CONTROLLER ────────────────────────────
    [JsonSerializable(typeof(LoginDto))]
    [JsonSerializable(typeof(RefreshDto))]

    // ───────────────────────── SCHOOLADMIN CONTROLLER ─────────────────────────
    [JsonSerializable(typeof(InstructorCreateDto))]
    [JsonSerializable(typeof(StudentCreateDto))]
    [JsonSerializable(typeof(StudentDto))]
    [JsonSerializable(typeof(PaymentDto))]
    [JsonSerializable(typeof(FileDto))]
    [JsonSerializable(typeof(StudentUserDto))]
    [JsonSerializable(typeof(InstructorUserDto))]
    [JsonSerializable(typeof(TeachingCategoryDto))]
    [JsonSerializable(typeof(UserListItemDto))]
    [JsonSerializable(typeof(UpdateStudentDto))]
    [JsonSerializable(typeof(UpdateInstructorDto))]

    // ───────────────────────── STUDENT CONTROLLER ─────────────────────────
    [JsonSerializable(typeof(StudentFileDto))]

    // ─────────────────────── add new controller sections below ───────────────────────
    internal partial class AppJsonContext : JsonSerializerContext
    {
    }
}
