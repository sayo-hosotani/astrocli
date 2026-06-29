namespace AstroCli;

public sealed class AsteroidPositionCalculator
{
    private readonly IHorizonsClient horizonsClient;

    public AsteroidPositionCalculator(IHorizonsClient horizonsClient)
    {
        this.horizonsClient = horizonsClient;
    }

    public async Task<AsteroidPositionOutput> CalculateAsync(
        AsteroidPositionRequest request,
        CancellationToken cancellationToken = default)
    {
        var asteroids = new List<AsteroidStateVectorOutput>();
        foreach (var target in request.Targets)
        {
            var state = await horizonsClient.GetStateVectorAsync(target, request.At, cancellationToken).ConfigureAwait(false);
            asteroids.Add(CreateOutput(target, state));
        }

        return new AsteroidPositionOutput(
            request.At.ToString("yyyy-MM-dd HH:mm:ss zzz"),
            request.At.ToUniversalTime().UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            asteroids);
    }

    private static AsteroidStateVectorOutput CreateOutput(AsteroidTarget target, HorizonsStateVector state)
    {
        return new AsteroidStateVectorOutput(
            target.Id,
            target.HorizonsCommand,
            new GravitySimulatorStateVector(
                state.Epoch.ToUniversalTime().UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                "sun",
                "EQJ",
                "AU",
                "AU/day",
                state.X,
                state.Y,
                state.Z,
                state.Vx,
                state.Vy,
                state.Vz));
    }
}

public sealed record AsteroidPositionRequest(
    DateTimeOffset At,
    IReadOnlyList<AsteroidTarget> Targets);

public sealed record AsteroidPositionOutput(
    string InputDateTime,
    string UtcDateTime,
    IReadOnlyList<AsteroidStateVectorOutput> Asteroids);

public sealed record AsteroidStateVectorOutput(
    string Id,
    string HorizonsCommand,
    GravitySimulatorStateVector StateVector);

public sealed record GravitySimulatorStateVector(
    string Epoch,
    string Origin,
    string Frame,
    string PositionUnit,
    string VelocityUnit,
    double X,
    double Y,
    double Z,
    double Vx,
    double Vy,
    double Vz);
