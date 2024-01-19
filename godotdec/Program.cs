using BioLib;
using BioLib.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace godotdec {
	class Program {
		private const string VERSION = "2.1.2";
		private const string PROMPT_ID = "godotdec_overwrite";
		private const int MAGIC_PACKAGE = 0x43504447;

		private static string inputFile;
		private static string outputDirectory;
		private static bool convertAssets;

		static void Main(string[] args) {
			Bio.Header("godotdec", VERSION, "2018-2020", "A simple unpacker for Godot Engine package files (.pck|.exe)",
				"[<options>] <input_file> [<output_directory>]\n\nOptions:\n-c\t--convert\tConvert textures and audio files");

			if (Bio.HasCommandlineSwitchHelp(args)) return;
			ParseCommandLine(args.ToList());

			var failed = 0;
			using (var inputStream = new BinaryReader(File.Open(inputFile, FileMode.Open))) {
				if (inputStream.ReadInt32() != MAGIC_PACKAGE) {
					inputStream.BaseStream.Seek(-4, SeekOrigin.End);

					CheckMagic(inputStream.ReadInt32());

					inputStream.BaseStream.Skip(-12);
					var offset = inputStream.ReadInt64();
					inputStream.BaseStream.Skip(-offset - 8);

					CheckMagic(inputStream.ReadInt32());
				}
				
				int packFormatVersion = inputStream.ReadInt32();
				Bio.Cout($"Package format version: {packFormatVersion}");
				Bio.Cout($"Godot Engine version: {inputStream.ReadInt32()}.{inputStream.ReadInt32()}.{inputStream.ReadInt32()}");

				if (packFormatVersion <= 1) {
					// No special handling
				}
				else if (packFormatVersion == 2) {
					uint packFlags = inputStream.ReadUInt32();
					if ((packFlags & 1) != 0) // PACK_DIR_ENCRYPTED
						Bio.Error("Encrypted directory not supported.", Bio.EXITCODE.NOT_SUPPORTED);
				}
				else {
					Bio.Error($"Package format version {packFormatVersion} is not supported.", Bio.EXITCODE.NOT_SUPPORTED);
				}

				long filesBaseOffset = 0;
				if (packFormatVersion >= 2) filesBaseOffset = inputStream.ReadInt64();

				// Skip reserved bytes (16x Int32)
				inputStream.BaseStream.Skip(16 * 4);

				var fileCount = inputStream.ReadInt32();
				Bio.Cout($"Found {fileCount} files in package");
				Bio.Cout("Reading file index");

				var fileIndex = new List<FileEntry>();
				for (var i = 0; i < fileCount; i++) {
					var pathLength = inputStream.ReadInt32();
					var path = Encoding.UTF8.GetString(inputStream.ReadBytes(pathLength));
					var fileEntry = new FileEntry(path.ToString(), inputStream.ReadInt64() + filesBaseOffset, inputStream.ReadInt64());
					fileIndex.Add(fileEntry);
					//Bio.Debug(fileEntry);
					inputStream.BaseStream.Skip(16);

					if (packFormatVersion >= 2) {
						var fileFlags = inputStream.ReadUInt32();
						if ((fileFlags & 1) != 0) Bio.Error("Encrypted files not supported.", Bio.EXITCODE.NOT_SUPPORTED);
					}
				}

				if (fileIndex.Count < 1) Bio.Error("No files were found inside the archive", Bio.EXITCODE.RUNTIME_ERROR);
				fileIndex.Sort((a, b) => (int) (a.offset - b.offset));

				Bio.Cout("Extracting files...");
				if (convertAssets) Bio.Cout("File conversion is enabled");

				var fileIndexEnd = inputStream.BaseStream.Position;
				for (var i = 0; i < fileIndex.Count; i++) {
					var fileEntry = fileIndex[i];
					Bio.Progress(fileEntry.path, i+1, fileIndex.Count);

					if (fileEntry.offset < fileIndexEnd) {
						Bio.Warn("Invalid file offset: " + fileEntry.offset);
						continue;
					}

					// TODO: Only PNG compression is supported
					if (convertAssets) {
						var internalPath = fileEntry.path.ToLower();
						// https://github.com/godotengine/godot/blob/master/editor/import/resource_importer_texture.cpp#L222
						if (internalPath.EndsWith(".stex")) {
							fileEntry.Resize(32);
							fileEntry.ChangeExtension(".stex", ".png");
							Bio.Debug(fileEntry);
						}
						// https://github.com/godotengine/godot/blob/master/core/io/resource_format_binary.cpp#L836
						else if (internalPath.EndsWith(".oggstr")) {
							fileEntry.Resize(279, 4);
							fileEntry.ChangeExtension(".oggstr", ".ogg");
						}
						// https://github.com/godotengine/godot/blob/master/scene/resources/audio_stream_sample.cpp#L518
						else if (internalPath.EndsWith(".sample")) {
							// TODO
							Bio.Warn("The file type '.sample' is currently not supported");
						}
					}

					inputStream.BaseStream.Position = fileEntry.offset;
					var destination = Path.Combine(outputDirectory, fileEntry.path);

					try {
						Action<Stream, Stream> copyFunction = (input, output) => input.Copy(output, (int) fileEntry.size);
						inputStream.BaseStream.WriteToFileRelative(destination, PROMPT_ID, copyFunction);
					}
					catch (Exception e) {
						Bio.Error(e);
						failed++;
					}
				}
			}

			Bio.Cout();
			Bio.Cout(failed < 1? "All OK": failed + " files failed to extract");
			Bio.Pause();
		}

		static void ParseCommandLine(List<string> args) {
			convertAssets = args.Remove("--convert") || args.Remove("-c");
			if (args.Count < 1) Bio.Error("Please specify the path to a Godot .pck or .exe file.", Bio.EXITCODE.INVALID_INPUT);

			inputFile = args[0];
			if (!File.Exists(inputFile)) Bio.Error("The input file " + inputFile + " does not exist. Please make sure the path is correct.", Bio.EXITCODE.IO_ERROR);
			
			outputDirectory = args.Count > 1? args[1]: Path.Combine(Path.GetDirectoryName(inputFile), Path.GetFileNameWithoutExtension(inputFile));

			Bio.Debug("Input file: " + inputFile);
			Bio.Debug("Output directory: " + outputDirectory);
		}

		static void CheckMagic(int magic) {
			if (magic == MAGIC_PACKAGE) return;
			Bio.Error("The input file is not a valid Godot package file.", Bio.EXITCODE.INVALID_INPUT);
		}
	}
}

class FileEntry {
	public string path;
	public long offset;
	public long size;
	//public var md5:String;

	public FileEntry (string path, long offset, long size) {
		this.path = path.Substring(6).TrimEnd('\0');
		this.offset = offset;
		this.size = size;
	}

	public void Resize(int by, int stripAtEnd = 0) {
		offset += by;
		size -= by + stripAtEnd;
	}

	public void ChangeExtension(string from, string to) {
		path = path.Replace(from, to);
	}

	public override string ToString() {
		return $"{offset:000000} {path}, {size}";
	}
}
