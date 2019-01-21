package;
import haxe.Int64;

/**
 * ...
 * @author Bioruebe
 */
class FileMeta{
	public var path:String;
	public var offset:Int64;
	public var size:Int;
	//public var md5:String;
	
	public function new(path:String) {
		this.path = path.substr(6);
	}
	
	public function toString() {
		return '${StringTools.lpad(Std.string(offset), "0", 12)}  $path, $size';
	}
	
}