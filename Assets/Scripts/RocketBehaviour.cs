using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RocketBehaviour : MonoBehaviour
{
    [Header("Used GameObjects")]
    [SerializeField] private Rigidbody rocketRigidbody;
    [SerializeField] private ParticleSystem explosion;

    [Header("Rocket Parameters")]
    [SerializeField] private float maxSpeed;
    [SerializeField] private float maxTurnAngle;
    [SerializeField] private float minDistanceBoost;

    [Header("Player Rocket Parameters")]
    [SerializeField] private bool PlayerControlled;
    [SerializeField] private float SteeringSens;

    [Header("Rocket Targets")]
    [SerializeField] private GameObject targetMain;
    [SerializeField] private GameObject targetSecondary;

    //Some consts
    private Vector3 inverseGravityVector = Vector3.up * 10f;
    private Vector3 gravityVector = Vector3.down * 10f;
    private Vector3 distanceToPlane;

    // Start is called before the first frame update
    void Start()
    {
        if (PlayerControlled)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // Update is called once per frame
    void Update()
    {
        RocketVelocity();

        if(PlayerControlled)
        {
            RockeSteering();
        }
        else
        {
            RocketTargeing();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.name == "Raptor")
        {
            Destroy(this.gameObject);
        }
        if (collision.gameObject.name == "Terrain")
        {
            Instantiate(explosion, rocketRigidbody.transform.position, Quaternion.identity);
            Destroy(this.gameObject);
        }

    }

    void RocketVelocity()
    {
        // forces affecting the rocket shamelessly stolen from AirController
        // project plane vertical vector on world vertical vector
        Vector3 planeVerticalVectorProjected = Vector3.Project(rocketRigidbody.transform.up, Vector3.up);
        float verticalSpeedCut = planeVerticalVectorProjected.magnitude;

        // compute lift vector (consider lift force to be proportional to vertical speed) 
        Vector3 liftVector = inverseGravityVector * verticalSpeedCut;
        Vector3 thrustVector = transform.forward.normalized * maxSpeed;
        Vector3 finalVelocity = thrustVector + liftVector + gravityVector;
        rocketRigidbody.velocity = finalVelocity;
    }

    void RocketTargeing()
    {
        float distanceBoost = 0f;
        Vector3 target = targetMain.transform.position - transform.position;

        //boost rotation clamp if too far
        if (distanceToPlane.magnitude > minDistanceBoost)
        {
            distanceBoost = 90 - maxTurnAngle;
        }

        //check if secondary target is seen
        RaycastHit hit;
        if (Physics.SphereCast(transform.position, 10f, transform.forward, out hit))
        {
            if (hit.collider.name == targetSecondary.name + "(Clone)")
            {
                target = new Vector3(hit.transform.position.x,
                    hit.transform.position.y - 100f,
                    hit.transform.position.z);
            }
        }

        Quaternion toRotation;
        //Probably most stupid math to clamp rotation
        if (Vector3.Angle(transform.forward, target) > maxTurnAngle + distanceBoost)
        {
            Vector3 RotationVector = Vector3.Cross(transform.forward, target);
            toRotation = Quaternion.LookRotation(Quaternion.AngleAxis(maxTurnAngle, RotationVector) * transform.forward);
        }
        else
        {
            toRotation = Quaternion.LookRotation(target);
        }
        //assign new rotation
        transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, 1.25f * Time.deltaTime);
    }


    private void RockeSteering()
    {
        float M_x = Input.GetAxis("Mouse X") * SteeringSens * Time.deltaTime;
        float M_y = Input.GetAxis("Mouse Y") * SteeringSens * Time.deltaTime * -1;

        transform.Rotate(M_y, M_x, 0f);
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0f);
    }

}
