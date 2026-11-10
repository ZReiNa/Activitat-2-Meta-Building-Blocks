using Oculus.Interaction;
using UnityEngine;

[RequireComponent(typeof(EnergyOrb))]
public class OrbGrabDetector : MonoBehaviour
{
    [Tooltip("The Grabbable component on this orb.")]
    [SerializeField] private Grabbable grabbable;

    private EnergyOrb orb;

    private void Awake()
    {
        orb = GetComponent<EnergyOrb>();

        if (grabbable == null)
        {
            grabbable = GetComponentInChildren<Grabbable>();
        }
    }

    private void OnEnable()
    {
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised += OnPointerEvent;
        }
    }

    private void OnDisable()
    {
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised -= OnPointerEvent;
        }
    }

    private void OnPointerEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select)
        {
            orb.Collect();
        }
    }
}