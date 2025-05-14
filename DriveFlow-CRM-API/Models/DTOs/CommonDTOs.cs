using System;
using System.Collections.Generic;

namespace DriveFlow_CRM_API.Models.DTOs
{
    /// <summary>
    /// DTO representing detailed information about a student's file.
    /// </summary>
    public sealed class FileDetailsDto
    {
        /// <summary>
        /// Unique identifier of the file.
        /// </summary>
        public int FileId { get; init; }
        
        /// <summary>
        /// Current status of the file.
        /// </summary>
        public string Status { get; init; } = default!;
        
        /// <summary>
        /// Date when the scholarship started.
        /// </summary>
        public DateTime? ScholarshipStartDate { get; init; }
        
        /// <summary>
        /// Expiry date of the criminal record.
        /// </summary>
        public DateTime? CriminalRecordExpiryDate { get; init; }
        
        /// <summary>
        /// Expiry date of the medical record.
        /// </summary>
        public DateTime? MedicalRecordExpiryDate { get; init; }
        
        /// <summary>
        /// Payment details associated with the file.
        /// </summary>
        public PaymentDetailsDto? Payment { get; init; }
        
        /// <summary>
        /// Instructor details associated with the file.
        /// </summary>
        public InstructorDetailsDto? Instructor { get; init; }
        
        /// <summary>
        /// Vehicle details associated with the file.
        /// </summary>
        public VehicleDetailsDto? Vehicle { get; init; }
        
        /// <summary>
        /// List of appointments associated with the file.
        /// </summary>
        public List<AppointmentDetailsDto> Appointments { get; init; } = new();
        
        /// <summary>
        /// Count of completed appointments.
        /// </summary>
        public int AppointmentsCompleted { get; init; }
    }

    /// <summary>
    /// DTO representing payment details for a file.
    /// </summary>
    public sealed class PaymentDetailsDto
    {
        /// <summary>
        /// Indicates if the payment is for a scholarship.
        /// </summary>
        public bool ScholarshipPayment { get; init; }
        
        /// <summary>
        /// Number of sessions paid for.
        /// </summary>
        public int SessionsPayed { get; init; }
    }

    /// <summary>
    /// DTO representing instructor details.
    /// </summary>
    public sealed class InstructorDetailsDto
    {
        /// <summary>
        /// Unique identifier of the instructor.
        /// </summary>
        public string UserId { get; init; } = default!;
        
        /// <summary>
        /// First name of the instructor.
        /// </summary>
        public string? FirstName { get; init; }
        
        /// <summary>
        /// Last name of the instructor.
        /// </summary>
        public string? LastName { get; init; }
        
        /// <summary>
        /// Email address of the instructor.
        /// </summary>
        public string? Email { get; init; }
        
        /// <summary>
        /// Phone number of the instructor.
        /// </summary>
        public string? Phone { get; init; }
        
        /// <summary>
        /// Role of the instructor.
        /// </summary>
        public string Role { get; init; } = default!;
    }

    /// <summary>
    /// DTO representing vehicle details.
    /// </summary>
    public sealed class VehicleDetailsDto
    {
        /// <summary>
        /// License plate number of the vehicle.
        /// </summary>
        public string LicensePlateNumber { get; init; } = default!;
        
        /// <summary>
        /// Transmission type of the vehicle.
        /// </summary>
        public string TransmissionType { get; init; } = default!;
        
        /// <summary>
        /// Color of the vehicle.
        /// </summary>
        public string? Color { get; init; }
        
        /// <summary>
        /// Vehicle brand/manufacturer.
        /// </summary>
        public string? Brand { get; init; }
        
        /// <summary>
        /// Vehicle model.
        /// </summary>
        public string? Model { get; init; }
        
        /// <summary>
        /// Year of production.
        /// </summary>
        public int? YearOfProduction { get; init; }
        
        /// <summary>
        /// Fuel type.
        /// </summary>
        public string? FuelType { get; init; }
        
        /// <summary>
        /// Engine size in liters.
        /// </summary>
        public decimal? EngineSizeLiters { get; init; }
        
        /// <summary>
        /// Powertrain type.
        /// </summary>
        public string? PowertrainType { get; init; }
        
        /// <summary>
        /// Type of license associated with the vehicle.
        /// </summary>
        public string? Type { get; init; }
    }

    /// <summary>
    /// DTO representing appointment details.
    /// </summary>
    public sealed class AppointmentDetailsDto
    {
        /// <summary>
        /// Unique identifier of the appointment.
        /// </summary>
        public int AppointmentId { get; init; }
        
        /// <summary>
        /// Date of the appointment.
        /// </summary>
        public DateTime Date { get; init; }
        
        /// <summary>
        /// Start hour of the appointment.
        /// </summary>
        public string StartHour { get; init; } = default!;
        
        /// <summary>
        /// End hour of the appointment.
        /// </summary>
        public string EndHour { get; init; } = default!;
        
        /// <summary>
        /// Status of the appointment.
        /// </summary>
        public string Status { get; init; } = default!;
    }
} 