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
        Func<double, string?>? houseForLongitude = null,
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
        var earthState = simulator.SolarSystemBodyState(Body.Earth);
        var positions = new List<BodyPosition>();
        for (var index = 0; index < KnownAsteroids.FixedTargets.Count; index++)
        {
            var target = KnownAsteroids.FixedTargets[index];
            var geocentricEqj = CalculateLightTimeCorrectedVector(time, stateVectors, earthState, index);
            var geocentricEcliptic = Astronomy.RotateVector(Astronomy.Rotation_EQJ_ECT(time), geocentricEqj);
            var spherical = Astronomy.SphereFromVector(geocentricEcliptic);
            positions.Add(CreatePosition(target.JsonName, spherical.lon, houseForLongitude));
        }

        return positions;
    }

    private static AstroVector CalculateLightTimeCorrectedVector(
        AstroTime observationTime,
        IReadOnlyList<StateVector> stateVectors,
        StateVector earthAtObservation,
        int asteroidIndex)
    {
        var positionFunction = new AsteroidLightTimePositionFunction(
            observationTime,
            stateVectors,
            earthAtObservation,
            asteroidIndex);

        return Astronomy.CorrectLightTravel(positionFunction, observationTime);
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

    private static BodyPosition CreatePosition(string name, double longitude, Func<double, string?>? houseForLongitude)
    {
        longitude = NormalizeDegrees(longitude);
        var sign = Zodiac.SignForLongitude(longitude);

        return new BodyPosition(
            name,
            SexagesimalDegreeFormatter.Format(longitude),
            sign.Name,
            SexagesimalDegreeFormatter.Format(sign.DegreeInSign),
            houseForLongitude?.Invoke(longitude));
    }

    private static double NormalizeDegrees(double value)
    {
        var normalized = value % 360.0;
        return normalized < 0 ? normalized + 360.0 : normalized;
    }

    private sealed class AsteroidLightTimePositionFunction : IPositionFunction
    {
        private readonly AstroTime observationTime;
        private readonly IReadOnlyList<StateVector> stateVectors;
        private readonly StateVector earthAtObservation;
        private readonly int asteroidIndex;

        public AsteroidLightTimePositionFunction(
            AstroTime observationTime,
            IReadOnlyList<StateVector> stateVectors,
            StateVector earthAtObservation,
            int asteroidIndex)
        {
            this.observationTime = observationTime;
            this.stateVectors = stateVectors;
            this.earthAtObservation = earthAtObservation;
            this.asteroidIndex = asteroidIndex;
        }

        public AstroVector Position(AstroTime time)
        {
            var simulator = new GravitySimulator(Body.Sun, observationTime, stateVectors);
            var updatedStates = new StateVector[stateVectors.Count];
            simulator.Update(time, updatedStates);
            var asteroidAtEmission = updatedStates[asteroidIndex];

            return new AstroVector(
                asteroidAtEmission.x - earthAtObservation.x,
                asteroidAtEmission.y - earthAtObservation.y,
                asteroidAtEmission.z - earthAtObservation.z,
                time);
        }
    }
}
