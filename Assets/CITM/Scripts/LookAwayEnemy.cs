using UnityEngine;

public class LookAwayEnemy : MonoBehaviour
{
    [Header("Timing")]
    [Min(0.1f)]
    [SerializeField] private float lifetime = 3f;

    [Min(0f)]
    [SerializeField] private float gracePeriod = 0.5f;

    [Min(0.1f)]
    [SerializeField] private float allowedLookTime = 0.8f;

    [Header("Detection")]
    [Tooltip("Half-angle of the cone in front of the headset.")]
    [Range(1f, 90f)]
    [SerializeField] private float lookAngle = 18f;

    [Tooltip("Optional point on the enemy to measure the look direction.")]
    [SerializeField] private Transform lookTarget;

    private MRGameManager gameManager;
    private Transform head;
    private float elapsed;
    private float continuousLookTime;

    public void Initialize(MRGameManager manager, Transform playerHead)
    {
        gameManager = manager;
        head = playerHead;
    }

    private void Update()
    {
        if (gameManager == null ||
            head == null ||
            !gameManager.CanPlay)
        {
            return;
        }

        elapsed += Time.deltaTime;

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (elapsed < gracePeriod)
        {
            return;
        }

        Vector3 targetPosition =
            lookTarget != null ? lookTarget.position : transform.position;

        Vector3 direction = targetPosition - head.position;

        bool lookingAtEnemy =
            direction.sqrMagnitude > 0.0001f &&
            Vector3.Angle(head.forward, direction) <= lookAngle;

        if (lookingAtEnemy)
        {
            continuousLookTime += Time.deltaTime;

            if (continuousLookTime >= allowedLookTime)
            {
                gameManager.LoseGame(
                    "You looked at the rare enemy for too long."
                );
            }
        }
        else
        {
            continuousLookTime = 0f;
        }
    }
}