using NaughtyAttributes;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;
    public bool targetPlayer { get; set; } = false;

    public float rotationSpeed;
    public Vector2 verticalClamp;
    public float offset;
    public Vector2 zoomRange;
    public float zoomSpeed;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        SwitchModes(false);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SwapModes();
        }

        if (Input.GetKey(KeyCode.LeftAlt)) Cursor.lockState = CursorLockMode.Confined; // free mouse
        else // rotate camera
        {
            Cursor.lockState = CursorLockMode.Locked;

            Vector2 input = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")); // mouse delta

            if (targetPlayer)
            {
                transform.parent.position = player.position;

                Vector3 currentEuler = transform.parent.localEulerAngles;
                float pitch = currentEuler.x - input.y * rotationSpeed;
                float yaw = currentEuler.y + input.x * rotationSpeed;

                pitch = (pitch > 180) ? pitch - 360 : pitch;
                pitch = Mathf.Clamp(pitch, verticalClamp.x, verticalClamp.y);

                transform.parent.localEulerAngles = new Vector3(pitch, yaw, 0);

                float scrollInput = Input.GetAxis("Mouse ScrollWheel");
                offset += -scrollInput * zoomSpeed;

                Vector3 direction = -transform.parent.forward;
                bool hasHit = Physics.Raycast(player.position, direction, out RaycastHit hit, offset);
                float currentOffset = hasHit ? hit.distance : offset;

                transform.position = player.position + direction * currentOffset;
                transform.LookAt(player);
            }
            else
            {
                Vector3 currentEuler = transform.parent.localEulerAngles;
                float pitch = currentEuler.x - input.y * rotationSpeed;
                float yaw = currentEuler.y + input.x * rotationSpeed;

                pitch = (pitch > 180) ? pitch - 360 : pitch;
                pitch = Mathf.Clamp(pitch, verticalClamp.x, verticalClamp.y);

                transform.parent.localEulerAngles = new Vector3(pitch, yaw, 0);

                transform.position = transform.parent.position + -transform.forward * offset;
                transform.LookAt(transform.parent);
            }
        }
    }

    public void SwitchModes(bool targetPlayer)
    {
        this.targetPlayer = targetPlayer;
        if (targetPlayer)
        {
            Camera.main.orthographic = false;
            Camera.main.fieldOfView = 60;
            transform.parent.parent = player;
        }
        else
        {
            Camera.main.orthographic = true;
            Camera.main.orthographicSize = Mathf.Max(DungeonGenerator.instance.size.x / 2f, DungeonGenerator.instance.size.y / 2f);
            offset = Mathf.Max(DungeonGenerator.instance.size.x / 2f, DungeonGenerator.instance.size.y / 2f) * Mathf.Sqrt(2);
            transform.parent.parent = null;
            transform.parent.position = new Vector3(DungeonGenerator.instance.size.x / 2f, 0f, DungeonGenerator.instance.size.y / 2f);
        }
    }

    [Button] void SwapModes()
    {
        SwitchModes(!targetPlayer);
    }
}
