using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Min(0)] public float range = 10;
    public Transform player;
    public float speed = 1;


    Rigidbody2D rb;
    float jumpHeight = 7;
    //float fallMultiplier = 3f;
    //float lowJumpMultiplier = 8f;
    public bool canJump = true;
    public Vector3 lastPlayerSeenPos = Vector3.negativeInfinity;
    public bool playerSeen = false;

    private float nextActionTime = 5f;
    public float period = 5f;
    public GameObject bulletPrefab;
    public float projectileVelocity = 10f;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update() {
        if (seePlayer(player)) {
            lastPlayerSeenPos = player.position;
            playerSeen = true;
        }
        if (seePlayer(player) && Time.time > nextActionTime) {
            nextActionTime = Time.time + period;

            Vector3 dirNorm = (lastPlayerSeenPos - transform.position).normalized;
            float angle = Mathf.Atan2(dirNorm.y, dirNorm.x) * Mathf.Rad2Deg;
            var bullet = Instantiate(bulletPrefab, transform.position, Quaternion.AngleAxis(angle, Vector3.forward));
        }
        if (playerSeen) {
            Walk(((lastPlayerSeenPos.x - transform.position.x) >= 0) ? 1 : -1);
        }

        //if (Physics2D.Raycast(transform.position, Vector2.down, 0.55f, LayerMask.GetMask("Floor"))) {
        //    canJump = true;
        //}
        //if ((Time.time > nextActionTime) && canJump) {
        //    canJump = false;
        //    Jump();
        //}
    }
    bool seePlayer(Transform player) {
        Vector2 raycastDir = player.position - transform.position;
        var floorHit = Physics2D.Raycast(transform.position, raycastDir, range, 64, 0);
        var playerHit = Physics2D.Raycast(transform.position, raycastDir, range, 256, 0);
        // Not hitting a wall but hitting a player

        return(floorHit.distance == 0 && playerHit.distance != 0);
    }
    private void Walk(float xDir) {
        rb.velocity = new Vector2(xDir * speed, rb.velocity.y);
    }
    private void Jump() {
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.velocity += Vector2.up * jumpHeight;
    }
    void OnDrawGizmos() {
        Gizmos.color = Color.red;

        Vector2 raycastDir = lastPlayerSeenPos - transform.position;
        Gizmos.DrawRay(transform.position, raycastDir);
    }
    void OnTriggerEnter2D(Collider2D collider) {
        if (collider.tag == "Bullet") {
            Debug.Log(collider);
        }
    }
}
