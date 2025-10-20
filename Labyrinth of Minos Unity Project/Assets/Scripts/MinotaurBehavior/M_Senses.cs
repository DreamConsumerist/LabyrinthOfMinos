using System;
using UnityEngine;

public class Minotaur_Senses : MonoBehaviour
{
    public struct SenseReport
    {
        public bool playerSpotted;
        public Vector2Int playerLocation;
    }
    
    public SenseReport SensoryUpdate()
    {
        SenseReport currSenses = new SenseReport();
        currSenses = IsPlayerVisible(currSenses);
        return currSenses;
    }

    private SenseReport IsPlayerVisible(SenseReport currSenses)
    {
        return currSenses;
    }
}
