using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketBehaviour : MonoBehaviour
{
    [Header("Used GameObjects")]
    [SerializeField] private Rigidbody rocketRigidbody;
    [SerializeField] private ParticleSystem explosion;
    [SerializeField] private AudioSource lockOnSound;

    [Header("Rocket Parameters")]
    [SerializeField] private float maxSpeed;
    [SerializeField] private float maxTurnAngle;
    [SerializeField] private float minDistanceBoost;

    private float _minLockOnFrequency = 0.1f;
    private float _lockOnCooldown = 0f;

    public void RotationToTarget(Vector3 targetPosition)
    {
        float distanceBoost = 0f;
        Vector3 targetDirection = targetPosition - transform.position;

        //boost rotation clamp if too far
        if (targetDirection.magnitude > minDistanceBoost)
        {
            distanceBoost = 90 - maxTurnAngle;
        }

        if(targetDirection.magnitude < 25f)
        {
            if(_lockOnCooldown < Time.time)
            {
                _lockOnCooldown = Time.time + targetDirection.magnitude / 50;
                lockOnSound.Play();
            }
            
        }

        Quaternion toRotation;
        //Probably most stupid math to clamp rotation
        if (Vector3.Angle(transform.forward, targetDirection) > maxTurnAngle + distanceBoost)
        {
            Vector3 rotationVector = Vector3.Cross(transform.forward, targetDirection);
            toRotation = Quaternion.LookRotation(Quaternion.AngleAxis(maxTurnAngle, rotationVector) * transform.forward);
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

        Vector3 fullLiftVector = -Physics.gravity * verticalSpeedCut;

        // I have no fucking idea why force has to be divided by two. Seems to work though
        Vector3 finalVelocity = thrustVector * rocketRigidbody.mass / 2 + fullLiftVector * rocketRigidbody.mass;

        rocketRigidbody.AddForce(finalVelocity, ForceMode.Force);
    }
}
