# godotdec

A simple unpacker for Godot Engine package files (.pck)

### Usage
`godotdec [<options>] <input_file> [<output_dir>]`

###### Options:

| Flag (short) | Flag (long) | Description                                                  |
| ------------ | ----------- | ------------------------------------------------------------ |
| -c           | --convert   | Convert certain engine-specific file types (textures, some audio streams) to standard formats. |

### Technical details

Godot Engine's package format is specified as:

| Value/Type | Description                                                  |
| ---------- | ------------------------------------------------------------ |
| 0x43504447 | Magic number (GDPC)                                          |
| 4 x Int32  | Engine version: version, major, minor, revision              |
| 16 x Int32 | Reserved space, 0                                            |
| Int32      | Number of files in archive                                   |
|            | ----- Begin of file index, for each file the following data is stored ---- |
| Int32      | Length of path string                                        |
| String     | Path as string, e.g. res://actors/Enemy/enemy.atex           |
| Int64      | File offset                                                  |
| Int64      | File size                                                    |
| 16 bytes   | MD5                                                          |
|            | ----- Begin of file contents -----                           |

The source code of the .pck packer can be found [here](https://github.com/godotengine/godot/blob/master/core/io/pck_packer.cpp)

### Limitations

- Modified engine versions may use a custom package format, which godotdec does not support
- MD5 checksum is not used to verify extracted files
- Format conversion is only supported for .png, .ogg

### Remarks
No reverse engineering has been used to write this tool.

I released it as a helper for artists to search games for unlicensed use of their assets. It is not meant to encourage extraction with the sole purpose of using assets in your own products without permission of the copyright holder.

Remember: don't steal assets from other people's games. Respect copyrights. And don't protect your own games - it's unnecessary effort.
