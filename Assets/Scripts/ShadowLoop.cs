using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowLoop : MonoBehaviour
{
    [SerializeField][Range(0f, 200f)] float speed = 3f;
    private float normalSpeed = 3f;
    private float targetSpeed = 0f;

    float timeElapsed = 0f;

    float bounds;
    Vector2 startPos;
    float newPos;

    void Start()
    {
        normalSpeed = speed;
        targetSpeed = speed;
        startPos = transform.position;
        bounds = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    private void FixedUpdate()
    {
        speed = Mathf.Lerp(speed, targetSpeed, Time.fixedDeltaTime * 5f);
        timeElapsed += Time.deltaTime * speed;
    }

    public void SetWaveSpeed(float waveSpeed)
    {
        targetSpeed = waveSpeed;
    }

    public void SetNormalSpeed()
    {
        targetSpeed = normalSpeed;
    }

    void Update()
    {
        newPos = Mathf.Repeat(timeElapsed, bounds);

        transform.position = startPos + Vector2.left * newPos;
    }
}
