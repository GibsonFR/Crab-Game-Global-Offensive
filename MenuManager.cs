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
            displayButton5 = MenuFunctions.HandleMenuDisplay(5, () => "Flung Detector", () => MenuFunctions.DisplayButtonState(5)) + MenuFunctions.GetSelectedFlungDetectorParam();
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
}
