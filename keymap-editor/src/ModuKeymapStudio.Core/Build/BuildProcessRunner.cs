using System.Diagnostics;

namespace ModuKeymapStudio.Core.Build;

public sealed record BuildOutput(string Text, bool IsError);
public sealed record BuildResult(int ExitCode, bool WasCancelled, TimeSpan Duration)
{
    public bool Succeeded => ExitCode == 0 && !WasCancelled;
}

public sealed class BuildProcessRunner
{
    public async Task<BuildResult> RunAsync(
        ProcessStartInfo startInfo,
        Action<BuildOutput> onOutput,
        CancellationToken cancellationToken)
    {
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.CreateNoWindow = true;

        using var process = new Process { StartInfo = startInfo };
        var stopwatch = Stopwatch.StartNew();
        if (!process.Start()) throw new InvalidOperationException("빌드 프로세스를 시작하지 못했습니다.");

        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException) { }
        });

        var standardOutput = PumpAsync(process.StandardOutput, false, onOutput);
        var standardError = PumpAsync(process.StandardError, true, onOutput);

        try
        {
            await process.WaitForExitAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The cancellation registration owns termination of the process tree.
        }

        await Task.WhenAll(standardOutput, standardError).ConfigureAwait(false);
        stopwatch.Stop();
        return new BuildResult(process.HasExited ? process.ExitCode : -1, cancellationToken.IsCancellationRequested, stopwatch.Elapsed);
    }

    private static async Task PumpAsync(StreamReader reader, bool isError, Action<BuildOutput> onOutput)
    {
        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
            onOutput(new BuildOutput(line, isError));
    }
}
