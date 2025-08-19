using System.Collections.Concurrent;

namespace Scoreboard.Api.Infrastructure;

/// <summary>
/// Estado efímero (en memoria) por partido: segundos restantes cuando está pausado.
/// No se guarda en BD, tal como se solicitó.
/// </summary>
public class MatchRuntimeStore
{
    private readonly ConcurrentDictionary<int, int> _pausedRemaining = new();

    public int GetPausedRemaining(int matchId)
        => _pausedRemaining.TryGetValue(matchId, out var s) ? s : 0;

    public void SetPausedRemaining(int matchId, int seconds)
    {
        if (seconds <= 0) _pausedRemaining.TryRemove(matchId, out _);
        else _pausedRemaining[matchId] = seconds;
    }

    public void Clear(int matchId) => _pausedRemaining.TryRemove(matchId, out _);
}
