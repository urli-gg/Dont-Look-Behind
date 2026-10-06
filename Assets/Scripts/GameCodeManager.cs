using UnityEngine;
using TMPro;

public class GameCodeManager : MonoBehaviour
{
    [Header("Configuracion del codigo")]
    public int codeLength = 4;

    [Header("Prefab del numero")]
    public GameObject bloodNumberPrefab;

    [Header("Lugares donde apareceran")]
    public Transform[] numberSpawnPoints;

    public string GeneratedCode { get; private set; }

    void Start()
    {
        GenerateCode();
    }

    void GenerateCode()
    {
        GeneratedCode = "";

        for (int i = 0; i < codeLength; i++)
        {
            int digit;

            // Evitamos que la contraseña empiece con 0
            if (i == 0)
                digit = Random.Range(1, 10);
            else
                digit = Random.Range(0, 10);

            GeneratedCode += digit.ToString();

            GameObject numberObject = Instantiate(
                bloodNumberPrefab,
                numberSpawnPoints[i].position,
                numberSpawnPoints[i].rotation
            );

            TMP_Text numberText =
                numberObject.GetComponentInChildren<TMP_Text>();

            if (numberText != null)
            {
                numberText.text = digit.ToString();
            }
        }

        // Solo para pruebas. Después podemos borrar esta línea.
        Debug.Log("Codigo generado: " + GeneratedCode);
    }
}
