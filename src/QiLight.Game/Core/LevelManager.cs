namespace QiLight.Game.Core;

public class LevelManager
{
    public int CurrentLevel { get; private set; } = 1;
    public float QixSpeedMultiplier => 1f + (CurrentLevel - 1) * 0.1f;
    public float SparxSpeedMultiplier => 1f + (CurrentLevel - 1) * 0.05f;
    public int SparxCount => CurrentLevel >= 3 ? 2 : 1;

    public void NextLevel()
    {
        CurrentLevel++;
    }

    public void Reset()
    {
        CurrentLevel = 1;
    }
}
