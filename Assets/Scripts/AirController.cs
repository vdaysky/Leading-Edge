using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirController : MonoBehaviour
{
    [SerializeField] Rigidbody _planeRigidBody;
    [SerializeField] AnimationCurve aoaCoefficient;
    
    private Vector3 ThrustVector;
    private Vector3 GravityVector = Vector3.down * 10;
    private Vector3 LiftVector;

    private float angleOfAttack;

    private Vector3 LocalVelocity;
    private Vector3 LocalAngularVelocity;

    // Start is called before the first frame update
    private void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //Thrust change mostly for debug
        if(Input.GetButton("Jump"))
        {
            ThrustVector = _planeRigidBody.transform.forward * 10f;
        }
        else
        {
            ThrustVector = _planeRigidBody.transform.forward * 30f;
        }

        float aoaLift = aoaCoefficient.Evaluate(angleOfAttack * Mathf.Rad2Deg);
        LiftVector = _planeRigidBody.transform.up * (_planeRigidBody.velocity.sqrMagnitude * aoaLift * 0.01f);

        Quaternion invRotation = Quaternion.Inverse(_planeRigidBody.rotation);
        LocalVelocity = invRotation * _planeRigidBody.velocity;
        LocalAngularVelocity = invRotation * _planeRigidBody.angularVelocity;

        _planeRigidBody.velocity = ThrustVector + GravityVector + LiftVector;
    }  

    //FixedUpdate is called zero, one or multipe times per frame
    private void FixedUpdate()
    {
        angleOfAttack = Mathf.Atan2(-LocalVelocity.y, LocalVelocity.z);
        PlaneSteering();
    }



    void PlaneSteering()
    {
        float Vert = Input.GetAxisRaw("Vertical") * 30f * Time.deltaTime;
        float Roll = Input.GetAxisRaw("Roll") * 60f * Time.deltaTime;
        float Hor = Input.GetAxisRaw("Horizontal") * 30f * Time.deltaTime;
        Vector3 TargetTorque = new Vector3(Vert - LocalAngularVelocity.x, Hor - LocalAngularVelocity.y, Roll - LocalAngularVelocity.z);

        _planeRigidBody.AddRelativeTorque(TargetTorque, ForceMode.VelocityChange);
    }
}
