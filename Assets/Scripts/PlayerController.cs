using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerController : MonoBehaviour {
    public float speed = 10;
    public float jumpHeight = 7;
    [SerializeField] private GameObject crosshair;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float fallMultiplier = 3f;
    [SerializeField] private float lowJumpMultiplier = 8f;
    [SerializeField] private GameObject bulletPrefab;
    private bool canJump = true;

    // Start is called before the first frame update
    void Start() {
        rb = GetComponent<Rigidbody2D>();        
    }

    void Update() {
        float x = Input.GetAxis("Horizontal");
        //float y = Input.GetAxis("Vertical");
        //Vector2 dir = new Vector2(x, y);
        Walk(x);

        if (Physics2D.Raycast(transform.position, Vector2.down, 0.55f,
            (LayerMask.GetMask("Floor") + LayerMask.GetMask("Default") + LayerMask.GetMask("Bullet")))) {
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

        Vector3 raycastDir = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        //Debug.Log(raycastDir);
        var hit = Physics2D.Raycast(transform.position, raycastDir, 10, 65, 0);
        if (hit) {
            crosshair.GetComponent<Renderer>().enabled = true;
            crosshair.transform.position = hit.point;
        }
        else {
            crosshair.GetComponent<Renderer>().enabled = false;
        }

        if (Input.GetButtonDown("Fire1")) {
            Vector3 dirNorm = raycastDir.normalized;
            float angle = Mathf.Atan2(dirNorm.y, dirNorm.x) * Mathf.Rad2Deg;
            var bullet = Instantiate(bulletPrefab, transform.position, Quaternion.AngleAxis(angle, Vector3.forward));
            bullet.GetComponent<Bullet>().SpawnedBy = gameObject;
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
        Gizmos.color = Color.cyan;

        Vector2 raycastDir = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        Gizmos.DrawRay(transform.position, raycastDir);
    }
    void OnTriggerEnter2D(Collider2D collider) {
        var bullet = collider.gameObject.GetComponent<Bullet>();
        if (collider.tag == "Bullet" && bullet.SpawnedBy != gameObject) {
            Debug.Log(collider);
        }
    }
}
