using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using ECN.MediaPlayer;
using memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Timer = System.Timers.Timer;

namespace CMI
{
    public partial class CMI : Form
    {
        private const string eventFlagManQuery = "48 8B 3D ?? ?? ?? ?? 48 85 FF ?? ?? 32 C0 E9";
        private const string gameDataManQuery = "48 8B 05 ?? ?? ?? ?? 48 85 C0 74 05 48 8B 40 58 C3 C3";
        private const string worldChrManQuery = "48 8B 05 ?? ?? ?? ?? 48 85 C0 74 0F 48 39 88";
        // TODO: WIP
        private const string menuManQuery = "";
        public static string appRootPath = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}";
        public static string modSoundFolderPath;
        public static string soundJsonName;
        private static JObject soundJson;
        private static Process mainEldenRingProcess;
        private static IntPtr eldenRingProcessHandle;
        private static IntPtr eventFlagMan;
        private static IntPtr worldChrMan;
        private static long gameDataMan;
        private static long menuMan;
        private static Scanner eventFlagManScanner;
        private static Scanner gameDataManScanner;
        private static Scanner worldChrManScanner;
        private static int masterVolume;
        private static bool shouldResetUIPlayerPosition;
        private static readonly List<SoundEvent> soundEvents = new List<SoundEvent>();
        private static readonly MediaPlayer musicMediaPlayer = new MediaPlayer(0, true, 0);
        private static readonly MediaPlayer soundEffectsMediaPlayer = new MediaPlayer(0, true, 0);
        private static readonly MediaPlayer voiceMediaPlayer = new MediaPlayer(0, true, 0);

        public CMI()
        {
            InitializeComponent();
            CenterToScreen();
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        private static byte[] ReadValue(IntPtr address, int length)
        {
            byte[] result = new byte[length];
            ReadProcessMemory(eldenRingProcessHandle, address, result, length, out IntPtr _);
            return result;
        }

        private static int ReadInt(IntPtr address)
        {
            return BitConverter.ToInt32(ReadValue(address, 4), 0);
        }

        private static long ReadLong(IntPtr address)
        {
            return BitConverter.ToInt64(ReadValue(address, 8), 0);
        }

        private static byte ReadByte(IntPtr address)
        {
            return ReadValue(address, 1)[0];
        }

        private static int ReadVolume(long baseAddress, int offset)
        {
            return ReadByte((IntPtr)ReadLong((IntPtr)baseAddress + 0x58) + offset) * 10;
        }

        private static bool IsHPInvalid()
        {
            IntPtr addr = (IntPtr)(ReadLong(worldChrMan) + 0x10EF8);
            addr = (IntPtr)ReadLong(addr);
            addr = (IntPtr)(ReadLong(addr) + 0x190);
            addr = (IntPtr)ReadLong(addr);
            addr = (IntPtr)(ReadLong(addr) + 0x138);
            // TODO: Double check
            return addr.ToInt64() == 312;
        }

        /*
        private static bool ReadIsLoadingScreenActive(long baseAddress, int offset)
        {
            return ReadByte((IntPtr)ReadLong((IntPtr)baseAddress + 0x3D6B7D0) + offset) != 0;
        }
        */

        /*
        private static bool ReadIsLoadingScreenActive(IntPtr baseAddress, int offset)
        {
            return ReadByte((IntPtr)ReadLong(baseAddress + 0x3D6B7D0) + offset) != 0;
        }
        */

        private void SendStatusLogMessage(string message)
        {
            int messageIndex = statusLogTextBox.Text.LastIndexOf(message, StringComparison.Ordinal);
            if (messageIndex == statusLogTextBox.TextLength - message.Length - 2) return;
            statusLogTextBox.AppendText($"{message}\r\n");
            statusLogTextBox.SelectionStart = statusLogTextBox.TextLength;
            statusLogTextBox.ScrollToCaret();
        }

        private static Process[] GetERProcesses()
        {
            return Process.GetProcessesByName("eldenring");
        }

        private static Scanner ConfigureMemoryScanner(string searchQuery)
        {
            Scanner memoryScanner = new Scanner(mainEldenRingProcess, eldenRingProcessHandle, searchQuery);
            memoryScanner.setModule(mainEldenRingProcess.MainModule);
            return memoryScanner;
        }

        private static void ConfigureMemoryScanners()
        {
            eventFlagManScanner = ConfigureMemoryScanner(eventFlagManQuery);
            gameDataManScanner = ConfigureMemoryScanner(gameDataManQuery);
            worldChrManScanner = ConfigureMemoryScanner(worldChrManQuery);
        }

        private static IntPtr GetQueryResultAsPointer(Memory scanner)
        {
            IntPtr pointer = (IntPtr)scanner.FindPattern();
            pointer += ReadInt(pointer + 3) + 7;
            return pointer;
        }

        private static void SetEventFlagMan()
        {
            eventFlagMan = GetQueryResultAsPointer(eventFlagManScanner);
        }

        private static void SetGameDataMan()
        {
            gameDataMan = ReadLong(GetQueryResultAsPointer(gameDataManScanner));
        }

        private static void SetWorldChrMan()
        {
            worldChrMan = GetQueryResultAsPointer(worldChrManScanner);
        }

        private static void PostAttachToGameSetup()
        {
            ConfigureMemoryScanners();
            SetEventFlagMan();
            SetWorldChrMan();
        }

        private async Task AttachToGame()
        {
            Process[] eldenRingProcesses;
            while (true)
            {
                eldenRingProcesses = GetERProcesses();
                if (eldenRingProcesses.Length == 0)
                {
                    SendStatusLogMessage("ELDEN RING is not currently running, waiting...");
                    await Task.Delay(2000);
                }
                else break;
            }
            SendStatusLogMessage("Attaching to ELDEN RING...");
            mainEldenRingProcess = eldenRingProcesses[0];
            eldenRingProcessHandle = OpenProcess(0x001F0FFF, false, mainEldenRingProcess.Id);
            SendStatusLogMessage($"ELDEN RING process ID: {eldenRingProcessHandle}");
            PostAttachToGameSetup();
            BeginGameStateTimer();
        }

        private bool ReadSoundJSON()
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                Stream stream = assembly.GetManifestResourceStream(soundJsonName);
                StreamReader reader = new StreamReader(stream ?? throw new Exception());
                string jsonContent = reader.ReadToEnd();
                SendStatusLogMessage($"Reading sound configuration: \"{soundJsonName}\"");
                soundJson = JsonConvert.DeserializeObject<JObject>(jsonContent);
                return true;
            }
            catch
            {
                SendStatusLogMessage("Failed to read sound configuration, cannot attach to game");
                return false;
            }
        }

        private void LoadSoundEvents()
        {
            foreach (JProperty soundEvent in soundJson.Properties())
                soundEvents.Add(SoundEvent.Serialize(soundEvent.Name, (JObject)soundEvent.Value));
            foreach (SoundEvent soundEvent in soundEvents)
            {
                foreach (PropertyInfo prop in typeof(SoundEvent).GetProperties().Skip(6))
                {
                    TreeNode propertyNode = new TreeNode { Text = prop.Name };
                    propertyNode.Nodes.Add(prop.GetValue(soundEvent).ToString());
                    soundEvent.EventNode.Nodes.Add(propertyNode);
                }
                soundEventsListBox.Nodes.Add(soundEvent.EventNode);
            }
        }

        private int GetCurrentVolume(int valueOffset, SoundEvent soundEvent = null, int savedVolume = -1)
        {
            int currentVolume = ReadVolume(gameDataMan, valueOffset);
            if (soundEvent != null)
            {
                currentVolume = (int)((double)currentVolume / 100 * masterVolume / 100 * 100);
                soundEvent.MediaPlayer.Volume = currentVolume;
            }
            if (savedVolume == -1 && soundEvent != null) savedVolume = soundEvent.Volume;
            if (currentVolume == savedVolume) return currentVolume;
            string volumeHost = soundEvent == null ? "Master" : soundEvent.Name;
            SendStatusLogMessage($"{volumeHost} volume changed to {currentVolume}%");
            return currentVolume;
        }

        private void UpdateUISoundPlayerState(SoundEvent soundEvent)
        {
            string originalUiSoundPlayerURL = uiSoundPlayer.URL;
            if (!soundEvent.Activated || soundEvent.MediaPlayer.CurrentSong != soundEvent.SoundPath)
            {
                uiSoundPlayer.Ctlcontrols.stop();
                uiSoundPlayer.URL = originalUiSoundPlayerURL;
                return;
            }
            uiSoundPlayer.settings.autoStart = false;
            uiSoundPlayer.URL = soundEvent.MediaPlayer.CurrentSong;
            uiSoundPlayer.Ctlcontrols.currentPosition = shouldResetUIPlayerPosition ? 0 : soundEvent.MediaPlayer.Position;
            uiSoundPlayer.settings.volume = 0;
            uiSoundPlayer.Ctlenabled = false;
            uiSoundPlayer.Ctlcontrols.play();
        }

        private void SelectEventNode(TreeNode eventNode, bool resetUIPlayerPos)
        {
            if (resetUIPlayerPos) shouldResetUIPlayerPosition = true;
            soundEventsListBox.SelectedNode = null;
            soundEventsListBox.SelectedNode = eventNode;
            shouldResetUIPlayerPosition = false;
        }

        private void SelectCurrentlyActivatedEventNode()
        {
            TreeNode currActivatedEventNode = soundEvents.LastOrDefault(i => i.Activated)?.EventNode;
            SelectEventNode(currActivatedEventNode ?? soundEvents[0].EventNode, true);
        }

        private void UpdateGameState()
        {
            masterVolume = GetCurrentVolume(0x20, null, masterVolume);
            foreach (SoundEvent soundEvent in soundEvents)
            {
                soundEvent.Activated = soundEvent.IsActivated();
                soundEvent.SetEventNodeIcon(soundEvent.Activated ? 2 : 1);
            }
            foreach (SoundEvent soundEvent in soundEvents)
            {
                soundEvent.Volume = GetCurrentVolume(soundEvent.Type, soundEvent);
                if (soundEvent.ShouldStopEvent(soundEventsListBox))
                {
                    soundEvent.StopEvent();
                    SelectCurrentlyActivatedEventNode();
                }
                if (!soundEvent.ShouldPlayEvent()) continue;
                soundEvent.PlayEvent();
                SelectEventNode(soundEvent.EventNode, true);
            }
        }

        private void BeginGameStateTimer()
        {
            gameStateTimer.Tick += (s, e) =>
            {
                if (soundEventFadeTimer.Enabled) return;
                SetGameDataMan();
                UpdateGameState();
                if (GetERProcesses().Length != 0) return;
                Environment.Exit(0);
            };
            gameStateTimer.Start();
        }

        private void CMI_Click(object sender, EventArgs e)
        {
            statusLogGroupBox.Focus();
        }

        private async void CMI_Shown(object sender, EventArgs e)
        {
            // Hide();
            if (!ReadSoundJSON()) return;
            LoadSoundEvents();
            await AttachToGame();
        }

        private void SoundEventsListBox_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Parent == null) UpdateUISoundPlayerState(soundEvents[e.Node.Index]);
        }

        /*
        private static bool IsLoadingScreenActive()
        {
            ProcessModule mainModule = mainEldenRingProcess.MainModule;
            return mainModule != null && ReadIsLoadingScreenActive(mainModule.BaseAddress, 0x728);
        }
        */

        public class SoundEvent
        {
            public readonly Timer LoopTimer = new Timer();
            public bool Activated { get; set; }
            public MediaPlayer MediaPlayer { get; set; }
            public string Name { get; set; }
            public TreeNode EventNode { get; set; }
            public int Volume { get; set; }
            public string SoundPath { get; set; }
            public int Pointer1 { get; set; }
            public int Pointer2 { get; set; }
            public int Startbit { get; set; }
            public int Type { get; set; }
            public int FadeInterval { get; set; }
            public bool Loop { get; set; }
            public int StartSeconds { get; set; }
            public int LoopStartSeconds { get; set; }

            public static SoundEvent Serialize(string name, JObject soundEventJson)
            {
                SoundEvent soundEvent = new SoundEvent
                {
                    Name = name,
                    EventNode = new TreeNode { ImageIndex = 1, SelectedImageIndex = 1, Name = name, Text = name },
                    SoundPath = $"{modSoundFolderPath}\\{soundEventJson.GetValue("SoundPath")}",
                    Pointer1 = Convert.ToInt32(soundEventJson.GetValue("Pointer1").ToString(), 16),
                    Pointer2 = Convert.ToInt32(soundEventJson.GetValue("Pointer2").ToString(), 16),
                    Startbit = Convert.ToInt32(soundEventJson.GetValue("Startbit").ToString(), 16),
                    Type = Convert.ToInt32(soundEventJson.GetValue("Type").ToString()),
                    FadeInterval = Convert.ToInt32(soundEventJson.GetValue("FadeInterval").ToString()),
                    Loop = bool.Parse(soundEventJson.GetValue("Loop").ToString()),
                    StartSeconds = Convert.ToInt32(soundEventJson.GetValue("StartSeconds").ToString()),
                    LoopStartSeconds = Convert.ToInt32(soundEventJson.GetValue("LoopStartSeconds").ToString())
                };
                soundEvent.MediaPlayer = soundEvent.GetMediaPlayer();
                return soundEvent;
            }

            public bool IsActivated()
            {
                byte eventByte = ReadByte((IntPtr)ReadLong((IntPtr)ReadLong(eventFlagMan) + Pointer1) + Pointer2);
                char[] paddedEventBytes = Convert.ToString(eventByte, 2).PadLeft(8, '0').ToCharArray().Reverse().ToArray();
                return int.Parse(new string(paddedEventBytes).Substring(Startbit, 1)) == 1;
            }

            public void SetEventNodeIcon(int iconIndex)
            {
                if (EventNode.ImageIndex == iconIndex) return;
                EventNode.ImageIndex = iconIndex;
                EventNode.SelectedImageIndex = iconIndex;
            }

            public bool DoesOtherEventOverride()
            {
                SoundEvent overrideSoundEvent = soundEvents.LastOrDefault(i => i.Name != Name && i.Activated && i.Type == Type);
                return soundEvents.IndexOf(overrideSoundEvent) >= soundEvents.IndexOf(this);
            }

            public bool ShouldStopEvent(TreeView soundEventsListBox)
            {
                return IsHPInvalid() || !Activated && soundEventsListBox.SelectedNode == EventNode && MediaPlayer.CurrentSong == SoundPath;
            }

            public bool ShouldPlayEvent()
            {
                return !IsHPInvalid() && Activated && !DoesOtherEventOverride() && MediaPlayer.CurrentSong != SoundPath;
            }

            public void StopEvent()
            {
                MediaPlayer.CurrentSong = null;
                MediaPlayer.Stop(false);
            }

            public void PlayEvent()
            {
                if (Loop)
                {
                    // TODO: We might need to adjust this to work for all end user systems...
                    LoopTimer.Interval = 1;
                    LoopTimer.Elapsed -= OnTimedEvent;
                    LoopTimer.Elapsed += OnTimedEvent;
                    LoopTimer.Start();
                }
                MediaPlayer.FadeTime = FadeInterval;
                MediaPlayer.CrossfadeTime = FadeInterval;
                MediaPlayer.Play(SoundPath, FadeInterval > 0);
                MediaPlayer.CurrentSong = SoundPath;
                // TODO: We also need to correctly set the UI player position...
                MediaPlayer.Position = StartSeconds;
            }

            private void OnTimedEvent(object source, ElapsedEventArgs e)
            {
                if (MediaPlayer.CurrentSong != SoundPath || MediaPlayer.Position <= 0) return;
                // TODO: Allow for setting StartSeconds and LoopStartSeconds in milliseconds...
                if (Math.Abs(MediaPlayer.Position - MediaPlayer.Duration) < 0.25)
                    MediaPlayer.Position = LoopStartSeconds;
            }

            private MediaPlayer GetMediaPlayer()
            {
                MediaPlayer mediaPlayer;
                switch (Type)
                {
                    case 4:
                        mediaPlayer = musicMediaPlayer;
                        break;
                    case 5:
                        mediaPlayer = soundEffectsMediaPlayer;
                        break;
                    case 6:
                        mediaPlayer = voiceMediaPlayer;
                        break;
                    default:
                        mediaPlayer = null;
                        break;
                }
                return mediaPlayer;
            }
        }

        public static void Main()
        {
            try
            {
                modSoundFolderPath = $"{appRootPath}\\sound";
                soundJsonName = "CMI.sound.json";
            }
            catch
            {
                Environment.Exit(0);
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new CMI());
        }
    }
}