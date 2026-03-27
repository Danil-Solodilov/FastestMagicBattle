using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;


[RequireComponent(typeof(CharacterController))]

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private float rotationSpeed = 7f;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector3 moveDirection;
    private Transform cameraTransform;
    private PlayerShooting _playerShooting;
    private PlayerStats _playerStats;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        // Кэшируем ссылку на главную камеру для расчетов
        if (Camera.main != null) cameraTransform = Camera.main.transform;
        _playerShooting = GetComponent<PlayerShooting>();
        _playerStats = GetComponent<PlayerStats>();
    }

    // Этот метод вызывается компонентом Player Input (через Send Messages)
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    private void Update()
    {
        MovePlayer();
    }

    private void MovePlayer()
    {
        // 1. Превращаем 2D ввод в 3D направление относительно камеры
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        // Обнуляем Y, чтобы персонаж не пытался лететь вверх/вниз при наклоне камеры
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        moveDirection = forward * moveInput.y + right * moveInput.x;

        //Скорость движения
        float finalSpeed = moveSpeed * _playerStats.moveSpeedMultiplier;

        // 2. Двигаем персонажа через CharacterController
        controller.Move(moveDirection * finalSpeed * Time.deltaTime);

        // 3. Поворачиваем персонажа лицом в сторону движения
        if (moveDirection != Vector3.zero && (_playerShooting == null || _playerShooting.FindNearestValidEnemy() == null))
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        // Добавляем простую гравитацию, чтобы куб не висел в воздухе
        if (!controller.isGrounded)
        {
            controller.Move(Vector3.down * 9.81f * Time.deltaTime);
        }
    }
}
