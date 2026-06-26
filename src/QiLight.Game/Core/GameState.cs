namespace QiLight.Game.Core;

public enum GamePhase
{
    Menu,
    Playing,
    Paused,
    GameOver,
    Win
}

public class GameStateManager
{
    public GamePhase CurrentPhase { get; private set; } = GamePhase.Menu;
    public GamePhase PreviousPhase { get; private set; } = GamePhase.Menu;

    public void TransitionTo(GamePhase newPhase)
    {
        PreviousPhase = CurrentPhase;
        CurrentPhase = newPhase;
    }

    public void TogglePause()
    {
        if (CurrentPhase == GamePhase.Playing)
            TransitionTo(GamePhase.Paused);
        else if (CurrentPhase == GamePhase.Paused)
            TransitionTo(GamePhase.Playing);
    }
}
