namespace ColdChain.Api.Services;

/// <summary>
/// Drives the simulator on a timer so telemetry keeps arriving while the
/// frontend is open, the way real devices would keep reporting.
/// </summary>
public class TelemetryBackgroundService : BackgroundService
{
    private readonly TelemetrySimulator _simulator;
    private readonly ILogger<TelemetryBackgroundService> _logger;

    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    public TelemetryBackgroundService(TelemetrySimulator simulator, ILogger<TelemetryBackgroundService> logger)
    {
        _simulator = simulator;
        _logger = logger;
    }

    // Co-authored by Claude
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _simulator.Seed();
        _logger.LogInformation("Cold-chain simulator seeded. Emitting telemetry every {Seconds}s.", Interval.TotalSeconds);

        using var timer = new PeriodicTimer(Interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                _simulator.Tick();
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }
}
