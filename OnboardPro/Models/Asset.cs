using System.ComponentModel.DataAnnotations;

namespace OnboardPro.Models;

public sealed class Asset
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string Category { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string SerialNumber { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Notes { get; set; }

    public bool IsRetired { get; set; }
    public ICollection<AssetAllocation> Allocations { get; set; } = new List<AssetAllocation>();
}
