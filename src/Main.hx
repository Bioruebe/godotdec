package;

import haxe.Int64;
import haxe.crypto.Md5;
import haxe.io.Bytes;
import haxe.io.Eof;
import sys.FileSystem;
import sys.io.File;
import sys.io.FileInput;
import sys.io.FileSeek;
using Lambda;
using haxe.Int64;

/**
 * ...
 * @author Bioruebe
 */
class Main {
	static var file:FileInput;
	static var index = new List<FileMeta>();
	
	static function main() {
		Bio.Header("godotdec", "1.0.0", "2018", "A simple unpacker for Godot Engine package files (.pck)", "<input_file> [<output_dir>]");
		Bio.Seperator();
		
		var args = Sys.args();
		if (args.length < 1) Bio.Error("Please specify the path to a .pck file.", 1);
		
		var pck = args[0];
		if (!FileSystem.exists(pck)) Bio.Error("The input file " + pck + " does not exist.", 1);
		
		file = File.read(pck);
		if (file.readInt32() != 0x43504447) Bio.Error("The input file is not a valid .pck file.", 2);
		
		var outdir = args.length > 1? args[1]: Bio.FileGetParts(pck).name;
		if (!FileSystem.exists(outdir)) FileSystem.createDirectory(outdir);
		outdir = Bio.PathAppendSeperator(outdir);
		Bio.Cout("Output directory: " + outdir);
		Bio.Cout('Godot Engine version: ${file.readInt32()}.${file.readInt32()}.${file.readInt32()}.${file.readInt32()}');
		
		// Skip reserved space
		for (i in 0...16) {
			file.readInt32();
		}
		
		var iFiles = file.readInt32();
		var bytesBuffer = Bytes.alloc(16);
		Bio.Cout("Found " + iFiles + " files in package");
		Bio.Cout("Reading file index");
		
		var i = 1;
		while (true) {
			try {
				var meta = findFileEntry();
				if (meta != null) {
					Bio.Cout('$i/$iFiles\t' + outdir + meta.path);
					index.remove(meta);
					var buffer = Bytes.alloc(meta.size);
					file.readBytes(buffer, 0, meta.size);
					File.saveBytes(Bio.AssurePathExists(outdir + meta.path), buffer);
					//file.seek(meta.size.toInt(), FileSeek.SeekCur);
					i++;
					continue;
				}
				
				var nameLength = file.readInt32();
				var f = new FileMeta(file.readString(nameLength));
				f.offset = readInt64();
				f.size = readInt64().toInt();
				file.seek(16, FileSeek.SeekCur);
				//file.readBytes(bytesBuffer, 0, 16);
				//f.md5 = bytesBuffer.toHex();
				index.add(f);
			}
			catch (eof:Eof){
				break;
			}
		}
	}
	
	static function readInt64() {
		var a = file.readInt32(), b = file.readInt32();
		return Int64.make(b, a);
	}
	
	static function findFileEntry():FileMeta {
		var pos = file.tell();
		return index.find(function (el) {
			return el.offset.compare(pos) == 0;
		});
	}
	
}