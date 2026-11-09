using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using R3;
using Zoo.Application;
using Zoo.Domain;
using Zoo.Headless;

if (args.Length == 0 || args.Contains("--help"))
{
    Console.WriteLine("Usage: Zoo.Headless.Runner <settings.json> [--seconds 60] [--seed 1701]");
    return args.Length == 0 ? 1 : 0;
}

try
{
    var seconds = 60d;
    int? seedOverride = null;
    for (var index = 1; index < args.Length; index += 2)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException("Missing option value: " + args[index]);
        }

        switch (args[index])
        {
            case "--seconds":
                seconds = double.Parse(args[index + 1], CultureInfo.InvariantCulture);
                break;
            case "--seed":
                seedOverride = int.Parse(args[index + 1], CultureInfo.InvariantCulture);
                break;
            default:
                throw new ArgumentException("Unknown option: " + args[index]);
        }
    }

    if (!double.IsFinite(seconds) || seconds <= 0 || seconds > 86400)
    {
        throw new ArgumentException("Duration must be in (0, 86400] seconds.");
    }

    var settings = ConfigurationJson.Load(args[0]);
    var seed = HeadlessSimulation.ResolveSeed(settings, seedOverride);
    using var session = HeadlessSimulation.Create(settings, seed);
    using var model = new ZooViewModel(session.World);
    var spawned = 0;
    using var subscription = session.World.Created.Subscribe(_ => spawned++);
    session.Start();
    var stepSeconds = Math.Round(settings.fixedDelta, 6);
    var steps = (int)Math.Ceiling(seconds / stepSeconds);
    for (var step = 0; step < steps; step++)
    {
        session.Advance(settings.fixedDelta);
    }

    var output = new
    {
        schemaVersion = settings.schemaVersion,
        physics = "BepuPhysics 2.4.0",
        seed,
        seconds = steps * stepSeconds,
        spawned,
        alive = model.Population.CurrentValue,
        deadPrey = model.DeadPrey.CurrentValue,
        deadPredators = model.DeadPredators.CurrentValue,
        entities = session.World.Entities.OrderBy(entity => entity.Id).Select(entity => new { entity.Id, entity.TypeId, position = new { x = entity.Pose.CurrentValue.Position.X, y = entity.Pose.CurrentValue.Position.Y, z = entity.Pose.CurrentValue.Position.Z } }).ToArray()
    };
    Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine("Headless simulation failed: " + exception.Message);
    return 1;
}
