using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Timer")]
    [Min(0.1f)]
    [SerializeField] private float lifetime = 12f;

    [Header("Color")]
    [Tooltip("The visible mesh renderer, for example Body.")]
    [SerializeField] private Renderer bodyRenderer;

    [SerializeField] private Color startingColor = Color.green;
    [SerializeField] private Color finalColor = Color.red;

    private MRGameManager gameManager;
    private MaterialPropertyBlock propertyBlock;
    private int colorProperty;
    private float elapsed;
    private bool defeated;

    public void Initialize(MRGameManager manager)
    {
        gameManager = manager;
    }

    private void Awake()
    {
        if (bodyRenderer == null)
        {
            bodyRenderer = GetComponentInChildren<Renderer>();
        }

        if (bodyRenderer == null ||
            bodyRenderer.sharedMaterial == null)
        {
            Debug.LogError(
                "Enemy: assign a renderer with a material.",
                this
            );

            enabled = false;
            return;
        }

        Material material = bodyRenderer.sharedMaterial;

        if (material.HasProperty("_BaseColor"))
        {
            colorProperty = Shader.PropertyToID("_BaseColor");
        }
        else if (material.HasProperty("_Color"))
        {
            colorProperty = Shader.PropertyToID("_Color");
        }
        else
        {
            Debug.LogError(
                "Enemy material has no supported color property. " +
                "Use a standard URP Lit or Unlit material.",
                this
            );

            enabled = false;
            return;
        }

        propertyBlock = new MaterialPropertyBlock();
        UpdateColor(0f);
    }

    private void Update()
    {
        if (gameManager == null ||
            !gameManager.CanPlay ||
            defeated)
        {
            return;
        }

        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, lifetime));

        UpdateColor(progress);

        if (elapsed >= lifetime)
        {
            gameManager.LoseGame(
                "A normal enemy was not defeated in time."
            );
        }
    }

    public void Hit()
    {
        if (defeated ||
            gameManager == null ||
            !gameManager.CanPlay)
        {
            return;
        }

        defeated = true;
        gameManager.EnemyDefeated(this);
    }

    private void UpdateColor(float progress)
    {
        bodyRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetColor(
            colorProperty,
            Color.Lerp(startingColor, finalColor, progress)
        );

        bodyRenderer.SetPropertyBlock(propertyBlock);
    }
}