using UnityEngine;

public class StrategyCameraController : MonoBehaviour
{
    public Transform cameraTransform;
    public Transform cameraMoveRig;
    Vector3 cameraStartPosition;

    [Header("Speed Settings")]
    public AnimationCurve cameraCloseCurve = DefaultCameraCloseCurve();
    public float timeSpeed = 5;
    public float moveSpeed = 1;
    public float rotateSpeed = 1;
    public float zoomSpeed = 2;
    public float mouseZoomMultiplier = 5;
    [Range(0, 1)]
    public float panBorderEffectRange = 0.01f;
    float panBorderThickness;
    Vector3 zoomVector;

    private float CameraCloseMultiplier{
        get{
            if (!evaluateCameraCloseMultiplier)
                return 1;
            return cameraCloseCurve.Evaluate(cameraTransform.localPosition.y / maxPosition.y);
        }
    }
    
    Vector3 newPos;
    Vector3 newZoom;

    float horizontalAngel;
    float verticalAngel;

    Vector3 rotStartPos;
    Vector3 rotCurrentPos;

    [Header("Camera Limits")]
    public Vector3 minPosition = new Vector3(-50, 10, -50);
    public Vector3 maxPosition = new Vector3(50, 100, 50);
    [Range(0, 360)]
    public float minVerticalAngle = 280; 
    [Range(0, 360)]
    public float maxVerticalAngle = 358;
    [Range(0, 360)]
    public float minHorizontalAngle = 0; 
    [Range(0, 360)]
    public float maxHorizontalAngle = 0;
    public bool limitVerticalRotation = true;
    public bool limitHorizontalRotation = false;


    [Header("Functionality Settings")]
    public bool useBorderMovement = true;
    public bool cameraCanMove = true;
    public bool cameraCanZoom = true;
    public bool cameraCanRotateHorizontally = true;
    public bool cameraCanRotateVertically = true;
    public bool evaluateCameraCloseMultiplier = true;

    public void Start()
    {
        cameraCloseCurve = DefaultCameraCloseCurve();
        cameraStartPosition = cameraMoveRig.localPosition;

        panBorderThickness = Screen.height * panBorderEffectRange;

        zoomVector = new Vector3(0, -zoomSpeed, 0);
        newZoom = cameraTransform.localPosition;
        newPos = cameraMoveRig.localPosition;

        horizontalAngel = cameraMoveRig.localRotation.eulerAngles.y;
        verticalAngel = transform.localRotation.eulerAngles.x;
    }

    void Update()
    {
        MouseInput();
        KeyboardInput();
        ClampPosition();
        Move();
        Rotate();
    }

    public void MouseInput()
    {
        if (cameraCanZoom && Input.mouseScrollDelta.y != 0)
            ZoomInput(Input.mouseScrollDelta.y * zoomVector * mouseZoomMultiplier * CameraCloseMultiplier);

        if ((cameraCanRotateVertically || cameraCanRotateHorizontally) && Input.GetMouseButtonDown(2))
            rotStartPos = Input.mousePosition;

        if ((cameraCanRotateVertically || cameraCanRotateHorizontally) && Input.GetMouseButton(2))
        {
            rotCurrentPos = Input.mousePosition;
            Vector3 direction = rotCurrentPos - rotStartPos;
            rotStartPos = rotCurrentPos;
            
            if (cameraCanRotateVertically)
                RotateInput(verticalAngel, Vector3.right, -direction.y/5f);
            if (cameraCanRotateHorizontally)
                RotateInput(horizontalAngel, Vector3.up, -direction.x/5f);
        }

        if (useBorderMovement && cameraCanMove)
        {
            if (Input.mousePosition.y >= Screen.height - panBorderThickness)
                MoveInput(cameraMoveRig.forward * moveSpeed * CameraCloseMultiplier);
            if (Input.mousePosition.y <= panBorderThickness)
                MoveInput(-cameraMoveRig.forward * moveSpeed * CameraCloseMultiplier);
            if (Input.mousePosition.x >= Screen.width - panBorderThickness)
                MoveInput(cameraMoveRig.right * moveSpeed * CameraCloseMultiplier);
            if (Input.mousePosition.x <= panBorderThickness)
                MoveInput(-cameraMoveRig.right * moveSpeed * CameraCloseMultiplier);
        }
    }

    public void KeyboardInput()
    {
        if (cameraCanMove)
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                MoveInput(cameraMoveRig.forward * moveSpeed * CameraCloseMultiplier);
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                MoveInput(-cameraMoveRig.forward * moveSpeed * CameraCloseMultiplier);
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                MoveInput(cameraMoveRig.right * moveSpeed * CameraCloseMultiplier);
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                MoveInput(-cameraMoveRig.right * moveSpeed * CameraCloseMultiplier);
        }

        if (cameraCanRotateVertically)
        {
            if (Input.GetKey(KeyCode.T))
                RotateInput(verticalAngel, Vector3.right, rotateSpeed);
            if (Input.GetKey(KeyCode.G))
                RotateInput(verticalAngel, Vector3.right, -rotateSpeed);
        }
        if (cameraCanRotateHorizontally)
        {
            if (Input.GetKey(KeyCode.Q))
                RotateInput(horizontalAngel, Vector3.up, rotateSpeed);
            if (Input.GetKey(KeyCode.E))
                RotateInput(horizontalAngel, Vector3.up, -rotateSpeed);
        }

        if (cameraCanZoom && Input.GetKey(KeyCode.R))
            ZoomInput(zoomVector * CameraCloseMultiplier);
        if (cameraCanZoom && Input.GetKey(KeyCode.F))
            ZoomInput(-zoomVector * CameraCloseMultiplier);
    }

    public void ZoomInput(Vector3 value)
    {
        newZoom += value;
    }

    public void MoveInput(Vector3 value)
    {
        newPos += value;
    }

    public void RotateInput(float angle, Vector3 axis, float value)
    {
        if (axis != Vector3.up && axis != Vector3.right)
        {
            Debug.LogError("Expected axis to be Vector3.up or Vector3.right but it was: " + axis);
            return;
        }

        float newAngle = value + angle;
        if (limitVerticalRotation || limitHorizontalRotation)
        {
            newAngle = LimitCameraAngle(axis, newAngle);
        }

        if (axis == Vector3.right) 
            verticalAngel = newAngle;
        if (axis == Vector3.up) 
            horizontalAngel = newAngle;
    }

    public float LimitCameraAngle(Vector3 axis, float angle)
    {
        if (axis == Vector3.right && limitVerticalRotation)
            angle = Mathf.Clamp(angle, minVerticalAngle, maxVerticalAngle);
        if (axis == Vector3.up && limitHorizontalRotation)
            angle = Mathf.Clamp(angle, minHorizontalAngle, maxHorizontalAngle);
        return angle;
    }

    public void ClampPosition()
    {
        if (cameraCanMove)
            newPos = new Vector3(
                Mathf.Clamp(newPos.x, cameraStartPosition.x + minPosition.x, cameraStartPosition.x + maxPosition.x),
                newPos.y,
                Mathf.Clamp(newPos.z, cameraStartPosition.z + minPosition.z, cameraStartPosition.z + maxPosition.z));
        
        if (cameraCanZoom)
            newZoom = new Vector3(0, Mathf.Clamp(newZoom.y, minPosition.y, maxPosition.y), 0);
    }

    public void Move()
    {
        if (cameraCanMove)
            cameraMoveRig.localPosition = Vector3.Lerp(cameraMoveRig.localPosition, newPos, Time.deltaTime * timeSpeed);
        if (cameraCanZoom)
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, newZoom, Time.deltaTime * timeSpeed);
    }

    public void Rotate()
    {
        if (cameraCanRotateVertically)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(verticalAngel, 0, 0), Time.deltaTime * timeSpeed);
        }
            
        if (cameraCanRotateHorizontally)
        {
            cameraMoveRig.localRotation = Quaternion.Slerp(cameraMoveRig.localRotation, Quaternion.Euler(0, horizontalAngel, 0), Time.deltaTime * timeSpeed);
        }
    }

    private static AnimationCurve DefaultCameraCloseCurve()
    {
        float rad = Mathf.Deg2Rad * 82;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(new Keyframe(0, 0.32f));
        curve.AddKey(new Keyframe(1, 1, rad, rad));
        return curve;
    }
}
