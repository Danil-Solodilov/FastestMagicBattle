using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;


[RequireComponent(typeof(CharacterController))]

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 10f; //Скорость вращения игрока

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector3 moveDirection;
    private Transform cameraTransform;
    private PlayerShooting _playerShooting;
    private PlayerStats _playerStats;
    private Animator _animator;


    [Header("DownPanel UI")]
    [SerializeField] private TextMeshProUGUI speedText; //Текст скорости

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        // Кэшируем ссылку на главную камеру для расчетов
        if (Camera.main != null) cameraTransform = Camera.main.transform;
        _playerShooting = GetComponent<PlayerShooting>();
        _playerStats = GetComponent<PlayerStats>();
        speedText.text = $"{_playerStats.moveSpeed}";
        if (_animator == null) _animator = GetComponent<Animator>();
    }

    // Этот метод вызывается компонентом Player Input (через Send Messages)
    public void OnMove(InputAction.CallbackContext context)
    {
        _animator.SetBool("IsMoving", true);
        moveInput = context.ReadValue<Vector2>();
    }

    private void Update()
    {
        MovePlayer();

        // Обновляем анимацию на основе того, есть ли у нас направление движения
        bool isMoving = moveDirection.sqrMagnitude > 0.001f;
        _animator.SetBool("IsMoving", isMoving);
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

        // 2. Двигаем персонажа через CharacterController
        controller.Move(moveDirection * Mathf.Clamp(_playerStats.moveSpeed, 0, 40f) * Time.deltaTime);
        speedText.text = $"{Mathf.RoundToInt(_playerStats.moveSpeed)}";

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
