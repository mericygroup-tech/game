using UnityEngine;
using UnityEngine.Serialization;

public class MinionSpawner3D : MonoBehaviour
{
    [FormerlySerializedAs("enemyPrefab")]
    public GameObject minionPrefab;
    public Transform player;
    public Transform[] spawnPoints;

    public int maxEnemies = 3;
    public float spawnInterval = 4f;
    public int initialSpawnCount = 1;
    public bool spawnOnStart = true;

    private float lastSpawnTime;
    private int currentEnemyCount;
    private bool spawningActive;

    private void Start()
    {
        currentEnemyCount = 0;
        spawningActive = spawnOnStart;
        if (spawningActive)
            SpawnInitialEnemies();

        lastSpawnTime = Time.time;
    }

    private void Update()
    {
        if (!spawningActive)
            return;

        if (minionPrefab == null || player == null || spawnPoints == null || spawnPoints.Length == 0)
            return;

        if (currentEnemyCount >= maxEnemies)
            return;

        if (Time.time >= lastSpawnTime + spawnInterval)
        {
            SpawnEnemy();
            lastSpawnTime = Time.time;
        }
    }

    private void SpawnInitialEnemies()
    {
        for (int i = 0; i < initialSpawnCount; i++)
        {
            if (currentEnemyCount < maxEnemies)
            {
                SpawnEnemyAt(GetInitialSpawnPoint(i));
            }
        }
    }

    public void BeginSpawning()
    {
        if (spawningActive)
            return;

        spawningActive = true;
        SpawnInitialEnemies();
        lastSpawnTime = Time.time;
    }

    private void SpawnEnemy()
    {
        SpawnEnemyAt(spawnPoints[Random.Range(0, spawnPoints.Length)]);
    }

    private Transform GetInitialSpawnPoint(int index)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        return spawnPoints[index % spawnPoints.Length];
    }

    private void SpawnEnemyAt(Transform spawnPoint)
    {
        if (spawnPoint == null || minionPrefab == null)
            return;

        Quaternion spawnRotation = Quaternion.Euler(0f, spawnPoint.eulerAngles.y, 0f);

        GameObject enemy = Instantiate(
            minionPrefab,
            spawnPoint.position,
            spawnRotation
        );
        enemy.transform.rotation = spawnRotation;

        MinionChase3D enemyChase = enemy.GetComponent<MinionChase3D>();

        if (enemyChase != null)
        {
            enemyChase.target = player;
            enemyChase.chaseRange = Mathf.Max(enemyChase.chaseRange, 120f);
            enemyChase.ResetForSpawn(player);
            enemyChase.ForceVisualAnimation(enemyChase.moveState);
        }

        MinionDeathNotifier deathNotifier = enemy.GetComponent<MinionDeathNotifier>();

        if (deathNotifier == null)
        {
            deathNotifier = enemy.AddComponent<MinionDeathNotifier>();
        }

        deathNotifier.spawner = this;
        deathNotifier.notifyOnlyWhenKilled = true;

        currentEnemyCount++;

        Debug.Log("Spawn Hắc Tinh. Số quái hiện tại: " + currentEnemyCount);
    }

    public void NotifyEnemyDied()
    {
        currentEnemyCount--;

        if (currentEnemyCount < 0)
            currentEnemyCount = 0;

        Debug.Log("Một Hắc Tinh đã bị tiêu diệt. Còn lại: " + currentEnemyCount);
    }
}
