using UnityEngine;

public class Enemy : MonoBehaviour
{
    private MRGameManager gameManager;
    private bool defeated;

    public void Initialize(MRGameManager manager)
    {
        gameManager = manager;
    }

    public void Hit()
    {
        if (defeated || gameManager == null || !gameManager.CanPlay)
        {
            return;
        }

        defeated = true;
        gameManager.EnemyDefeated(this);
    }
}
