using System.Collections;
using System.Collections.Generic;
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

    public void RotationToTarget(Vector3 targetPosition)
    {
        float distanceBoost = 0f;
        Vector3 targetDirection = targetPosition - transform.position;

        //boost rotation clamp if too far
        if (targetDirection.magnitude > minDistanceBoost)
        {
            distanceBoost = 90 - maxTurnAngle;
        }

        Quaternion toRotation;
        //Probably most stupid math to clamp rotation
        if (Vector3.Angle(transform.forward, targetDirection) > maxTurnAngle + distanceBoost)
        {
            Vector3 RotationVector = Vector3.Cross(transform.forward, targetDirection);
            toRotation = Quaternion.LookRotation(Quaternion.AngleAxis(maxTurnAngle, RotationVector) * transform.forward);
        }
        else
        {
            toRotation = Quaternion.LookRotation(targetDirection);
        }
        //assign new rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, 0.1f);
    }

    //FixedUpdate is called zero, one or multipe times per frame
    private void FixedUpdate()
    {
        RocketVelocity();
    }

    private void OnCollisionEnter(Collision collision)
    {
        Instantiate(explosion, rocketRigidbody.transform.position, Quaternion.identity);
        Destroy(this.gameObject);
    }

    private void RocketVelocity()
    {
        // forces affecting the rocket shamelessly stolen from AirController
        Vector3 thrustVector = rocketRigidbody.transform.forward * maxSpeed;

        // project plane vertical vector on world vertical vector
        Vector3 planeVerticalVector = rocketRigidbody.transform.up;
        Vector3 worldVerticalVector = Vector3.up;
        Vector3 planeVerticalVectorProjected = Vector3.Project(planeVerticalVector, worldVerticalVector);
        float verticalSpeedCut = planeVerticalVectorProjected.magnitude;

        float engineFunction = 1;
        float liftWithoutEngine = 1 - 0.3f;
        float wingLiftFactor = 1 * 1;

        Vector3 fullLiftVector = -Physics.gravity * verticalSpeedCut;

        // compute lift vector depending on engine and wing health
        Vector3 liftVector = (fullLiftVector * (liftWithoutEngine * wingLiftFactor)) +
                             (fullLiftVector * (0.3f * engineFunction));

        // I have no fucking idea why force has to be divided by two. Seems to work though
        Vector3 finalVelocity = thrustVector * rocketRigidbody.mass / 2 + liftVector * rocketRigidbody.mass;

        rocketRigidbody.AddForce(finalVelocity, ForceMode.Force);
    }
}
