using Microsoft.AspNetCore.SignalR;

namespace Scoreboard.Api.Hubs;

public class ScoreHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var matchIdStr = Context.GetHttpContext()?.Request.Query["matchId"];
        if (int.TryParse(matchIdStr, out var matchId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"match-{matchId}");
        await base.OnConnectedAsync();
    }
}
