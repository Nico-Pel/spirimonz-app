using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SpirimonzAdder : GameBehaviour
{
    public SpirimonzSettings spirimonzToAddInTeam;
    public int forcedPos = -1;

    private InventoryManager _inventoryManager;

    private void Start()
    {
        _inventoryManager = InventoryManager.Instance;
    }

    public void AddSpirimonz()
    {
        if (_inventoryManager == null || spirimonzToAddInTeam == null) return;

        if (forcedPos >= 0)
        {
            _inventoryManager.AddSpirimonzToTeam(spirimonzToAddInTeam, forcedPos);
        }
        else
        {
            _inventoryManager.AddSpirimonzToTeam(spirimonzToAddInTeam);
        }
    }
}