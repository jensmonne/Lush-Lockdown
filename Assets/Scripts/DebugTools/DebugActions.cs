using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The commands behind the debug menu. Plain static methods, so they can also be called from
/// other scripts, keyboard shortcuts or context menus.
/// </summary>
public static class DebugActions
{
    private static PuzzleSequenceManager sequenceManager;

    private static PuzzleSequenceManager SequenceManager
    {
        get
        {
            if (sequenceManager == null) sequenceManager = Object.FindAnyObjectByType<PuzzleSequenceManager>();
            return sequenceManager;
        }
    }

    public static void SkipCurrentPuzzle()
    {
        if (SequenceManager == null)
        {
            Debug.LogWarning("Debug menu: no PuzzleSequenceManager in this scene.");
            return;
        }

        SequenceManager.SkipCurrentPuzzle();
    }

    public static void ReloadScene()
    {
        Scene scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
            scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(scene.buildIndex);
#endif
    }

    private static CodeCrackerScript codeCracker;

    public static string GetVaultCode()
    {
        if (codeCracker == null) codeCracker = Object.FindAnyObjectByType<CodeCrackerScript>();
        return codeCracker != null ? $"Vault code: {codeCracker.Code:D4}" : "Vault code: no code cracker in scene";
    }

    public static string GetPuzzleProgress()
    {
        var manager = SequenceManager;
        if (manager == null) return "No PuzzleSequenceManager";
        if (manager.TotalCount == 0) return "Sequence is empty";
        if (manager.IsComplete) return $"All {manager.TotalCount} puzzles solved";

        return $"Puzzle {manager.SolvedCount + 1}/{manager.TotalCount}: {manager.CurrentPuzzle.PuzzleName}";
    }
}
