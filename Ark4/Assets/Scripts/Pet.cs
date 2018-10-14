using MEC;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Pet : MonoBehaviour
{
    public int FoodLevel
    {
        get
        {
            return _foodLevel;
        }

        set
        {
            _foodLevel = value;
            FoodLevelSlider.value = FoodLevel;
        }
    }
    public int OilLevel
    {
        get
        {
            return _oilLevel;
        }

        set
        {
            _oilLevel = value;
            OilLevelSlider.value = OilLevel;
        }
    }

    public Slider FoodLevelSlider;
    public Slider OilLevelSlider;

    public BodyPart ActiveTopHeadPiece;
    public BodyPart ActiveBottomHeadPiece;
    public BodyPart ActiveEyePiece;
    public BodyPart ActiveLegPiece;

    public Transform TopHeadJoint;
    public Transform BottomHeadJoint;
    public Transform EyeJoint;
    public Transform LegJoint;

    private int _foodLevel;
    private int _oilLevel;

    private const float FOOD_CHANGE_TIME_MIN = 5f;
    private const float FOOD_CHANGE_TIME_MAX = 7f;
    private const int FOOD_CHANGE_STEP = 5;
    private const int FOOD_GRANT_PERCENTAGE = 15;

    private const float OIL_TIME_MIN = 8f;
    private const float OIL_TIME_MAX = 12f;
    private const int OIL_CHANGE_STEP = 5;
    private const int OIL_GRANT_PERCENTAGE = 10;

    private const float PART_TIME_MIN = 3f;
    private const float PART_TIME_MAX = 6f;

    private CoroutineHandle? _oilHandler;
    private CoroutineHandle? _foodHandler;
    private CoroutineHandle? _partHandler;
    private CoroutineHandle? _replenishFoodCoroutine;
    private CoroutineHandle? _replenishOilCoroutine;
    private CoroutineHandle? _handleActionsCoroutine;

    private DragAssist _dragAssist;
    private Animator _animator;

    public bool GameOver = false;
    public GameObject GameOverOverlay;

    public bool IsIdle { get { return _animator.GetCurrentAnimatorStateInfo(0).IsTag("Idle"); } }
    public Queue<QueuedPetActions> queuedActions = new Queue<QueuedPetActions>();
    private const float TIME_TO_IDLE = 5f;
    private static readonly int FOOD_ANIMATOR_TRIGGER = Animator.StringToHash("HandleFood");
    private static readonly int OIL_ANIMATOR_TRIGGER = Animator.StringToHash("HandleOil");

    private static readonly int IDLE1_ANIMATOR_TRIGGER = Animator.StringToHash("HandleIdle1");
    private static readonly int IDLE2_ANIMATOR_TRIGGER = Animator.StringToHash("HandleIdle2");
    private static readonly int IDLE3_ANIMATOR_TRIGGER = Animator.StringToHash("HandleIdle3");
    private static readonly int IDLE4_ANIMATOR_TRIGGER = Animator.StringToHash("HandleIdle4");
    private int[] _idleAnims;

    [FMODUnity.EventRef]
    public string EatingSFX;
    [FMODUnity.EventRef]
    public string OilingSFX;
    [FMODUnity.EventRef]
    public string BreakingSFX;
    [FMODUnity.EventRef]
    public string FixingSFX;
    [FMODUnity.EventRef]
    public string GameOverSFX;

    [FMODUnity.EventRef]
    public List<string> _soundIdleAnims;

    private const string BGM_PARAMETER = "progress";
    private FMODUnity.StudioEventEmitter _bgmAudioEmitter;
    private FMOD.Studio.ParameterInstance _parameter;
    private bool _hasParameterForBGM;

    private void Awake()
    {
        _dragAssist = FindObjectOfType<DragAssist>();
        _animator = GetComponent<Animator>();
        _idleAnims = new int[4];
        _idleAnims[0] = IDLE1_ANIMATOR_TRIGGER;
        _idleAnims[1] = IDLE2_ANIMATOR_TRIGGER;
        _idleAnims[2] = IDLE3_ANIMATOR_TRIGGER;
        _idleAnims[3] = IDLE4_ANIMATOR_TRIGGER;

        FoodLevelSlider.minValue = 0;
        FoodLevelSlider.maxValue = 100;

        OilLevelSlider.minValue = 0;
        OilLevelSlider.maxValue = 100;

        _bgmAudioEmitter = FindObjectOfType<FMODUnity.StudioEventEmitter>();
        _hasParameterForBGM = false;
        GameOverOverlay.SetActive(false);
    }

    private void Start()
    {
        FoodLevel = 100;
        OilLevel = 100;

        EnablePieces();

        _handleActionsCoroutine = Timing.RunCoroutine(HandleActions());

        _oilHandler = Timing.RunCoroutine(HandleOilLevel());
        _foodHandler = Timing.RunCoroutine(HandleFoodLevel());
        _partHandler = Timing.RunCoroutine(HandlePartLevel());
    }

    private IEnumerator<float> HandleActions()
    {
        var timer = 0f;
        while (!GameOver)
        {
            if (queuedActions.Count > 0)
            {
                timer = 0f;
                if (IsIdle)
                {
                    var currentAction = queuedActions.Dequeue();
                    currentAction.Handled = true;
                    yield return Timing.WaitForSeconds(0.1f); //Wait a bit for that handle to trigger
                }
            }
            else
            {
                timer += Time.deltaTime;
                if (timer >= TIME_TO_IDLE)
                {
                    timer = 0;
                    _animator.SetTrigger(GetRandomIdle());
                }
            }

            yield return Timing.WaitForOneFrame;
        }
    }

    private int GetRandomIdle()
    {
        var i = Random.Range(0, 4);
        FMODUnity.RuntimeManager.PlayOneShot(_soundIdleAnims[i], transform.position);
        return _idleAnims[i];
    }

    private void EnablePieces()
    {
        ActiveTopHeadPiece = _dragAssist.GetRandomPart(SlotType.TopHeadPlate, null);
        ActiveTopHeadPiece.Enable(true, false);

        ActiveBottomHeadPiece = _dragAssist.GetRandomPart(SlotType.BottomHeadPlate, null);
        ActiveBottomHeadPiece.Enable(true, false);

        ActiveEyePiece = _dragAssist.GetRandomPart(SlotType.Eye, null);
        ActiveEyePiece.Enable(true, false);

        ActiveLegPiece = _dragAssist.GetRandomPart(SlotType.Leg, null);
        ActiveLegPiece.Enable(true, false);
    }

    private void OnDestroy()
    {
        TimingHandlers.CleanlyKillCoroutine(ref _oilHandler);
        TimingHandlers.CleanlyKillCoroutine(ref _foodHandler);
        TimingHandlers.CleanlyKillCoroutine(ref _partHandler);
        TimingHandlers.CleanlyKillCoroutine(ref _replenishFoodCoroutine);
        TimingHandlers.CleanlyKillCoroutine(ref _replenishOilCoroutine);
        TimingHandlers.CleanlyKillCoroutine(ref _handleActionsCoroutine);
    }

    private IEnumerator<float> HandlePartLevel()
    {
        var partNextWait = 0f;
        var slotBeingHandled = SlotType.End;
        BodyPart oldPart = null;
        yield return Timing.WaitForSeconds(5f);

        while (!GameOver)
        {
            SetTimer(ref partNextWait, PART_TIME_MIN, PART_TIME_MAX);
            yield return Timing.WaitForSeconds(partNextWait);
            RefreshValidDroppingPartType(ref slotBeingHandled);
            DisableCurrentActiveSlot(slotBeingHandled, ref oldPart);
            FMODUnity.RuntimeManager.PlayOneShot(BreakingSFX, transform.position);
            UpdateAudioLevel();
            yield return Timing.WaitForSeconds(0.5f); // just wait a bit, we're in no hurry
            CheckIfGameOver();
            _dragAssist.SetRandomPartInUI(slotBeingHandled, oldPart);
        }
    }

    private void DisableCurrentActiveSlot(SlotType type, ref BodyPart oldPart)
    {
        switch (type)
        {
            case SlotType.TopHeadPlate:
                ActiveTopHeadPiece.Disable(true);
                oldPart = ActiveTopHeadPiece;
                ActiveTopHeadPiece = null;
                break;
            case SlotType.BottomHeadPlate:
                ActiveBottomHeadPiece.Disable(true);
                oldPart = ActiveBottomHeadPiece;
                ActiveBottomHeadPiece = null;
                break;
            case SlotType.Eye:
                ActiveEyePiece.Disable(true);
                oldPart = ActiveEyePiece;
                ActiveEyePiece = null;
                break;
            case SlotType.Leg:
                ActiveLegPiece.Disable(true);
                oldPart = ActiveLegPiece;
                ActiveLegPiece = null;
                break;
        }
    }

    private void RefreshValidDroppingPartType(ref SlotType slotBeingHandled)
    {
        slotBeingHandled = _dragAssist.GetRandomSlotType();
        while (!CheckIfActiveSlot(slotBeingHandled))
        {
            slotBeingHandled = _dragAssist.GetRandomSlotType();
        }
    }

    private bool CheckIfActiveSlot(SlotType type)
    {
        switch (type)
        {
            case SlotType.TopHeadPlate:
                return ActiveTopHeadPiece != null;
            case SlotType.BottomHeadPlate:
                return ActiveBottomHeadPiece != null;
            case SlotType.Eye:
                return ActiveEyePiece != null;
            case SlotType.Leg:
                return ActiveLegPiece != null;
        }

        return false;
    }

    private void CheckIfGameOver()
    {
        if (GameOver)
        {
            return;
        }

        if ((ActiveTopHeadPiece == null && ActiveBottomHeadPiece == null &&
            ActiveEyePiece == null && ActiveLegPiece == null) || OilLevel <= 0 || FoodLevel <= 0)
        {
            Debug.Log("GameOver");
            GameOver = true;
            FMODUnity.RuntimeManager.PlayOneShot(GameOverSFX, transform.position);
            _parameter.setValue(0);
            GameOverOverlay.SetActive(true);
        }
    }

    private IEnumerator<float> HandleFoodLevel()
    {
        var foodNextWait = 0f;
        while (!GameOver)
        {
            SetTimer(ref foodNextWait, FOOD_CHANGE_TIME_MIN, FOOD_CHANGE_TIME_MAX);
            yield return Timing.WaitForSeconds(foodNextWait);
            FoodLevel -= FOOD_CHANGE_STEP;

            var queuedAction = new QueuedPetActions();
            queuedActions.Enqueue(queuedAction);
            yield return Timing.WaitUntilDone(queuedAction);

            CheckIfGameOver();
            yield return Timing.WaitForSeconds(0.5f);
            HandleFoodToPlayer();
        }
    }

    private void HandleFoodToPlayer()
    {
        var i = Random.Range(0f, 100f);
        if (i < FOOD_GRANT_PERCENTAGE)
        {
            _dragAssist.SetRandomPartInUI(SlotType.Food, null);
        }
    }

    private IEnumerator<float> HandleOilLevel()
    {
        var oilNextWait = 0f;
        while (!GameOver)
        {
            SetTimer(ref oilNextWait, OIL_TIME_MIN, OIL_TIME_MAX);
            yield return Timing.WaitForSeconds(oilNextWait);
            OilLevel -= OIL_CHANGE_STEP;

            var queuedAction = new QueuedPetActions();
            queuedActions.Enqueue(queuedAction);
            yield return Timing.WaitUntilDone(queuedAction);

            CheckIfGameOver();
            yield return Timing.WaitForSeconds(0.5f);
            HandleOilToPlayer();
        }
    }

    private void HandleOilToPlayer()
    {
        var i = Random.Range(0f, 100f);
        if (i < OIL_GRANT_PERCENTAGE)
        {
            _dragAssist.SetRandomPartInUI(SlotType.Oil, null);
        }
    }

    private void SetTimer(ref float timer, float min, float max)
    {
        timer = Random.Range(min, max);
    }

    public void ReplenishOil()
    {
        _replenishOilCoroutine = Timing.RunCoroutine(ReplenishOilCoroutine());
    }

    private IEnumerator<float> ReplenishOilCoroutine()
    {
        var queuedAction = new QueuedPetActions();
        queuedActions.Enqueue(queuedAction);
        yield return Timing.WaitUntilDone(queuedAction);

        OilLevel = 100;
        _animator.SetTrigger(OIL_ANIMATOR_TRIGGER);
        FMODUnity.RuntimeManager.PlayOneShot(OilingSFX, transform.position);
    }

    public void ReplenishFood()
    {
        _replenishFoodCoroutine = Timing.RunCoroutine(ReplenishFoodCoroutine());
    }

    private IEnumerator<float> ReplenishFoodCoroutine()
    {
        var queuedAction = new QueuedPetActions();
        queuedActions.Enqueue(queuedAction);
        yield return Timing.WaitUntilDone(queuedAction);

        FoodLevel = 100;
        _animator.SetTrigger(FOOD_ANIMATOR_TRIGGER);
        FMODUnity.RuntimeManager.PlayOneShot(EatingSFX, transform.position);
    }

    public void SetSlot(BodyPart part)
    {
        if (GameOver)
        {
            return;
        }

        part.Enable(true);

        switch (part.Type)
        {
            case SlotType.TopHeadPlate:
                SetSlot(ref ActiveTopHeadPiece, part, TopHeadJoint);
                break;
            case SlotType.BottomHeadPlate:
                SetSlot(ref ActiveBottomHeadPiece, part, BottomHeadJoint);
                break;
            case SlotType.Eye:
                SetSlot(ref ActiveEyePiece, part, EyeJoint);
                break;
            case SlotType.Leg:
                SetSlot(ref ActiveLegPiece, part, LegJoint);
                break;
        }

        UpdateAudioLevel();
        FMODUnity.RuntimeManager.PlayOneShot(FixingSFX, transform.position);
    }

    private void UpdateAudioLevel()
    {
        if (GameOver)
        {
            return;
        }

        var i = 0;
        if (ActiveTopHeadPiece == null)
        {
            i++;
        }

        if (ActiveLegPiece == null)
        {
            i++;
        }

        if (ActiveBottomHeadPiece == null)
        {
            i++;
        }

        if (ActiveEyePiece == null)
        {
            i++;
        }

        i = Mathf.Clamp(i, 0, 3);

        if (!_hasParameterForBGM)
        {
            _bgmAudioEmitter.EventInstance.getParameter(BGM_PARAMETER, out _parameter);
            _hasParameterForBGM = true;
        }

        _parameter.setValue(i);
    }

    private void SetSlot(ref BodyPart activePieceSlot, BodyPart part, Transform joint)
    {
        activePieceSlot = part;
        activePieceSlot.SetDefaultPosition();
    }
}