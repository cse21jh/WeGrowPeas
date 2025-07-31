using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowLoop : MonoBehaviour
{
    [SerializeField][Range(0f, 200f)] float speed = 3f;
    float bounds;
    Vector2 startPos;
    float newPos;

    void Start()
    {
        startPos = transform.position;
        bounds = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void Update()
    {
        newPos = Mathf.Repeat(Time.time * speed, bounds);

        transform.position = startPos + Vector2.left * newPos;
    }
}
