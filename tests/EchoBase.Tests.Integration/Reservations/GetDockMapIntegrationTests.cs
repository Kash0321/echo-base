using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Reservations.Queries;
using EchoBase.Tests.Integration.Infrastructure;

namespace EchoBase.Tests.Integration.Reservations;

/// <summary>
/// Tests de integración para <see cref="GetDockMapQuery"/>.
/// Verifican que la consulta del mapa de bahías recupera correctamente
/// la orientación de zona y los localizadores de mesa desde la base de datos.
/// </summary>
public sealed class GetDockMapIntegrationTests : IntegrationTestBase
{
    // ── IT-DM-01 ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetDockMap_ReturnsZonesWithOrientationFromDatabase()
    {
        // Act
        var result = await Mediator.Send(new GetDockMapQuery(Today));

        // Assert
        var nostromo = result.Zones.Single(z => z.Name == "Nostromo");
        var derelict = result.Zones.Single(z => z.Name == "Derelict");
        Assert.Equal(ZoneOrientation.Horizontal, nostromo.Orientation);
        Assert.Equal(ZoneOrientation.Vertical,   derelict.Orientation);
    }

    // ── IT-DM-02 ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetDockMap_ReturnsTablesWithLocatorFromDockTableRecords()
    {
        // Act
        var result = await Mediator.Send(new GetDockMapQuery(Today));

        // Assert
        var nostromoTable = result.Zones.Single(z => z.Name == "Nostromo").Tables[0];
        Assert.Equal("Mesa única 12 puestos", nostromoTable.Locator);

        var derelictTables = result.Zones.Single(z => z.Name == "Derelict").Tables;
        Assert.Equal("Mesa AiQube",   derelictTables[0].Locator);
        Assert.Equal("Mesa central",  derelictTables[1].Locator);
        Assert.Equal("Mesa ventanal", derelictTables[2].Locator);
    }

    // ── IT-DM-03 ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetDockMap_ReturnsQueriedDateInDto()
    {
        // Act
        var result = await Mediator.Send(new GetDockMapQuery(Today));

        // Assert
        Assert.Equal(Today, result.Date);
    }
}
