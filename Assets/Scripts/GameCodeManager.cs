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
            // Genera únicamente números del 1 al 9.
            // El 10 no se incluye en Random.Range con enteros.
            int digit = Random.Range(1, 10);

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

        // Para pruebas. Puedes borrar esta línea al final del proyecto.
        Debug.Log("Codigo generado: " + GeneratedCode);
    }
}