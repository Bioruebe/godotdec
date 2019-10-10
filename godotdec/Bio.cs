using System;
using System.Collections.Generic;
using System.IO;

namespace cicdec {
	public static class Bio {
		public const string SEPERATOR = "\n---------------------------------------------------------------------";
		private static readonly Dictionary<string, PROMPT_SETTING> promptSettings = new Dictionary<string, PROMPT_SETTING>();

		public static void CopyStream(Stream input, Stream output, int bytes = -1, bool keepPosition = true, int bufferSize = 1024) {
			var buffer = new byte[bufferSize];
			long initialPosition = 0;
			if (keepPosition) initialPosition = input.Position;
			int read;
			if (bytes < 1) bytes = (int) (input.Length - input.Position);

			while (bytes > 0 && (read = input.Read(buffer, 0, Math.Min(bufferSize, bytes))) > 0) {
				output.Write(buffer, 0, read);
				bytes -= read;
			}

			if (keepPosition) input.Seek(initialPosition, SeekOrigin.Begin);
		}

		public static string FileReplaceInvalidChars(string filename, string by = "_") {
			return string.Join(by, filename.Split(Path.GetInvalidPathChars()));
		}

		public static void Header(string name, string version, string year, string description, string usage = "") {
			var header = string.Format("{0} by Bioruebe (https://bioruebe.com), {1}, Version {2}, Released under a BSD 3-Clause style license\n\n{3}{4}\n{5}", name, year, version, description, (usage == null ? "" : "\n\nUsage: " + GetProgramName() + " " + usage), SEPERATOR);

			Console.WriteLine(header);
		}

		public static void Seperator() {
			Console.WriteLine(SEPERATOR + "\n");
		}

		public static string GetProgramName() {
			return System.Diagnostics.Process.GetCurrentProcess().ProcessName;
		}

		public static bool Prompt(string msg, string id = "", string choices =
		"[Y]es | [N]o | [A]lways | n[E]ver", string chars = "ynae") {
			// Check setting from previous function calls
			promptSettings.TryGetValue(id, out var setting);
			if (setting == PROMPT_SETTING.ALWAYS) return true;
			if (setting == PROMPT_SETTING.NEVER) return false;

			var aChars = chars.ToCharArray();
			int input;

			while (true) {
				Console.WriteLine(msg + $" {choices}");
				input = Console.ReadKey().KeyChar;
				Console.WriteLine();

				if (input == aChars[0]) return true;
				if (input == aChars[1]) return false;
				if (input == aChars[2]) {
					promptSettings[id] = PROMPT_SETTING.ALWAYS;
					return true;
				}

				if (input == aChars[3]) {
					promptSettings[id] = PROMPT_SETTING.NEVER;
					return false;
				}
			}

		}

		public static void Cout(string msg, LOG_SEVERITY logSeverity = LOG_SEVERITY.MESSAGE) {
#if !DEBUG
			if (logSeverity == LOG_SEVERITY.DEBUG) return;
#endif
			if (msg.StartsWith("\n")) {
				Console.WriteLine();
				msg = msg.Substring(1);
			}

			if (logSeverity != LOG_SEVERITY.MESSAGE) msg = string.Format("[{0}] {1}", logSeverity, msg);

			switch (logSeverity) {
				case LOG_SEVERITY.ERROR:
				case LOG_SEVERITY.CRITICAL:
					Console.Error.WriteLine();
					Console.WriteLine(msg);
					break;
				default:
					Console.WriteLine(msg);
					break;
			}
		}

		public static void Cout(object msg, LOG_SEVERITY logSeverity = LOG_SEVERITY.MESSAGE) {
			Cout(msg.ToString(), logSeverity);
		}

		public static void Debug(object msg) {
			Cout(msg, LOG_SEVERITY.DEBUG);
		}

		public static void Warn(object msg) {
			Cout(msg, LOG_SEVERITY.WARNING);
		}

		public static void Error(object msg, int exitCode = -1) {
			Cout(msg, LOG_SEVERITY.ERROR);
#if DEBUG
			Console.ReadKey(false);
#endif
			if (exitCode > -1) Environment.Exit(exitCode);
		}

		public static void Pause() {
#if DEBUG
			Console.ReadKey();
#endif
		}

		public enum LOG_SEVERITY {
			DEBUG,
			INFO,
			WARNING,
			ERROR,
			CRITICAL,
			MESSAGE,
			UNITTEST
		}

		public enum PROMPT_SETTING {
			NONE,
			ALWAYS,
			NEVER
		}
	}
}