namespace HealthCarePlus.API.DTOs;

public class LogAdherenceDto
{
    public string PrescriptionId { get; set; } = null!;
    public string MedicationId { get; set; } = null!;
    public DateTime ScheduledTime { get; set; }
    public string Status { get; set; } = "Taken"; // Taken, Missed, Late
    public string? Notes { get; set; }
}

public class RefillRequestDto
{
    public string PrescriptionId { get; set; } = null!;
    public string Notes { get; set; } = null!;
}