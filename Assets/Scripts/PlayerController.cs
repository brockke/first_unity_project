using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public Transform crosshair;
    Rigidbody2D rb;
    float speed = 10;
    float jumpHeight = 7;
    float fallMultiplier = 3f;
    float lowJumpMultiplier = 8f;
    bool canJump = true;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();        
    }

    // Update is called once per frame
    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        //float y = Input.GetAxis("Vertical");
        //Vector2 dir = new Vector2(x, y);
        Walk(x);

        if (Physics2D.Raycast(transform.position, Vector2.down, 0.55f, LayerMask.GetMask("Floor"))) {
            canJump = true;
        }
        if (Input.GetButtonDown("Jump") && canJump) {
            canJump = false;
            Jump();
        }

        // TODO: Currently this should only work on the one keypress from jump but you can do multiple,
        // chaing the velocity
        if (rb.velocity.y < 0) {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        else if (rb.velocity.y > 0 && !Input.GetButton("Jump")) {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }

        Vector2 raycastDir = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        var hit = Physics2D.Raycast(transform.position, raycastDir, 10, 65, 0);
        if (hit) {
            Debug.Log(hit.point);
            crosshair.position = hit.point;
        }
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

        Vector2 raycastDir = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        Gizmos.DrawRay(transform.position, raycastDir);
    }
}
