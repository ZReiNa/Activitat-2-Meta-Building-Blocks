using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MRGameManager gameManager;
    [SerializeField] private Transform beamOrigin;
    [SerializeField] private LineRenderer beam;

    [Header("Beam")]
    [Min(0.1f)]
    [SerializeField] private float range = 10f;

    [Min(0f)]
    [SerializeField] private float batteryDrainPerSecond = 10f;

    [Range(0f, 1f)]
    [SerializeField] private float triggerThreshold = 0.5f;

    [Min(0.05f)]
    [SerializeField] private float timeBetweenHits = 0.25f;

    [Tooltip("Select only the Enemy layer.")]
    [SerializeField] private LayerMask enemyLayers;

    private float nextHitTime;

    private void Start()
    {
        if (gameManager == null || beamOrigin == null || beam == null)
        {
            Debug.LogError(
                "FlashlightController: assign Game Manager, Beam Origin, " +
                "and Beam in the Inspector.",
                this
            );

            enabled = false;
            return;
        }

        beam.useWorldSpace = true;
        beam.positionCount = 2;
        beam.enabled = false;
    }

    private void Update()
    {
        bool triggerPressed = OVRInput.Get(
            OVRInput.Axis1D.PrimaryIndexTrigger,
            OVRInput.Controller.RTouch
        ) >= triggerThreshold;

        if (!gameManager.CanPlay ||
            !triggerPressed ||
            gameManager.Battery <= 0f)
        {
            beam.enabled = false;
            return;
        }

        Vector3 start = beamOrigin.position;
        Vector3 direction = beamOrigin.forward;
        Vector3 end = start + direction * range;

        if (Physics.Raycast(
            start,
            direction,
            out RaycastHit hit,
            range,
            enemyLayers,
            QueryTriggerInteraction.Ignore))
        {
            end = hit.point;

            if (Time.time >= nextHitTime)
            {
                Enemy enemy = hit.collider.GetComponentInParent<Enemy>();

                if (enemy != null)
                {
                    nextHitTime = Time.time + timeBetweenHits;
                    enemy.Hit();
                }
            }
        }

        beam.enabled = true;
        beam.SetPosition(0, start);
        beam.SetPosition(1, end);

        gameManager.DrainBattery(batteryDrainPerSecond * Time.deltaTime);

        if (!gameManager.CanPlay || gameManager.Battery <= 0f)
        {
            beam.enabled = false;
        }
    }
}