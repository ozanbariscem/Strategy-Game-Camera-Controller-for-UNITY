using UnityEngine;

public class StrategyCameraController : MonoBehaviour
{
    public Transform cameraTransform;
    public Transform cameraMoveRig;
    Vector3 cameraStartPosition;

    [Header("Speed Settings")]
    public AnimationCurve cameraCloseCurve = DefaultCameraCloseCurve();
    public float lerpSpeed = 5;
    public float moveSpeed = 2;
    public float rotateSpeed = 2;
    public float zoomSpeed = 4;
    public float mouseZoomMultiplier = 5;
    [Range(0, 1)]
    public float borderMovementEffectRange = 0.01f;
    float borderMovementThickness;
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
        moveSpeed *= 100;
        rotateSpeed *= 100;
        zoomSpeed *= 100;
        mouseZoomMultiplier *= 100;

        cameraCloseCurve = DefaultCameraCloseCurve();
        cameraStartPosition = cameraMoveRig.localPosition;

        borderMovementThickness = Screen.height * borderMovementEffectRange;

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
    }

    void FixedUpdate()
    {
        Move();
        Rotate();
    }

    public void MouseInput()
    {
        if (cameraCanZoom && Input.mouseScrollDelta.y != 0)
            ZoomInput(Input.mouseScrollDelta.y * zoomVector * mouseZoomMultiplier * CameraCloseMultiplier * Time.deltaTime);

        if ((cameraCanRotateVertically || cameraCanRotateHorizontally) && Input.GetMouseButtonDown(2))
            rotStartPos = Input.mousePosition;

        if ((cameraCanRotateVertically || cameraCanRotateHorizontally) && Input.GetMouseButton(2))
        {
            rotCurrentPos = Input.mousePosition;
            Vector3 direction = rotCurrentPos - rotStartPos;
            rotStartPos = rotCurrentPos;
            
            if (cameraCanRotateVertically)
                RotateInput(verticalAngel, Vector3.right, -direction.y * 20 * Time.deltaTime);
            if (cameraCanRotateHorizontally)
                RotateInput(horizontalAngel, Vector3.up, -direction.x * 20 * Time.deltaTime);
        }

        if (useBorderMovement && cameraCanMove)
        {
            if (Input.mousePosition.y >= Screen.height - borderMovementThickness)
                MoveInput(cameraMoveRig.forward * moveSpeed * CameraCloseMultiplier * Time.deltaTime);
            if (Input.mousePosition.y <= borderMovementThickness)
                MoveInput(-cameraMoveRig.forward * moveSpeed * CameraCloseMultiplier * Time.deltaTime);
            if (Input.mousePosition.x >= Screen.width - borderMovementThickness)
                MoveInput(cameraMoveRig.right * moveSpeed * CameraCloseMultiplier * Time.deltaTime);
            if (Input.mousePosition.x <= borderMovementThickness)
                MoveInput(-cameraMoveRig.right * moveSpeed * CameraCloseMultiplier * Time.deltaTime);
        }
    }

    public void KeyboardInput()
    {
        if (cameraCanMove)
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                MoveInput(cameraMoveRig.forward * moveSpeed * CameraCloseMultiplier * Time.deltaTime);
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                MoveInput(-cameraMoveRig.forward * moveSpeed * CameraCloseMultiplier * Time.deltaTime);
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                MoveInput(cameraMoveRig.right * moveSpeed * CameraCloseMultiplier * Time.deltaTime);
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                MoveInput(-cameraMoveRig.right * moveSpeed * CameraCloseMultiplier * Time.deltaTime);
        }

        if (cameraCanRotateVertically)
        {
            if (Input.GetKey(KeyCode.T))
                RotateInput(verticalAngel, Vector3.right, rotateSpeed * Time.deltaTime);
            if (Input.GetKey(KeyCode.G))
                RotateInput(verticalAngel, Vector3.right, -rotateSpeed * Time.deltaTime);
        }
        if (cameraCanRotateHorizontally)
        {
            if (Input.GetKey(KeyCode.Q))
                RotateInput(horizontalAngel, Vector3.up, rotateSpeed * Time.deltaTime);
            if (Input.GetKey(KeyCode.E))
                RotateInput(horizontalAngel, Vector3.up, -rotateSpeed * Time.deltaTime);
        }

        if (cameraCanZoom && Input.GetKey(KeyCode.R))
            ZoomInput(zoomVector * CameraCloseMultiplier * Time.deltaTime);
        if (cameraCanZoom && Input.GetKey(KeyCode.F))
            ZoomInput(-zoomVector * CameraCloseMultiplier * Time.deltaTime);
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
            LerpMove(cameraMoveRig, newPos, Time.fixedDeltaTime * lerpSpeed);
        if (cameraCanZoom)
            LerpMove(cameraTransform, newZoom, Time.fixedDeltaTime * lerpSpeed);
    }

    private void LerpMove(Transform transform, Vector3 pos, float lerpValue)
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, pos, lerpValue);
    }

    public void Rotate()
    {
        if (cameraCanRotateVertically)
            LerpRotate(transform, Quaternion.Euler(verticalAngel, 0, 0), Time.fixedDeltaTime * lerpSpeed);
        if (cameraCanRotateHorizontally)
            LerpRotate(cameraMoveRig, Quaternion.Euler(0, horizontalAngel, 0), Time.fixedDeltaTime * lerpSpeed);
    }

    private void LerpRotate(Transform transform, Quaternion quaternion, float lerpValue)
    {
        transform.localRotation = Quaternion.Slerp(transform.localRotation, quaternion, lerpValue);
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
