using TMPro;
using UnityEngine;

public class GameCodeManager : MonoBehaviour
{
    [Min(1)] public int codeLength = 4;
    public GameObject bloodNumberPrefab;
    public Transform[] numberSpawnPoints;

    public string GeneratedCode { get; private set; }
    public TMP_Text[] InstalledClues { get; private set; }

    private void Start()
    {
        if (numberSpawnPoints == null || codeLength < 1 || numberSpawnPoints.Length != codeLength ||
            bloodNumberPrefab == null || bloodNumberPrefab.GetComponentInChildren<TMP_Text>(true) == null ||
            System.Array.Exists(numberSpawnPoints, point => point == null))
        {
            Debug.LogError("El código requiere un prefab de texto y un anclaje por cada posición.", this);
            return;
        }

        var digits = new char[codeLength];
        InstalledClues = new TMP_Text[codeLength];
        for (int index = 0; index < codeLength; index++)
        {
            digits[index] = (char)('0' + Random.Range(1, 10));
            Transform point = numberSpawnPoints[index];
            GameObject clue = Instantiate(bloodNumberPrefab, point, false);
            clue.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            TMP_Text text = clue.GetComponentInChildren<TMP_Text>(true);
            text.text = digits[index].ToString();
            InstalledClues[index] = text;
        }

        GeneratedCode = new string(digits);
    }
}