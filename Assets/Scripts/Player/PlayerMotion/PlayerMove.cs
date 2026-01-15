using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public float speed = 3;

    private Animator animator;
    public Vector3 movement;
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        
        movement = new Vector3(x, 0, z);
        
        transform.LookAt(transform.position + movement);
        transform.position += movement * speed *  Time.deltaTime;

        UpdateAnim();
    }

    void UpdateAnim()
    {
        animator.SetFloat("Speed", movement.magnitude);
    }
}
