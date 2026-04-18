using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Domain.Entities;

public class GardenMember
{
    public Guid Id { get; set; }
    public Guid GardenId { get; set; }
    public Guid UserId { get; set; }
    public GardenRole Role { get; set; } = GardenRole.Contributor;
    public Guid? InvitedByUserId { get; set; }

    // Navigation
    public Garden Garden { get; set; } = null!;
    public User User { get; set; } = null!;
    public User? InvitedBy { get; set; }
}
