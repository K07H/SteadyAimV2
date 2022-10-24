using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace SteadyAimV2
{
    /// <summary>
    /// Steady Aim V2 is a mod for Green Hell that allows the player to tweak aim settings precisely.
    /// It is an enhanced version of the old SteadyAim mod from Werkrat.
    /// Usage: Simply press the shortcut to open settings window (by default it is NumPad5).
    /// Author: OSubMarin
    /// </summary>
    public class SteadyAimV2 : MonoBehaviour
    {
        #region Enums

        public enum MessageType
        {
            Info,
            Warning,
            Error
        }

        #endregion

        #region Constructors/Destructor

        public SteadyAimV2()
        {
            Instance = this;
        }

        private static SteadyAimV2 Instance;

        public static SteadyAimV2 Get() => SteadyAimV2.Instance;

        #endregion

        #region Attributes

        /// <summary>The name of this mod.</summary>
        public static readonly string ModName = nameof(SteadyAimV2);

        /// <summary>Path to ModAPI runtime configuration file (contains game shortcuts).</summary>
        private static readonly string RuntimeConfigurationFile = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), "RuntimeConfiguration.xml");

        /// <summary>Path to SteadyAimV2 mod configuration file (if it does not already exist it will be automatically created on first run).</summary>
        private static readonly string SteadyAimV2ConfigurationFile = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), "SteadyAimV2.txt");

        /// <summary>Default shortcut to show mod settings.</summary>
        private static readonly KeyCode DefaultModKeybindingId = KeyCode.Keypad5;

        /// <summary>The ModAPI ID of the shortcut to show mod settings.</summary>
        private static readonly string ModKeybindingName = "SteadyAimV2Settings";

        private static KeyCode ModKeybindingId { get; set; } = DefaultModKeybindingId;

        // Game handles attributes.

        private static HUDManager LocalHUDManager = null;
        private static Player LocalPlayer = null;

        // UI attributes.

        private static readonly float ModScreenTotalWidth = 800f;
        private static readonly float ModScreenTotalHeight = 80f;
        private static readonly float ModScreenMinWidth = 800f;
        private static readonly float ModScreenMaxWidth = 850f;
        private static readonly float ModScreenMinHeight = 50f;
        private static readonly float ModScreenMaxHeight = 300f;
        private static float ModScreenStartPositionX { get; set; } = Screen.width / 7f;
        private static float ModScreenStartPositionY { get; set; } = Screen.height / 7f;
        public static Rect SteadyAimV2Screen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);

        private Color DefaultGuiColor = GUI.color;
        private bool IsMinimized = false;
        private bool ShowUI = false;

        // Options attributes.

        public static bool SteadyAimV2Enabled { get; set; } = false;
        private bool SteadyAimV2EnabledOrig = false;

        public static float Power { get; set; } = 10.0f;
        private float PowerOrig = 10.0f;

        public static float Speed { get; set; } = 1.0f;
        private float SpeedOrig = 1.0f;

        public static float Duration { get; set; } = 1.0f;
        private float DurationOrig = 1.0f;


        // Permission attributes.

        public static readonly string PermissionRequestBegin = "Can I use \"";
        public static readonly string PermissionRequestEnd = "\" mod? (Host can reply \"Allowed\" to give permission)";
        public static readonly string PermissionRequestFinal = PermissionRequestBegin + "Steady Aim v2" + PermissionRequestEnd;

        public static bool DoRequestPermission = false;
        public static bool PermissionGranted = false;
        public static bool PermissionDenied = false;
        public static int NbPermissionRequests = 0;

        public static bool WaitingPermission = false;
        public static long PermissionAskTime = -1L;
        public static long WaitAMinBeforeFirstRequest = -1L;

        public static bool OtherWaitingPermission = false;
        public static long OtherPermissionAskTime = -1L;

        #endregion

        #region Static functions

        private static void ShowHUDBigInfo(string text, float duration)
        {
            string header = "Steady Aim v2 Info";
            string textureName = HUDInfoLogTextureType.Reputation.ToString();
            HUDBigInfo obj = (HUDBigInfo)LocalHUDManager.GetHUD(typeof(HUDBigInfo));
            HUDBigInfoData.s_Duration = duration;
            HUDBigInfoData data = new HUDBigInfoData
            {
                m_Header = header,
                m_Text = text,
                m_TextureName = textureName,
                m_ShowTime = Time.time
            };
            obj.AddInfo(data);
            obj.Show(show: true);
        }

        public static void ShowHUDInfo(string msg, float duration = 6f, Color? color = null) => ShowHUDBigInfo($"<color=#{ColorUtility.ToHtmlStringRGBA(color != null && color.HasValue ? color.Value : Color.green)}>Info</color>\n{msg}", duration);
        public static void ShowHUDError(string msg, float duration = 6f, Color? color = null) => ShowHUDBigInfo($"<color=#{ColorUtility.ToHtmlStringRGBA(color != null && color.HasValue ? color.Value : Color.red)}>Error</color>\n{msg}", duration);

        public static string ReadNetMessage(P2PNetworkReader reader)
        {
            uint readerPrePos = reader.Position;
            if (readerPrePos >= int.MaxValue) // Failsafe for uint cast to int.
                return null;
            string message = reader.ReadString();
            reader.Seek(-1 * ((int)reader.Position - (int)readerPrePos));
            return message;
        }

        public static void TextChatEvnt(P2PNetworkMessage net_msg)
        {
            try
            {
                if (!SteadyAimV2.PermissionDenied && !SteadyAimV2.PermissionGranted && net_msg.m_MsgType == 10 && net_msg.m_ChannelId == 1)
                {
                    bool peerIsMaster = net_msg.m_Connection.m_Peer.IsMaster();
                    if (!peerIsMaster)
                    {
                        string message = ReadNetMessage(net_msg.m_Reader);
                        if (!string.IsNullOrWhiteSpace(message) && message.StartsWith(SteadyAimV2.PermissionRequestBegin, StringComparison.InvariantCulture) && message.IndexOf(SteadyAimV2.PermissionRequestEnd, StringComparison.InvariantCulture) > 0)
                        {
                            SteadyAimV2.OtherPermissionAskTime = DateTime.Now.Ticks / 10000000L;
                            SteadyAimV2.OtherWaitingPermission = true;
                        }
                    }
                    if (!SteadyAimV2.OtherWaitingPermission && SteadyAimV2.WaitingPermission && peerIsMaster)
                    {
                        string message = ReadNetMessage(net_msg.m_Reader);
                        if (!string.IsNullOrWhiteSpace(message) && string.Compare(message, "Allowed", StringComparison.InvariantCulture) == 0)
                        {
                            SteadyAimV2.WaitingPermission = false;
                            SteadyAimV2.PermissionAskTime = -1L;
                            SteadyAimV2.PermissionGranted = true;
                            ShowHUDInfo("Host gave you permission to use \"Steady Aim v2\" mod.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModAPI.Log.Write($"[{ModName}:TextChatRecv] Exception caught while receiving text chat: [{ex.ToString()}]");
            }
        }

        public static void RestorePermissionStateToOrig()
        {
            SteadyAimV2.OtherWaitingPermission = false;
            SteadyAimV2.OtherPermissionAskTime = -1L;
            SteadyAimV2.WaitingPermission = false;
            SteadyAimV2.PermissionAskTime = -1L;
            SteadyAimV2.WaitAMinBeforeFirstRequest = -1L;
            SteadyAimV2.PermissionDenied = false;
            SteadyAimV2.PermissionGranted = false;
            SteadyAimV2.DoRequestPermission = false;
            SteadyAimV2.NbPermissionRequests = 0;
        }

        #endregion

        #region Methods

        private KeyCode GetConfigurableKey(string buttonId, KeyCode defaultValue)
        {
            if (File.Exists(RuntimeConfigurationFile))
            {
                string[] lines = null;
                try
                {
                    lines = File.ReadAllLines(RuntimeConfigurationFile);
                }
                catch (Exception ex)
                {
                    ModAPI.Log.Write($"[{ModName}:GetConfigurableKey] Exception caught while reading shortcuts configuration: [{ex.ToString()}].");
                }
                if (lines != null && lines.Length > 0)
                {
                    string sttDelim = $"<Button ID=\"{buttonId}\">";
                    string endDelim = "</Button>";
                    foreach (string line in lines)
                    {
                        if (line.Contains(sttDelim) && line.Contains(endDelim))
                        {
                            int stt = line.IndexOf(sttDelim);
                            if ((stt >= 0) && (line.Length > (stt + sttDelim.Length)))
                            {
                                string split = line.Substring(stt + sttDelim.Length);
                                if (split != null && split.Contains(endDelim))
                                {
                                    int end = split.IndexOf(endDelim);
                                    if ((end > 0) && (split.Length > end))
                                    {
                                        string parsed = split.Substring(0, end);
                                        if (!string.IsNullOrEmpty(parsed))
                                        {
                                            parsed = parsed.Replace("NumPad", "Keypad").Replace("Oem", "");
                                            if (!string.IsNullOrEmpty(parsed) && Enum.TryParse<KeyCode>(parsed, true, out KeyCode parsedKey))
                                            {
                                                ModAPI.Log.Write($"[{ModName}:GetConfigurableKey] Shortcut for \"{buttonId}\" has been parsed ({parsed}).");
                                                return parsedKey;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            ModAPI.Log.Write($"[{ModName}:GetConfigurableKey] Could not parse shortcut for \"{buttonId}\". Using default value ({defaultValue.ToString()}).");
            return defaultValue;
        }

        private void SaveSettings()
        {
            try
            {
                string powerStr = Convert.ToString(Power, CultureInfo.InvariantCulture);
                string speedStr = Convert.ToString(Speed, CultureInfo.InvariantCulture);
                string durationStr = Convert.ToString(Duration, CultureInfo.InvariantCulture);
                File.WriteAllText(SteadyAimV2ConfigurationFile, $"IsEnabled={(SteadyAimV2Enabled ? "true" : "false")}\r\nPower={powerStr}\r\nSpeed={speedStr}\r\nDuration={durationStr}\r\n", Encoding.UTF8);
                ModAPI.Log.Write($"[{ModName}:SaveSettings] Configuration saved (Is enabled: {(SteadyAimV2Enabled ? "true" : "false")}. Power: {powerStr}. Speed: {speedStr}. Duration: {durationStr}.");
            }
            catch (Exception ex)
            {
                ModAPI.Log.Write($"[{ModName}:SaveSettings] Exception caught while saving configuration: [{ex.ToString()}].");
            }
        }

        private void LoadSettings()
        {
            if (!File.Exists(SteadyAimV2ConfigurationFile))
            {
                ModAPI.Log.Write($"[{ModName}:LoadSettings] Configuration file was not found, creating it.");
                SaveSettings();
            }
            else
            {
                ModAPI.Log.Write($"[{ModName}:LoadSettings] Parsing configuration file...");
                string[] lines = null;
                try
                {
                    lines = File.ReadAllLines(SteadyAimV2ConfigurationFile, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    ModAPI.Log.Write($"[{ModName}:LoadSettings] Exception caught while reading configuration file: [{ex.ToString()}].");
                }

                if (lines != null && lines.Length > 0)
                {
                    bool isEnabledFound = false;
                    bool powerFound = false;
                    bool speedFound = false;
                    bool durationFound = false;

                    foreach (string line in lines)
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            if (line.StartsWith("IsEnabled="))
                            {
                                isEnabledFound = true;
                                if (line.Contains("true", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    SteadyAimV2Enabled = true;
                                    SteadyAimV2EnabledOrig = true;
                                }
                                else
                                {
                                    SteadyAimV2Enabled = false;
                                    SteadyAimV2EnabledOrig = false;
                                }
                            }
                            else if (line.StartsWith("Power=") && line.Length > "Power=".Length)
                            {
                                powerFound = true;
                                string split = line.Substring("Power=".Length).Trim();
                                if (!string.IsNullOrWhiteSpace(split) && float.TryParse(split, NumberStyles.Float, CultureInfo.InvariantCulture, out float power) && power >= 0.0f && power <= 100.0f)
                                {
                                    Power = power;
                                    PowerOrig = power;
                                }
                                else
                                    ModAPI.Log.Write($"[{ModName}:LoadSettings] Warning: Power value was not correct (it must be between 0 and 100).");
                            }
                            else if (line.StartsWith("Speed=") && line.Length > "Speed=".Length)
                            {
                                speedFound = true;
                                string split = line.Substring("Speed=".Length).Trim();
                                if (!string.IsNullOrWhiteSpace(split) && float.TryParse(split, NumberStyles.Float, CultureInfo.InvariantCulture, out float speed) && speed >= 0.0f && speed <= 10.0f)
                                {
                                    Speed = speed;
                                    SpeedOrig = speed;
                                }
                                else
                                    ModAPI.Log.Write($"[{ModName}:LoadSettings] Warning: Speed value was not correct (it must be between 0 and 10).");
                            }
                            else if (line.StartsWith("Duration=") && line.Length > "Duration=".Length)
                            {
                                durationFound = true;
                                string split = line.Substring("Duration=".Length).Trim();
                                if (!string.IsNullOrWhiteSpace(split) && float.TryParse(split, NumberStyles.Float, CultureInfo.InvariantCulture, out float duration) && duration >= 0.0f && duration <= 10.0f)
                                {
                                    Duration = duration;
                                    DurationOrig = duration;
                                }
                                else
                                    ModAPI.Log.Write($"[{ModName}:LoadSettings] Warning: Duration value was not correct (it must be between 0 and 10).");
                            }
                        }

                    if (isEnabledFound && powerFound && speedFound && durationFound)
                        ModAPI.Log.Write($"[{ModName}:LoadSettings] Successfully parsed configuration file.");
                    else
                        ModAPI.Log.Write($"[{ModName}:LoadSettings] Warning: Configuration file was parsed but some values were missing (Found IsEnabled: {(isEnabledFound ? "true" : "false")}. Found Power: {(powerFound ? "true" : "false")}. Found Speed: {(speedFound ? "true" : "false")}). Found Duration: {(durationFound ? "true" : "false")}).");
                }
                else
                    ModAPI.Log.Write($"[{ModName}:LoadSettings] Warning: Configuration file was empty. Using default values.");
                ModAPI.Log.Write($"[{ModName}:LoadSettings] Is enabled: {(SteadyAimV2Enabled ? "true" : "false")}. Power: {Power.ToString(CultureInfo.InvariantCulture)}. Speed: {Speed.ToString(CultureInfo.InvariantCulture)}. Duration: {Duration.ToString(CultureInfo.InvariantCulture)}.");
            }
        }

        #endregion

        #region UI methods

        private void InitWindow()
        {
            int wid = GetHashCode();
            SteadyAimV2Screen = GUILayout.Window(wid,
                SteadyAimV2Screen,
                InitSteadyAimV2Screen,
                "Steady Aim v2.0.0.2, by OSubMarin",
                GUI.skin.window,
                GUILayout.ExpandWidth(true),
                GUILayout.MinWidth(ModScreenMinWidth),
                GUILayout.MaxWidth(ModScreenMaxWidth),
                GUILayout.ExpandHeight(true),
                GUILayout.MinHeight(ModScreenMinHeight),
                GUILayout.MaxHeight(ModScreenMaxHeight));
        }

        private void GetGameHandles()
        {
            LocalHUDManager = HUDManager.Get();
            LocalPlayer = Player.Get();
        }

        private void InitSteadyAimV2Screen(int windowID)
        {
            ModScreenStartPositionX = SteadyAimV2Screen.x;
            ModScreenStartPositionY = SteadyAimV2Screen.y;

            using (var modContentScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                ScreenMenuBox();
                if (!IsMinimized)
                    ModOptionsBox();
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void ScreenMenuBox()
        {
            if (GUI.Button(new Rect(SteadyAimV2Screen.width - 40f, 0f, 20f, 20f), "-", GUI.skin.button))
                CollapseWindow();

            if (GUI.Button(new Rect(SteadyAimV2Screen.width - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
                CloseWindow();
        }

        private void OnClickRestoreDefaultsButton()
        {
            Power = 10.0f;
            PowerOrig = 10.0f;
            Speed = 1.0f;
            SpeedOrig = 1.0f;
            Duration = 1.0f;
            DurationOrig = 1.0f;
            SaveSettings();
        }

        private void ModOptionsBox()
        {
            bool isSingleplayerOrMaster = (P2PSession.Instance.GetGameVisibility() == P2PGameVisibility.Singleplayer || ReplTools.AmIMaster());
            bool hasPermission = (SteadyAimV2.PermissionGranted && !SteadyAimV2.PermissionDenied);
            if (isSingleplayerOrMaster || hasPermission)
            {
                using (var optionsScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUIStyle descriptionStyle = new GUIStyle(GUI.skin.label);
                    descriptionStyle.fontStyle = FontStyle.Italic;
                    descriptionStyle.fontSize = descriptionStyle.fontSize - 2;

                    SteadyAimV2Enabled = GUILayout.Toggle(SteadyAimV2Enabled, "Enable \"Steady Aim\" feature?", GUI.skin.toggle);
                    GUILayout.Label("This allows you to configure shaking power/speed/duration while using the bow.", descriptionStyle);

                    if (SteadyAimV2Enabled)
                    {
                        GUILayout.Space(15.0f);
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Shaking power: ", GUI.skin.label);
                        GUILayout.Label(Power.ToString("F3", CultureInfo.InvariantCulture) + " ", descriptionStyle);
                        GUILayout.FlexibleSpace();
                        Power = GUILayout.HorizontalSlider(Power, 0.0f, 100.0f, GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb, GUILayout.MinWidth(550.0f));
                        GUILayout.EndHorizontal();

                        GUILayout.Space(8.0f);
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Shaking speed: ", GUI.skin.label);
                        GUILayout.Label(Speed.ToString("F3", CultureInfo.InvariantCulture) + " ", descriptionStyle);
                        GUILayout.FlexibleSpace();
                        Speed = GUILayout.HorizontalSlider(Speed, 0.0f, 10.0f, GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb, GUILayout.MinWidth(550.0f));
                        GUILayout.EndHorizontal();

                        GUILayout.Space(8.0f);
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Shaking duration: ", GUI.skin.label);
                        GUILayout.Label(Duration.ToString("F3", CultureInfo.InvariantCulture) + " ", descriptionStyle);
                        GUILayout.FlexibleSpace();
                        Duration = GUILayout.HorizontalSlider(Duration, 0.0f, 10.0f, GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb, GUILayout.MinWidth(550.0f));
                        GUILayout.EndHorizontal();

                        GUILayout.Space(8.0f);
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Restore default settings", GUI.skin.button, GUILayout.MaxWidth(200f)))
                            OnClickRestoreDefaultsButton();
                        GUILayout.EndHorizontal();
                    }

                    if (Power != PowerOrig)
                    {
                        if (Power >= 0.0f && Power <= 100.0f)
                            PowerOrig = Power;
                        else
                        {
                            Power = 10.0f;
                            PowerOrig = 10.0f;
                        }
                        SaveSettings();
                    }
                    if (Speed != SpeedOrig)
                    {
                        if (Speed >= 0.0f && Speed <= 10.0f)
                            SpeedOrig = Speed;
                        else
                        {
                            Speed = 1.0f;
                            SpeedOrig = 1.0f;
                        }
                        SaveSettings();
                    }
                    if (Duration != DurationOrig)
                    {
                        if (Duration >= 0.0f && Duration <= 10.0f)
                            DurationOrig = Duration;
                        else
                        {
                            Duration = 1.0f;
                            DurationOrig = 1.0f;
                        }
                        SaveSettings();
                    }
                    if (SteadyAimV2Enabled != SteadyAimV2EnabledOrig)
                    {
                        SteadyAimV2EnabledOrig = SteadyAimV2Enabled;
                        if (SteadyAimV2Enabled)
                        {
                            ShowHUDInfo("Steady Aim feature enabled.", 4f);
#if VERBOSE
                            ModAPI.Log.Write($"[{ModName}:ModOptionsBox] Steady Aim feature has been enabled.");
#endif
                        }
                        else
                        {
                            ShowHUDInfo("Steady Aim feature disabled.", 4f, Color.yellow);
#if VERBOSE
                            ModAPI.Log.Write($"[{ModName}:ModOptionsBox] Steady Aim feature has been disabled.");
#endif
                        }
                        SaveSettings();
                        InitWindow();
                    }
                }
            }
            else
            {
                using (var optionsScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    if (!hasPermission && SteadyAimV2.NbPermissionRequests >= 3)
                    {
                        GUI.color = Color.yellow;
                        GUILayout.Label("Host did not reply to your permission requests or has denied permission to use Steady Aim v2 mod.", GUI.skin.label);
                        GUI.color = DefaultGuiColor;
                    }
                    else
                    {
                        GUILayout.Label("It seems that you are not the host. You can ask permission to use Steady Aim v2 mod with the button below:", GUI.skin.label);
                        if (GUILayout.Button("Ask permission", GUI.skin.button, GUILayout.MinWidth(120f)))
                            SteadyAimV2.DoRequestPermission = true;
                    }
                }
            }
        }

        private void CollapseWindow()
        {
            if (!IsMinimized)
            {
                SteadyAimV2Screen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenMinHeight);
                IsMinimized = true;
            }
            else
            {
                SteadyAimV2Screen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);
                IsMinimized = false;
            }
            InitWindow();
        }

        private void CloseWindow()
        {
            ShowUI = false;
            EnableCursor(false);
        }

        private void EnableCursor(bool blockPlayer = false)
        {
            CursorManager.Get().ShowCursor(blockPlayer, false);

            if (blockPlayer)
            {
                LocalPlayer.BlockMoves();
                LocalPlayer.BlockRotation();
                LocalPlayer.BlockInspection();
            }
            else
            {
                LocalPlayer.UnblockMoves();
                LocalPlayer.UnblockRotation();
                LocalPlayer.UnblockInspection();
            }
        }

        #endregion

        #region Unity methods

        private void Start()
        {
            ModAPI.Log.Write($"[{ModName}:Start] Initializing {ModName}...");
            GetGameHandles();
            ModKeybindingId = GetConfigurableKey(ModKeybindingName, DefaultModKeybindingId);
            LoadSettings();
            ModAPI.Log.Write($"[{ModName}:Start] {ModName} initialized.");
        }

        private void OnDestroy()
        {
            SteadyAimV2.RestorePermissionStateToOrig();
        }

        private void OnGUI()
        {
            if (ShowUI)
            {
                GetGameHandles();
                GUI.skin = ModAPI.Interface.Skin;
                InitWindow();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(ModKeybindingId))
            {
                if (!ShowUI)
                {
                    GetGameHandles();
                    EnableCursor(true);
                }
                ShowUI = !ShowUI;
                if (!ShowUI)
                    EnableCursor(false);
            }
            if (!(P2PSession.Instance.GetGameVisibility() == P2PGameVisibility.Singleplayer || ReplTools.AmIMaster() || SteadyAimV2.PermissionDenied || SteadyAimV2.PermissionGranted))
            {
                long currTime = DateTime.Now.Ticks / 10000000L;
                if (SteadyAimV2.WaitingPermission)
                {
                    if ((currTime - SteadyAimV2.PermissionAskTime) > 56L)
                    {
                        if (SteadyAimV2.NbPermissionRequests >= 3)
                            SteadyAimV2.PermissionDenied = true;
                        SteadyAimV2.WaitingPermission = false;
                        SteadyAimV2.PermissionAskTime = -1L;
                        ShowHUDInfo($"Host did not reply to your permission request{(SteadyAimV2.PermissionDenied ? "" : ", please try again")}.", 6f, Color.yellow);
                    }
                }
                if (SteadyAimV2.OtherWaitingPermission)
                {
                    if ((currTime - SteadyAimV2.OtherPermissionAskTime) > 59L)
                    {
                        SteadyAimV2.OtherWaitingPermission = false;
                        SteadyAimV2.OtherPermissionAskTime = -1L;
                    }
                }
            }
        }

        #endregion
    }
}
