using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System;
using System.Linq;

namespace MPTickBar
{
    public class PlayerState
    {
        private ClientState ClientState { get; set; }

        private JobGauges JobGauges { get; set; }

        private Condition Condition { get; set; }

        private PlayerCharacter Player => this.ClientState?.LocalPlayer;

        private Data<uint> MP { get; } = new();

        private Data<ushort> Territory { get; } = new();

        private Data<bool> IsDead { get; } = new();

        public byte UmbralIceRegenStack { get; private set; }

        public byte LucidDreamingRegenStack { get; private set; }

        public static byte LucidDreamingRegenStackMax => 2;

        public bool IsPlayingAsBlackMage => (this.ClientState != null) && (this.Player != null) && this.ClientState.IsLoggedIn;

        public bool IsInCombat => this.CheckCondition(new[] { ConditionFlag.InCombat });

        public bool IsBetweenAreas => this.CheckCondition(new[] { ConditionFlag.BetweenAreas, ConditionFlag.BetweenAreas51 });

        public bool IsOccupied => this.CheckCondition(new[] { ConditionFlag.OccupiedInCutSceneEvent, ConditionFlag.Occupied33, ConditionFlag.Occupied38, ConditionFlag.OccupiedInQuestEvent });

        private bool CheckCondition(ConditionFlag[] conditionFlags) => this.IsPlayingAsBlackMage && (this.Condition != null) && conditionFlags.Any(x => this.Condition[x]);

        public bool IsUmbralIceActivated => (this.UmbralIceStacks > 0);

        private byte UmbralIceStacks => (this.IsPlayingAsBlackMage && (this.JobGauges != null)) ? this.JobGauges.Get<BLMGauge>().UmbralIceStacks : (byte)0;

        private bool IsLucidDreamingActivated => this.IsEffectActivated(1204);

        private bool IsCircleOfPowerActivated => this.IsEffectActivated(738);

        private bool IsEffectActivated(uint statusId) => this.IsPlayingAsBlackMage && this.Player.StatusList.Any(x => x.StatusId == statusId);

        public bool CheckPlayerId(uint targetActorId) => this.IsPlayingAsBlackMage && this.Player.IsValid() && !this.IsDead.Current && (this.Player.ObjectId == targetActorId);

        public bool CheckPlayerStatus(int hp, int mp) => this.IsPlayingAsBlackMage && !this.IsDead.Current && (this.Player.CurrentHp == hp) && (this.Player.CurrentMp == mp);

        private class Data<T> where T : struct
        {
            public T Current { get; private set; }

            public T Last { get; private set; }

            public void Update(T data) => this.Current = data;

            public void SaveData() => this.Last = this.Current;
        }

        public void ServicesUpdate(ClientState clientState, JobGauges jobGauges, Condition condition)
        {
            this.ClientState = clientState;
            this.JobGauges = jobGauges;
            this.Condition = condition;
        }

        public void StateUpdate()
        {
            var isPlayingAsBlackMage = this.IsPlayingAsBlackMage;
            this.MP.Update(isPlayingAsBlackMage ? this.Player.CurrentMp : uint.MinValue);
            this.Territory.Update(isPlayingAsBlackMage ? this.ClientState.TerritoryType : ushort.MinValue);
            this.IsDead.Update(isPlayingAsBlackMage && (this.Player.CurrentHp == 0));
        }

        public void SaveData()
        {
            this.MP.SaveData();
            this.Territory.SaveData();
            this.IsDead.SaveData();
        }

        public void MPRegenStackUpdate()
        {
            if (!this.IsPlayingAsBlackMage)
                return;

            var onlucidDreamingMPRegen = false;
            if (this.IsLucidDreamingActivated)
            {
                var lucidDreamingMPRegen = 550;
                var mpRecovered = (int)(this.MP.Current - this.MP.Last);
                var recoveringMPToFull = (this.MP.Current == this.Player.MaxMp) && (this.MP.Last > (this.Player.MaxMp - lucidDreamingMPRegen)) && (mpRecovered > 0);
                onlucidDreamingMPRegen = (mpRecovered == lucidDreamingMPRegen || recoveringMPToFull);
            }
            else
                this.LucidDreamingRegenStack = 0;

            if (this.MP.Last > this.MP.Current)
                this.LucidDreamingRegenStack = 0;
            else if (onlucidDreamingMPRegen)
                this.LucidDreamingRegenStack = (byte)Math.Min(this.LucidDreamingRegenStack + 1, PlayerState.LucidDreamingRegenStackMax);

            this.UmbralIceRegenStack = (byte)((this.MP.Current + ((this.MP.Current / 6200) * 200)) / 3200);
        }

        public bool IsValidState()
        {
            var onZoneChange = (this.Territory.Last != this.Territory.Current);
            var onDeath = (!this.IsDead.Last && this.IsDead.Current);
            return !onZoneChange && !onDeath;
        }

        public float GetFastFireIIICastTime()
        {
            unsafe
            {
                var level = this.IsPlayingAsBlackMage ? this.Player.Level : 0;
                if (level == 0)
                    return 0.0f;

                var gcd35 = 3500;
                var astralUmbral = 0.5;
                var leyLines = this.IsCircleOfPowerActivated ? 0.85 : 1.0;
                var sub = LevelModifier.GetLevelModifierSub(level);
                var div = LevelModifier.GetLevelModifierDiv(level);
                var spellSpeed = UIState.pInstance->PlayerState.Attributes[46];

                return (float)(gcd35 * (1000 + Math.Ceiling(130.0 * (sub - spellSpeed) / div)) / 10000 / 100 * astralUmbral * leyLines);
            }
        }
    }
}
