using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cicdec;

namespace godotdec {
	class Program {
		static void Main(string[] aArgs) {
			Bio.Header("godotdec", "2.0.0", "2018-2019", "A simple unpacker for Godot Engine package files (.pck|.exe)",
				"[<options>] <input_file> [<output_directory>]\n\nOptions:\n-c\t--convert\tConvert textures and audio files");

			var args = aArgs.ToList();
			
			if (args.Contains("-h") || args.Contains("--help") || args.Contains("/?") || args.Contains("-?") ||
				args.Contains("/h")) return;

			var convert = args.Remove("--convert") || args.Remove("-c");
			if (args.Count < 1) Bio.Error("Please specify the path to a Godot .pck or .exe file.", 1);

			var inputFile = args[0];
			if (!File.Exists(inputFile)) Bio.Error("The input file " + inputFile + " does not exist.", 1);
			var outdir = args.Count > 1? args[1]: Path.Combine(Path.GetDirectoryName(inputFile), Path.GetFileNameWithoutExtension(inputFile));
			Bio.Debug("Input file: " + inputFile);
			Bio.Debug("Output directory: " + outdir);

			var failed = 0;
			using (var inputStream = new BinaryReader(File.Open(inputFile, FileMode.Open))) {
				if (inputStream.ReadInt32() != 0x43504447) {
					inputStream.BaseStream.Seek(-4, SeekOrigin.End);

					CheckMagic(inputStream.ReadInt32());

					inputStream.BaseStream.Seek(-12, SeekOrigin.Current);
					var offset = inputStream.ReadInt64();
					inputStream.BaseStream.Seek(-offset - 8, SeekOrigin.Current);

					CheckMagic(inputStream.ReadInt32());
				}
				
				Bio.Cout($"Godot Engine version: {inputStream.ReadInt32()}.{inputStream.ReadInt32()}.{inputStream.ReadInt32()}.{inputStream.ReadInt32()}");

				// Skip reserved bytes (16x Int32)
				inputStream.BaseStream.Seek(16 * 4, SeekOrigin.Current);

				var fileCount = inputStream.ReadInt32();
				Bio.Cout($"Found {fileCount} files in package");
				Bio.Cout("Reading file index");

				var fileIndex = new List<FileEntry>();
				for (var i = 0; i < fileCount; i++) {
					var pathLength = inputStream.ReadInt32();
					var path = Encoding.UTF8.GetString(inputStream.ReadBytes(pathLength));
					var fileEntry = new FileEntry(path.ToString(), inputStream.ReadInt64(), inputStream.ReadInt64());
					fileIndex.Add(fileEntry);
					Bio.Debug(fileEntry);
					inputStream.BaseStream.Seek(16, SeekOrigin.Current);
					//break;
				}

				if (fileIndex.Count < 1) Bio.Error("No files were found inside the archive", 2);
				fileIndex.Sort((a, b) => (int) (a.offset - b.offset));

				var fileIndexEnd = inputStream.BaseStream.Position;
				for (var i = 0; i < fileIndex.Count; i++) {
					var fileEntry = fileIndex[i];
					Bio.Cout($"{i+1}/{fileIndex.Count}\t{fileEntry.path}");
					//break;
					if (fileEntry.offset < fileIndexEnd) {
						Bio.Warn("Invalid file offset: " + fileEntry.offset);
						continue;
					}

					// TODO: Only PNG compression is supported
					if (convert) {
						// https://github.com/godotengine/godot/blob/master/editor/import/resource_importer_texture.cpp#L222
						if (fileEntry.path.EndsWith(".stex") && fileEntry.path.Contains(".png")) {
							fileEntry.Resize(32);
							fileEntry.ChangeExtension(".stex", ".png");
							Bio.Debug(fileEntry);
						}
						// https://github.com/godotengine/godot/blob/master/core/io/resource_format_binary.cpp#L836
						else if (fileEntry.path.EndsWith(".oggstr")) {
							fileEntry.Resize(279, 4);
							fileEntry.ChangeExtension(".oggstr", ".ogg");
						}
						// https://github.com/godotengine/godot/blob/master/scene/resources/audio_stream_sample.cpp#L518
						else if (fileEntry.path.EndsWith(".sample")) {
							// TODO
						}
					}

					var destination = Path.Combine(outdir, Bio.FileReplaceInvalidChars(fileEntry.path));
					inputStream.BaseStream.Seek(fileEntry.offset, SeekOrigin.Begin);

					try {
						var fileMode = FileMode.CreateNew;
						Directory.CreateDirectory(Path.GetDirectoryName(destination));
						if (File.Exists(destination)) {
							if (!Bio.Prompt($"The file {fileEntry.path} already exists. Overwrite?", "godotdec_overwrite")) continue;
							fileMode = FileMode.Create;
						}
						using (var outputStream = new FileStream(destination, fileMode)) {
							Bio.CopyStream(inputStream.BaseStream, outputStream, (int) fileEntry.size, false);
						}
					}
					catch (Exception e) {
						Bio.Error(e);
						failed++;
					}
				}
			}

			Bio.Cout(failed < 1? "All OK": failed + " files failed to extract");
			Bio.Pause();
		}

		static void CheckMagic(int magic) {
			if (magic == 0x43504447) return;
			Bio.Error("The input file is not a valid Godot package file.", 2);
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
