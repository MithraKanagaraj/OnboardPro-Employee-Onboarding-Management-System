using System.ComponentModel.DataAnnotations;

namespace OnboardPro.Models;

public sealed class Role
{
    public int Id { get; set; }

    [Required, StringLength(40)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
