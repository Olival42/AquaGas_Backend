using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using EmployeeEntity = AquaGas.Api.Modules.Employee.Domain.Models.Employee;
using AquaGas.IntegrationTests.Common;
using AquaGas.IntegrationTests.Fixtures;

public class EmployeeConfigurationTests : BaseIntegrationTest
{
    public EmployeeConfigurationTests(PostgreSqlContainerFixture fixture)
        : base(fixture)
    {
    }

    private async Task<IEntityType> GetEntityAsync()
    {
        return await ExecuteDbContextAsync(async context =>
        {
            return context.Model.FindEntityType(typeof(EmployeeEntity))!;
        });
    }

    [Fact]
    public async Task Should_Map_Employee_Entity()
    {
        var entity = await GetEntityAsync();

        entity.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Have_Primary_Key()
    {
        var entity = await GetEntityAsync();

        var pk = entity.FindPrimaryKey();

        pk.Should().NotBeNull();
        pk!.Properties.Should().ContainSingle();

        pk.Properties[0].Name.Should().Be("Id");
    }
    [Fact]
    public async Task Should_Configure_Name_Property()
    {
        var entity = await GetEntityAsync();

        var owned = entity.GetNavigations()
            .First(x => x.Name == "Name")
            .TargetEntityType;

        var property = owned.FindProperty("Value");

        property.Should().NotBeNull();

        property!.GetColumnName().Should().Be("Name");
        property.GetMaxLength().Should().Be(150);
        property.IsNullable.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Configure_CPF_Property()
    {
        var entity = await GetEntityAsync();

        var owned = entity.GetNavigations()
            .First(x => x.Name == "CPF")
            .TargetEntityType;

        var property = owned.FindProperty("Value");

        property.Should().NotBeNull();

        property!.GetColumnName().Should().Be("CPF");
        property.GetMaxLength().Should().Be(11);
        property.IsNullable.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Configure_Email_Property()
    {
        var entity = await GetEntityAsync();

        var owned = entity.GetNavigations()
            .First(x => x.Name == "Email")
            .TargetEntityType;

        var property = owned.FindProperty("Value");

        property.Should().NotBeNull();

        property!.GetColumnName().Should().Be("Email");
        property.GetMaxLength().Should().Be(200);
        property.IsNullable.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Configure_Phone_Property()
    {
        var entity = await GetEntityAsync();

        var owned = entity.GetNavigations()
            .First(x => x.Name == "Phone")
            .TargetEntityType;

        var property = owned.FindProperty("Value");

        property.Should().NotBeNull();

        property!.GetColumnName().Should().Be("Phone");
        property.GetMaxLength().Should().Be(11);
        property.IsNullable.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Require_IsActive()
    {
        var entity = await GetEntityAsync();

        var property = entity.FindProperty(nameof(EmployeeEntity.IsActive));

        property.Should().NotBeNull();

        property!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Require_CreatedAt()
    {
        var entity = await GetEntityAsync();

        var property = entity.FindProperty(nameof(EmployeeEntity.CreatedAt));

        property.Should().NotBeNull();

        property!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Have_One_To_One_Relationship_With_User()
    {
        var entity = await GetEntityAsync();

        var navigation = entity
            .GetNavigations()
            .FirstOrDefault(x => x.Name == "User");

        navigation.Should().NotBeNull();

        var foreignKey = navigation!.ForeignKey;

        foreignKey.IsUnique.Should().BeTrue();

        foreignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
    }

    [Fact]
    public async Task Should_Create_Unique_Index_For_CPF()
    {
        var entity = await GetEntityAsync();

        var owned = entity.GetNavigations()
            .First(x => x.Name == "CPF")
            .TargetEntityType;

        var indexes = owned.GetIndexes();

        indexes.Any(x =>
                x.IsUnique &&
                x.Properties.Any(p => p.Name == "Value"))
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task Should_Create_Unique_Index_For_Email()
    {
        var entity = await GetEntityAsync();

        var owned = entity.GetNavigations()
            .First(x => x.Name == "Email")
            .TargetEntityType;

        var indexes = owned.GetIndexes();

        indexes.Any(x =>
                x.IsUnique &&
                x.Properties.Any(p => p.Name == "Value"))
            .Should()
            .BeTrue();
    }
}