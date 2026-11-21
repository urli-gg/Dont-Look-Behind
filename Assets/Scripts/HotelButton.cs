using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRSimpleInteractable))]
public class HotelButton : MonoBehaviour
{
    public UnityEvent onPress = new();
    public AudioSource click;
    public Transform cap;

    private XRSimpleInteractable interactable;
    private static readonly List<IXRInteractable> Targets = new();
    private Vector3 rest;
    private float nextPress;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        if (cap != null)
            rest = cap.localPosition;
    }

    private void OnEnable()
    {
        interactable.firstSelectEntered.AddListener(OnSelected);
        interactable.activated.AddListener(OnActivated);
    }

    private void OnDisable()
    {
        interactable.firstSelectEntered.RemoveListener(OnSelected);
        interactable.activated.RemoveListener(OnActivated);
        StopAllCoroutines();
        if (cap != null)
            cap.localPosition = rest;
    }

    private void OnSelected(SelectEnterEventArgs args) => PressFrom(args.interactorObject.transform);

    private void OnActivated(ActivateEventArgs args)
    {
        if (args.interactorObject is not NearFarInteractor ray || ray.hasSelection)
            return;

        // Hovered activation can notify several targets. Use the same first target as selection.
        ray.GetValidTargets(Targets);
        bool isFirst = Targets.Count > 0 && ReferenceEquals(Targets[0], interactable);
        Targets.Clear();
        if (isFirst)
            PressFrom(ray.transform);
    }

    private void PressFrom(Transform interactor)
    {
        if (interactor.GetComponentInParent<HapticImpulsePlayer>() is { } haptics)
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
