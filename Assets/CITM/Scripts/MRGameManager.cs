using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class MRGameManager : MonoBehaviour
{
    private enum GameState
    {
        WaitingForTracking,
        Menu,
        Playing,
        Won,
        Lost
    }

    [Header("Tracking")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform floorOrigin;

    [Header("Prefabs")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject orbPrefab;
    [SerializeField] private GameObject lookAwayEnemyPrefab;

    [Header("Win condition")]
    [Min(1)]
    [SerializeField] private int pointsToWin = 20;

    [Header("Normal enemies")]
    [Min(1)]
    [SerializeField] private int maxEnemies = 4;

    [Min(0.1f)]
    [SerializeField] private float enemySpawnInterval = 2f;

    [Min(0.1f)]
    [SerializeField] private float enemySpawnRadiusMin = 2f;

    [Min(0.1f)]
    [SerializeField] private float enemySpawnRadiusMax = 3f;

    [Min(0f)]
    [SerializeField] private float enemyCenterHeight = 0.8f;

    [Header("Energy orbs")]
    [Min(1)]
    [SerializeField] private int maxOrbs = 2;

    [Min(0.1f)]
    [SerializeField] private float orbSpawnInterval = 5f;

    [Min(0.1f)]
    [SerializeField] private float orbSpawnRadiusMin = 0.7f;

    [Min(0.1f)]
    [SerializeField] private float orbSpawnRadiusMax = 1f;

    [Min(0f)]
    [SerializeField] private float orbHeight = 1.1f;

    [Header("Look-away enemy")]
    [Min(1f)]
    [SerializeField] private float specialIntervalMin = 25f;

    [Min(1f)]
    [SerializeField] private float specialIntervalMax = 40f;

    [Min(0.3f)]
    [SerializeField] private float specialSpawnDistance = 1.5f;

    [Min(0f)]
    [SerializeField] private float specialCenterHeight = 1.35f;

    [Header("Battery")]
    [Min(1f)]
    [SerializeField] private float maxBattery = 100f;

    [Header("Stage panel")]
    [Tooltip("The parent object containing the stage image and poke button.")]
    [SerializeField] private GameObject stagePanelRoot;

    [SerializeField] private Image stageImage;
    [SerializeField] private Sprite stageOneImage;
    [SerializeField] private Sprite stageTwoImage;
    [SerializeField] private Sprite stageThreeImage;

    [Min(0.1f)]
    [SerializeField] private float secondsPerStage = 8f;

    [Min(0f)]
    [SerializeField] private float panelResetBatteryCost = 20f;

    [Tooltip("Local offset from the player's head when the game starts.")]
    [SerializeField]
    private Vector3 stagePanelOffset =
        new Vector3(0.20f, -0.25f, 0.15f);

    [Header("Start menu")]
    [Tooltip("The parent object containing the menu and its poke button.")]
    [SerializeField] private GameObject menuRoot;

    [SerializeField] private TMP_Text menuText;

    [Tooltip("Local offset from the player's head for the start menu.")]
    [SerializeField]
    private Vector3 menuOffset =
        new Vector3(0f, -0.1f, 0.10f);

    [Header("Wrist display")]
    [SerializeField] private TMP_Text wristText;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip enemyDefeatedSound;
    [SerializeField] private AudioClip orbCollectedSound;
    [SerializeField] private AudioClip batteryEmptySound;
    [SerializeField] private AudioClip batteryFullSound;
    [SerializeField] private AudioClip specialSpawnSound;
    [SerializeField] private AudioClip panelStageChangedSound;
    [SerializeField] private AudioClip panelResetSound;
    [SerializeField] private AudioClip insufficientBatterySound;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip loseSound;
    [SerializeField] private AudioClip startSound;

    private readonly List<Enemy> enemies = new List<Enemy>();
    private readonly List<EnergyOrb> orbs = new List<EnergyOrb>();

    private LookAwayEnemy activeSpecial;
    private GameState state = GameState.WaitingForTracking;

    private float battery;
    private int score;
    private int currentStage;
    private float stageElapsed;

    private float nextEnemySpawnTime;
    private float nextOrbSpawnTime;
    private float nextSpecialSpawnTime;
    private bool startRequested;

    public bool CanPlay => state == GameState.Playing;
    public float Battery => battery;

    private void OnValidate()
    {
        pointsToWin = Mathf.Max(1, pointsToWin);
        maxEnemies = Mathf.Max(1, maxEnemies);
        maxOrbs = Mathf.Max(1, maxOrbs);
        maxBattery = Mathf.Max(1f, maxBattery);
        panelResetBatteryCost = Mathf.Max(0f, panelResetBatteryCost);
        secondsPerStage = Mathf.Max(0.1f, secondsPerStage);

        enemySpawnInterval = Mathf.Max(0.1f, enemySpawnInterval);
        orbSpawnInterval = Mathf.Max(0.1f, orbSpawnInterval);

        enemySpawnRadiusMin = Mathf.Max(0.1f, enemySpawnRadiusMin);
        enemySpawnRadiusMax = Mathf.Max(
            enemySpawnRadiusMin, enemySpawnRadiusMax);

        orbSpawnRadiusMin = Mathf.Max(0.1f, orbSpawnRadiusMin);
        orbSpawnRadiusMax = Mathf.Max(
            orbSpawnRadiusMin, orbSpawnRadiusMax);

        specialIntervalMin = Mathf.Max(1f, specialIntervalMin);
        specialIntervalMax = Mathf.Max(
            specialIntervalMin, specialIntervalMax);
    }

    private IEnumerator Start()
    {
        if (!ReferencesAreValid())
        {
            enabled = false;
            yield break;
        }

        menuRoot.SetActive(false);
        stagePanelRoot.SetActive(false);
        wristText.text = "WAITING FOR HEADSET TRACKING...";

        bool warned = false;
        float waitStarted = Time.realtimeSinceStartup;

        while (true)
        {
            InputDevice device =
                InputDevices.GetDeviceAtXRNode(XRNode.Head);

            if (device.isValid &&
                device.TryGetFeatureValue(
                    CommonUsages.isTracked, out bool tracked) &&
                tracked)
            {
                break;
            }

            if (!warned &&
                Time.realtimeSinceStartup - waitStarted > 10f)
            {
                Debug.LogWarning(
                    "Still waiting for headset tracking. " +
                    "Check the XR provider and headset.",
                    this
                );

                warned = true;
            }

            yield return null;
        }

        battery = maxBattery;
        state = GameState.Menu;
        ShowMenu("READY?");
        UpdateWrist();
    }

    private bool ReferencesAreValid()
    {
        if (head == null || floorOrigin == null ||
            enemyPrefab == null || orbPrefab == null ||
            lookAwayEnemyPrefab == null ||
            menuRoot == null || menuText == null ||
            stagePanelRoot == null || stageImage == null ||
            stageOneImage == null || stageTwoImage == null ||
            stageThreeImage == null ||
            wristText == null || audioSource == null)
        {
            Debug.LogError(
                "MRGameManager: assign all tracking, prefab, menu, " +
                "stage panel, sprite, wrist text and audio references.",
                this
            );

            return false;
        }

        if (enemyPrefab.GetComponent<Enemy>() == null ||
            orbPrefab.GetComponent<EnergyOrb>() == null ||
            lookAwayEnemyPrefab.GetComponent<LookAwayEnemy>() == null)
        {
            Debug.LogError(
                "Each prefab must have its matching script on its root: " +
                "Enemy, EnergyOrb or LookAwayEnemy.",
                this
            );

            return false;
        }

        if (menuRoot == stagePanelRoot ||
            transform.IsChildOf(menuRoot.transform) ||
            transform.IsChildOf(stagePanelRoot.transform))
        {
            Debug.LogError(
                "Menu Root and Stage Panel Root must be separate objects " +
                "and must not contain the GameManager.",
                this
            );

            return false;
        }

        return true;
    }

    private void Update()
    {
        if (startRequested)
        {
            startRequested = false;

            if (state == GameState.Menu ||
                state == GameState.Won ||
                state == GameState.Lost)
            {
                StartNewGame();
            }

            return;
        }

        if (state != GameState.WaitingForTracking &&
            OVRInput.GetDown(
                OVRInput.Button.One,
                OVRInput.Controller.RTouch))
        {
            StartNewGame();
            return;
        }

        if (!CanPlay)
        {
            return;
        }

        UpdateStageTimer();

        if (!CanPlay)
        {
            return;
        }

        if (Time.time >= nextEnemySpawnTime)
        {
            enemies.RemoveAll(enemy => enemy == null);

            if (enemies.Count < maxEnemies)
            {
                SpawnEnemy();
            }

            nextEnemySpawnTime = Time.time + enemySpawnInterval;
        }

        if (Time.time >= nextOrbSpawnTime)
        {
            orbs.RemoveAll(orb => orb == null);

            if (orbs.Count < maxOrbs)
            {
                SpawnOrb();
            }

            nextOrbSpawnTime = Time.time + orbSpawnInterval;
        }

        if (Time.time >= nextSpecialSpawnTime)
        {
            if (activeSpecial == null)
            {
                SpawnSpecial();
            }

            ScheduleNextSpecial();
        }
    }

    public void RequestStart()
    {
        if (state == GameState.Menu ||
            state == GameState.Won ||
            state == GameState.Lost)
        {
            startRequested = true;
        }
    }

    private void StartNewGame()
    {
        ClearRemainingObjects();

        score = 0;
        battery = maxBattery;
        currentStage = 0;
        stageElapsed = 0f;
        state = GameState.Playing;

        menuRoot.SetActive(false);

        PlaceInFrontOfPlayer(stagePanelRoot.transform, stagePanelOffset);
        stagePanelRoot.SetActive(true);
        UpdateStageImage();

        nextEnemySpawnTime = Time.time;
        nextOrbSpawnTime = Time.time;
        ScheduleNextSpecial();

        PlaySound(startSound);
        UpdateWrist();
    }

    private void UpdateStageTimer()
    {
        stageElapsed += Time.deltaTime;

        while (stageElapsed >= secondsPerStage && CanPlay)
        {
            stageElapsed -= secondsPerStage;
            currentStage++;

            if (currentStage >= 3)
            {
                LoseGame("The stage panel reached its limit.");
                return;
            }

            UpdateStageImage();
            PlaySound(panelStageChangedSound);
        }
    }

    public void ResetStagePanel()
    {
        if (!CanPlay)
        {
            return;
        }

        if (battery < panelResetBatteryCost)
        {
            PlaySound(insufficientBatterySound);
            return;
        }

        SpendBattery(panelResetBatteryCost);
        currentStage = 0;
        stageElapsed = 0f;

        UpdateStageImage();
        UpdateWrist();
        PlaySound(panelResetSound);
    }

    private void UpdateStageImage()
    {
        switch (currentStage)
        {
            case 0:
                stageImage.sprite = stageOneImage;
                break;
            case 1:
                stageImage.sprite = stageTwoImage;
                break;
            default:
                stageImage.sprite = stageThreeImage;
                break;
        }
    }

    public void DrainBattery(float amount)
    {
        if (!CanPlay || amount <= 0f)
        {
            return;
        }

        SpendBattery(amount);
        UpdateWrist();
    }

    private void SpendBattery(float amount)
    {
        float previousBattery = battery;
        battery = Mathf.Max(0f, battery - amount);

        if (previousBattery > 0f && battery <= 0f)
        {
            PlaySound(batteryEmptySound);
        }
    }

    public void CollectOrb(EnergyOrb orb, float rechargeAmount)
    {
        if (!CanPlay || orb == null || !orbs.Remove(orb))
        {
            return;
        }

        float previousBattery = battery;

        battery = Mathf.Clamp(
            battery + Mathf.Max(0f, rechargeAmount),
            0f,
            maxBattery
        );

        PlaySound(orbCollectedSound);

        if (previousBattery < maxBattery &&
            battery >= maxBattery)
        {
            PlaySound(batteryFullSound);
        }

        UpdateWrist();
        Destroy(orb.gameObject);
    }

    public void EnemyDefeated(Enemy enemy)
    {
        if (!CanPlay || enemy == null || !enemies.Remove(enemy))
        {
            return;
        }

        score++;
        PlaySound(enemyDefeatedSound);
        Destroy(enemy.gameObject);

        if (score >= pointsToWin)
        {
            EndGame(true, "You defeated enough enemies!");
        }
        else
        {
            UpdateWrist();
        }
    }

    public void LoseGame(string reason)
    {
        if (CanPlay)
        {
            EndGame(false, reason);
        }
    }

    private void EndGame(bool victory, string reason)
    {
        state = victory ? GameState.Won : GameState.Lost;

        ClearRemainingObjects();
        stagePanelRoot.SetActive(false);

        PlaySound(victory ? winSound : loseSound);
        UpdateWrist();

        ShowMenu(
            (victory ? "YOU WIN!" : "GAME OVER") +
            "\n" + reason
        );
    }

    private void SpawnEnemy()
    {
        Vector3 position = RandomPositionAroundPlayer(
            enemySpawnRadiusMin,
            enemySpawnRadiusMax,
            enemyCenterHeight
        );

        GameObject instance = Instantiate(
            enemyPrefab, position, Quaternion.identity);

        Enemy enemy = instance.GetComponent<Enemy>();
        enemy.Initialize(this);
        enemies.Add(enemy);
    }

    private void SpawnOrb()
    {
        Vector3 position = RandomPositionAroundPlayer(
            orbSpawnRadiusMin,
            orbSpawnRadiusMax,
            orbHeight
        );

        GameObject instance = Instantiate(
            orbPrefab, position, Quaternion.identity);

        EnergyOrb orb = instance.GetComponent<EnergyOrb>();
        orb.Initialize(this);
        orbs.Add(orb);
    }

    private void SpawnSpecial()
    {
        Vector3 forward = Vector3.ProjectOnPlane(
            head.forward, Vector3.up);

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();

        float side = Random.value < 0.5f ? -1f : 1f;
        Vector3 direction =
            Quaternion.Euler(0f, side * 75f, 0f) * forward;

        Vector3 position = head.position +
            direction * specialSpawnDistance;

        position.y = floorOrigin.position.y + specialCenterHeight;

        GameObject instance = Instantiate(
            lookAwayEnemyPrefab, position, Quaternion.identity);

        activeSpecial = instance.GetComponent<LookAwayEnemy>();
        activeSpecial.Initialize(this, head);

        PlaySound(specialSpawnSound);
    }

    private void ScheduleNextSpecial()
    {
        nextSpecialSpawnTime = Time.time +
            Random.Range(specialIntervalMin, specialIntervalMax);
    }

    private Vector3 RandomPositionAroundPlayer(
        float minRadius,
        float maxRadius,
        float heightAboveFloor)
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(minRadius, maxRadius);

        return new Vector3(
            head.position.x + Mathf.Cos(angle) * radius,
            floorOrigin.position.y + heightAboveFloor,
            head.position.z + Mathf.Sin(angle) * radius
        );
    }

    private void ShowMenu(string heading)
    {
        PlaceInFrontOfPlayer(menuRoot.transform, menuOffset);

        menuText.text =
            heading + "\n\n" +
            $"Defeat {pointsToWin} enemies with the right controller trigger.\n" +
            "Enemies change color: defeat them before they turn red.\n" +
            "Grab energy orbs with side button to recharge.\n" +
            "Look away from the purple enemy.\n" +
            $"Touch RESET PANEL before stage 4 " +
            $"(cost: {panelResetBatteryCost:0} battery).\n\n" +
            "Touch START to play.";

        menuRoot.SetActive(true);
    }

    private void PlaceInFrontOfPlayer(
        Transform target,
        Vector3 offset)
    {
        Vector3 forward = Vector3.ProjectOnPlane(
            head.forward, Vector3.up);

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward);

        target.position =
            head.position +
            right * offset.x +
            Vector3.up * offset.y +
            forward * offset.z;

        target.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    private void ClearRemainingObjects()
    {
        foreach (Enemy enemy in enemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }

        foreach (EnergyOrb orb in orbs)
        {
            if (orb != null)
            {
                Destroy(orb.gameObject);
            }
        }

        enemies.Clear();
        orbs.Clear();

        if (activeSpecial != null)
        {
            Destroy(activeSpecial.gameObject);
            activeSpecial = null;
        }
    }

    private void UpdateWrist()
    {
        int percentage =
            Mathf.RoundToInt(battery / maxBattery * 100f);

        if (state == GameState.Won)
        {
            wristText.text =
                "YOU WIN!\n" +
                $"SCORE: {score} / {pointsToWin}\n" +
                "Touch START or press A";
        }
        else if (state == GameState.Lost)
        {
            wristText.text =
                "GAME OVER\n" +
                $"SCORE: {score} / {pointsToWin}\n" +
                "Touch START or press A";
        }
        else if (state == GameState.Menu)
        {
            wristText.text = "Poke START to play";
        }
        else
        {
            wristText.text =
                $"BATTERY: {percentage}%\n" +
                $"SCORE: {score} / {pointsToWin}\n" +
                "Right Trigger: fire\n" +
                "Grab orbs: recharge\n" +
                "A: restart";
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}