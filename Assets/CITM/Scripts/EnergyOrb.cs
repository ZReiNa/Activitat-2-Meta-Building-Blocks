using UnityEngine;

public class EnergyOrb : MonoBehaviour
{
    [Min(0f)]
    [SerializeField] private float rechargeAmount = 30f;

    private MRGameManager gameManager;
    private bool collected;

    public void Initialize(MRGameManager manager)
    {
        gameManager = manager;
    }

    public void Collect()
    {
        if (collected || gameManager == null || !gameManager.CanPlay)
        {
            return;
        }

        collected = true;
        gameManager.CollectOrb(this, rechargeAmount);
    }
}