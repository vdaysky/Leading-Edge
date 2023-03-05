using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketBehaviour
{
    private Rigidbody rocketRigidbody;
    private float maxSpeed;

    public RocketBehaviour(Rigidbody rocket, float speed)
    {
        rocketRigidbody = rocket;
        maxSpeed = speed;
    }

    public void RocketVelocity()
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
