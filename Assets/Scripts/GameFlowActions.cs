using UnityEngine;

public class GameFlowActions : MonoBehaviour
{
  public void StartNewGame()
  {
    GameFlowManager.Instance?.StartNewGame();
  }

  public void PauseGame()
  {
    GameFlowManager.Instance?.PauseGame();
  }

  public void ResumeGame()
  {
    GameFlowManager.Instance?.ResumeGame();
  }

  public void RestartLevel()
  {
    GameFlowManager.Instance?.RestartLevel();
  }

  public void LoadNextLevel()
  {
    GameFlowManager.Instance?.LoadNextLevel();
  }

  public void GoToMainMenu()
  {
    GameFlowManager.Instance?.GoToMainMenu();
  }

  public void QuitGame()
  {
    GameFlowManager.Instance?.QuitGame();
  }
}
