using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Min(0)] public float range = 10;
    public Transform player;
    public bool seePlayer = false;
    public float speed = 1;


    Rigidbody2D rb;
    float jumpHeight = 7;
    float fallMultiplier = 3f;
    float lowJumpMultiplier = 8f;
    public bool canJump = true;

    private float nextActionTime = 5f;
    public float period = 5f;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update() {
        //inRange = (Vector2.Distance(player.position, transform.position) <= range);
        RaycastTowardsPlayer(player);
        if (seePlayer) {
            Walk(((player.position.x - transform.position.x) >= 0) ? 1 : -1);
        }

        if (Physics2D.Raycast(transform.position, Vector2.down, 0.55f, LayerMask.GetMask("Floor"))) {
            canJump = true;
        }
        if ((Time.time > nextActionTime) && canJump) {
            canJump = false;
            nextActionTime = Time.time + period;
            Jump();
        }
    }
    void RaycastTowardsPlayer(Transform player) {
        Vector2 raycastDir = player.position - transform.position;
        var floorHit = Physics2D.Raycast(transform.position, raycastDir, range, 64, 0);
        var playerHit = Physics2D.Raycast(transform.position, raycastDir, range, 256, 0);
        // Not hitting a wall but hitting a player
        seePlayer = (floorHit.distance == 0 && playerHit.distance != 0);
    }
    private void Walk(float xDir) {
        rb.velocity = new Vector2(xDir * speed, rb.velocity.y);
    }
    private void Jump() {
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.velocity += Vector2.up * jumpHeight;
    }
}