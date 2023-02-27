using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RocketBehaviour : MonoBehaviour
{
    [SerializeField] private bool PlayerControlled;
    [SerializeField] private Rigidbody rocketRigidbody;
    [SerializeField] private float maxSpeed;

    [SerializeField] private GameObject targetPlane;
    [SerializeField] private GameObject targetFlare;

    [SerializeField] private ParticleSystem explosion;

    [SerializeField] private TextMeshProUGUI distanceText;//Delete after all debug ended

    [SerializeField, Range(0.0015f, 0.01f)] private float aimModifier;//a.k.a. "craziness level"

    private Vector3 inverseGravityVector = Vector3.up * 10f;
    private Vector3 gravityVector = Vector3.down * 10f;
    private const float SteeringSens = 26;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        RocketVelocity();

        if(targetPlane != null)
        {
            Vector3 distanceToPlane = targetPlane.transform.position - transform.position;

            distanceText.text = $"Rocket Distance: { distanceToPlane.magnitude }";//Delete after all debug ended
        }

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
        Vector3 target = targetPlane.transform.position;

        RaycastHit hit;
        if (Physics.SphereCast(transform.position, 10f, transform.forward, out hit))
        {
            if (hit.collider.name == targetFlare.name + "(Clone)")
            {
                target = new Vector3(hit.transform.position.x,
                    hit.transform.position.y - 100f,
                    hit.transform.position.z);
            }
        }

        Quaternion toRotation = Quaternion.LookRotation(target - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, aimModifier);
    }


    private void RockeSteering()
    {
        // that's also was shamelessly stolen from AirController
        float vert = Input.GetAxisRaw("Vertical") * SteeringSens;
        float hor = Input.GetAxisRaw("Horizontal") * SteeringSens;
        float roll = Input.GetAxisRaw("Roll") * SteeringSens;

        // get plane torque in local space
        Vector3 torque = rocketRigidbody.transform.InverseTransformDirection(rocketRigidbody.angularVelocity);
        torque *= 2;

        Vector3 targetTorque = new Vector3(-vert, hor, roll);
        rocketRigidbody.AddRelativeTorque(targetTorque - torque, ForceMode.Force);

        
    }

}
