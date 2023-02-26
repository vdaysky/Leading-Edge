using System;
using System.Globalization;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;



public class AirController : MonoBehaviour
{
    private enum CameraView {
        Back,
        Front
    }
    
    [SerializeField] private Rigidbody planeRigidBody;
    
    [SerializeField] private Camera mainCamera;
    
    [SerializeField] private ParticleSystem explosion;
    
    [SerializeField] private TextMeshProUGUI speedText;
    
    [SerializeField] private TextMeshProUGUI torqueText;
    
    [SerializeField] private TextMeshProUGUI rotationText;

    private CameraView _cameraView = CameraView.Back;
    
    private const float MaxSpeed = 52f;
    private const float WingRollRate = 8;
    private const float SteeringVSens = 26;
    private const float SteeringHSens = 52;
    private const float TorqueYawStartRollingBack = 0.05f;
    private const float MaxPlaneTurningRoll = 35;

    private void Update()
    {
        Vector3 thrustVector;
        var gravityVector = Vector3.down * 10;
        
        // lets say that when perfectly flat plane can counter gravity
        var inverseGravityVector = Vector3.up * 10; 

        //Thrust change mostly for debug
        if (Input.GetButton("Jump"))
        {
            thrustVector = planeRigidBody.transform.forward * 10f;
        }
        else
        {
            thrustVector = planeRigidBody.transform.forward * MaxSpeed;
        }
        
        // change camera view
        if (Input.GetKeyDown(KeyCode.C)) {
            _cameraView = _cameraView == CameraView.Back ? CameraView.Front : CameraView.Back;
        }
        
        // get vertical component of plane speed
        float verticalSpeedCut = (MaxSpeed - Math.Abs(planeRigidBody.velocity.y)) / MaxSpeed;
        
        // compute lift vector (consider lift force to be proportional to vertical speed) 
        Vector3 liftVector = inverseGravityVector * verticalSpeedCut;
        
        Vector3 finalVelocity = thrustVector + liftVector + gravityVector;
        planeRigidBody.velocity = finalVelocity;

        speedText.text = $"Speed: {planeRigidBody.velocity.magnitude}\n" +
                         $"{planeRigidBody.velocity}";
    }  

    //FixedUpdate is called zero, one or multipe times per frame
    private void FixedUpdate()
    {
        PlaneSteering();
        CameraFollow();
    }

    private void OnCollisionEnter(Collision collision)
    {
        var other = collision.gameObject;
        
        if (other.CompareTag("WantedTarget"))
        {
            // TODO:
            // Win, hook up to something here
        }
        else
        {
            // TODO:
            // Lost, hook up to something here
        }

        Instantiate(explosion, planeRigidBody.transform.position, Quaternion.identity);
        Destroy(planeRigidBody.gameObject, 0);
        
        
    }

    private void CameraFollow()
    {
        // set camera position
        var planeTransform = planeRigidBody.transform;
        var planeRotation = planeTransform.rotation;
        
        var distance = 30;
        const int height = 5;
        
        var view = Quaternion.Euler(0, 0, 0);
        if (_cameraView == CameraView.Front)
        {
            distance = -distance;
            view = Quaternion.Euler(0, 180, 0);
        }

        mainCamera.transform.position = planeTransform.position - planeTransform.forward * distance + planeTransform.up * height;
        
        // set camera rotation slowly over time to follow plane, but ignore roll
        Quaternion planeRotationFixed = Quaternion.Euler(
            planeRotation.eulerAngles.x, 
            planeRotation.eulerAngles.y, 
            0
        );
        planeRotationFixed *= view;
        mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, planeRotationFixed, 0.125f);
    }

    private void PlaneSteering()
    {
        Vector3 localAngularVelocity = planeRigidBody.transform.InverseTransformDirection(planeRigidBody.angularVelocity);
        
        rotationText.text = $"Rotation:\nUp: {Math.Round(planeRigidBody.rotation.eulerAngles.x, 2)}\n" +
                            $"Left: {Math.Round(planeRigidBody.rotation.eulerAngles.y, 2)}\n" +
                            $"Roll: {Math.Round(planeRigidBody.rotation.eulerAngles.z, 2)}";
        
        float vert = Input.GetAxisRaw("Vertical") * SteeringVSens;
        float hor = Input.GetAxisRaw("Horizontal") * SteeringHSens;
        
        var planeTransform = planeRigidBody.transform;
        var planeRotation = planeTransform.rotation;

        Vector3 targetTorque = new Vector3(-vert, hor, 0);
        planeRigidBody.AddRelativeTorque(targetTorque, ForceMode.Force);
        
        float planeRoll = planeRotation.eulerAngles.z;
        float torqueYaw = localAngularVelocity.y;
        
        bool rollSign = planeRoll > 180;
        bool torqueSign = torqueYaw > 0;
        
        // if plane is not turning, we have to gradually reduce roll to 0
        if (Math.Abs(torqueYaw) < TorqueYawStartRollingBack)
        {  
            // roll is small enough
            if (Math.Abs(planeRoll) < 1)
            {
                // set the roll on plane explicitly to 0
                // to avoid it jumping back to 360
                planeTransform.rotation = Quaternion.Euler(
                    planeRotation.eulerAngles.x, 
                    planeRotation.eulerAngles.y, 
                    0
                );
            }
            else // roll is too big
            {
                // find direction in which to roll (clockwise or counter-clockwise)
                var counterPlaneRoll = planeRoll > 180 ? 360 - planeRoll : -planeRoll;

                // slowly set plane roll close to 0
                planeRigidBody.AddRelativeTorque(new Vector3(0, 0, counterPlaneRoll), ForceMode.Force);
            }
        }
        else
        {
            var absolutePlaneRoll = planeRoll > 180 ? 360 - planeRoll : planeRoll;
            
            // if plane roll is not too big in turn direction, or plane is turning in opposite direction to wing tilt
            if (absolutePlaneRoll < MaxPlaneTurningRoll || rollSign != torqueSign)
            {
                // slowly set roll proportional to turn speed
                planeRigidBody.AddRelativeTorque(new Vector3(0, 0, -torqueYaw * WingRollRate), ForceMode.Impulse);
            }
        }
        
        // get plane torque in local space
        Vector3 torque = planeRigidBody.transform.InverseTransformDirection(planeRigidBody.angularVelocity);
        torqueText.text = "Torque:\n" +
                          $"Up: {Math.Round(torque.x, 2)}\n" +
                          $"Left: {Math.Round(torque.y, 2)}\n" +
                          $"Roll: {Math.Round(torque.z, 2)}";
    }
}
