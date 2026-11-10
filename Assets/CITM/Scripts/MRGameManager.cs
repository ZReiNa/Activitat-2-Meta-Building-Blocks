using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR;

public class MRGameManager : MonoBehaviour
{
    [Header("Tracking")]
    [Tooltip("The headset's center-eye transform.")]
    [SerializeField] private Transform head;

    [Tooltip("The rig's floor-level tracking-space transform.")]
    [SerializeField] private Transform floorOrigin;

    [Header("Prefabs")]
    [SerializeField] private Enemy enemyPrefab;
    [SerializeField] private EnergyOrb orbPrefab;

    [Header("Win condition")]
    [Min(1)]
    [SerializeField] private int pointsToWin = 20;

    [Header("Enemy spawning")]
    [Min(1)]
    [SerializeField] private int maxEnemies = 4;

    [Min(0.1f)]
    [SerializeField] private float enemySpawnInterval = 2f;

    [Min(0.1f)]
    [SerializeField] private float enemySpawnRadiusMin = 2f;

    [Min(0.1f)]
    [SerializeField] private float enemySpawnRadiusMax = 3f;

    [Tooltip("Height of the enemy prefab's root above the tracking floor.")]
    [Min(0f)]
    [SerializeField] private float enemyCenterHeight = 0.8f;

    [Header("Orb spawning")]
    [Min(1)]
    [SerializeField] private int maxOrbs = 2;

    [Min(0.1f)]
    [SerializeField] private float orbSpawnInterval = 5f;

    [Min(0.1f)]
    [SerializeField] private float orbSpawnRadiusMin = 0.7f;

    [Min(0.1f)]
    [SerializeField] private float orbSpawnRadiusMax = 1f;

    [Tooltip("Height of the orb above the tracking floor.")]
    [Min(0f)]
    [SerializeField] private float orbHeight = 1.1f;

    [Header("Battery")]
    [Min(1f)]
    [SerializeField] private float maxBattery = 100f;

    [Header("Wrist display")]
    [SerializeField] private TMP_Text wristText;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip enemyDefeatedSound;
    [SerializeField] private AudioClip orbCollectedSound;
    [SerializeField] private AudioClip batteryEmptySound;
    [SerializeField] private AudioClip batteryFullSound;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip restartSound;

    private readonly List<Enemy> enemies = new List<Enemy>();
    private readonly List<EnergyOrb> orbs = new List<EnergyOrb>();

    private float battery;
    private int score;
    private float nextEnemySpawnTime;
    private float nextOrbSpawnTime;
    private bool trackingReady;
    private bool playing;
    private bool won;

    public bool CanPlay => trackingReady && playing && !won;
    public float Battery => battery;

    private void OnValidate()
    {
        pointsToWin = Mathf.Max(1, pointsToWin);
        maxEnemies = Mathf.Max(1, maxEnemies);
        maxOrbs = Mathf.Max(1, maxOrbs);
        maxBattery = Mathf.Max(1f, maxBattery);
        enemySpawnInterval = Mathf.Max(0.1f, enemySpawnInterval);
        orbSpawnInterval = Mathf.Max(0.1f, orbSpawnInterval);

        enemySpawnRadiusMin = Mathf.Max(0.1f, enemySpawnRadiusMin);
        enemySpawnRadiusMax = Mathf.Max(
            enemySpawnRadiusMin,
            enemySpawnRadiusMax
        );

        orbSpawnRadiusMin = Mathf.Max(0.1f, orbSpawnRadiusMin);
        orbSpawnRadiusMax = Mathf.Max(
            orbSpawnRadiusMin,
            orbSpawnRadiusMax
        );
    }

    private IEnumerator Start()
    {
        if (head == null || floorOrigin == null ||
            enemyPrefab == null || orbPrefab == null ||
            wristText == null || audioSource == null)
        {
            Debug.LogError(
                "MRGameManager: assign Head, Floor Origin, both prefabs, " +
                "Wrist Text, and Audio Source in the Inspector.",
                this
            );

            enabled = false;
            yield break;
        }

        wristText.text = "WAITING FOR HEADSET TRACKING...";

        bool warned = false;
        float waitStarted = Time.realtimeSinceStartup;

        while (true)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.Head);

            if (device.isValid &&
                device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) &&
                tracked)
            {
                break;
            }

            if (!warned && Time.realtimeSinceStartup - waitStarted > 10f)
            {
                Debug.LogWarning(
                    "Still waiting for headset tracking. Check that the " +
                    "Quest XR provider is running and the headset is awake.",
                    this
                );

                warned = true;
            }

            yield return null;
        }

        trackingReady = true;
        ResetGame(false);
    }

    private void Update()
    {
        if (!trackingReady)
        {
            return;
        }

        if (OVRInput.GetDown(
            OVRInput.Button.One,
            OVRInput.Controller.RTouch))
        {
            ResetGame(true);
            return;
        }

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
    }

    public void DrainBattery(float amount)
    {
        if (!CanPlay || amount <= 0f || battery <= 0f)
        {
            return;
        }

        float previousBattery = battery;
        battery = Mathf.Max(0f, battery - amount);

        if (previousBattery > 0f && battery <= 0f)
        {
            PlaySound(batteryEmptySound);
        }

        UpdateWrist();
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

        if (previousBattery < maxBattery && battery >= maxBattery)
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
            won = true;
            playing = false;
            ClearRemainingObjects();
            PlaySound(winSound);
        }

        UpdateWrist();
    }

    private void SpawnEnemy()
    {
        Vector3 position = RandomPositionAroundPlayer(
            enemySpawnRadiusMin,
            enemySpawnRadiusMax,
            enemyCenterHeight
        );

        Enemy enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
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

        EnergyOrb orb = Instantiate(orbPrefab, position, Quaternion.identity);
        orb.Initialize(this);
        orbs.Add(orb);
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

    private void ResetGame(bool playRestartSound)
    {
        ClearRemainingObjects();

        battery = maxBattery;
        score = 0;
        won = false;
        playing = true;

        nextEnemySpawnTime = Time.time;
        nextOrbSpawnTime = Time.time;

        if (playRestartSound)
        {
            PlaySound(restartSound);
        }

        UpdateWrist();
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
    }

    private void UpdateWrist()
    {
        int percentage = Mathf.RoundToInt(battery / maxBattery * 100f);

        if (won)
        {
            wristText.text =
                "YOU WIN!\n" +
                $"SCORE: {score} / {pointsToWin}\n" +
                "Press A to restart";
        }
        else
        {
            wristText.text =
                $"BATTERY: {percentage}%\n" +
                $"SCORE: {score} / {pointsToWin}\n" +
                "Trigger: fire\n" +
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