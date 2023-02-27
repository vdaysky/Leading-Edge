using System;
using TMPro;
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
    
    [SerializeField] private ParticleSystem flares;
    
    [SerializeField] private TextMeshProUGUI speedText;
    
    [SerializeField] private TextMeshProUGUI torqueText;
    
    [SerializeField] private TextMeshProUGUI rotationText;

    private CameraView _cameraView = CameraView.Back;
    
    private const float MaxSpeed = 52f;
    private const float SteeringVSens = 26;
    private const float SteeringHSens = 52;
    private const long FlaresCooldownMs = 10000;

    
    private long _lastFlaresUsage;

    private void TryActivateFlares()
    {
        if (_lastFlaresUsage + FlaresCooldownMs >= DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) 
            return;
        
        var planeTransform = planeRigidBody.transform;
        var planePos = planeTransform.position;
        var right = planeTransform.right;
        var planeLeftWingPos = planePos + right * 2;
        var planeRightWingPos = planePos - right * 2;
                
        Instantiate(flares, planePos, planeRigidBody.rotation * Quaternion.Euler(15, 0, 0));
        Instantiate(flares, planePos, planeRigidBody.rotation * Quaternion.Euler(0, 30, 0));
        Instantiate(flares, planePos, planeRigidBody.rotation * Quaternion.Euler(0, -30, 0));
        Instantiate(flares, planeLeftWingPos, planeRigidBody.rotation * Quaternion.Euler(-15, 45, 0));
        Instantiate(flares, planeRightWingPos, planeRigidBody.rotation * Quaternion.Euler(-15, -45, 0));
        
        _lastFlaresUsage = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    }

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

        if (Input.GetKeyDown(KeyCode.F))
        {
            TryActivateFlares();
        }

        // project plane vertical vector on world vertical vector
        Vector3 planeVerticalVector = planeRigidBody.transform.up;
        Vector3 worldVerticalVector = Vector3.up;
        Vector3 planeVerticalVectorProjected = Vector3.Project(planeVerticalVector, worldVerticalVector);
        float verticalSpeedCut = planeVerticalVectorProjected.magnitude;
        
        // compute lift vector (consider lift force to be proportional to vertical speed) 
        Vector3 liftVector = inverseGravityVector * verticalSpeedCut;
        
        Vector3 finalVelocity = thrustVector + liftVector + gravityVector;
        planeRigidBody.velocity = finalVelocity;

        speedText.text = $"Speed: {planeRigidBody.velocity.magnitude}\n" +
                         $"{planeRigidBody.velocity}\nLift: {verticalSpeedCut}";
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
        rotationText.text = $"Rotation:\nUp: {Math.Round(planeRigidBody.rotation.eulerAngles.x, 2)}\n" +
                            $"Left: {Math.Round(planeRigidBody.rotation.eulerAngles.y, 2)}\n" +
                            $"Roll: {Math.Round(planeRigidBody.rotation.eulerAngles.z, 2)}";
        
        float vert = Input.GetAxisRaw("Vertical") * SteeringVSens;
        float hor = Input.GetAxisRaw("Horizontal") * SteeringHSens;
        float roll = Input.GetAxisRaw("Roll") * SteeringHSens;

        Vector3 targetTorque = new Vector3(-vert, hor, roll);
        planeRigidBody.AddRelativeTorque(targetTorque, ForceMode.Force);

        // get plane torque in local space
        Vector3 torque = planeRigidBody.transform.InverseTransformDirection(planeRigidBody.angularVelocity);
        torqueText.text = "Torque:\n" +
                          $"Up: {Math.Round(torque.x, 2)}\n" +
                          $"Left: {Math.Round(torque.y, 2)}\n" +
                          $"Roll: {Math.Round(torque.z, 2)}";
    }
}
