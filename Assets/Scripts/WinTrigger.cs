using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class WinTrigger : MonoBehaviour
{
    public Transform playerHead;
    public Creature creature;
    public HotelFade fade;
    public AudioSource release;

    public TeleportationArea exitTeleport;

    private bool won;

    private void OnEnable()
    {
        if (exitTeleport != null)
            exitTeleport.enabled = true;
    }

    private void Update()
    {
        if (playerHead == null)
            return;
        Vector3 head = transform.InverseTransformPoint(playerHead.position);
        if (head.z >= 0f && Mathf.Abs(head.x) < 1.2f && head.y > 0f && head.y < 3f)
            Finish();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (playerHead == null && other.transform.root.CompareTag("Player"))
            Finish();
    }

    private void Finish()
    {
        if (won || (creature != null && !creature.TryDisarm()))
            return;
        won = true;
        StartCoroutine(PlayWin());
    }

    private IEnumerator PlayWin()
    {
        if (release != null)
            release.Play();
        for (float time = 0f; time < 0.9f; time += Time.unscaledDeltaTime)
        {
            if (fade != null)
                fade.SetOpacity(Mathf.SmoothStep(0f, 1f, time / 0.9f));
            yield return null;
        }
        if (fade != null)
            fade.SetOpacity(1f);
        yield return SceneManager.LoadSceneAsync("WinScene");
    }
}