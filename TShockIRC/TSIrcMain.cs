using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace TShockIRC
{
	[ApiVersion(2, 1)]
	public class TSIrcMain : TerrariaPlugin
	{
		#region Plugin Information
		public override string Author => "MarioE";
		public override string Description => "Provides an IRC interface.";
		public override string Name => "TShockIRC";
		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
		#endregion

		public TSIrcMain(Main game)
			: base(game)
		{
			Order = Int32.MaxValue;
		}

		#region Initialize/Dispose
		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
			ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer);
			ServerApi.Hooks.ServerChat.Register(this, OnChat);
			ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
			PlayerHooks.PlayerCommand += OnPlayerCommand;
			PlayerHooks.PlayerPostLogin += OnPostLogin;
			GeneralHooks.ReloadEvent += OnReload;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreetPlayer);
				ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
				ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
				PlayerHooks.PlayerCommand -= OnPlayerCommand;
				PlayerHooks.PlayerPostLogin -= OnPostLogin;
				GeneralHooks.ReloadEvent -= OnReload;

				TSIrcClient.IrcClient.Dispose();
			}
		}
		#endregion

		#region Hooks
		private void OnInitialize(EventArgs e)
		{
			IRCCommands.Initialize();
			Commands.ChatCommands.Add(new Command("tshockirc.manage", IRCRestart, "ircrestart"));

			string configPath = Path.Combine(TShock.SavePath, "tshockircconfig.json");
			(TSIrcClient.Config = Config.Read(configPath)).Write(configPath);

			TSIrcClient.Connect();
		}

		private void OnGreetPlayer(GreetPlayerEventArgs e)
		{
			if (!TSIrcClient.IrcClient.IsConnected)
				TSIrcClient.Connect();
			else
			{
				TSPlayer tsplr = TShock.Players[e.Who];
				if (!String.IsNullOrEmpty(TSIrcClient.Config.ServerJoinMessageFormat))
					TSIrcClient.SendMessage(TSIrcClient.Config.Channel, String.Format(TSIrcClient.Config.ServerJoinMessageFormat, tsplr.Name));
				if (!String.IsNullOrEmpty(TSIrcClient.Config.ServerJoinAdminMessageFormat))
					TSIrcClient.SendMessage(TSIrcClient.Config.AdminChannel, String.Format(TSIrcClient.Config.ServerJoinAdminMessageFormat, tsplr.Name, tsplr.IP));
			}
		}

		private void OnPostLogin(PlayerPostLoginEventArgs e)
		{
			if (!TSIrcClient.IrcClient.IsConnected)
				TSIrcClient.Connect();
			else if (!String.IsNullOrEmpty(TSIrcClient.Config.ServerLoginAdminMessageFormat))
				TSIrcClient.SendMessage(TSIrcClient.Config.AdminChannel, String.Format(TSIrcClient.Config.ServerLoginAdminMessageFormat, e.Player.Name, e.Player.User.Name, e.Player.IP));
		}

		private void OnChat(ServerChatEventArgs e)
		{
			TSPlayer tsPlr = TShock.Players[e.Who];
			if (!TSIrcClient.IrcClient.IsConnected)
				TSIrcClient.Connect();
			else if (e.Text != null && !e.Text.StartsWith(TShock.Config.CommandSpecifier) && !e.Text.StartsWith(TShock.Config.CommandSilentSpecifier) && tsPlr != null &&
				!tsPlr.mute && tsPlr.HasPermission(Permissions.canchat) && !String.IsNullOrEmpty(TSIrcClient.Config.ServerChatMessageFormat) &&
				!TSIrcClient.Config.IgnoredServerChatRegexes.Any(s => Regex.IsMatch(e.Text, s)))
			{
				TSIrcClient.SendMessage(TSIrcClient.Config.Channel, String.Format(TSIrcClient.Config.ServerChatMessageFormat, tsPlr.Group.Prefix, tsPlr.Name, e.Text, tsPlr.Group.Suffix));
			}
		}

		private void OnPlayerCommand(PlayerCommandEventArgs e)
		{
			if (!TSIrcClient.IrcClient.IsConnected)
				TSIrcClient.Connect();
			else if (e.Player.RealPlayer)
			{
				if (String.Equals(e.CommandName, "me", StringComparison.CurrentCultureIgnoreCase) && e.CommandText.Length > 2)
				{
					if (!e.Player.mute && e.Player.HasPermission(Permissions.cantalkinthird) && !String.IsNullOrEmpty(TSIrcClient.Config.ServerActionMessageFormat))
						TSIrcClient.SendMessage(TSIrcClient.Config.Channel, String.Format(TSIrcClient.Config.ServerActionMessageFormat, e.Player.Name, e.CommandText.Substring(3)));
				}
				else if (e.CommandList.Count() == 0 || e.CommandList.First().DoLog)
				{
					if (!String.IsNullOrEmpty(TSIrcClient.Config.ServerCommandMessageFormat))
						TSIrcClient.SendMessage(TSIrcClient.Config.AdminChannel, String.Format(TSIrcClient.Config.ServerCommandMessageFormat, e.Player.Group.Prefix, e.Player.Name, e.CommandText));
				}
			}
		}

		private void OnLeave(LeaveEventArgs e)
		{
			TSPlayer tsplr = TShock.Players[e.Who];
			if (!TSIrcClient.IrcClient.IsConnected)
				TSIrcClient.Connect();
			else if (tsplr != null && tsplr.ReceivedInfo && tsplr.State >= 3 && !tsplr.SilentKickInProgress)
			{
				if (!String.IsNullOrEmpty(TSIrcClient.Config.ServerLeaveMessageFormat))
					TSIrcClient.SendMessage(TSIrcClient.Config.Channel, String.Format(TSIrcClient.Config.ServerLeaveMessageFormat, tsplr.Name));
				if (!String.IsNullOrEmpty(TSIrcClient.Config.ServerLeaveAdminMessageFormat))
					TSIrcClient.SendMessage(TSIrcClient.Config.AdminChannel, String.Format(TSIrcClient.Config.ServerLeaveAdminMessageFormat, tsplr.Name, tsplr.IP));
			}
		}

		private void OnReload(ReloadEventArgs args)
		{
			string configPath = Path.Combine(TShock.SavePath, "tshockircconfig.json");
			(TSIrcClient.Config = Config.Read(configPath)).Write(configPath);
		}
		#endregion

		#region Commands
		private void IRCRestart(CommandArgs e)
		{
			TSIrcClient.IrcClient.Quit("Restarting...");
			TSIrcClient.Restart();
			e.Player.SendInfoMessage("Restarted the IRC bot.");
		}
		#endregion
	}
}
