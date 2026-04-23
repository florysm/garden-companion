namespace GardenCompanion.Api.Tests.Features.GardenTasks;

public class CreateGardenTaskHandlerTests
{
    [Fact]
    public async Task Handle_CreatesTaskAndReturnsAssigneeDisplayName()
    {
        await using var testDb = await SqliteTestDb.CreateAsync();
        await using var db = testDb.CreateContext();

        var owner = TestDataFactory.CreateUser(displayName: "Owner");
        var assignee = TestDataFactory.CreateUser(displayName: "Helper");
        var household = TestDataFactory.CreateHousehold(owner);
        var ownerHouseholdMember = TestDataFactory.CreateHouseholdMember(household, owner, HouseholdRole.Owner);
        var assigneeHouseholdMember = TestDataFactory.CreateHouseholdMember(household, assignee, HouseholdRole.Contributor);
        var garden = TestDataFactory.CreateGarden(household);
        var ownerGardenMember = TestDataFactory.CreateGardenMember(garden, owner, GardenRole.Owner);
        var assigneeGardenMember = TestDataFactory.CreateGardenMember(garden, assignee, GardenRole.Contributor, owner.Id);

        db.AddRange(
            owner,
            assignee,
            household,
            ownerHouseholdMember,
            assigneeHouseholdMember,
            garden,
            ownerGardenMember,
            assigneeGardenMember);
        await db.SaveChangesAsync();

        var handler = new CreateGardenTaskHandler(db);

        var result = await handler.Handle(
            new CreateGardenTaskCommand(
                owner.Id,
                garden.Id,
                GardenBedId: null,
                PlantingId: null,
                AssignedToUserId: assignee.Id,
                Title: "Water seedlings",
                Description: "Check moisture levels before sunset.",
                TaskType.Water,
                DueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))),
            CancellationToken.None);

        result.GardenId.Should().Be(garden.Id);
        result.AssignedToUserId.Should().Be(assignee.Id);
        result.AssignedToDisplayName.Should().Be("Helper");
        result.Title.Should().Be("Water seedlings");
        result.TaskType.Should().Be(TaskType.Water.ToString());

        var task = await db.GardenTasks.SingleAsync();
        task.AssignedToUserId.Should().Be(assignee.Id);
        task.Title.Should().Be("Water seedlings");
    }

    [Fact]
    public async Task Handle_Throws_WhenUserDoesNotHaveGardenAccess()
    {
        await using var testDb = await SqliteTestDb.CreateAsync();
        await using var db = testDb.CreateContext();

        var owner = TestDataFactory.CreateUser();
        var outsider = TestDataFactory.CreateUser();
        var household = TestDataFactory.CreateHousehold(owner);
        var householdMember = TestDataFactory.CreateHouseholdMember(household, owner, HouseholdRole.Owner);
        var garden = TestDataFactory.CreateGarden(household);
        var gardenMember = TestDataFactory.CreateGardenMember(garden, owner, GardenRole.Owner);

        db.AddRange(owner, outsider, household, householdMember, garden, gardenMember);
        await db.SaveChangesAsync();

        var handler = new CreateGardenTaskHandler(db);

        var act = () => handler.Handle(
            new CreateGardenTaskCommand(
                outsider.Id,
                garden.Id,
                GardenBedId: null,
                PlantingId: null,
                AssignedToUserId: null,
                Title: "Inspect tomatoes",
                Description: null,
                TaskType.Inspect,
                DueDate: null),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{garden.Id}*");
    }
}
