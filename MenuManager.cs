namespace GibsonCrabGameGlobalOffensive
{
    public class MenuManager : MonoBehaviour
    {
        DateTime lastActionTime = DateTime.Now;

        private const int WAIT = 150;
        // Importation des fonctions de la librairie user32.dll
        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);
        const int VK_RBUTTON = 0x02;
        void Update()
        {

            TimeSpan elapsed = DateTime.Now - lastActionTime;
            HandleMenuDisplays();
            HandleMenuActions(elapsed);
            HandleMenuSpeedHelper(elapsed);

            if (Input.GetMouseButtonDown(2))
            {
                onSubButton = false;
                onButton = false;
                subMenuSelector = -1;
            }

        }

        private void HandleMenuDisplays()
        {
            displayButton0 = MenuFunctions.HandleMenuDisplay(0, () => "Skip Lobby", () => MenuFunctions.DisplayButtonState(0));
            displayButton1 = MenuFunctions.HandleMenuDisplay(1, () => "Skip Prematch Timer", () => MenuFunctions.DisplayButtonState(1));
            displayButton2 = MenuFunctions.HandleMenuDisplay(2, () => "Auto Server Message", () => MenuFunctions.DisplayButtonState(2));
            displayButton3 = MenuFunctions.HandleMenuDisplay(3, () => "Speak PostMortem", () => MenuFunctions.DisplayButtonState(3));
            displayButton4 = MenuFunctions.HandleMenuDisplay(4, () => "Host AFK", () => MenuFunctions.DisplayButtonState(4));
            displayButton5 = MenuFunctions.HandleMenuDisplay(5, () => "Flung Detector", () => MenuFunctions.DisplayButtonState(5)) + MenuFunctions.GetSelectedFlungDetector_();
            displayButton6 = MenuFunctions.HandleMenuDisplay(6, () => "Item Detector", () => MenuFunctions.DisplayButtonState(6));
            displayButton7 = MenuFunctions.HandleMenuDisplay(7, () => "Fast Fire Detector", () => MenuFunctions.DisplayButtonState(7));
        }

        private void HandleMenuActions(TimeSpan elapsed)
        {
            bool moletteH = false;
            bool moletteB = false;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f && menuTrigger)
            {
                moletteH = true;
            }
            else if (scroll < 0f && menuTrigger)
            {
                moletteB = true;
            }

            if (!menuTrigger || elapsed.TotalMilliseconds < WAIT)
            {
                if (!moletteB && !moletteH)
                    return;
            }

            menuSpeedHelperFast = 0;

            bool f5KeyPressed = moletteH;
            bool f6KeyPressed = GetAsyncKeyState(VK_RBUTTON) < 0;
            bool f7KeyPressed = moletteB;
            if (f5KeyPressed || f6KeyPressed || f7KeyPressed)
            {
                UpdateMenuSpeed(elapsed);
                HandleKeyActions(f5KeyPressed, f6KeyPressed, f7KeyPressed);
                lastActionTime = DateTime.Now;
            }
        }

        private void HandleMenuSpeedHelper(TimeSpan elapsed)
        {
            if (elapsed.TotalMilliseconds >= WAIT + menuSpeedHelperFast)
            {
                if (menuSpeedHelper > 0)
                    menuSpeedHelper -= 1;
                menuSpeedHelperFast += WAIT;
            }
        }

        private void UpdateMenuSpeed(TimeSpan elapsed)
        {
            if (elapsed.TotalMilliseconds <= 200)
                menuSpeedHelper += 2;
            if (menuSpeedHelper > 8)
                menuSpeed = 5;
            else
                menuSpeed = 1;

            Utility.PlayMenuSound();

        }

        private void HandleKeyActions(bool f5KeyPressed, bool f6KeyPressed, bool f7KeyPressed)
        {
            if (f5KeyPressed)
            {
                HandleF5KeyPressed();
            }

            if (f6KeyPressed)
            {
                HandleF6KeyPressed();
            }

            if (f7KeyPressed)
            {
                HandleF7KeyPressed();
            }
        }

        private void HandleF5KeyPressed()
        {
            if (!onButton)
                menuSelector = menuSelector > 0 ? menuSelector - 1 : buttonStates.Length - 1;
            else if (!onSubButton)
            {
                switch (menuSelector)
                {
                    case 4:
                        subMenuSelector = subMenuSelector > 0 ? subMenuSelector - 1 : 2;
                        break;
                    default:
                        return;
                }
            }
            else
            {
                switch (menuSelector)
                {
                    case 5:
                        if (subMenuSelector == 0)
                        {
                            checkFrequency = checkFrequency - 0.01f * menuSpeed;
                            if (checkFrequency < 0)
                                checkFrequency = 0;
                        }
                        if (subMenuSelector == 1)
                        {
                            alertLevel = alertLevel - 1 * menuSpeed;
                            if (alertLevel < 0)
                                alertLevel = 0;
                            break;
                        }
                        break;
                    default:
                        return;
                }
            }
        }

        private void HandleF7KeyPressed()
        {
            if (!onButton)
                menuSelector = menuSelector < buttonStates.Length - 1 ? menuSelector + 1 : 0;
            else if (!onSubButton)
            {
                switch (menuSelector)
                {
                    case 5:
                        subMenuSelector = subMenuSelector < 2 ? subMenuSelector + 1 : 0;
                        break;
                    default:
                        return;
                }
            }
            else
            {
                switch (menuSelector)
                {
                    case 5:
                        if (subMenuSelector == 0)
                        {
                            checkFrequency = checkFrequency < 1 ? checkFrequency + 0.01f * menuSpeed : 1;
                        }
                        if (subMenuSelector == 1)
                        {
                            alertLevel = alertLevel < 2 ? alertLevel + 1 * menuSpeed : 2;
                        }
                        break;
                    default:
                        return;
                }
            }
        }

        public static void HandleF6KeyPressed()
        {
            if (menuSelector < buttonStates.Length)
            {
                MenuFunctions.ExecuteSubMenuAction();

                if (!onButton && IsSpecialMenu(menuSelector))
                    subMenuSelector = 0;

                bool previousButtonState = buttonStates[menuSelector];
                if (!IsSpecialMenu(menuSelector))
                {
                    buttonStates[menuSelector] = !previousButtonState;
                    onButton = buttonStates[menuSelector];
                }
                else
                {
                    onButton = true;
                }

                if (!IsSpecialMenu(menuSelector))
                    onButton = false;
            }
        }
        private static bool IsSpecialMenu(int menuSelector)
        {
            return menuSelector == 5;
        }
    }

    public class MenuFunctions
    {
        public static void CheckMenuFileExists()
        {
            string menuContent = "\t\r\n\tPosition : [POSITION]  |  Speed : [SPEED]  |  Rotation : [ROTATION]\t\t<b> \r\n\r\n\t______________________________________________________________________</b>\r\n\r\n\r\n\t<b><color=orange>[OTHERPLAYER]</color></b>  |  Position: [OTHERPOSITION]  |  Speed : [OTHERSPEED] | Selecteur :  [SELECTEDINDEX] | <b>Status : [STATUS]</b> \r\n\r\n\t\t\t\r\n\t\r\n\r\n\t______________________________________________________________________\r\n\r\n\t\t\r\n     <b>[MENUBUTTON0]\r\n\r\n\t[MENUBUTTON1]\r\n\r\n\t[MENUBUTTON2]\r\n\r\n\t[MENUBUTTON3]\r\n\r\n\t[MENUBUTTON4]\r\n\r\n\t_______________________________ANTICHEAT_______________________________\r\n\r\n\t[MENUBUTTON5]\r\n\r\n\t[MENUBUTTON6]\r\n\r\n\t[MENUBUTTON7]</b>";

            if (System.IO.File.Exists(menuFilePath))
            {
                string currentContent = System.IO.File.ReadAllText(menuFilePath, System.Text.Encoding.UTF8);


                if (currentContent != menuContent)
                {
                    System.IO.File.WriteAllText(menuFilePath, menuContent, System.Text.Encoding.UTF8);
                }
            }
            else
            {
                // Si le fichier n'existe pas, créez-le avec le contenu fourni
                System.IO.File.WriteAllText(menuFilePath, menuContent, System.Text.Encoding.UTF8);
            }
        }
        public static void RegisterDataCallbacks(System.Collections.Generic.Dictionary<string, System.Func<string>> dict)
        {
            foreach (System.Collections.Generic.KeyValuePair<string, System.Func<string>> pair in dict)
            {
                DebugDataCallbacks.Add(pair.Key, pair.Value);
            }
        }
        public static void LoadMenuLayout()
        {
            layout = System.IO.File.ReadAllText(menuFilePath, System.Text.Encoding.UTF8);
        }
        public static void RegisterDefaultCallbacks()
        {
            RegisterDataCallbacks(new System.Collections.Generic.Dictionary<string, System.Func<string>>(){
                {"POSITION", ClientData.GetClientPositionString},
                {"SPEED", ClientData.GetClientSpeedString},
                {"ROTATION", ClientData.GetClientRotationString},
                {"SELECTEDINDEX", () => playerIndex.ToString()},
                {"OTHERPLAYER", MultiPlayersData.GetOtherPlayerUsername},
                {"OTHERPOSITION", MultiPlayersData.GetOtherPlayerPositionAsString},
                {"OTHERSPEED", MultiPlayersData.GetOtherPlayerSpeed},
                {"STATUS", MultiPlayersData.GetStatus},
                {"MENUBUTTON0",() => displayButton0},
                {"MENUBUTTON1",() => displayButton1},
                {"MENUBUTTON2",() => displayButton2},
                {"MENUBUTTON3",() => displayButton3},
                {"MENUBUTTON4",() => displayButton4},
                {"MENUBUTTON5",() => displayButton5},
                {"MENUBUTTON6",() => displayButton6},
                {"MENUBUTTON7",() => displayButton7},
            });
        }

        public static string DisplayButtonState(int index)
        {
            if (buttonStates[index])
                return "<b><color=red>ON</color></b>";
            else
                return "<b><color=blue>OFF</color></b>";
        }
        public static string FormatLayout()
        {
            string formatted = layout;
            foreach (System.Collections.Generic.KeyValuePair<string, System.Func<string>> pair in DebugDataCallbacks)
            {
                formatted = formatted.Replace("[" + pair.Key + "]", pair.Value());
            }
            return formatted;
        }
        public static string HandleMenuDisplay(int buttonIndex, Func<string> getButtonLabel, Func<string> getButtonSpecificData)
        {
            string buttonLabel = getButtonLabel();

            if (menuSelector != buttonIndex)
            {
                return $" {buttonLabel} <b>{getButtonSpecificData()}</b>";
            }

            if (!buttonStates[buttonIndex])
            {
                return $"■<color=yellow>{buttonLabel}</color>■  <b>{getButtonSpecificData()}</b>";
            }
            else
            {
                return $"<color=red>■</color><color=yellow>{buttonLabel}</color><color=red>■</color>  <b>{getButtonSpecificData()}</b>";
            }
        }
        public static string GetSelectedFlungDetector_()
        {
            if (menuSelector == 5)
            {
                switch (subMenuSelector)
                {
                    case 0:
                        if (onSubButton)
                            return "  |  " + $"<color=red>■</color><color=orange>Check Frequency : {checkFrequency.ToString("F2")}</color><color=red>■</color>" + $"  |  Alert Level" + $"  |  Flung Detector Status";
                        else
                            return "  |  " + $"■<color=orange>Check Frequency : {checkFrequency.ToString("F2")}</color>■" + $"  |  Alert Level" + $"  |  Flung Detector Status";
                    case 1:
                        if (onSubButton)
                            return "  |  " + $"Check Frequency" + $"  |  <color=red>■</color><color=orange>Alert Level : {alertLevel.ToString()}</color><color=red>■</color>" + $"  |  Flung Detector Status";
                        else
                            return "  |  " + $"Check Frequency" + $"  |  ■<color=orange>Alert Level : {alertLevel.ToString()}</color>■" + $"  |  Flung Detector Status";
                    case 2:
                        return "  |  " + $"Check Frequency" + $"  |  Alert Level" + $"  |  ■<color=orange>Flung Dector Status : {buttonStates[5].ToString()}</color>■";
                    default:
                        return "";
                }
            }
            else
                return "";
        }
        public static void ExecuteSubMenuAction()
        {
            if (!onButton)
            {
                var selectors = (menuSelector, subMenuSelector);

                switch (selectors)
                {
                    case (40, -1):
                        break;
                }
            }
            if (onButton)
            {
                var selectors = (menuSelector, subMenuSelector);

                switch (selectors)
                {
                    case (5, 0):
                        onSubButton = !onSubButton;
                        break;
                    case (5, 1):
                        onSubButton = !onSubButton;
                        break;
                    case (5, 2):
                        buttonStates[5] = !buttonStates[5];

                        if (buttonStates[5])
                            Utility.ForceMessage("■<color=yellow>(FD))Flung Detector ON</color>■");
                        else
                            Utility.ForceMessage("■<color=yellow>(FD)Flung Detector OFF</color>■");
                        break;
                }
            }
        }
    }
}
