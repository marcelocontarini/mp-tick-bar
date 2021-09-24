﻿using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.Game;
using MPTickBar.Properties;

namespace MPTickBar
{
    public class MPTickBarPlugin : IDalamudPlugin
    {
        public string Name => "MP Tick Bar";

        private string CommandName => "/mptb";

        [PluginService]
        private static DalamudPluginInterface PluginInterface { get; set; }

        [PluginService]
        private static CommandManager CommandManager { get; set; }

        [PluginService]
        private static Framework Framework { get; set; }

        [PluginService]
        private static ClientState ClientState { get; set; }

        [PluginService]
        private static JobGauges JobGauges { get; set; }

        private MPTickBarPluginUI MPTickBarPluginUI { get; set; }

        private Configuration Configuration { get; set; }

        private UpdateEventState UpdateEventState { get; set; }

        public MPTickBarPlugin()
        {
            this.Configuration = MPTickBarPlugin.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(MPTickBarPlugin.PluginInterface);
            this.UpdateEventState = new UpdateEventState();

            var gaugeDefault = MPTickBarPlugin.PluginInterface.UiBuilder.LoadImage(Resources.GaugeDefault);
            var gaugeMaterialUIBlack = MPTickBarPlugin.PluginInterface.UiBuilder.LoadImage(Resources.GaugeMaterialUIBlack);
            var GaugeMaterialUIDiscord = MPTickBarPlugin.PluginInterface.UiBuilder.LoadImage(Resources.GaugeMaterialUIDiscord);
            var jobStackDefault = MPTickBarPlugin.PluginInterface.UiBuilder.LoadImage(Resources.JobStackDefault);
            var jobStackMaterialUI = MPTickBarPlugin.PluginInterface.UiBuilder.LoadImage(Resources.JobStackMaterialUI);
            var numberPercentage = MPTickBarPlugin.PluginInterface.UiBuilder.LoadImage(Resources.NumberPercentage);
            this.MPTickBarPluginUI = new MPTickBarPluginUI(this.Configuration, gaugeDefault, gaugeMaterialUIBlack, GaugeMaterialUIDiscord, jobStackDefault, jobStackMaterialUI, numberPercentage);

            MPTickBarPlugin.CommandManager.AddHandler(this.CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens MP Tick Bar configuration menu.",
                ShowInHelp = true
            });

            MPTickBarPlugin.PluginInterface.UiBuilder.DisableAutomaticUiHide = false;
            MPTickBarPlugin.PluginInterface.UiBuilder.DisableCutsceneUiHide = false;
            MPTickBarPlugin.PluginInterface.UiBuilder.DisableGposeUiHide = false;
            MPTickBarPlugin.PluginInterface.UiBuilder.DisableUserUiHide = false;

            MPTickBarPlugin.ClientState.Login += this.UpdateEventState.Login;
            MPTickBarPlugin.PluginInterface.UiBuilder.Draw += this.Draw;
            MPTickBarPlugin.PluginInterface.UiBuilder.OpenConfigUi += this.OpenConfigUi;
            MPTickBarPlugin.Framework.Update += this.Update;
        }

        public void Dispose()
        {
            this.MPTickBarPluginUI.Dispose();
            MPTickBarPlugin.ClientState.Login -= this.UpdateEventState.Login;
            MPTickBarPlugin.PluginInterface.UiBuilder.Draw -= this.Draw;
            MPTickBarPlugin.PluginInterface.UiBuilder.OpenConfigUi -= this.OpenConfigUi;
            MPTickBarPlugin.Framework.Update -= this.Update;
            MPTickBarPlugin.CommandManager.RemoveHandler(this.CommandName);
            MPTickBarPlugin.PluginInterface.Dispose();
        }

        private void OnCommand(string command, string args)
        {
            this.OpenConfigUi();
        }

        private void Draw()
        {
            this.MPTickBarPluginUI.Draw();
        }

        private void OpenConfigUi()
        {
            this.MPTickBarPluginUI.IsConfigurationWindowVisible = !this.MPTickBarPluginUI.IsConfigurationWindowVisible;
        }

        private void Update(Framework framework)
        {
            var currentPlayer = MPTickBarPlugin.ClientState.LocalPlayer;
            this.MPTickBarPluginUI.IsMPTickBarVisible = MPTickBarPlugin.ClientState.IsLoggedIn && PlayerHelpers.IsBlackMage(currentPlayer);

            if (!this.MPTickBarPluginUI.IsMPTickBarVisible)
                return;

            unsafe
            {
                this.UpdateEventState.Update(currentPlayer, MPTickBarPlugin.ClientState, *ActionManager.Instance(), this.MPTickBarPluginUI, this.Configuration);
            }
        }
    }
}