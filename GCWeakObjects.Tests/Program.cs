using System;

namespace Kerosene.Tools.Tests
{
	// ====================================================
	/// <summary>
	/// Represents the current program.
	/// </summary>
	public class Program
	{
		/// <summary>
		/// The entry point of the program.
		/// </summary>
		/// <param name="args"></param>
		static void Main(string[] args)
		{
			DebugEx.IndentSize = 3;
			DebugEx.AutoFlush = true;
			DebugEx.AddConsoleListener();
			ConsoleEx.AskInteractive();

			Launcher.Execute();
			ConsoleEx.ReadLine("\n=== Press [Enter] to finish... ");
		}
	}
}
