//controla el tiempo


using System.Collections.Concurrent;

namespace Scoreboard.Infrastructure;

public record TimerSnapshot(bool IsRunning, int RemainingSeconds, DateTime? EndsAt);

// Contrato
public interface IMatchRunTimeStore
{
    TimerSnapshot GetOrCreate(int matchId, int defaultSeconds);
    TimerSnapshot Get(int matchId);
    void Start(int matchId, int seconds);
    int Pause(int matchId);
    void Resume(int matchId);
    void Reset(int matchId);
    void Stop(int matchId);
}

internal class MatchRunTimeStore : IMatchRunTimeStore
{
    private class State
    {
        public bool Running;
        public int Remaining;          // en segundos
        public DateTime? EndsAt;       // reloj de periodo
        public int DefaultSeconds;     // duración por defecto (p/ arranque si no viene)
        public readonly object Gate = new();
    }

    private readonly ConcurrentDictionary<int, State> _states = new();

    public TimerSnapshot GetOrCreate(int matchId, int defaultSeconds)
    {
        var s = _states.GetOrAdd(matchId, _ => new State
        {
            Running = false,
            Remaining = 0,
            EndsAt = null,
            DefaultSeconds = defaultSeconds
        });

        lock (s.Gate)
        {
            // actualizar remaining si está corriendo
            if (s.Running && s.EndsAt is not null)
            {
                var now = DateTime.Now;
                var rem = (int)Math.Ceiling((s.EndsAt.Value - now).TotalSeconds);
                if (rem <= 0) { s.Running = false; s.Remaining = 0; s.EndsAt = null; }
                else s.Remaining = rem;
            }

            return new TimerSnapshot(s.Running, s.Remaining, s.EndsAt);
        }
    }

    public TimerSnapshot Get(int matchId)
    {
        if (!_states.TryGetValue(matchId, out var s))
            return new TimerSnapshot(false, 0, null);

        lock (s.Gate)
        {
            if (s.Running && s.EndsAt is not null)
            {
                var now = DateTime.Now;
                var rem = (int)Math.Ceiling((s.EndsAt.Value - now).TotalSeconds);
                if (rem <= 0) { s.Running = false; s.Remaining = 0; s.EndsAt = null; }
                else s.Remaining = rem;
            }
            return new TimerSnapshot(s.Running, s.Remaining, s.EndsAt);
        }
    }

    public void Start(int matchId, int seconds)
    {
        var s = _states.GetOrAdd(matchId, _ => new State());
        lock (s.Gate)
        {
            s.DefaultSeconds = seconds;
            s.Running = true;
            s.Remaining = seconds;
            s.EndsAt = DateTime.Now.AddSeconds(seconds);
        }
    }

    public int Pause(int matchId)
    {
        var s = _states.GetOrAdd(matchId, _ => new State());
        lock (s.Gate)
        {
            if (s.Running && s.EndsAt is not null)
            {
                var now = DateTime.Now;
                var rem = (int)Math.Ceiling((s.EndsAt.Value - now).TotalSeconds);
                s.Remaining = Math.Max(0, rem);
            }
            s.Running = false;
            s.EndsAt = null;
            return s.Remaining;
        }
    }

    public void Resume(int matchId)
    {
        var s = _states.GetOrAdd(matchId, _ => new State());
        lock (s.Gate)
        {
            if (s.Remaining <= 0) return;
            s.Running = true;
            s.EndsAt = DateTime.Now.AddSeconds(s.Remaining);
        }
    }

    public void Reset(int matchId)
    {
        var s = _states.GetOrAdd(matchId, _ => new State());
        lock (s.Gate)
        {
            s.Running = false;
            s.Remaining = 0;
            s.EndsAt = null;
        }
    }

    public void Stop(int matchId) => Reset(matchId);
}
