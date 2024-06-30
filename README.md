# BTFTool
Manipulation tool for language files (*.btf) of the game: Workers & Resources: Soviet Republic.    
It can export strings to a text file and import them back to a BTF file.

## Usage 
```
Usage: BTFTool.exe btfFile [OPTIONS]
  btfFile            Language file (*.btf) of the game Workers & Resources: Soviet Republic
Options:
  --help             Show this message and exit
  --verbose          Enable verbose mode
  --export=<file>    Specify the output file for the extracted strings from the btf file
  --import=<file>    Provide an input file with strings to be inserted into the btf file

Example:
BTFTool.exe S:\Steam\steamapps\common\SovietRepublic\media_soviet\sovietEnglish.btf "--export=S:\Data\EN.txt"
BTFTool.exe S:\Steam\steamapps\common\SovietRepublic\media_soviet\sovietEnglish.btf "--import=S:\Data\EN.txt"
```

## Format of the text file
```
The format for import is the same as for export. File encoding is UTF-8 with BOM.  
Each line is a new string. Format of the line is:  
  
String <ID>: "<string>"  
  
<ID> is number of the string in the btf file.  
<string> is the text of the string with escaped characters, so \r\n is a new line etc.  
         for more info visit: https://en.wikipedia.org/wiki/Escape_sequences_in_C  
The exact regex used for the import (case insensitive): ^String\s*([0-9]+):\s*(.*)$  
If a line does not follow this format, it is ignored! So you can add your own comments.  
If the line has the correct format but <string> is empty, the ID is removed from the btf file.  
The imported file can contain only some strings, the rest will remain unchanged in the btf file.  
```
