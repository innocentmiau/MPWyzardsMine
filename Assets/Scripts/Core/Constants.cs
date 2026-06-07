namespace Scripts.Core
{
    public static class Constants
    {

        public const float BETTER_BUTTON_ANIMATION_DURATION = .1f;

        public const string LEADERBOARD_ID = "elo-ranking";
        public const int DEFAULT_ELO = 1000;

        public const int BASE_ELO_EARNING = 25;
        public const float BASE_TIME_DURATION = 300f;
        public const float MAX_TIME_BONUS_DURATION = 720f;

        public const float LOBBY_GAME_STARTING_TIMER = 3f;
        public const float RANKED_LOBBY_COUNTDOWN = 10f;
        public const float CONSOLE_EACH_LINE_HEIGHT = 20f;

        public const double TIME_QUEUED_BEFORE_CREATE_LOBBY = 5d;
        
        public static readonly string[] PLAYER_NAMES = new[]
        {
            "Jak", "Daxter", "Ratchet", "Clank",
            "Steve", "Robot", "Bot", "Player",
            "Slave", "White", "Black",
            "Giant", "Minion", "Dwarf",
            "Rich", "Poor", "Homeless",
            "Angel", "Demon", "Kevin"
        };

    }
}