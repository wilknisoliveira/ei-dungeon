using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Domain.Entity
{
    public class Play : Base
    {
        public Play(Guid gameId, PlayType playType, string response)
        {
            GameId = gameId;
            PlayType = playType;
            Response = response;
        }

        public Play(Game game, PlayType playType, string response)
        {
            GameId = game.Id;
            Game = game;
            PlayType = playType;
            Response = response;
            SetCreatedDate(DateTimeOffset.UtcNow);
        }

        public Game Game { get; private set; }
        public Guid GameId { get; private set; }
        public PlayType PlayType { get; private set; }
        public string Response { get; private set; }

        public void SetResponse(string response)
        {
            Response = response;
        }
    }
}
