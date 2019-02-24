
This is a fast video capture wrapper using the 'ffmpeg' executable.

Download ffmpeg from here: https://github.com/keijiro/FFmpegOutBinaries/releases

Note that ffmpeg is licensed under the GPL.

To use FastVideoCapture.cs in a project, you need to go to Project Settings, Player,
Other Settings, and check "Allow 'unsafe' Code".  This allows the .cs files to make
use of the 'unsafe' keyword.  In this case, I found out that using 'unsafe' code
completely avoids a big slow-down in one key part of the code.
