using Microsoft.AspNetCore.SignalR;

namespace Scoreboard.Hubs;

public class ScoreHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var http = Context.GetHttpContext();
        var matchId = http?.Request.Query["matchId"].ToString();
        if (!string.IsNullOrEmpty(matchId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"match-{matchId}");
        }
        await base.OnConnectedAsync();
    }

}
