using System.Text.Json;

namespace AstroCli.Tests;

public class AsteroidCommandTests
{
    private const string At = "2026-06-29 22:00:00 +09:00";

    [Fact]
    public void KnownAsteroids_FixedTargets_ContainsFiveSupportedAsteroids()
    {
        var targets = KnownAsteroids.FixedTargets;

        Assert.Equal(["キロン", "セレス", "パラス", "ジュノー", "ベスタ"], targets.Select(target => target.Id));
        Assert.Equal(["chiron", "ceres", "pallas", "juno", "vesta"], targets.Select(target => target.JsonName));
        Assert.Equal(["2060;", "1;", "2;", "3;", "4;"], targets.Select(target => target.HorizonsCommand));
    }

    [Fact]
    public void HorizonsStateVectorParser_ParsesNamedVectorResponse()
    {
        var json = """
            {
              "result": "$$SOE\n X = 1.100000000000000E+00 Y = 2.200000000000000E+00 Z = 3.300000000000000E-01\n VX= 4.400000000000000E-03 VY= 5.500000000000000E-03 VZ= 6.600000000000000E-04\n$$EOE"
            }
            """;

        var vector = HorizonsStateVectorParser.ParseJson("test", DateTimeOffset.Parse("2026-06-29T13:00:00Z"), json);

        Assert.Equal(1.1, vector.X, 12);
        Assert.Equal(2.2, vector.Y, 12);
        Assert.Equal(0.33, vector.Z, 12);
        Assert.Equal(0.0044, vector.Vx, 12);
        Assert.Equal(0.0055, vector.Vy, 12);
        Assert.Equal(0.00066, vector.Vz, 12);
    }

    [Fact]
    public void HorizonsStateVectorParser_ParsesCsvVectorResponse()
    {
        var json = """
            {
              "result": "$$SOE\n2460491.041666667, A.D. 2026-Jun-29 13:00:00.0000, 1.1, 2.2, 0.33, 0.0044, 0.0055, 0.00066, 0, 0, 0\n$$EOE"
            }
            """;

        var vector = HorizonsStateVectorParser.ParseJson("test", DateTimeOffset.Parse("2026-06-29T13:00:00Z"), json);

        Assert.Equal(1.1, vector.X, 12);
        Assert.Equal(2.2, vector.Y, 12);
        Assert.Equal(0.33, vector.Z, 12);
        Assert.Equal(0.0044, vector.Vx, 12);
        Assert.Equal(0.0055, vector.Vy, 12);
        Assert.Equal(0.00066, vector.Vz, 12);
    }

    [Fact]
    public void RunAsteroid_WritesGravitySimulatorStateVectorsToStandardOutput()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var stdin = new StringReader(string.Empty);

        var exitCode = AsteroidCommand.Run(
            ["--at", At],
            stdin,
            stdout,
            stderr,
            new FakeHorizonsClient());

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());

        using var document = JsonDocument.Parse(stdout.ToString());
        var root = document.RootElement;
        Assert.Equal(At, root.GetProperty("inputDateTime").GetString());
        Assert.Equal("2026-06-29T13:00:00Z", root.GetProperty("utcDateTime").GetString());
        Assert.False(root.TryGetProperty("location", out _));

        var asteroids = root.GetProperty("asteroids").EnumerateArray().ToArray();
        Assert.Equal(5, asteroids.Length);
        Assert.Equal(["キロン", "セレス", "パラス", "ジュノー", "ベスタ"], asteroids.Select(asteroid => asteroid.GetProperty("id").GetString()));
        Assert.Equal(["2060;", "1;", "2;", "3;", "4;"], asteroids.Select(asteroid => asteroid.GetProperty("horizonsCommand").GetString()));

        foreach (var asteroid in asteroids)
        {
            Assert.False(asteroid.TryGetProperty("ra", out _));
            Assert.False(asteroid.TryGetProperty("dec", out _));
            Assert.False(asteroid.TryGetProperty("azimuth", out _));
            Assert.False(asteroid.TryGetProperty("altitude", out _));
            Assert.False(asteroid.TryGetProperty("distanceAu", out _));

            var stateVector = asteroid.GetProperty("stateVector");
            Assert.Equal("2026-06-29T13:00:00Z", stateVector.GetProperty("epoch").GetString());
            Assert.Equal("sun", stateVector.GetProperty("origin").GetString());
            Assert.Equal("EQJ", stateVector.GetProperty("frame").GetString());
            Assert.Equal("AU", stateVector.GetProperty("positionUnit").GetString());
            Assert.Equal("AU/day", stateVector.GetProperty("velocityUnit").GetString());
            Assert.Equal(1.8, stateVector.GetProperty("x").GetDouble(), 12);
            Assert.Equal(0.2, stateVector.GetProperty("y").GetDouble(), 12);
            Assert.Equal(0.1, stateVector.GetProperty("z").GetDouble(), 12);
            Assert.Equal(-0.002, stateVector.GetProperty("vx").GetDouble(), 12);
            Assert.Equal(0.006, stateVector.GetProperty("vy").GetDouble(), 12);
            Assert.Equal(0.001, stateVector.GetProperty("vz").GetDouble(), 12);
        }
    }

    [Fact]
    public void RunAsteroid_WithOutputFile_WritesJsonOutputFile()
    {
        var directory = Directory.CreateTempSubdirectory("astrocli-asteroid-test-");
        var outputPath = Path.Combine(directory.FullName, "result.json");

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = AsteroidCommand.Run(
            ["--at", At, "--output", outputPath],
            TextReader.Null,
            stdout,
            stderr,
            new FakeHorizonsClient());

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Equal(string.Empty, stderr.ToString());
        Assert.True(File.Exists(outputPath));

        using var document = JsonDocument.Parse(File.ReadAllText(outputPath));
        Assert.Equal(5, document.RootElement.GetProperty("asteroids").GetArrayLength());
    }

    [Fact]
    public void RunAsteroid_WithLocationOption_ReturnsNonZero()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = AsteroidCommand.Run(
            ["--at", At, "--location", "35°41’22″N,139°41’30″E"],
            TextReader.Null,
            stdout,
            stderr,
            new FakeHorizonsClient());

        Assert.NotEqual(0, exitCode);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("Invalid arguments", stderr.ToString());
    }

    private sealed class FakeHorizonsClient : IHorizonsClient
    {
        public Task<HorizonsStateVector> GetStateVectorAsync(
            AsteroidTarget target,
            DateTimeOffset at,
            CancellationToken cancellationToken = default)
        {
            var vector = new HorizonsStateVector(
                target.Id,
                at,
                1.8,
                0.2,
                0.1,
                -0.002,
                0.006,
                0.001);
            return Task.FromResult(vector);
        }
    }
}
