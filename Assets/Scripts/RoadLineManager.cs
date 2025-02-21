using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DG.Tweening;

public class RoadLineManager : MonoBehaviour
{
    [Header("Line Spawn Settings")]
    public GameObject roadLinePrefab;
    public int poolSize = 4;
    
    [Header("Line Position Settings")]
    public float startYPosition = 5.7f;
    public float startXPosition = 0.15f;
    public float endYPosition = -1f;
    public float fixedLineSpacing = 1.8f;
    
    [Header("Line Scale Settings")]
    public float startScaleX = 0.05f;
    public float startScaleY = 0.02f;
    public float endScaleX = 0.4f;
    public float endScaleY = 0.5f;
    
    [Header("Movement Settings")]
    public float initialMoveSpeed = 5f;
    public float currentMoveSpeed;
    public float maxMoveSpeed = 15f;
    public float accelerationRate = 2f;
    
    [Header("Line Settings")]
    public int maxVisibleLines = 4;

    private List<GameObject> linePool;
    private float[] linePositions;
    private float[] spacings = new float[] { 0.8f, 1.2f, 1.8f, 2.5f, 3.3f, 4.2f, 5.2f, 6.3f, 7.5f,8.8f };
    
    private void Awake()
    {
        linePool = new List<GameObject>();
        linePositions = new float[maxVisibleLines];
        InitializePool();
        currentMoveSpeed = initialMoveSpeed;
        
        DOTween.SetTweensCapacity(tweenersCapacity: 8000, sequencesCapacity: 200);
    }

    private void Start()
    {
        UpdateLinePositions();
        SpawnInitialLines();
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject line = Instantiate(roadLinePrefab);
            line.SetActive(false);
            linePool.Add(line);
        }
    }

    private void UpdateLinePositions()
    {
        linePositions[0] = startYPosition;
        
        for (int i = 1; i < maxVisibleLines; i++)
        {
            float spacing = fixedLineSpacing * spacings[i-1] * 3;
            linePositions[i] = linePositions[i-1] - spacing;
        }
    }
    
    private void SpawnInitialLines()
    {
        for (int i = 0; i < maxVisibleLines; i++)
        {
            SpawnLineAtPosition(linePositions[i]);
        }
    }

    private void SpawnLineAtPosition(float yPosition)
    {
        GameObject line = GetInactiveLine();
        if (line != null)
        {
            float t = (startYPosition - yPosition) / (startYPosition - endYPosition);
            t = Mathf.Clamp01(t);
            
            float scaleX = Mathf.Lerp(startScaleX, endScaleX, t);
            float scaleY = Mathf.Lerp(startScaleY, endScaleY, t);
            
            line.transform.position = new Vector3(startXPosition, yPosition, 0);
            line.transform.localScale = new Vector3(scaleX, scaleY, 1);
            
            // 초기 속도 계산 및 적용
            float initialSpeedMultiplier = scaleY / startScaleY;
            float initialSpeed = currentMoveSpeed * initialSpeedMultiplier;
        
            // 여기에 초기 속도를 라인에 적용하는 로직 추가
            line.GetComponent<Rigidbody2D>().velocity = Vector3.down * initialSpeed;
            
            line.SetActive(true);
        }
    }
    
    private void Update()
    {
        //UpdateLines();
        UpdateDoTweenLines();
    }

    private void UpdateLines()
    {
        int visibleLineCount = 0;
        float highestY = float.MinValue;

        foreach (GameObject line in linePool)
        {
            if (line.activeInHierarchy)
            {
                Rigidbody2D rb = line.GetComponent<Rigidbody2D>();
                
                Vector3 pos = line.transform.position;
                float t = (startYPosition - pos.y) / (startYPosition - endYPosition);
                t = Mathf.Clamp01(t);

                float scaleX = Mathf.Lerp(startScaleX, endScaleX, t);
                float scaleY = Mathf.Lerp(startScaleY, endScaleY, t);

                float speedMultiplier = scaleY / startScaleY;
                float targetSpeed = currentMoveSpeed * speedMultiplier;
                // 스케일이 클수록 더 빠르게 이동하도록 수정
                //pos.y -= (currentMoveSpeed * speedMultiplier) * Time.deltaTime;
            
                // 속도를 부드럽게 조정
                rb.velocity = Vector3.Lerp(rb.velocity, Vector3.down * targetSpeed, Time.deltaTime * 5f);
                
                line.transform.localScale = new Vector3(scaleX, scaleY, 1);
                line.transform.position = pos;

                if (pos.y < endYPosition)
                {
                    line.SetActive(false);
                }
                else
                {
                    highestY = Mathf.Max(highestY, pos.y);
                    visibleLineCount++;
                }
            }
        }

        if (visibleLineCount < maxVisibleLines)
        {
            if (highestY == float.MinValue)
            {
                SpawnLineAtPosition(startYPosition);
            }
            else
            {
                int currentIndex = visibleLineCount - 1;
                float spacing = fixedLineSpacing * spacings[currentIndex];
                float newYPos = highestY + spacing;
                
                if (newYPos <= startYPosition)
                {
                    SpawnLineAtPosition(newYPos);
                }
            }
        }
    }

    private void UpdateDoTweenLines()
    {
        int visibleLineCount = 0;
        float highestY = float.MinValue;
        
        foreach (GameObject line in linePool)
        {
            if (line.activeInHierarchy)
            {
                Vector3 pos = line.transform.position;
                float t = (startYPosition - pos.y) / (startYPosition - endYPosition);
                t = Mathf.Clamp01(t);

                // DOTween 스케일 변경
                line.transform.DOScale(new Vector3(Mathf.Lerp(startScaleX, endScaleX, t),
                        Mathf.Lerp(startScaleY, endScaleY, t),
                        1),
                    Time.deltaTime).SetAutoKill(false);
                // 스케일에 따른 속도계산
                float speedMultiplier = line.transform.localScale.y / startScaleY;
                float moveDistance = currentMoveSpeed * speedMultiplier * Time.deltaTime;

                //DoTween 위치 변경
                line.transform.DOMoveY(pos.y - moveDistance, Time.deltaTime)
                    .SetAutoKill(false)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        if (line.transform.position.y < endYPosition)
                        {
                            line.SetActive(false);
                            visibleLineCount--;
                        }
                    });

                highestY = Mathf.Max(highestY, pos.y);
                visibleLineCount++;

            }
        }
        
        if (visibleLineCount < maxVisibleLines)
        {
            if (highestY == float.MinValue)
            {
                SpawnLineAtPosition(startYPosition);
            }
            else
            {
                int currentIndex = visibleLineCount - 1;
                float spacing = fixedLineSpacing * spacings[currentIndex];
                float newYPos = highestY + spacing;
                
                if (newYPos <= startYPosition)
                {
                    SpawnLineAtPosition(newYPos);
                }
            }
        }
    }
    
    private GameObject GetInactiveLine()
    {
        return linePool.Find(line => !line.activeInHierarchy);
    }

    public void IncreaseSpeed()
    {
        currentMoveSpeed = Mathf.Min(currentMoveSpeed + accelerationRate, maxMoveSpeed);
    }
    
    public void DecreaseSpeed()
    {
        currentMoveSpeed = Mathf.Max(currentMoveSpeed - accelerationRate, initialMoveSpeed);
    }
    
    public void ResetSpeed()
    {
        currentMoveSpeed = initialMoveSpeed;
    }
}