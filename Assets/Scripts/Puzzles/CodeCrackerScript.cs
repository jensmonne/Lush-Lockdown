using UnityEngine;

public class CodeCrackerScript : MonoBehaviour
{
    [SerializeField] private GameObject codeCanvas;
    [SerializeField] private VaultDoor vaultDoor;
    [Tooltip("Optional. Links this code cracker to the PuzzleSequenceManager. Found on this GameObject if left empty.")]
    [SerializeField] private Puzzle puzzle;
    public int Code;

    public bool AcceptsInput => puzzle == null || (puzzle.IsUnlocked && !puzzle.IsSolved);

    private void Awake()
    {
        if (puzzle == null) puzzle = GetComponent<Puzzle>();
        if (puzzle != null) puzzle.Solved += OnPuzzleSolved;

        GenerateRandomCode();
    }

    private void OnDestroy()
    {
        if (puzzle != null) puzzle.Solved -= OnPuzzleSolved;
    }

    public int GenerateRandomCode()
    {
        Code = UnityEngine.Random.Range(0, 10000);
        Debug.Log(Code.ToString("D4"));
        return Code;

    }

    public void CodeCracked()
    {
        if (puzzle == null)
        {
            vaultDoor.OpenVault();
            return;
        }

        puzzle.Complete();
    }

    private void OnPuzzleSolved(Puzzle solvedPuzzle)
    {
        vaultDoor.OpenVault();
    }
}