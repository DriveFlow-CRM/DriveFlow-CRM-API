using DriveFlow_CRM_API.Controllers;
using DriveFlow_CRM_API.Models.DTOs;          
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
    [JsonSerializable(typeof(FileDetailsDto))]
    [JsonSerializable(typeof(PaymentDetailsDto))]
    [JsonSerializable(typeof(InstructorDetailsDto))]
    [JsonSerializable(typeof(VehicleDetailsDto))]
    [JsonSerializable(typeof(AppointmentDetailsDto))]

    // ───────────────────────── COUNTY CONTROLLER ─────────────────────────
    [JsonSerializable(typeof(CountyCreateDto))]
    [JsonSerializable(typeof(CountyDto))]

    // ───────────────────────── CITY CONTROLLER ─────────────────────────
    [JsonSerializable(typeof(CityCreateDto))]
    [JsonSerializable(typeof(CityDto))]

    // ───────────────────────── ADDRESS CONTROLLER ─────────────────────────
    [JsonSerializable(typeof(AddressCreateDto))]
    [JsonSerializable(typeof(AddressDto))]
    [JsonSerializable(typeof(AddressUpdateDto))]

    // ─────────────────────── AUTOSCHOOL CONTROLLER ───────────────────────
    [JsonSerializable(typeof(SchoolAdminInfoDto))]
    [JsonSerializable(typeof(AutoSchoolDto))]
    [JsonSerializable(typeof(AutoSchoolCreateDto))]
    [JsonSerializable(typeof(AutoSchoolInnerDto))]
    [JsonSerializable(typeof(SchoolAdminCreateDto))]
    [JsonSerializable(typeof(AutoSchoolUpdateDto))]
    [JsonSerializable(typeof(SchoolAdminUpdateDto))]
    [JsonSerializable(typeof(CreateAutoSchoolDto))]
    [JsonSerializable(typeof(NewAutoSchoolDto))]
    [JsonSerializable(typeof(NewSchoolAdminDto))]
    [JsonSerializable(typeof(MoneyDto))]
    [JsonSerializable(typeof(SchoolKpisDto))]

    // ───────────────────────── VEHICLE CONTROLLER ─────────────────────────
    [JsonSerializable(typeof(VehicleDto))]
    [JsonSerializable(typeof(VehicleCreateDto))]
    [JsonSerializable(typeof(VehicleUpdateDto))]

    // ───────────────────────── LICENSE CONTROLLER ─────────────────────────
    [JsonSerializable(typeof(LicenseDto))]
    [JsonSerializable(typeof(LicenseCreateDto))]
    [JsonSerializable(typeof(LicenseUpdateDto))]


    // ───────────────────────── REQUEST CONTROLLER ─────────────────────────
    [JsonSerializable(typeof(CreateRequestDto))]
    [JsonSerializable(typeof(FetchRequestDto))]
    [JsonSerializable(typeof(UpdateRequestDto))]

    // ──────────────────────-──── FILE CONTROLLER ──-───────────────────────
    [JsonSerializable(typeof(CreateFileDto))]
    [JsonSerializable(typeof(CreateFileResponseDto))]
    [JsonSerializable(typeof(StudentFileRecordsDto))]
    [JsonSerializable(typeof(StudentFileDataDto))]
    [JsonSerializable(typeof(StudentFileDataInstructorDto))]
    [JsonSerializable(typeof(StudentDataDto))]
    [JsonSerializable(typeof(FileVehicleDto))]
    [JsonSerializable(typeof(FilePaymentDto))]
    [JsonSerializable(typeof(EditFileDto))]

    // ───────────────────── AUTOSCHOOLPAGE CONTROLLER ─────────────────────
    [JsonSerializable(typeof(AutoSchoolLandingDto))]
    [JsonSerializable(typeof(AutoSchoolDetailsDto))]
    [JsonSerializable(typeof(AddressDetailsDto))]
    [JsonSerializable(typeof(TeachingCategoryDetailsDto))]
    [JsonSerializable(typeof(SchoolVehicleDto))]
    [JsonSerializable(typeof(List<AutoSchoolLandingDto>))]
    [JsonSerializable(typeof(List<SchoolVehicleDto>))]
    [JsonSerializable(typeof(List<TeachingCategoryDetailsDto>))]

    // ───────────────────── TEACHINGCATEGORY CONTROLLER ─────────────────────
    [JsonSerializable(typeof(TeachingCategoryResponseDto))]
    [JsonSerializable(typeof(TeachingCategoryCreateDto))]
    [JsonSerializable(typeof(TeachingCategoryUpdateDto))]
    [JsonSerializable(typeof(List<TeachingCategoryResponseDto>))]

    // ───────────────────── INSTRUCTOR CONTROLLER ─────────────────────
    [JsonSerializable(typeof(InstructorAssignedFileDto))]
    [JsonSerializable(typeof(List<InstructorAssignedFileDto>))]
    [JsonSerializable(typeof(InstructorFileDetailsDto))]
    [JsonSerializable(typeof(InstructorAppointmentDto))]
    [JsonSerializable(typeof(List<InstructorAppointmentDto>))]
    [JsonSerializable(typeof(Bucket))]
    [JsonSerializable(typeof(List<Bucket>))]
    [JsonSerializable(typeof(StudentItemAgg))]
    [JsonSerializable(typeof(List<StudentItemAgg>))]
    [JsonSerializable(typeof(List<(int,int)>))]
    [JsonSerializable(typeof(InstructorCohortStatsDto))]

    // ───────────────────── INSTRUCTOR CATEGORIES CONTROLLER ─────────────────────
    [JsonSerializable(typeof(InstructorTeachingCategoryResponseDto))]
    [JsonSerializable(typeof(List<InstructorTeachingCategoryResponseDto>))]
    [JsonSerializable(typeof(TeachingCategoryInstructorResponseDto))]
    [JsonSerializable(typeof(List<TeachingCategoryInstructorResponseDto>))]
    [JsonSerializable(typeof(InstructorTeachingCategoryLinkDto))]

    // ───────────────────── INSTRUCTOR AVAILABILITY CONTROLLER ─────────────────────
    [JsonSerializable(typeof(InstructorAvailabilityDto))]
    [JsonSerializable(typeof(List<InstructorAvailabilityDto>))]
    [JsonSerializable(typeof(CreateInstructorAvailabilityDto))]

    // ───────────────────────── AI CONTROLLER ─────────────────────────
    [JsonSerializable(typeof(ChatRequest))]
    [JsonSerializable(typeof(ChatMessage))]
    [JsonSerializable(typeof(List<ChatMessage>))]
    [JsonSerializable(typeof(AiStudentContextResponse))]  // Used internally by AiContextBuilder
    [JsonSerializable(typeof(StudentContextDto))]
    [JsonSerializable(typeof(StudentSummaryDto))]
    [JsonSerializable(typeof(CategoryProgressDto))]
    [JsonSerializable(typeof(List<CategoryProgressDto>))]
    [JsonSerializable(typeof(OverallProgressDto))]
    [JsonSerializable(typeof(MistakeSummaryDto))]
    [JsonSerializable(typeof(List<MistakeSummaryDto>))]
    [JsonSerializable(typeof(SessionEvaluationDto))]
    [JsonSerializable(typeof(List<SessionEvaluationDto>))]
    [JsonSerializable(typeof(MistakeDetailDto))]
    [JsonSerializable(typeof(List<MistakeDetailDto>))]
    [JsonSerializable(typeof(SessionHighlightDto))]
    [JsonSerializable(typeof(List<SessionHighlightDto>))]
    [JsonSerializable(typeof(DataAvailabilityDto))]

    internal partial class AppJsonContext : JsonSerializerContext
    {
    }
}
