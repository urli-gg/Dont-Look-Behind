using TMPro;
using UnityEngine;

public class GameCodeManager : MonoBehaviour
{
    [Min(1)] public int codeLength = 4;
    public GameObject bloodNumberPrefab;
    public Transform[] numberSpawnPoints;

    public string GeneratedCode { get; private set; }
    public TMP_Text[] InstalledClues { get; private set; }
    public Transform playerHead;
    public LayerMask clueOcclusion;
    public HorrorTensionDirector tension;
    [Min(0.5f)] public float readRange = 2.65f;
    public int CluesFound { get; private set; }
    public event System.Action<int> ClueDiscovered;

    private bool[] found;
    private int reading = -1;
    private float readSince;
    private float nextSample;

    public bool HasFoundClue(int index) => found != null && index >= 0 && index < found.Length && found[index];

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
        found = new bool[codeLength];
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

    private void Update()
    {
        if (playerHead == null || found == null || CluesFound == found.Length || Time.time < nextSample)
            return;
        nextSample = Time.time + 0.075f;
        float bestDot = 0.88f;
        int candidate = -1;
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i])
                continue;
            Vector3 target = numberSpawnPoints[i].position;
            Vector3 direction = target - playerHead.position;
            if (direction.sqrMagnitude > readRange * readRange)
                continue;
            float alignment = Vector3.Dot(playerHead.forward, direction.normalized);
            if (alignment <= bestDot || Physics.Linecast(playerHead.position, target, clueOcclusion, QueryTriggerInteraction.Ignore))
                continue;
            bestDot = alignment;
            candidate = i;
        }

        if (candidate != reading)
        {
            reading = candidate;
            readSince = Time.time;
        }
        float dwell = tension != null && tension.Profile != null ? tension.Profile.clueDwell : 0.44f;
        if (candidate < 0 || Time.time - readSince < dwell)
            return;

        found[candidate] = true;
        CluesFound++;
        InstalledClues[candidate].color = new Color(0.72f, 0.86f, 0.80f);
        ClueDiscovered?.Invoke(candidate);
    }
}