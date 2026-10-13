using UnityEngine;
using UnityEngine.SceneManagement;

public class WinTrigger : MonoBehaviour
{
    private bool hasWon = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasWon)
            return;

        if (other.transform.root.CompareTag("Player"))
        {
            hasWon = true;

            SceneManager.LoadScene("WinScene");
        }
    }
}
