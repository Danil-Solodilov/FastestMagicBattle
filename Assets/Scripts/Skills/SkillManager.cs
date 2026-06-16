using UnityEngine;
using UnityEngine.InputSystem; // Важно для нового Input System
using UnityEngine.UI;

public class SkillManager : MonoBehaviour
{
    [Header("Slots")]
    public SkillData[] activeSkills = new SkillData[3]; // 3 обычных слота
    public SkillData ultimateSkill;                     // 1 слот ульты
    private int _lastReplacedSlot = -1; // Чтобы знать, какой слот заменять следующим

    // Переменные для перезарядки (индексы: 0, 1, 2 для обычных; 3 для ульты)
    private float[] _cooldownTimers = new float[4];
    private float[] _cooldownDurations = new float[4]; // Храним длительность перезарядки

    // Состояние прицеливания
    [Header("Aiming")]
    private bool _isAiming = false;
    private int _aimingSlotIndex = -1; // Индекс слота, который сейчас наводится
    private SkillData _selectedSkillToAim;

    [Header("Indicators")]
    [SerializeField] private GameObject arrowIndicator; // Префаб стрелки
    [SerializeField] private GameObject circleIndicator; // Префаб круга
    [SerializeField] private LayerMask groundLayer;     // Слой земли для Raycast

    [Header("Targeting Settings")]
    [SerializeField] private float maxAimDistance = 100f; // Макс. дальность прицеливания
    private float indicatorYOffset = 0.01f; // Для небольшого смещения вверх (по оси Y)
    private Vector3 AOEskillPlace;

    [Header("Skill UI")]
    [SerializeField] private Image[] skillCooldownMasks = new Image[3]; // Маски для 1, 2, 3 скиллов
    [SerializeField] private Image ultimateCooldownMask;              // Маска для ульты
    [SerializeField] private Image[] skillIcons = new Image[3]; // Добавь ссылки на иконки скиллов в UI
    [SerializeField] private Image ultimateIcon;

    private Camera _mainCamera;

    // Уровень ультимейта
    public int ultimateLevel = 0;

    // Анимация
    private Animator _animator;

    void Awake()
    {
        _mainCamera = Camera.main;
        // Скрываем индикаторы по умолчанию
        arrowIndicator.SetActive(false);
        circleIndicator.SetActive(false);
        if (_animator == null) _animator = GetComponent<Animator>();

        UpdateUIAppearance();
    }

    void Update()
    {
        // Обновляем перезарядку
        for (int i = 0; i < _cooldownTimers.Length; i++)
        {
            if (_cooldownTimers[i] > 0)
            {
                _cooldownTimers[i] -= Time.deltaTime;
                UpdateCooldownUI(i, _cooldownTimers[i], _cooldownDurations[i]);
            }
            else
            {
                // Если кулдаун закончился, убедимся, что UI тоже сброшен
                UpdateCooldownUI(i, 0, _cooldownDurations[i]);
            }
        }

        // Обновляем позицию индикатора, если мы в режиме прицеливания
        if (_isAiming)
        {
            UpdateAimingVisuals();
        }
    }

    // Вызывается событием "Skill1" (клавиша 1)
    public void ActivateSkillSlot1() => TryActivateSkill(0);
    // Вызывается событием "Skill2" (клавиша 2)
    public void ActivateSkillSlot2() => TryActivateSkill(1);
    // Вызывается событием "Skill3" (клавиша 3)
    public void ActivateSkillSlot3() => TryActivateSkill(2);
    // Вызывается событием "Ultimate" (клавиша U)
    public void ActivateUltimate() => TryActivateSkill(3);

    // Вызывается событием "Aim" (правая кнопка мыши)
    public void AimOrCancel()
    {
        if (_isAiming)
        {
            CancelAiming(); // Если прицеливаемся, отменяем
        }
        else
        {
            // Если не прицеливаемся, начинаем прицеливание для текущего выбранного скилла
            // (Этот вызов будет при первом нажатии кнопки скилла)
        }
    }

    // Вызывается событием "Fire" (левая кнопка мыши)
    public void CastSkill()
    {
        if (_isAiming && Time.time > 0)
        {
            // Если прицеливаемся, кастуем скилл
            PerformSkillCast(_selectedSkillToAim, _aimingSlotIndex);
            CancelAiming();
        }
    }

    

    //Активация нужного скила
    private void TryActivateSkill(int slotIndex)
    {
        SkillData skill = GetSkillBySlotIndex(slotIndex);
        if (skill == null || IsSkillOnCooldown(slotIndex)) return;
        if (Time.time < 1) return;  

        if (skill.type == SkillType.Self)
        {
            // Для скиллов типа Self - кастуем сразу
            PerformSkillCast(skill, slotIndex); // Позиция игрока
            StartCooldown(skill, slotIndex);
        }
        else
        {
            // Для направленных скиллов - переходим в режим прицеливания
            StartAiming(skill, slotIndex);
        }
        
    }

    private void StartAiming(SkillData skill, int slotIndex)
    {
        _isAiming = true;
        _selectedSkillToAim = skill;
        _aimingSlotIndex = slotIndex;

        // Включаем нужный индикатор
        arrowIndicator.SetActive(skill.type == SkillType.TargetDirection);
        circleIndicator.SetActive(skill.type == SkillType.GroundTarget);

        // TODO: Можно добавить затемнение экрана или другие UI-эффекты
    }

    private void UpdateAimingVisuals()
    {
        Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue()); // Используем новый Input System для мыши

        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, groundLayer))
        {
            Vector3 targetPoint = hit.point;
         
            if (_selectedSkillToAim.type == SkillType.TargetDirection)
            {
                // Поворачиваем стрелку от игрока в сторону цели
                Vector3 direction = (targetPoint - transform.position);

                // Онуляем Y в направлении, чтобы стрелка не "смотрела" вверх или вниз, 
                // если курсор на холме или в яме. Она будет строго горизонтальной.
                direction.y = 0;
                direction.Normalize();
                
                // 2. Устанавливаем позицию стрелки
                Vector3 arrowPos = transform.position;
                // ВАЖНО: Берем высоту Y из targetPoint (где луч попал в землю) 
                // и добавляем наш отступ, чтобы не было мерцания.
                arrowPos.y = targetPoint.y + indicatorYOffset;
                arrowIndicator.transform.position = arrowPos;

                // 3. Поворачиваем стрелку
                arrowIndicator.transform.rotation = Quaternion.LookRotation(direction);
                arrowIndicator.transform.localScale = new Vector3(1, 1, _selectedSkillToAim.range / 10);
            }
            else if (_selectedSkillToAim.type == SkillType.GroundTarget)
            {
                // Перемещаем круг под курсором, но ограничиваем дальностью
                float distanceToGroundTarget = Vector3.Distance(transform.position, targetPoint);
                if (distanceToGroundTarget > _selectedSkillToAim.range)
                {
                    targetPoint = transform.position + (targetPoint - transform.position).normalized * _selectedSkillToAim.range;
                }

                // ПРИПОДНИМАЕМ ТОЧКУ НАД ЗЕМЛЕЙ
                targetPoint.y += indicatorYOffset;

                circleIndicator.transform.position = targetPoint;
                AOEskillPlace = targetPoint;
                // Масштабируем круг под радиус эффекта
                float scale = _selectedSkillToAim.effectRadius;
                circleIndicator.transform.localScale = new Vector3(scale, scale, scale); // Для круга используем одинаковый масштаб
            }
        }
        else
        {
            // ОТЛАДКА: Если луч НИЧЕГО не нашел
            Debug.LogWarning("Raycast did not hit anything on the ground layer!");
        }
    }

    private void PerformSkillCast(SkillData skill, int slotIndex)
    {
        if (skill.effectPrefab != null)
        {
            _animator.SetTrigger("Cast");
            GameObject spawnedEffect = null; // Создаем переменную для ссылки на объект

            if (skill.type == SkillType.Self)
            {
                if (skill.isUlt)
                {
                    spawnedEffect = Instantiate(skill.effectPrefab, transform.position, Quaternion.identity);
                }
                else
                {
                    spawnedEffect = Instantiate(skill.effectPrefab, transform.position, Quaternion.identity, transform);
                }
            }
            else if (skill.type == SkillType.TargetDirection)
            {
                Vector3 spawnPos = new Vector3(transform.position.x, 1f, transform.position.z); 
                // Создаем снаряд и сохраняем ссылку на него в переменную 'projectile'
                spawnedEffect = Instantiate(skill.effectPrefab, spawnPos, arrowIndicator.transform.rotation);
                IceBoltSkill iceBolt = spawnedEffect.GetComponent<IceBoltSkill>();
                if (iceBolt != null)
                {
                    // Передаем range и damage из данных скилла в скрипт снаряда
                    iceBolt.Initialize(skill.damage, skill.range, skill.duration);
                }

                // Получаем направление из индикатора стрелки
                Vector3 leapDir = arrowIndicator.transform.forward;
                LeapSkill leapLogic = spawnedEffect.GetComponent<LeapSkill>();
                if (leapLogic != null)
                {
                    leapLogic.Initialize(skill.damage, skill.range, leapDir);
                }

                FireBallSkill fireBall = spawnedEffect.GetComponent<FireBallSkill>();
                if (fireBall != null)
                {
                    // Передаем range и damage из данных скилла в скрипт снаряда
                    fireBall.Initialize(skill.damage, skill.range, skill.duration);
                }
            }
            else if (skill.type == SkillType.GroundTarget)
            {
                spawnedEffect = Instantiate(skill.effectPrefab, AOEskillPlace, Quaternion.identity);
                FireZoneEffect fireZone = spawnedEffect.GetComponent<FireZoneEffect>();
                BlackHole blackHole = spawnedEffect.GetComponent<BlackHole>();
                StunningZoneEffect stunningZone = spawnedEffect.GetComponent <StunningZoneEffect>();
                if (fireZone != null)
                {
                    // Передаем радиус из SkillData
                    fireZone.effectRadius = skill.effectRadius;
                    // Можно также передать duration, tickDamage, tickInterval, если они будут отличаться для разных скиллов
                }
                if (blackHole != null)
                {
                    blackHole.Initialize(ultimateSkill);
                }
                if (stunningZone != null)
                {
                    stunningZone.effectRadius = skill.effectRadius;
                }
            }

            // --- УНИВЕРСАЛЬНАЯ ИНИЦИАЛИЗАЦИЯ ДЛЯ УЛЬТЫ ---
            if (skill.isUlt && spawnedEffect != null)
            {
                // Пытаемся найти один из скриптов ульты на созданном объекте
                if (spawnedEffect.TryGetComponent(out BlackHole bh)) bh.Initialize(ultimateSkill);
                if (spawnedEffect.TryGetComponent(out ChainLightning cl)) cl.Initialize(ultimateSkill);
                if (spawnedEffect.TryGetComponent(out MeteorShower ms)) ms.Initialize(ultimateSkill);
            }

            Debug.Log($"Кастуем {skill.skillName}");
        }
        else
        {
            Debug.LogWarning($"У скилла {skill.skillName} нет префаба эффекта!");
        }

        // Начинаем перезарядку
        StartCooldown(skill, slotIndex);
    }

    private void UpdateCooldownUI(int slotIndex, float currentCooldown, float maxCooldown)
    {
        Image cooldownMask = null;

        if (slotIndex == 3) // Ульта
        {
            cooldownMask = ultimateCooldownMask;
        }
        else if (slotIndex >= 0 && slotIndex < skillCooldownMasks.Length) // Обычные скиллы
        {
            cooldownMask = skillCooldownMasks[slotIndex];
        }

        if (cooldownMask != null)
        {
            if (currentCooldown > 0)
            {
                // Рассчитываем прогресс заполнения (от 0 до 1)
                float progress = 1f - (currentCooldown / maxCooldown);
                cooldownMask.fillAmount = progress;
            }
            else
            {
                // Скилл готов, заполнение должно быть 0 (или 1, если делаешь "убирание")
                cooldownMask.fillAmount = 0f;
            }
        }
    }

    private void UpdateUIAppearance()
    {
        // Обновляем иконки в интерфейсе
        for (int i = 0; i < activeSkills.Length; i++)
        {
            if (skillIcons[i] != null)
            {
                if (activeSkills[i] != null)
                {
                    skillIcons[i].sprite = activeSkills[i].icon;
                    skillIcons[i].enabled = true;
                }
                else skillIcons[i].enabled = false;
            }
        }

        if (ultimateIcon != null)
        {
            if (ultimateSkill != null)
            {
                ultimateIcon.sprite = ultimateSkill.icon;
                ultimateIcon.enabled = true;
            }
            else ultimateIcon.enabled = false;
        }
    }

    private void StartCooldown(SkillData skill, int slotIndex)
    {
        _cooldownTimers[slotIndex] = skill.cooldown;
        _cooldownDurations[slotIndex] = skill.cooldown;

        UpdateCooldownUI(slotIndex, _cooldownTimers[slotIndex], _cooldownDurations[slotIndex]);
    }

    public void CancelAiming()
    {
        _isAiming = false;
        arrowIndicator.SetActive(false);
        circleIndicator.SetActive(false);
        _selectedSkillToAim = null;
        _aimingSlotIndex = -1;
    }

    // --- Вспомогательные методы ---

    private SkillData GetSkillBySlotIndex(int index)
    {
        if (index == 3) return ultimateSkill;
        if (index >= 0 && index < activeSkills.Length) return activeSkills[index];
        return null;
    }

    private bool IsSkillOnCooldown(int slotIndex)
    {
        return _cooldownTimers[slotIndex] > 0;
    }

    public float GetCooldownProgress(int slotIndex)
    {
        if (_cooldownDurations[slotIndex] <= 0) return 0; // Если нет перезарядки или длительность 0
        return 1f - (_cooldownTimers[slotIndex] / _cooldownDurations[slotIndex]);
    }

    public bool IsSkillReady(int slotIndex)
    {
        return _cooldownTimers[slotIndex] <= 0;
    }


    // --- МЕТОДЫ ДЛЯ UPGRADE MANAGER ---

    // 1. Проверка: есть ли уже такой скилл у игрока?
    public bool HasSkill(SkillData skillToCheck)
    {
        if (skillToCheck == null) return false;

        // Проверяем обычные скиллы
        foreach (var s in activeSkills)
        {
            if (s != null && s.skillName == skillToCheck.skillName) return true;
        }

        // Проверяем ульту
        if (ultimateSkill != null && ultimateSkill.skillName == skillToCheck.skillName) return true;

        return false;
    }

    // 2. Проверка: есть ли свободный слот для обычного скилла?
    public bool HasFreeSkillSlot()
    {
        for (int i = 0; i < activeSkills.Length; i++)
        {
            if (activeSkills[i] == null) return true;
        }
        return false;
    }

    // 3. Добавление нового скилла
    public void AddSkill(SkillData newSkillData)
    {
        for (int i = 0; i < activeSkills.Length; i++)
        {
            if (activeSkills[i] == null)
            {
                // Клонируем ScriptableObject, чтобы изменения урона не сохранялись в ассетах проекта!
                activeSkills[i] = Instantiate(newSkillData);

                // Сбрасываем кулдаун для нового скилла
                _cooldownDurations[i] = activeSkills[i].cooldown;
                _cooldownTimers[i] = 0;

                UpdateUIAppearance();
                Debug.Log($"Скилл {activeSkills[i].skillName} добавлен в слот {i + 1}");
                return;
            }
        }
    }
    public void AddOrReplaceSkill(SkillData skillPrefab)
    {
        SkillData newSkillInstance = Instantiate(skillPrefab);

        // 1. Пытаемся найти пустой слот
        for (int i = 0; i < activeSkills.Length; i++)
        {
            if (activeSkills[i] == null)
            {
                activeSkills[i] = newSkillInstance;
                UpdateUIAppearance();
                return;
            }
        }

        // 2. Если пустых нет, заменяем по очереди (0 -> 1 -> 2 -> 0...)
        _lastReplacedSlot = (_lastReplacedSlot + 1) % activeSkills.Length;
        activeSkills[_lastReplacedSlot] = newSkillInstance;

        Debug.Log($"Слот {_lastReplacedSlot} заменен на {newSkillInstance.skillName}");
        UpdateUIAppearance();
    }

    // 5. Выбор ультимейта (вызывается из UpgradeManager при выборе ульты)
    public void SetUltimate(SkillData ultimateData)
    {
        ultimateSkill = Instantiate(ultimateData);
        _cooldownDurations[3] = ultimateSkill.cooldown;
        ultimateLevel = 1;
        UpdateUIAppearance();
    }

    // 5. Улучшение ультимейта
    public void UpgradeUltimate(float cooldownMult, float damageIncrease, float radiusIncrease, float durationIncrease)
    {
        if (ultimateSkill != null)
        {
            ultimateLevel++;

            ultimateSkill.cooldown -= cooldownMult;
            ultimateSkill.effectRadius *= 1f + (radiusIncrease / 100f);
            ultimateSkill.damage *= 1f + (damageIncrease / 100f);
            ultimateSkill.duration += durationIncrease;

            _cooldownDurations[3] = ultimateSkill.cooldown;
            Debug.Log($"Ульта {ultimateSkill.skillName} улучшена до {ultimateLevel} уровня!");
        }
    }
}
