using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace EchoBase.Infrastructure.Data;

/// <summary>
/// Inicializa la base de datos con los datos maestros obligatorios:
/// las dos zonas físicas y los 30 puestos de trabajo según el mapa de specs.
/// </summary>
/// <remarks>
/// Los GUIDs de zonas y puestos son determinísticos y no deben modificarse
/// tras la primera ejecución en producción, ya que otras entidades pueden
/// referenciarlos.
/// </remarks>
public static class DbSeeder
{
    // ──────────────────────────────────────────────────────────────
    // GUIDs determinísticos — NO modificar tras la primera migración
    // ──────────────────────────────────────────────────────────────

    private static readonly Guid NostromoZoneId = new("a0000000-0000-0000-0000-000000000001");
    private static readonly Guid DerelictZoneId  = new("a0000000-0000-0000-0000-000000000002");

    // ── GUIDs de mesas (DockTable) ────────────────────────────
    private static readonly Guid NostromoTableId   = new("e0000000-0000-0000-0000-000000000001");
    private static readonly Guid DerelictTable1Id  = new("e0000000-0000-0000-0000-000000000002");
    private static readonly Guid DerelictTable2Id  = new("e0000000-0000-0000-0000-000000000003");
    private static readonly Guid DerelictTable3Id  = new("e0000000-0000-0000-0000-000000000004");

    // Nostromo · Lado A (N-A01 … N-A06)
    private static readonly Guid[] NostromoSideA =
    [
        new("b0000000-0000-0000-0001-000000000001"),
        new("b0000000-0000-0000-0001-000000000002"),
        new("b0000000-0000-0000-0001-000000000003"),
        new("b0000000-0000-0000-0001-000000000004"),
        new("b0000000-0000-0000-0001-000000000005"),
        new("b0000000-0000-0000-0001-000000000006"),
    ];

    // Nostromo · Lado B (N-B01 … N-B06)
    private static readonly Guid[] NostromoSideB =
    [
        new("b0000000-0000-0000-0002-000000000001"),
        new("b0000000-0000-0000-0002-000000000002"),
        new("b0000000-0000-0000-0002-000000000003"),
        new("b0000000-0000-0000-0002-000000000004"),
        new("b0000000-0000-0000-0002-000000000005"),
        new("b0000000-0000-0000-0002-000000000006"),
    ];

    // Derelict · Mesa 1 · Lado A (D-1A01 … D-1A03)
    private static readonly Guid[] DerelictTable1SideA =
    [
        new("c0000000-0000-0000-0001-000000000001"),
        new("c0000000-0000-0000-0001-000000000002"),
        new("c0000000-0000-0000-0001-000000000003"),
    ];

    // Derelict · Mesa 1 · Lado B (D-1B01 … D-1B03)
    private static readonly Guid[] DerelictTable1SideB =
    [
        new("c0000000-0000-0000-0002-000000000001"),
        new("c0000000-0000-0000-0002-000000000002"),
        new("c0000000-0000-0000-0002-000000000003"),
    ];

    // Derelict · Mesa 2 · Lado A (D-2A01 … D-2A03)
    private static readonly Guid[] DerelictTable2SideA =
    [
        new("c0000000-0000-0000-0003-000000000001"),
        new("c0000000-0000-0000-0003-000000000002"),
        new("c0000000-0000-0000-0003-000000000003"),
    ];

    // Derelict · Mesa 2 · Lado B (D-2B01 … D-2B03)
    private static readonly Guid[] DerelictTable2SideB =
    [
        new("c0000000-0000-0000-0004-000000000001"),
        new("c0000000-0000-0000-0004-000000000002"),
        new("c0000000-0000-0000-0004-000000000003"),
    ];

    // Derelict · Mesa 3 · Lado A (D-3A01 … D-3A03)
    private static readonly Guid[] DerelictTable3SideA =
    [
        new("c0000000-0000-0000-0005-000000000001"),
        new("c0000000-0000-0000-0005-000000000002"),
        new("c0000000-0000-0000-0005-000000000003"),
    ];

    // Derelict · Mesa 3 · Lado B (D-3B01 … D-3B03)
    private static readonly Guid[] DerelictTable3SideB =
    [
        new("c0000000-0000-0000-0006-000000000001"),
        new("c0000000-0000-0000-0006-000000000002"),
        new("c0000000-0000-0000-0006-000000000003"),
    ];

    private const string DefaultEquipment = "Monitor doble, teclado, ratón, silla ergonómica";

    // ──────────────────────────────────────────────────────────────
    // GUIDs determinísticos para roles — NO modificar
    // ──────────────────────────────────────────────────────────────

    private static readonly Guid BasicUserRoleId = new("d0000000-0000-0000-0000-000000000001");
    private static readonly Guid ManagerRoleId   = new("d0000000-0000-0000-0000-000000000002");
    private static readonly Guid SystemAdminRoleId = new("d0000000-0000-0000-0000-000000000003");

    // GUID del usuario de desarrollo (coincide con DevAuthHandler)
    private static readonly Guid DevUserId = new("00000000-0000-0000-0000-000000000001");

    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Inicializa las zonas y puestos de trabajo si la base de datos está vacía.
    /// Es idempotente: si ya existen datos en <c>DockZones</c> no realiza ninguna acción.
    /// </summary>
    /// <param name="context">Contexto de base de datos ya configurado y migrado.</param>
    public static async Task SeedAsync(EchoBaseDbContext context)
    {
        if (await context.DockZones.AnyAsync())
            return;

        var nostromo = new DockZone(NostromoZoneId)
        {
            Name = "Nostromo",
            Description = "Mesa corrida con 6 puestos a cada lado (6+6)",
            Orientation = ZoneOrientation.Horizontal,
            Order = 0
        };

        var derelict = new DockZone(DerelictZoneId)
        {
            Name = "Derelict",
            Description = "Tres mesas corridas con 3 puestos a cada lado en cada mesa (3+3, 3+3 y 3+3)",
            Orientation = ZoneOrientation.Vertical,
            Order = 1
        };

        context.DockZones.AddRange(nostromo, derelict);

        // ── Mesas (DockTable) ─────────────────────────────────────
        var nostromoTable  = AddTable(context, NostromoTableId,  tableKey: "N",   locator: "Mesa única 12 puestos", order: 0, nostromo);
        var derelictTable1 = AddTable(context, DerelictTable1Id, tableKey: "D-1", locator: "Mesa AiQube",           order: 0, derelict);
        var derelictTable2 = AddTable(context, DerelictTable2Id, tableKey: "D-2", locator: "Mesa central",          order: 1, derelict);
        var derelictTable3 = AddTable(context, DerelictTable3Id, tableKey: "D-3", locator: "Mesa ventanal",         order: 2, derelict);

        // ── Nostromo ──────────────────────────────────────────────
        AddDocks(context, NostromoSideA, "N-A", "Nostromo · Lado A · Posición {0}", nostromoTable,  DockSide.A);
        AddDocks(context, NostromoSideB, "N-B", "Nostromo · Lado B · Posición {0}", nostromoTable,  DockSide.B);

        // ── Derelict ──────────────────────────────────────────────
        AddDocks(context, DerelictTable1SideA, "D-1A", "Derelict · Mesa 1 · Lado A · Posición {0}", derelictTable1, DockSide.A);
        AddDocks(context, DerelictTable1SideB, "D-1B", "Derelict · Mesa 1 · Lado B · Posición {0}", derelictTable1, DockSide.B);
        AddDocks(context, DerelictTable2SideA, "D-2A", "Derelict · Mesa 2 · Lado A · Posición {0}", derelictTable2, DockSide.A);
        AddDocks(context, DerelictTable2SideB, "D-2B", "Derelict · Mesa 2 · Lado B · Posición {0}", derelictTable2, DockSide.B);
        AddDocks(context, DerelictTable3SideA, "D-3A", "Derelict · Mesa 3 · Lado A · Posición {0}", derelictTable3, DockSide.A);
        AddDocks(context, DerelictTable3SideB, "D-3B", "Derelict · Mesa 3 · Lado B · Posición {0}", derelictTable3, DockSide.B);

        await context.SaveChangesAsync();
    }

    private static DockTable AddTable(
        EchoBaseDbContext context,
        Guid id,
        string tableKey,
        string? locator,
        int order,
        DockZone zone)
    {
        var table = new DockTable(id)
        {
            TableKey = tableKey,
            Locator  = locator,
            Order    = order
        };
        table.AssignToZone(zone);
        context.DockTables.Add(table);
        return table;
    }

    private static void AddDocks(
        EchoBaseDbContext context,
        Guid[] ids,
        string codePrefix,
        string locationTemplate,
        DockTable table,
        DockSide side)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            int position = i + 1;
            var dock = new Dock(ids[i])
            {
                Code      = $"{codePrefix}{position:D2}",
                Location  = string.Format(locationTemplate, position),
                Equipment = DefaultEquipment
            };

            dock.AssignToTable(table, side);
            context.Docks.Add(dock);
        }
    }

    /// <summary>
    /// Inicializa los roles de autorización si no existen aún.
    /// Es idempotente: no duplica los roles en ejecuciones posteriores.
    /// </summary>
    /// <param name="context">Contexto de base de datos ya configurado y migrado.</param>
    public static async Task SeedRolesAsync(EchoBaseDbContext context)
    {
        var existingRoleIds = await context.Roles
            .Select(r => r.Id)
            .ToListAsync();

        if (!existingRoleIds.Contains(BasicUserRoleId))
            context.Roles.Add(new Role(BasicUserRoleId) { Name = "BasicUser" });

        if (!existingRoleIds.Contains(ManagerRoleId))
            context.Roles.Add(new Role(ManagerRoleId) { Name = "Manager" });

        if (!existingRoleIds.Contains(SystemAdminRoleId))
            context.Roles.Add(new Role(SystemAdminRoleId) { Name = "SystemAdmin" });

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Crea el usuario de desarrollo y le asigna el rol Manager si no existe aún.
    /// Solo debe llamarse en entorno de desarrollo.
    /// </summary>
    /// <param name="context">Contexto de base de datos ya configurado y migrado.</param>
    public static async Task SeedDevUserAsync(EchoBaseDbContext context)
    {
        var devUser = await context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == DevUserId);

        if (devUser is null)
        {
            devUser = new User(DevUserId) { Name = "Dev User", Email = "dev@localhost" };
            context.Users.Add(devUser);
        }

        bool hasManagerRole = devUser.Roles.Any(r => r.Id == ManagerRoleId);
        if (!hasManagerRole)
        {
            var managerRole = await context.Roles.FindAsync(ManagerRoleId);
            if (managerRole is not null)
                devUser.Roles.Add(managerRole);
        }

        bool hasSystemAdminRole = devUser.Roles.Any(r => r.Id == SystemAdminRoleId);
        if (!hasSystemAdminRole)
        {
            var systemAdminRole = await context.Roles.FindAsync(SystemAdminRoleId);
            if (systemAdminRole is not null)
                devUser.Roles.Add(systemAdminRole);
        }

        await context.SaveChangesAsync();
    }
}
