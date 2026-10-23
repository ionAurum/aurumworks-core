using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Aurumworks
{
    /// <summary>
    /// A self-test: a built player that plays itself when started with a flag of the game's, and
    /// tells whatever started it how that went by its exit code. This is the part every game's has
    /// in common. From Begin on, every error or exception logged counts against the run; a run in
    /// which nothing happens for too long ends as a failure; screenshots go to a folder; and Finish
    /// quits with 1 if anything was counted, else 0. What the test does is the game's: it runs its
    /// steps (Run), says when it gets somewhere (Progress) and checks what it expects (Check, or
    /// any Debug.LogError). Nothing here runs unless a game calls Begin.
    /// </summary>
    public class SelfTest : MonoBehaviour
    {
        private static SelfTest _instance;
        private static string _screenshotFolder;
        private static float _lastProgress;

        /// <summary>True from Begin on.</summary>
        public static bool IsRunning => _instance != null;

        /// <summary>True once Finish has been called: the player is quitting, so the test takes no more steps.</summary>
        public static bool IsFinished { get; private set; }

        /// <summary>How many errors and exceptions have been logged since Begin.</summary>
        public static int Errors { get; private set; }

        /// <summary>How long the run may go without Progress before it fails as stalled.</summary>
        public static float StallSeconds { get; set; } = 120f;

        /// <summary>Where screenshots go; null (the default) for none. Made if it isn't there.</summary>
        public static string ScreenshotFolder
        {
            get => _screenshotFolder;
            set
            {
                _screenshotFolder = value;
                if (value != null)
                {
                    Directory.CreateDirectory(value);
                }
            }
        }

        /// <summary>True when Shoot and Capture take screenshots: there is a folder, and a window to draw in.</summary>
        public static bool CanCapture => _screenshotFolder != null && !Application.isBatchMode;

        /// <summary>Raised by Finish before it quits, also when the run stalled: the game logs what its run did.</summary>
        public static event Action Finishing;

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            _screenshotFolder = null;
            IsFinished = false;
            Errors = 0;
            StallSeconds = 120f;
            Finishing = null;
        }

        //----------

        /// <summary>
        /// Starts the run: errors are counted and the stall clock runs from here, so call it before
        /// anything the test should answer for. Its object outlives scene loads.
        /// </summary>
        public static void Begin()
        {
            if (_instance != null)
            {
                return;
            }

            var host = new GameObject("Self-Test");
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<SelfTest>();
            _lastProgress = Time.realtimeSinceStartup;
            Application.logMessageReceived += OnLogged;
        }

        /// <summary>The run got somewhere (a new screen, a step done): the stall clock starts again.</summary>
        public static void Progress() => _lastProgress = Time.realtimeSinceStartup;

        /// <summary>Runs a test's steps on the self-test's own object, so they carry on across scene loads.</summary>
        public static Coroutine Run(IEnumerator steps) => _instance.StartCoroutine(steps);

        /// <summary>Logs what was expected, as an error if it didn't hold, and returns whether it held.</summary>
        public static bool Check(bool held, string expected)
        {
            if (held)
            {
                Debug.Log("Self-test ok: " + expected);
            }
            else
            {
                Debug.LogError("Self-test failed: " + expected);
            }

            return held;
        }

        /// <summary>
        /// Saves the frame being drawn as name.png, if screenshots are on. Call it at the end of a
        /// frame (after WaitForEndOfFrame); the file is written a frame later.
        /// </summary>
        public static void Shoot(string name)
        {
            if (CanCapture)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(_screenshotFolder, name + ".png"));
            }
        }

        /// <summary>A step that saves the screen as name.png, if screenshots are on, and waits until it is written.</summary>
        public static IEnumerator Capture(string name)
        {
            if (!CanCapture)
            {
                yield break;
            }

            yield return new WaitForEndOfFrame();
            Shoot(name);
            yield return null;
            yield return null;
        }

        /// <summary>Ends the run: quits with exit code 1 if any error was logged, else 0.</summary>
        public static void Finish()
        {
            if (IsFinished)
            {
                return;
            }

            IsFinished = true;
            Finishing?.Invoke();
            Debug.Log($"Self-test finished with {Errors} error(s).");
            Application.Quit(Errors > 0 ? 1 : 0);
        }

        //----------

        private static void OnLogged(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
            {
                Errors++;
            }
        }

        private void Update()
        {
            if (!IsFinished && Time.realtimeSinceStartup - _lastProgress > StallSeconds)
            {
                Debug.LogError($"Self-test stalled: nothing new for {StallSeconds} seconds.");
                Finish();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                Application.logMessageReceived -= OnLogged;
                _instance = null;
            }
        }
    }
}
