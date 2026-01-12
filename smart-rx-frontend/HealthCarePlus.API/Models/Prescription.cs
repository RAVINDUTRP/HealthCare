using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HealthCarePlus.API.Models;

public class Prescription
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    public string PrescriptionNumber { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string DoctorId { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string PatientId { get; set; } = null!;

    public List<Medication> Medications { get; set; } = new();
    public string Diagnosis { get; set; } = null!;
    public string Status { get; set; } = "Pending"; // Pending, Approved, Dispensed, Cancelled
    public string? DigitalSignature { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiryDate { get; set; }
    public string? Notes { get; set; }
}

public class Medication
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string DrugId { get; set; } = null!;
    public string DrugName { get; set; } = null!;
    public string Dosage { get; set; } = null!;
    public string Frequency { get; set; } = null!;
    public string Duration { get; set; } = null!;
    public int Quantity { get; set; }
    public int RefillsAllowed { get; set; }
    public string Instructions { get; set; } = null!;
}