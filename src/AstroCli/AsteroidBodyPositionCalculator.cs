using CosineKitty;

namespace AstroCli;

public sealed class AsteroidBodyPositionCalculator
{
    private readonly IHorizonsClient horizonsClient;

    public AsteroidBodyPositionCalculator(IHorizonsClient horizonsClient)
    {
        this.horizonsClient = horizonsClient;
    }

    public async Task<IReadOnlyList<BodyPosition>> CalculateAsync(
        DateTimeOffset at,
        CancellationToken cancellationToken = default)
    {
        var time = new AstroTime(at.ToUniversalTime().UtcDateTime);
        var stateVectors = new List<StateVector>();

        foreach (var target in KnownAsteroids.FixedTargets)
        {
            var state = await horizonsClient.GetStateVectorAsync(target, at, cancellationToken).ConfigureAwait(false);
            stateVectors.Add(ToStateVector(state, time));
        }

        var simulator = new GravitySimulator(Body.Sun, time, stateVectors);
        var updatedStates = new StateVector[stateVectors.Count];
        simulator.Update(time, updatedStates);

        var earthState = simulator.SolarSystemBodyState(Body.Earth);
        var positions = new List<BodyPosition>();
        for (var index = 0; index < KnownAsteroids.FixedTargets.Count; index++)
        {
            var target = KnownAsteroids.FixedTargets[index];
            var geocentricEqj = new AstroVector(
                updatedStates[index].x - earthState.x,
                updatedStates[index].y - earthState.y,
                updatedStates[index].z - earthState.z,
                time);
            var geocentricEcliptic = Astronomy.RotateVector(Astronomy.Rotation_EQJ_ECT(time), geocentricEqj);
            var spherical = Astronomy.SphereFromVector(geocentricEcliptic);
            positions.Add(CreatePosition(target.JsonName, spherical.lon));
        }

        return positions;
    }

    private static StateVector ToStateVector(HorizonsStateVector state, AstroTime time)
    {
        return new StateVector(
            state.X,
            state.Y,
            state.Z,
            state.Vx,
            state.Vy,
            state.Vz,
            time);
    }

    private static BodyPosition CreatePosition(string name, double longitude)
    {
        longitude = NormalizeDegrees(longitude);
        var sign = Zodiac.SignForLongitude(longitude);

        return new BodyPosition(
            name,
            SexagesimalDegreeFormatter.Format(longitude),
            sign.Name,
            SexagesimalDegreeFormatter.Format(sign.DegreeInSign));
    }

    private static double NormalizeDegrees(double value)
    {
        var normalized = value % 360.0;
        return normalized < 0 ? normalized + 360.0 : normalized;
    }
}
