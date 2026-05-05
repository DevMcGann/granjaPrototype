using Terresquall;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Gravity")]
    public float gravity = -9.8f;
    private float verticalVelocity;

    [Header("References")]
    public Transform cameraTransform;
    public VirtualJoystick joystick;

    private CharacterController controller;
    private Animator animator;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (controller == null)
            Debug.LogError("Falta CharacterController en Player");

        if (animator == null)
            Debug.LogError("Falta Animator en el Model");
    }

    void Update()
    {
        float h = joystick.GetAxis("Horizontal");
        float v = joystick.GetAxis("Vertical");

        Vector3 input = new Vector3(h, 0f, v);
        float inputMagnitude = Mathf.Clamp01(input.magnitude);

        // 🎬 Animación
        if (animator != null)
        {
            animator.SetFloat("Speed", inputMagnitude);
        }

        // 🎯 Dirección relativa a cámara
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0;
        camRight.y = 0;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camForward * v + camRight * h;

        // ✅ NORMALIZAR (clave)
        if (moveDir.magnitude > 1f)
            moveDir.Normalize();

        // 🔻 GRAVEDAD
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        // 🚶 Movimiento final
        Vector3 velocity = moveDir * moveSpeed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);

        // 🔄 ROTACIÓN PRO (suave y consistente)
        if (moveDir.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}