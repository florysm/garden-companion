namespace GardenCompanion.Api.Tests.Features.Plantings;

public class CreatePlantingHandlerTests
{
    [Fact]
    public async Task Handle_CreatesPlantingAndDerivesExpectedHarvestDate()
    {
        await using var testDb = await SqliteTestDb.CreateAsync();
        await using var db = testDb.CreateContext();

        var user = TestDataFactory.CreateUser();
        var household = TestDataFactory.CreateHousehold(user);
        var householdMember = TestDataFactory.CreateHouseholdMember(household, user, HouseholdRole.Owner);
        var garden = TestDataFactory.CreateGarden(household);
        var gardenMember = TestDataFactory.CreateGardenMember(garden, user, GardenRole.Owner);
        var bed = TestDataFactory.CreateGardenBed(garden);
        var plant = TestDataFactory.CreatePlant(daysToMaturity: 80);

        db.AddRange(user, household, householdMember, garden, gardenMember, bed, plant);
        await db.SaveChangesAsync();

        var handler = new CreatePlantingHandler(db);
        var plantedDate = new DateOnly(2026, 4, 18);

        var result = await handler.Handle(
            new CreatePlantingCommand(
                garden.Id,
                user.Id,
                bed.Id,
                plant.Id,
                plantedDate,
                ExpectedHarvestDate: null,
                PlantingType.Annual,
                Quantity: 6,
                SeasonYear: null,
                SeasonType: null),
            CancellationToken.None);

        result.GardenId.Should().Be(garden.Id);
        result.GardenBedId.Should().Be(bed.Id);
        result.PlantId.Should().Be(plant.Id);
        result.ExpectedHarvestDate.Should().Be(plantedDate.AddDays(80));
        result.SeasonYear.Should().Be(2026);
        result.SeasonType.Should().Be(SeasonType.Spring);

        var planting = await db.Plantings.SingleAsync();
        planting.Quantity.Should().Be(6);
        planting.Status.Should().Be(PlantingStatus.Planted);
        planting.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Throws_WhenUserDoesNotHaveGardenAccess()
    {
        await using var testDb = await SqliteTestDb.CreateAsync();
        await using var db = testDb.CreateContext();

        var owner = TestDataFactory.CreateUser();
        var requestingUser = TestDataFactory.CreateUser();
        var household = TestDataFactory.CreateHousehold(owner);
        var householdOwner = TestDataFactory.CreateHouseholdMember(household, owner, HouseholdRole.Owner);
        var garden = TestDataFactory.CreateGarden(household);
        var bed = TestDataFactory.CreateGardenBed(garden);
        var plant = TestDataFactory.CreatePlant();

        db.AddRange(owner, requestingUser, household, householdOwner, garden, bed, plant);
        await db.SaveChangesAsync();

        var handler = new CreatePlantingHandler(db);

        var act = () => handler.Handle(
            new CreatePlantingCommand(
                garden.Id,
                requestingUser.Id,
                bed.Id,
                plant.Id,
                new DateOnly(2026, 4, 18),
                null,
                PlantingType.Annual,
                3,
                null,
                null),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{garden.Id}*");
    }

    [Fact]
    public async Task Handle_Throws_WhenBedDoesNotBelongToGarden()
    {
        await using var testDb = await SqliteTestDb.CreateAsync();
        await using var db = testDb.CreateContext();

        var user = TestDataFactory.CreateUser();
        var household = TestDataFactory.CreateHousehold(user);
        var householdMember = TestDataFactory.CreateHouseholdMember(household, user, HouseholdRole.Owner);
        var accessibleGarden = TestDataFactory.CreateGarden(household, "Accessible Garden");
        var accessibleGardenMember = TestDataFactory.CreateGardenMember(accessibleGarden, user, GardenRole.Owner);
        var otherGarden = TestDataFactory.CreateGarden(household, "Other Garden");
        var otherBed = TestDataFactory.CreateGardenBed(otherGarden, "Wrong Bed");
        var plant = TestDataFactory.CreatePlant();

        db.AddRange(
            user,
            household,
            householdMember,
            accessibleGarden,
            accessibleGardenMember,
            otherGarden,
            otherBed,
            plant);
        await db.SaveChangesAsync();

        var handler = new CreatePlantingHandler(db);

        var act = () => handler.Handle(
            new CreatePlantingCommand(
                accessibleGarden.Id,
                user.Id,
                otherBed.Id,
                plant.Id,
                new DateOnly(2026, 4, 18),
                null,
                PlantingType.Annual,
                3,
                null,
                null),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{otherBed.Id}*");
    }
}
