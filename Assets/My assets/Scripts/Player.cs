using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public int[] coords = new int[2];
    public bool moving;
    public float moveDuration;
    public float moveTime = 0;
    Vector3 startCoords;
    Vector3 targetCoords;
    string state;
    Animator animator;
    SpriteRenderer spriteRenderer;
    [SerializeField] LevelLoader level;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        moveDuration = 0.3f;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!moving)
        {
            animator.speed = 0;
            if (Keyboard.current.wKey.isPressed)
            {
                tryMove(0, -1);
                animator.Play("MoveUp", 0);
            } else if (Keyboard.current.aKey.isPressed)
            {
                tryMove(-1, 0);
                animator.Play("MoveRight", 0);
                spriteRenderer.flipX = true;
            } else if (Keyboard.current.sKey.isPressed)
            {
                tryMove(0, 1);
                animator.Play("MoveDown", 0);
            } else if (Keyboard.current.dKey.isPressed)
            {
                tryMove(1, 0);
                animator.Play("MoveRight", 0);
                spriteRenderer.flipX = false;
            } 
        } else
        {
            animator.speed = 1;
            moveTime += Time.deltaTime;
            float percentMove = moveTime/moveDuration;
            if (percentMove < 1)
            {
                transform.position = Vector3.Lerp(startCoords, targetCoords, percentMove);
            } else
            {
                transform.position = targetCoords;
                moveTime = 0;
                moving = false;
            }
        }
    }
    public void tryMove(int Y, int X)
    {
        int y = coords[1] + X;
        int x = coords[0] + Y;
        if (level.TryMove(x, y))
        {
            startCoords = level.CoordToPosition(coords);
            coords[1] = y;
            coords[0] = x;
            moving = true;
            targetCoords = level.CoordToPosition(coords);
        }
    }
}
