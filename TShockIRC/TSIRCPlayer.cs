using IrcDotNet;
using Microsoft.Xna.Framework;
using TShockAPI;

namespace TShockIRC
{
	public class TSIrcPlayer : TSPlayer
	{
		const int MAX_CHARS_PER_LINE = 400;

		IIrcMessageTarget Target;

		public TSIrcPlayer(string name, Group group, IIrcMessageTarget target)
			: base(name)
		{
			Group = group;
			Target = target;
		}

		public override void SendMessage(string msg, Color color)
		{
			TSIrcClient.SendMessage(Target, msg);
		}
		public override void SendMessage(string msg, byte red, byte green, byte blue)
		{
			TSIrcClient.SendMessage(Target, msg);
		}
		public override void SendErrorMessage(string msg)
		{
			TSIrcClient.SendMessage(Target, "\u000305" + msg);
		}
		public override void SendInfoMessage(string msg)
		{
			TSIrcClient.SendMessage(Target, "\u000302" + msg);
		}
		public override void SendSuccessMessage(string msg)
		{
			TSIrcClient.SendMessage(Target, "\u000303" + msg);
		}
		public override void SendWarningMessage(string msg)
		{
			TSIrcClient.SendMessage(Target, "\u000305" + msg);
		}
	}
}
