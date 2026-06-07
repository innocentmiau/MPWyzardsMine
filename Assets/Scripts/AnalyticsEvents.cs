using Unity.Services.Analytics;

namespace Analytics.Events
{
    public class KillEnemy : Event
    {
        public KillEnemy(int enemyType) : base(nameof(KillEnemy))
        {
            SetParameter("enemyType", enemyType);
        }
    }

    public class LevelGained : Event
    {
        public LevelGained(int gainedLevel, string gainedAbility) : base(nameof(LevelGained))
        {
            SetParameter("gainedAbility", gainedAbility);
            SetParameter("gainedLevel", gainedLevel);
        }
    }
}