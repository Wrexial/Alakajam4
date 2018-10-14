using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum SlotType
{
    Food,
    Oil,
    Leg,
    Eye,
    TopHeadPlate,
    BottomHeadPlate,
    End,
}

public class DragAssist : MonoBehaviour
{
    public DragableObject FoodSlot;
    public DragableObject OilSlot;

    public DragableObject LegSlot;
    public DragableObject EyeSlot;
    public DragableObject TopHeadPlateSlot;
    public DragableObject BottomHeadPlateSlot;

    private List<BodyPart> _foodParts = new List<BodyPart>();
    private List<BodyPart> _oilParts = new List<BodyPart>();
    private List<BodyPart> _legParts = new List<BodyPart>();
    private List<BodyPart> _eyeParts = new List<BodyPart>();
    private List<BodyPart> _topHeadParts = new List<BodyPart>();
    private List<BodyPart> _bottomHeadParts = new List<BodyPart>();

    private Pet _pet;

    public void Awake()
    {
        var parts = FindObjectsOfType<BodyPart>();
        _pet = FindObjectOfType<Pet>();

        foreach (var part in parts)
        {
            switch (part.Type)
            {
                case SlotType.Food:
                    _foodParts.Add(part);
                    break;
                case SlotType.Oil:
                    _oilParts.Add(part);
                    break;
                case SlotType.Leg:
                    _legParts.Add(part);
                    break;
                case SlotType.Eye:
                    _eyeParts.Add(part);
                    break;
                case SlotType.TopHeadPlate:
                    _topHeadParts.Add(part);
                    break;
                case SlotType.BottomHeadPlate:
                    _bottomHeadParts.Add(part);
                    break;
            }

            part.Disable(true, false);
            part.Disable(false, false);
        }

        _foodParts.Shuffle();
        _oilParts.Shuffle();
        _legParts.Shuffle();
        _eyeParts.Shuffle();
        _topHeadParts.Shuffle();
        _bottomHeadParts.Shuffle();
    }

    public SlotType GetRandomSlotType()
    {
        var i = Random.Range((int)SlotType.Leg, (int)SlotType.End);
        return (SlotType)i;
    }

    public BodyPart GetRandomPart(SlotType slot, BodyPart part)
    {
        BodyPart bodyPart;
        var i = 0;
        switch (slot)
        {
            case SlotType.Food:
                i = Random.Range(0, _foodParts.Count);
                bodyPart = _foodParts[i];
                break;
            case SlotType.Oil:
                i = Random.Range(0, _oilParts.Count);
                bodyPart = _oilParts[i];
                break;
            case SlotType.Leg:
                i = Random.Range(0, _legParts.Count);
                bodyPart = _legParts[i];
                break;
            case SlotType.Eye:
                i = Random.Range(0, _eyeParts.Count);
                bodyPart = _eyeParts[i];
                break;
            case SlotType.TopHeadPlate:
                i = Random.Range(0, _topHeadParts.Count);
                bodyPart = _topHeadParts[i];
                break;
            default:
                i = Random.Range(0, _bottomHeadParts.Count);
                bodyPart = _bottomHeadParts[i];
                break;
        }

        if (bodyPart == part)
        {
            bodyPart = GetRandomPart(slot, part);
        }

        return bodyPart;
    }

    public void SetRandomPartInUI(SlotType slot, BodyPart oldPart)
    {
        var part = GetRandomPart(slot, oldPart);

        switch (slot)
        {
            case SlotType.Food:
                if (!FoodSlot.IsInUse)
                {
                    FoodSlot.SetTarget(part, OnFoodHandle);
                }
                break;
            case SlotType.Oil:
                if (!OilSlot.IsInUse)
                {
                    OilSlot.SetTarget(part, OnOilHandle);
                }
                break;
            case SlotType.Leg:
                LegSlot.SetTarget(part, OnLegHandle);
                break;
            case SlotType.Eye:
                EyeSlot.SetTarget(part, OnEyeHandle);
                break;
            case SlotType.TopHeadPlate:
                TopHeadPlateSlot.SetTarget(part, OnTopHeadHandle);
                break;
            case SlotType.BottomHeadPlate:
                BottomHeadPlateSlot.SetTarget(part, OnBottomHeadHandle);
                break;
        }
    }

    private void OnBottomHeadHandle(BodyPart obj)
    {
        _pet.SetSlot(obj);
    }

    private void OnTopHeadHandle(BodyPart obj)
    {
        _pet.SetSlot(obj);
    }

    private void OnEyeHandle(BodyPart obj)
    {
        _pet.SetSlot(obj);
    }

    private void OnLegHandle(BodyPart obj)
    {
        _pet.SetSlot(obj);
    }

    private void OnOilHandle(BodyPart obj)
    {
        _pet.ReplenishOil();
    }

    private void OnFoodHandle(BodyPart obj)
    {
        _pet.ReplenishFood();
    }
}