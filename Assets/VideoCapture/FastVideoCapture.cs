using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;


#if UNITY_2018_3_OR_NEWER
using UnityEngine.Rendering;
#else
using UnityEngine.Experimental.Rendering;
#endif


public class FastVideoCapture : MonoBehaviour
{
    public RenderTexture captureRenderTexture;
    public int frameRate = 30;
    public string outputPath = "FastVideoCapture.mp4";


    float nextTick;

    Queue<AsyncGPUReadbackRequest> _requests;
    FFMpegSession ffmpeg_session;
    RenderTexture render_tex;
    bool got_error;

    public bool GotError { get { return got_error || (ffmpeg_session != null && ffmpeg_session.got_error); } }

    void Close()
    {
        _requests = null;
        render_tex = null;
        if (ffmpeg_session != null)
        {
            ffmpeg_session.Close();
            got_error = GotError;   /* capture ffmpeg_session.got_error */
            ffmpeg_session = null;
        }
    }

    void OnDisable()
    {
        Close();
    }

    void OnDestroy()
    {
        Close();
    }

    void Update()
    {
        if (ffmpeg_session == null)
        {
            var render_tex_1 = captureRenderTexture;
            if (render_tex_1 == null)
            {
                var camera = GetComponent<Camera>();
                if (camera == null || camera.targetTexture == null)
                {
                    Debug.LogError("the FastVideoCapture component must be either have a " +
                        "'captureRenderTexture' set, or be with a Camera component with a " +
                        "non-null 'targetTexture'.");
                    enabled = false;
                    return;
                }
                render_tex_1 = camera.targetTexture;
            }
            got_error = false;
            ffmpeg_session = new FFMpegSession(render_tex_1.width, render_tex_1.height, frameRate, outputPath);
            render_tex = render_tex_1;
            _requests = new Queue<AsyncGPUReadbackRequest>();
            nextTick = Time.time;
        }

        while (_requests.Count > 0)
        {
            var req = _requests.Peek();

            if (req.hasError)
            {
                /* this could also occur theoretically if the requests are successful but in the wrong
                 * order: this request could have hasError return true now because it was successful
                 * in some previous frame. */
                Debug.Log("GPU readback error detected.");
                _requests.Dequeue();
            }
            else if (req.done)
            {
                NativeArray<byte> buffer = req.GetData<byte>();
                UnityEngine.Profiling.Profiler.BeginSample("FastVideoCapture.Write");
                ffmpeg_session.Write(buffer);
                UnityEngine.Profiling.Profiler.EndSample();
                _requests.Dequeue();
            }
            else
            {
                break;
            }
        }
    }

    void OnPostRender()
    {
        if (Time.time >= nextTick && _requests != null)
        {
            nextTick += 1f / frameRate;
            if (nextTick < Time.time)
                nextTick = Time.time;

            if (_requests.Count < 8)
                _requests.Enqueue(AsyncGPUReadback.Request(render_tex));
            else
                Debug.LogWarning("Too many requests waiting for the GPU, dropping frames from the video");
        }
    }

    public class FFMpegSession
    {
        System.Diagnostics.Process proc;
        FileStream pipe;
        int width, height;
        internal bool got_error;

        public FFMpegSession(int width, int height, int frameRate, string outputPath)
        {
            string arguments = "-y -f rawvideo -vcodec rawvideo -pixel_format rgba"
                            + " -colorspace bt709"
                            + " -video_size " + width + "x" + height
                            + " -framerate " + frameRate
                            + " -loglevel warning -i - -vf vflip -pix_fmt yuv420p"
                            + " \"" + outputPath + "\"";
            Debug.Log("ffmpeg arguments: " + arguments);
            proc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = ExecutablePath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            pipe = (FileStream)proc.StandardInput.BaseStream;
            this.width = width;
            this.height = height;
        }

        [DllImport("kernel32.dll")]
        private static extern bool WriteFile(IntPtr hFile, IntPtr lpBuffer, int NumberOfBytesToWrite, out int lpNumberOfBytesWritten, IntPtr lpOverlapped);

        public void Write(NativeArray<byte> bytes)
        {
            if (pipe == null)
                return;

            int scanline = width * 4;
            if (bytes.Length != scanline * height)
            {
                Debug.LogError("got an array of " + bytes.Length + " bytes, expected " + scanline + "*" + height);
                got_error = true;
                Close();
                return;
            }

            unsafe
            {
                IntPtr hFile = pipe.SafeFileHandle.DangerousGetHandle();
                IntPtr ptr = (IntPtr)bytes.GetUnsafeReadOnlyPtr();
                int bytes_written;
                int bytes_remaining = scanline * height;
                while (bytes_remaining > 0)
                {
                    if (!WriteFile(hFile, ptr, bytes_remaining, out bytes_written, (IntPtr)0))
                    {
                        Debug.LogError("Video encoder was shut down (WriteFile error to ffmpeg)");
                        got_error = true;
                        Close();
                        return;
                    }
                    bytes_remaining -= bytes_written;
                    ptr += bytes_written;
                }
            }
        }

        public void Close()
        {
            if (pipe != null)
            {
                pipe.Close();
                pipe = null;
            }
            if (proc != null)
            {
                proc.Close();
                proc = null;
            }
        }

        static string ExecutablePath
        {
            get
            {
                var basePath = UnityEngine.Application.streamingAssetsPath;
                var platform = UnityEngine.Application.platform;

                if (platform == UnityEngine.RuntimePlatform.OSXPlayer ||
                    platform == UnityEngine.RuntimePlatform.OSXEditor)
                    return basePath + "/FFmpegOut/macOS/ffmpeg";

                if (platform == UnityEngine.RuntimePlatform.LinuxPlayer ||
                    platform == UnityEngine.RuntimePlatform.LinuxEditor)
                    return basePath + "/FFmpegOut/Linux/ffmpeg";

                return basePath + "/FFmpegOut/Windows/ffmpeg.exe";
            }
        }
    }
}
