using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketPlayerControll : MonoBehaviour
{
    [Header("Used GameObjects")]
    [SerializeField] private Rigidbody rocketRigidbody;
    [SerializeField] private ParticleSystem explosion;

    [Header("Rocket Parameters")]
    [SerializeField] private float maxSpeed;
    [SerializeField] private float steeringSens;

    private RocketBehaviour rocketMovement;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        rocketMovement = new RocketBehaviour(rocketRigidbody, maxSpeed);
    }

    // Update is called once per frame
    void Update()
    {
        RocketSteering();
    }

    //FixedUpdate is called zero, one or multipe times per frame
    private void FixedUpdate()
    {
        rocketMovement.RocketVelocity();
    }

    private void RocketSteering()
    {
        float mX = Input.GetAxis("Mouse X") * steeringSens * Time.deltaTime;
        float mY = Input.GetAxis("Mouse Y") * steeringSens * Time.deltaTime * -1;

        transform.Rotate(mY, mX, 0f);
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0f);
    }
}
