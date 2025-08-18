// Refactored ItemSpawner.cs for clear enemy-type separation and modular logic
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    public enum GROUP_TYPE { NORMAL, CHASER, SLOW, HIGH, ADV }

    [Header("Enemy Spawning")]
    public GameObject mobgroup;
    public Item[] enemyPrefabs;
    public float spawnRate = 1.2f;
    public GROUP_TYPE currentGroupType = GROUP_TYPE.NORMAL;
    public GROUP_TYPE nextGroupType = GROUP_TYPE.NORMAL;

    [Header("Spawn Area")]
    public float horizontalLeft = -8;
    public float horizontalRight = 8;
    public float verticalBottom = 0;
    public float verticalTop = 16.5f;

    [Header("Player Reference")]
    [SerializeField] private Player player;

    private float nextSpawnTime = 0f;
    private float gameTime = 0f;

    private void Start()
    {
        if (player == null)
            player = FindObjectOfType<Player>();

        currentGroupType = GROUP_TYPE.NORMAL;
        nextGroupType = GROUP_TYPE.NORMAL;
    }

    private void Update()
    {
        gameTime = Time.time;

        if (gameTime >= nextSpawnTime)
        {
            SpawnEnemyWave();
            nextSpawnTime = gameTime + (1f / spawnRate);
        }
    }

    private void SpawnEnemyWave()
    {
        Vector2 spawnPosition = GetRandomEdgeSpawnPosition();

        if (gameTime < 10f)
        {
            SpawnEnemy(enemyPrefabs[0], spawnPosition); // footman
        }
        else if (gameTime < 40f)
        {
            Vector2 adjustedPosition = GetAdjustedWingmanPosition();
            SpawnEnemy(enemyPrefabs[1], adjustedPosition); // wingman
        }
        else
        {
            // Future logic for advanced enemy groups
            // Add more conditions and use group_type/nextGroupType to branch
        }
    }

    private Vector2 GetRandomEdgeSpawnPosition()
    {
        int edge = Random.Range(0, 4);
        switch (edge)
        {
            case 0: return new Vector2(horizontalLeft, Random.Range(verticalBottom, verticalTop));
            case 1: return new Vector2(horizontalRight, Random.Range(verticalBottom, verticalTop));
            case 2: return new Vector2(Random.Range(horizontalLeft, horizontalRight), verticalTop);
            case 3: return new Vector2(Random.Range(horizontalLeft, horizontalRight), verticalBottom);
            default: return Vector2.zero;
        }
    }

    private Vector2 GetAdjustedWingmanPosition()
    {
        float playerY = player.transform.position.y;
        float spawnY = Mathf.Min(verticalBottom, playerY + Random.Range(1.9f, 5.1f));
        float spawnX = Random.value > 0.5f ? horizontalLeft : Random.Range(horizontalLeft, horizontalRight);
        return new Vector2(spawnX, spawnY);
    }

    private void SpawnEnemy(Item enemyPrefab, Vector2 spawnPosition)
    {
        if (enemyPrefab != null)
        {
            Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("Enemy prefab is not assigned!");
        }
    }
}
