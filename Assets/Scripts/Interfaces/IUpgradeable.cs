using UnityEngine;

public interface IUpgradeable
{
    UpgradeData UpgradeData { get; }
    int CurrentLevel { get; }
    bool CanUpgrade();
    void Upgrade();
}