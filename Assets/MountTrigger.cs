using UnityEngine;
using UnityEngine.InputSystem;

public class MountTrigger : MonoBehaviour
{
    public HorseController horse;

    private Transform playerBody;
    public bool canMount = false;

    private InputAction mountAction;

    private void Start()
    {
        playerBody = GameObject.FindGameObjectWithTag("Player").transform;

        if (playerBody == null)
            Debug.LogError("MountTrigger: Could not find Player — is the tag set?");
        else
            Debug.Log($"MountTrigger: Found player — {playerBody.gameObject.name}");

        mountAction = new InputAction("Mount", binding: "<Keyboard>/e");
        mountAction.AddBinding("<Gamepad>/buttonSouth");
        mountAction.Enable();
    }

    private void OnDestroy()
    {
        mountAction.Disable();
        mountAction.Dispose();
    }

    private void Update()
    {
        if (mountAction.WasPressedThisFrame())
            Debug.Log($"E pressed — canMount: {canMount}, horse.IsMounted: {horse.IsMounted}");

        if (mountAction.WasPressedThisFrame() && canMount)
        {
            if (!horse.IsMounted)
                horse.Mount(playerBody);
            else
                horse.Dismount();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            canMount = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            canMount = false;
    }
}