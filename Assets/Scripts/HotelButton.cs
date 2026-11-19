using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class HotelButton : MonoBehaviour
{
    public UnityEvent onPress = new();
    public AudioSource click;
    public Transform cap;

    private XRSimpleInteractable interactable;
    private Vector3 rest;
    private float nextPress;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        if (cap != null)
            rest = cap.localPosition;
    }

    private void OnEnable() => interactable.firstSelectEntered.AddListener(OnSelected);

    private void OnDisable()
    {
        interactable.firstSelectEntered.RemoveListener(OnSelected);
        StopAllCoroutines();
        if (cap != null)
            cap.localPosition = rest;
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        if (args.interactorObject.transform.GetComponentInParent<HapticImpulsePlayer>() is { } haptics)
            haptics.SendHapticImpulse(0.15f, 0.055f);
        Press();
    }

    public void Press()
    {
        if (!isActiveAndEnabled || Time.unscaledTime < nextPress)
            return;
        nextPress = Time.unscaledTime + 0.16f;
        if (click != null)
            click.Play();
        if (cap != null)
            StartCoroutine(Depress());
        onPress.Invoke();
    }

    private IEnumerator Depress()
    {
        cap.localPosition = rest + Vector3.forward * 0.007f;
        yield return new WaitForSecondsRealtime(0.09f);
        cap.localPosition = rest;
    }
}
