ffmpeg version 4.1.1 Copyright (c) 2000-2019 the FFmpeg developers
Run 'ffmpeg -L' for more details.


=======
License
=======

'ffmpeg.exe' is licensed under the GPL, a copy of which is included in 'gpl-2.0.txt'.

The source is found here:
* http://www.videolan.org/developers/x264.html
* http://ffmpeg.org/


===================================
Instructions to rebuild from source
===================================

This assume you are running in a MSYS2 on Windows: see
https://www.ffmpeg.org/platform.html#Native-Windows-compilation-using-MinGW-or-MinGW_002dw64

Then follow the instructions at
https://github.com/alberthdev/alberthdev-misc/wiki/Build-your-own-tiny-FFMPEG
which mostly just work on MSYS2 on Windows too.

The precise binary included here was built with the following instructions:
#############################################################################


cd ffmpeg-4.1.1/x264/build
../configure --enable-static --disable-cli --disable-gpl --disable-opencl --disable-avs --disable-swscale --disable-lavf --disable-ffms --disable-gpac --disable-lsmash
make -j4
cp libx264.a x264_config.h ../


cd ffmpeg-4.1.1/build
../configure --disable-all `python ../../get_autodetect_options.py --opts` --enable-ffmpeg --enable-avcodec --enable-avformat --enable-avfilter --enable-filter=vflip,scale --enable-decoder=rawvideo --enable-encoder=libx264 --enable-protocol=file,pipe --enable-demuxer=rawvideo --enable-muxer=mp4 --enable-gpl --enable-libx264 --extra-cflags="-I../x264" --extra-cxxflags="-I../x264" --extra-ldflags="-L../x264" --enable-w32threads --enable-swscale
make -j4
cp ../../../../mingw64/bin/libwinpthread-1.dll .
