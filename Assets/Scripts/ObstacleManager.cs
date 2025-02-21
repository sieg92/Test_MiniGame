using UnityEngine;
using System.Collections.Generic;

public class ObstacleManager : MonoBehaviour
{
    // 도로의 전체 너비와 카메라 높이 설정
    [Header("Road Settings")]
    public int roadWidth = 1;
    public float cameraHeight = 1000;
    public float speed = 0.8f;
    
    [Header("Obstacle Settings")]
    public GameObject[] obstaclePrefabs;
    [SerializeField] private float obstacleSpeed = 2f;
    [SerializeField] private float spawnY = 3.16f;
    [SerializeField] private float endYPosition = -6f;
    [SerializeField] private int poolSize = 5;
    [SerializeField] private int maxObstacles = 4; // 동시에 존재할수 있는 최대 장애물 수
    [SerializeField] private float minObstacleSpacing = 0.5f; // 장애물 X간 최소 간격
    [SerializeField] private float heightCheck = 1f; // 장애물 Y간 최소 간격
    [SerializeField] private int consecutiveNum = 4; //연속 장애물 허용 범위
    [SerializeField] private float spawnRate = 0.02f;
    
    private Queue<GameObject>[] obstaclePools;
    private List<GameObject> activeObstacles = new List<GameObject>();
    private int consecutiveSpawnCount = 0;    // 연속 생성 횟수
    private bool? lastSpawnSide = null;       // 마지막 생성 방향 (null: 초기상태)

    private float spawnTimer;
    private readonly Vector3 scaleVector =Vector3.one;
    private void Awake()
    {
        activeObstacles = new List<GameObject>(maxObstacles);
        InitializeObstaclePools();
    }

    private void InitializeObstaclePools()
    {
        obstaclePools = new Queue<GameObject>[obstaclePrefabs.Length];
    
        for (int i = 0; i < obstaclePrefabs.Length; i++)
        {
            obstaclePools[i] = new Queue<GameObject>();
            for (int j = 0; j < poolSize; j++)
            {
                CreateObstacles(i);
            }
        }
    }

    private void CreateObstacles(int typeIndex)
    {
        GameObject obj = Instantiate(obstaclePrefabs[typeIndex], transform);
        obj.SetActive(false);
        obj.AddComponent<ObstacleData>();
        obstaclePools[typeIndex].Enqueue(obj);
    }

    private void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnRate)
        {
            spawnTimer = 0f;
            if (Random.value < spawnRate)
            {
                SpawnObstacle();
            }
        }
        UpdateObstacles();
    }

    private void UpdateObstacles()
    {
        Vector3 position;
        float progress, scale, xOffset, newX, newY;
        
        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            GameObject obstacle = activeObstacles[i];
            ObstacleData data = obstacle.GetComponent<ObstacleData>();
            position = obstacle.transform.position;
            
            // 장애물의 진행도 계산 (0~1 사이 값)
            progress = (spawnY - position.y) / (spawnY - (transform.position.y + endYPosition));
            // 진행도에 따른 크기 보간
            scale = Mathf.Lerp(0.1f, 3f, progress);
            // 바깥쪽으로 이동하는 힘을 더 강하게 수정
            xOffset = data.isLeftSide ? -progress * 4f : progress * 4f;
            newX = data.initialX + (xOffset * roadWidth);
            // 스케일이 커질수록 더 빠르게 이동 //scale 0.1_기본속도 1, scale 1.5_기본속도 4.5, scale 최대_기본속도 8.25
            newY = position.y - (obstacleSpeed * (1f + (scale - 0.1f) * 2.5f) * Time.deltaTime);
            
            position.x = newX;  
            position.y = newY;
            obstacle.transform.position = position;
            obstacle.transform.localScale = scaleVector * scale;

            if (newY < transform.position.y + endYPosition)
            {
                ReturnToPool(obstacle);
                activeObstacles.RemoveAt(i);
            }
        }
    }
    

    public class ObstacleData : MonoBehaviour // 장애물의 초기 x위치와 방향을 저장하는 컴포넌트
    {
        public float initialX;
        public bool isLeftSide;
    }
    
    private bool IsValidSpawnPosition(Vector3 newPosition)
    {
        
        foreach (GameObject obstacle in activeObstacles)
        {
            // y축 간격이 heightCheck의 간격보다 작은지 체크
            if (Mathf.Abs(obstacle.transform.position.y - newPosition.y) < heightCheck)
            {
                // x축 간격이 최소 간격보다 작으면 생성 불가
                if (Mathf.Abs(obstacle.transform.position.x - newPosition.x) < minObstacleSpacing)
                {
                    return false;
                }
            }
        }
        return true;
    }

    void SpawnObstacle()
    {
        if (activeObstacles.Count >= maxObstacles)
        {
            return;
        }

        //50% 확률로 장애물 왼쪽, 오른쪽으로 스폰
        bool spawnOnLeft = Random.value > 0.5f;
        HandleConsecutiveSpawns(ref spawnOnLeft);

        int typeIndex = Random.Range(0, obstaclePrefabs.Length);
        GameObject obstacle = GetObstacleFromPool(typeIndex);

        if (obstacle == null)
        {
            return;
        }

        // 초기 생성 위치를 도로 중앙 근처로 수정 //0.1 수치 증가시키면 장애물이 도로의 바깥쪽 생성,감소 >>중앙에 가깝게 생성.
        float xPos = spawnOnLeft ? -roadWidth * 0.1f : roadWidth * 0.1f;
        Vector3 newPosition = new Vector3(xPos, spawnY, 0);

        // 새로운 위치가 적절한 간격을 유지하는지 확인
        if (!IsValidSpawnPosition(newPosition))
        {
            ReturnToPool(obstacle);
            return;
        }

        SetupObstacle(obstacle, xPos, spawnOnLeft);
    }

    private void HandleConsecutiveSpawns(ref bool spawnOnLeft) //연속으로 같은 방향에 장애물이 생성되는 것을 제어
    {
        // 이전과 같은 방향으로 생성하려는 경우
        if (lastSpawnSide.HasValue && spawnOnLeft == lastSpawnSide.Value)
        {
            consecutiveSpawnCount++;
            // consecutiveNum번 연속 생성되면 반대편에 생성
            if (consecutiveSpawnCount >= consecutiveNum)
            {
                spawnOnLeft = !lastSpawnSide.Value;
                consecutiveSpawnCount = 0;
            }
        }
        else
        {
            consecutiveSpawnCount = 0;
        }
        lastSpawnSide =spawnOnLeft;
    }

    private void SetupObstacle(GameObject obstacle, float xPos, bool isLeft) // 생성된 장애물의 초기 설정을 담당
    {
        obstacle.transform.position = new Vector3(xPos, spawnY, 0);
        obstacle.transform.localScale = scaleVector * 0.1f;

        var data = obstacle.GetComponent<ObstacleData>();
        data.initialX = xPos;
        data.isLeftSide = isLeft;
        
        obstacle.SetActive(true);
        activeObstacles.Add(obstacle);
    }

    void ReturnToPool(GameObject obstacle)
    {
        obstacle.SetActive(false);
        for (int i = 0; i < obstaclePrefabs.Length; i++)
        {
            if (obstacle.name.Contains(obstaclePrefabs[i].name))
            {
                obstaclePools[i].Enqueue(obstacle);
                break;
            }
        }
    }

    GameObject GetObstacleFromPool(int typeIndex)
    {
        return obstaclePools[typeIndex].Count > 0 ? obstaclePools[typeIndex].Dequeue() : null;
    }
}