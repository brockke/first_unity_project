using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 20f;
    [SerializeField] private Rigidbody2D rb;
    public GameObject SpawnedBy { get; set; }

    void Start(){
        rb.velocity = transform.right * speed;
    }
    //void OnTriggerEnter(Collider collider) {
    //    //if (collider.tag == "Bullet") {
    //        Debug.Log(collider);
    //    //}
    //}
}
