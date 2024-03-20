﻿using Dalamud.Game;
using Dalamud.Game.Network;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;

namespace MPTickBar
{
    public sealed class MPTickBarPlugin : IDalamudPlugin
    {
        private static string ConfigCommand => "/mptb";

        public static string CountdownCommand => $"{MPTickBarPlugin.ConfigCommand}cd";

        [PluginService]
        private DalamudPluginInterface PluginInterface { get; init; }

        [PluginService]
        private ICommandManager CommandManager { get; init; }

        [PluginService]
        private IFramework Framework { get; init; }

        [PluginService]
        private IClientState ClientState { get; init; }

        [PluginService]
        private IJobGauges JobGauges { get; init; }

        [PluginService]
        private ICondition Condition { get; init; }

        [PluginService]
        private IGameNetwork GameNetwork { get; init; }

        [PluginService]
        private ISigScanner SigScanner { get; init; }

        private MPTickBarPluginUI MPTickBarPluginUI { get; }

        private Configuration Configuration { get; }

        private ProgressBarState ProgressBarState { get; } = new();

        private PlayerState PlayerState { get; } = new();

        private CountdownState CountdownState { get; } = new();

        private Network Network { get; } = new();

        private Chat Chat { get; }

        public MPTickBarPlugin()
        {
            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new();
            this.Configuration.Initialize(this.PluginInterface);
            this.MPTickBarPluginUI = new(this.Configuration, this.PluginInterface.UiBuilder);
            this.Chat = new(this.SigScanner);

            this.CommandManager.AddHandler(MPTickBarPlugin.ConfigCommand, new(this.OnConfigCommand)
            {
                HelpMessage = "Opens the configuration menu.",
                ShowInHelp = true
            });

            this.CommandManager.AddHandler(MPTickBarPlugin.CountdownCommand, new(this.OnCountdownCommand)
            {
                HelpMessage = $"Starts the countdown with X seconds after next tick and time offset. (e.g. {MPTickBarPlugin.CountdownCommand} 12)",
                ShowInHelp = true
            });

            this.PluginInterface.UiBuilder.DisableAutomaticUiHide = false;
            this.PluginInterface.UiBuilder.DisableCutsceneUiHide = false;
            this.PluginInterface.UiBuilder.DisableGposeUiHide = false;
            this.PluginInterface.UiBuilder.DisableUserUiHide = false;

            this.ClientState.Login += this.ProgressBarState.Login;
            this.PluginInterface.UiBuilder.Draw += this.Draw;
            this.PluginInterface.UiBuilder.OpenConfigUi += this.OpenConfigUi;
            this.Framework.Update += this.Update;
            this.GameNetwork.NetworkMessage += this.NetworkMessage;
        }

        public void Dispose()
        {
            this.MPTickBarPluginUI.Dispose();
            this.ClientState.Login -= this.ProgressBarState.Login;
            this.PluginInterface.UiBuilder.Draw -= this.Draw;
            this.PluginInterface.UiBuilder.OpenConfigUi -= this.OpenConfigUi;
            this.Framework.Update -= this.Update;
            this.GameNetwork.NetworkMessage -= this.NetworkMessage;
            this.CommandManager.RemoveHandler(MPTickBarPlugin.ConfigCommand);
            this.CommandManager.RemoveHandler(MPTickBarPlugin.CountdownCommand);
        }

        private void OnConfigCommand(string command, string args) => this.OpenConfigUi();

        private void OnCountdownCommand(string command, string args) => this.CountdownState.Start(args, this.Configuration.Countdown.StartingSeconds, this.Configuration.Countdown.TimeOffset);

        private void Draw() => this.MPTickBarPluginUI.Draw();

        private void OpenConfigUi() => this.MPTickBarPluginUI.OpenConfigUi();

        private void Update(IFramework framework)
        {
            if (this.PlayerState != null)
            {
                this.PlayerState.ServicesUpdate(this.ClientState, this.JobGauges, this.Condition);
                this.ProgressBarState.PlayerState = this.PlayerState;
                this.MPTickBarPluginUI.PlayerState = this.PlayerState;
                this.Network.PlayerState = this.PlayerState;
                this.PlayerState.OtherJobIdsUpdate([this.Configuration.General.IsDarkKnightEnabled]);
            }

            var progress = this.ProgressBarState.Update();
            this.MPTickBarPluginUI.Update(progress, this.Network.OpCode);
            this.CountdownState.Update(this.Chat, progress, this.Configuration.Countdown.TimeOffset);

            if (this.Network.Update())
                this.ProgressBarState.RestartProgress();
        }

        private void NetworkMessage(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
        {
            if (this.Network.NetworkMessage(dataPtr, opCode, targetActorId, direction))
                this.ProgressBarState.RestartProgress();
        }
    }
}