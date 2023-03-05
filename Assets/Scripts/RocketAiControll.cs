using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketAiControll : MonoBehaviour
{
    [Header("Used GameObjects")]
    [SerializeField] private Rigidbody rocketRigidbody;
    [SerializeField] private ParticleSystem explosion;

    [Header("Rocket Parameters")]
    [SerializeField] private float maxSpeed;
    [SerializeField] private float maxTurnAngle;
    [SerializeField] private float minDistanceBoost;

    [Header("Rocket Targets")]
    [SerializeField] private GameObject targetMain;
    [SerializeField] private GameObject targetSecondary;

    private RocketBehaviour rocketMovement;

    //flare offset
    private float aimOffsetVertical = 0f;
    private float aimOffsetHorizontal = 0f;
    private bool createNewOffset = true;

    // Start is called before the first frame update
    void Start()
    {
        rocketMovement = new RocketBehaviour(rocketRigidbody, maxSpeed);
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 target = targetMain.transform.position;
        if (targetMain.GetComponent<AirController>().HasTriggeredFlares())
        {
            if (createNewOffset)
            {
                aimOffsetVertical = Random.Range(-100f, 100);
                aimOffsetHorizontal = Random.Range(-100f, 100);
                createNewOffset = false;
            }
            target += transform.up * aimOffsetVertical + transform.right * aimOffsetHorizontal;
        }
        else
        {
            createNewOffset = true;
        }
        RotationToTarget(target);
    }

    //FixedUpdate is called zero, one or multipe times per frame
    private void FixedUpdate()
    {
        rocketMovement.RocketVelocity();
    }

    private void OnCollisionEnter(Collision collision)
    {
        Instantiate(explosion, rocketRigidbody.transform.position, Quaternion.identity);
        Destroy(this.gameObject);
    }

    void RotationToTarget(Vector3 targetPosition)
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
}