using System.ComponentModel.DataAnnotations;

namespace OnboardPro.Models;

public enum DocumentStatus
{
    PendingReview,
    Approved,
    Rejected
}

public sealed class Document
{
    public int Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required, StringLength(100)]
    public string DocumentType { get; set; } = string.Empty;

    [Required, StringLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required, StringLength(255)]
    public string StoredFileName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    [Range(1, 10485760)]
    public long SizeBytes { get; set; }

    [StringLength(500)]
    public string? ReviewNote { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.PendingReview;
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewedByUserId { get; set; }
    public Employee Employee { get; set; } = null!;
    public ApplicationUser? ReviewedBy { get; set; }
}
