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

    private const float FOOD_CHANGE_TIME_MIN = 6;
    private const float FOOD_CHANGE_TIME_MAX = 8;
    private const int FOOD_CHANGE_STEP = 5;
    private const int FOOD_GRANT_PERCENTAGE = 5;

    private const float OIL_TIME_MIN = 10;
    private const float OIL_TIME_MAX = 15;
    private const int OIL_CHANGE_STEP = 5;
    private const int OIL_GRANT_PERCENTAGE = 5;

    private const float PART_TIME_MIN = 20;
    private const float PART_TIME_MAX = 30;

    private CoroutineHandle? _oilHandler;
    private CoroutineHandle? _foodHandler;
    private CoroutineHandle? _partHandler;
    private CoroutineHandle? _setSlotCoroutine;
    private CoroutineHandle? _replenishFoodCoroutine;
    private CoroutineHandle? _replenishOilCoroutine;
    private CoroutineHandle? _handleActionsCoroutine;

    private DragAssist _dragAssist;
    private Animator _animator;

    public bool GameOver = false;

    public bool IsIdle { get { return _animator.GetCurrentAnimatorStateInfo(0).IsTag("Idle"); } }
    public Queue<QueuedPetActions> queuedActions = new Queue<QueuedPetActions>();

    private static readonly int FOOD_ANIMATOR_TRIGGER = Animator.StringToHash("HandleFood");
    private static readonly int OIL_ANIMATOR_TRIGGER = Animator.StringToHash("HandleOil");
    private static readonly int TOPHEAD_DROP_ANIMATOR_TRIGGER = Animator.StringToHash("HandleTopHeadDrop");
    private static readonly int TOPHEAD_REPAIR_ANIMATOR_TRIGGER = Animator.StringToHash("HandleTopHeadRepair");
    private static readonly int BOTTOMHEAD_DROP_ANIMATOR_TRIGGER = Animator.StringToHash("HandleBottomHeadDrop");
    private static readonly int BOTTOMHEAD_REPAIR_ANIMATOR_TRIGGER = Animator.StringToHash("HandleBottomHeadRepair");
    private static readonly int EYE_DROP_ANIMATOR_TRIGGER = Animator.StringToHash("HandleEyeDrop");
    private static readonly int EYE_REPAIR_ANIMATOR_TRIGGER = Animator.StringToHash("HandleEyeRepair");
    private static readonly int LEG_DROP_ANIMATOR_TRIGGER = Animator.StringToHash("HandleLegDrop");
    private static readonly int LEG_REPAIR_ANIMATOR_TRIGGER = Animator.StringToHash("HandleLegRepair");

    private void Awake()
    {
        _dragAssist = FindObjectOfType<DragAssist>();
        _animator = GetComponent<Animator>();


        FoodLevelSlider.minValue = 0;
        FoodLevelSlider.maxValue = 100;

        OilLevelSlider.minValue = 0;
        OilLevelSlider.maxValue = 100;
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
        while (!GameOver)
        {
            if (queuedActions.Count > 0)
            {
                if (IsIdle)
                {
                    var currentAction = queuedActions.Dequeue();
                    currentAction.Handled = true;
                    yield return Timing.WaitForSeconds(0.1f); //Wait a bit for that handle to trigger
                }
            }

            yield return Timing.WaitForOneFrame;
        }
    }

    private void EnablePieces()
    {
        if (ActiveTopHeadPiece != null)
        {
            ActiveTopHeadPiece.Enable(false);
        }

        if (ActiveBottomHeadPiece != null)
        {
            ActiveBottomHeadPiece.Enable(false);
        }

        if (ActiveEyePiece != null)
        {
            ActiveEyePiece.Enable(false);
        }

        if (ActiveLegPiece != null)
        {
            ActiveLegPiece.Enable(false);
        }
    }

    private void OnDestroy()
    {
        TimingHandlers.CleanlyKillCoroutine(ref _oilHandler);
        TimingHandlers.CleanlyKillCoroutine(ref _foodHandler);
        TimingHandlers.CleanlyKillCoroutine(ref _partHandler);
        TimingHandlers.CleanlyKillCoroutine(ref _setSlotCoroutine);
        TimingHandlers.CleanlyKillCoroutine(ref _replenishFoodCoroutine);
        TimingHandlers.CleanlyKillCoroutine(ref _replenishOilCoroutine);
        TimingHandlers.CleanlyKillCoroutine(ref _handleActionsCoroutine);
    }

    private IEnumerator<float> HandlePartLevel()
    {
        var partNextWait = 0f;
        var dropTriggerToPlay = 0;
        var slotBeingHandled = SlotType.End;
        
        while (!GameOver)
        {
            SetTimer(ref partNextWait, PART_TIME_MIN, PART_TIME_MAX);
            yield return Timing.WaitForSeconds(partNextWait);

            var queuedAction = new QueuedPetActions();
            queuedActions.Enqueue(queuedAction);
            yield return Timing.WaitUntilDone(queuedAction);

            RefreshValidDroppingPartType(ref slotBeingHandled);
            dropTriggerToPlay = GetDropTriggerToPlay(slotBeingHandled);
            _animator.SetTrigger(dropTriggerToPlay);
            //TODO: Hookup sfx

            yield return Timing.WaitForSeconds(10f); // This should be at least animation time
            DisableCurrentActiveSlot(slotBeingHandled);
            yield return Timing.WaitForSeconds(0.5f); // just wait a bit, we're in no hurry
            CheckIfGameOver();
            _dragAssist.SetRandomPartInUI(slotBeingHandled);
        }
    }

    private int GetDropTriggerToPlay(SlotType slotBeingHandled)
    {
        switch (slotBeingHandled)
        {
            case SlotType.TopHeadPlate:
                return TOPHEAD_DROP_ANIMATOR_TRIGGER;
            case SlotType.BottomHeadPlate:
                return BOTTOMHEAD_DROP_ANIMATOR_TRIGGER;
            case SlotType.Eye:
                return EYE_DROP_ANIMATOR_TRIGGER;
            case SlotType.Leg:
                return LEG_DROP_ANIMATOR_TRIGGER;
        }

        return -1;
    }

    private void DisableCurrentActiveSlot(SlotType type)
    {
        switch (type)
        {
            case SlotType.TopHeadPlate:
                ActiveTopHeadPiece.Disable(true);
                ActiveTopHeadPiece = null;
                break;
            case SlotType.BottomHeadPlate:
                ActiveBottomHeadPiece.Disable(true);
                ActiveBottomHeadPiece = null;
                break;
            case SlotType.Eye:
                ActiveEyePiece.Disable(true);
                ActiveEyePiece = null;
                break;
            case SlotType.Leg:
                ActiveLegPiece.Disable(true);
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
        if ((ActiveTopHeadPiece == null && ActiveBottomHeadPiece == null &&
            ActiveEyePiece == null && ActiveLegPiece == null) || OilLevel <= 0 || FoodLevel <= 0)
        {
            //TODO: Game over
            Debug.Log("GameOver");
            GameOver = true;
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
            _dragAssist.SetRandomPartInUI(SlotType.Food);
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
            _dragAssist.SetRandomPartInUI(SlotType.Oil);
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
        //TODO: Hookup sfx
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
        //TODO: Hookup sfx
    }

    public void SetSlot(BodyPart part)
    {
        part.Enable(true);
        _setSlotCoroutine = Timing.RunCoroutine(SetSlotCoroutine(part));
    }

    private IEnumerator<float> SetSlotCoroutine(BodyPart part)
    {
        var queuedAction = new QueuedPetActions();
        queuedActions.Enqueue(queuedAction);
        yield return Timing.WaitUntilDone(queuedAction);

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

        
        var setTriggerToPlay = GetSetTriggerToPlay(part.Type);
        _animator.SetTrigger(setTriggerToPlay);
        //TODO: Hookup sfx
    }

    private int GetSetTriggerToPlay(SlotType slotBeingHandled)
    {
        switch (slotBeingHandled)
        {
            case SlotType.TopHeadPlate:
                return TOPHEAD_REPAIR_ANIMATOR_TRIGGER;
            case SlotType.BottomHeadPlate:
                return BOTTOMHEAD_REPAIR_ANIMATOR_TRIGGER;
            case SlotType.Eye:
                return EYE_REPAIR_ANIMATOR_TRIGGER;
            case SlotType.Leg:
                return LEG_REPAIR_ANIMATOR_TRIGGER;
        }

        return -1;
    }

    private void SetSlot(ref BodyPart activePieceSlot, BodyPart part, Transform joint)
    {
        activePieceSlot = part;
        activePieceSlot.transform.SetParent(joint);
        activePieceSlot.transform.localPosition = Vector3.zero;
        activePieceSlot.transform.localScale = Vector3.one;
    }
}