using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LoLCrutch
{

    public partial class MainWindow : Window
    {

        // Currently only two simple applications states
        private static int STATE_STOPPED = 0;
        private static int STATE_RUNNING = 1;

        private static int MINION_SPAWN_TIME = 90;
        private static int MINION_SPAWN_INTERVAL = 30;
        private static int MINION_SPAWN_SIEGE_INTERVAL = 3;

        private static TimeSpan MINION_BARON_INITIAL = new TimeSpan(0, 0, 15, 0); // 15 minutes
        private static TimeSpan MINION_BARON_RESPAWN = new TimeSpan(0, 0, 7, 0); // 7 minutes

        private static TimeSpan MINION_DRAGON_INITIAL = new TimeSpan(0, 0, 2, 30); // 2 minutes 30 seconds
        private static TimeSpan MINION_DRAGON_RESPAWN = new TimeSpan(0, 0, 6, 0); // 6 minutes

        private static TimeSpan MINION_BUFF_INITIAL = new TimeSpan(0, 0, 1, 55); // 1 minutes 55 seconds
        private static TimeSpan MINION_BUFF_RESPAWN = new TimeSpan(0, 0, 5, 0); // 5 minutes

        private static TimeSpan MINION_MINOR_INITIAL = new TimeSpan(0, 0, 2, 5); // 2 minutes 5 seconds
        private static TimeSpan MINION_MINOR_RESPAWN = new TimeSpan(0, 0, 0, 50); // 50 seconds
        
        private int state = STATE_STOPPED;

        private DateTime started;

        private TimeSpan baron;
        private TimeSpan dragon;
        private TimeSpan theirBlue;
        private TimeSpan ourBlue;
        private TimeSpan theirRed;
        private TimeSpan ourRed;

        public MainWindow()
        {

            // Initializes the form
            InitializeComponent();

            // Reset crutch
            CrutchReset();

            // Setup
            SetupWorker();
            SetupHotkeys();

        }

        private void SetupWorker()
        {

            // Setup our timer to run every 100 ms
            DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(CrutchOnTick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            dispatcherTimer.Start();

        }

        private void SetupHotkeys()
        {

            // Register hotkeys
            HotKeyManager.RegisterHotKey(Keys.F6, KeyModifiers.None);
            HotKeyManager.RegisterHotKey(Keys.F6, KeyModifiers.Shift);
            HotKeyManager.RegisterHotKey(Keys.F7, KeyModifiers.None);
            HotKeyManager.RegisterHotKey(Keys.F7, KeyModifiers.Shift);
            HotKeyManager.RegisterHotKey(Keys.F8, KeyModifiers.None);
            HotKeyManager.RegisterHotKey(Keys.F9, KeyModifiers.None);

            // Add hotkey event handler
            HotKeyManager.HotKeyPressed += new EventHandler<HotKeyEventArgs>(kh_KeyPressed);

        }

        private void kh_KeyPressed(object sender, HotKeyEventArgs e)
        {

            // Calculate elapsed time since start of game
            TimeSpan elapsed = DateTime.Now.Subtract(started);

            // Handle various hotkeys
            if (KeyModifiers.Shift == e.Modifiers && Keys.F6 == e.Key)
            {
                theirBlue = MINION_BUFF_RESPAWN.Add(elapsed);
            }
            else if (KeyModifiers.None == e.Modifiers && Keys.F6 == e.Key)
            {
                ourBlue = MINION_BUFF_RESPAWN.Add(elapsed);
            }
            else if (KeyModifiers.Shift == e.Modifiers && Keys.F7 == e.Key)
            {
                theirRed = MINION_BUFF_RESPAWN.Add(elapsed);
            }
            else if (KeyModifiers.None == e.Modifiers && Keys.F7 == e.Key)
            {
                ourRed = MINION_BUFF_RESPAWN.Add(elapsed);
            }
            else if (KeyModifiers.None == e.Modifiers && Keys.F8 == e.Key)
            {
                dragon = MINION_DRAGON_RESPAWN.Add(elapsed);
            }
            else if (KeyModifiers.None == e.Modifiers && Keys.F9 == e.Key)
            {
                baron = MINION_BARON_RESPAWN.Add(elapsed);
            }
        }
        
        private void CrutchReset()
        {

            // Reset all timers to 0
            BlueTimers.Text = "00:00 | 00:00";
            RedTimers.Text = "00:00 | 00:00";
            DragonTimers.Text = "00:00";
            BaronTimers.Text = "00:00";

            // Set the creep spawns to 0
            MinionSpawns.Text = "0";

            // Set the game time to 00:00
            GameTimeBox.Text = "00:00";

        }

        private void CrutchOnTick(object sender, EventArgs e)
        {

            // We only need to do anything if the app is running
            if (STATE_RUNNING == state)
            {

                // calculate elapsed time since start of game
                var elapsed = DateTime.Now.Subtract(started);

                // Update interface
                UpdateTimeBox(elapsed);
                UpdateMinionSpawns(elapsed);
                UpdateBaron(elapsed);
                UpdateDragon(elapsed);
                UpdateBlue(elapsed);
                UpdateRed(elapsed);
                UpdateTmp(elapsed);

            }
        }

        private void InitTimers()
        {

            // calculate elapsed time since start of game
            TimeSpan elapsed = DateTime.Now.Subtract(started);

            // Baron hasn't spawned, show a countdown!
            if (elapsed.TotalSeconds < MINION_BARON_INITIAL.TotalSeconds)
            {
                baron = MINION_BARON_INITIAL;
            }

            // Dragon hasn't spawned, show a countdown!
            if (elapsed.TotalSeconds < MINION_DRAGON_INITIAL.TotalSeconds)
            {
                dragon = MINION_DRAGON_INITIAL;
            }

            // Buffs haven't spawned, show a countdown!
            if (elapsed.TotalSeconds < MINION_BUFF_INITIAL.TotalSeconds)
            {
                ourBlue = MINION_BUFF_INITIAL;
                ourRed = MINION_BUFF_INITIAL;
                theirBlue = MINION_BUFF_INITIAL;
                theirRed = MINION_BUFF_INITIAL;
            }
        }

        private string PrintTime(TimeSpan elapsed)
        {
            if(0 > elapsed.TotalSeconds) {
                return "Up!";
            }

            return elapsed.Minutes.ToString("00") + ":" + elapsed.Seconds.ToString("00");
        }

        private string PrintTimes(TimeSpan ours, TimeSpan theirs)
        {
            return PrintTime(ours) + " | " + PrintTime(theirs);
        }

        private void UpdateTmp(TimeSpan elapsed)
        {
            BaronTimers_Tmp.Text = PrintTime(MINION_BARON_RESPAWN.Add(elapsed));
            DragonTimers_Tmp.Text = PrintTime(MINION_DRAGON_RESPAWN.Add(elapsed));
            BuffTimers_Tmp.Text = PrintTime(MINION_BUFF_RESPAWN.Add(elapsed));
            MinorTimers_Tmp.Text = PrintTime(MINION_MINOR_RESPAWN.Add(elapsed));
        }

        private void UpdateBaron(TimeSpan elapsed)
        {
            BaronTimers.Text = PrintTime(baron.Subtract(elapsed));
        }

        private void UpdateDragon(TimeSpan elapsed)
        {
            DragonTimers.Text = PrintTime(dragon.Subtract(elapsed));
        }

        private void UpdateBlue(TimeSpan elapsed)
        {
            BlueTimers.Text = PrintTimes(ourBlue.Subtract(elapsed), theirBlue.Subtract(elapsed));
        }

        private void UpdateRed(TimeSpan elapsed)
        {
            RedTimers.Text = PrintTimes(ourRed.Subtract(elapsed), theirRed.Subtract(elapsed));
        }
        
        private void UpdateTimeBox(TimeSpan elapsed)
        {
            GameTimeBox.Text = elapsed.Minutes.ToString("00") + ":" + elapsed.Seconds.ToString("00");
        }

        private void UpdateMinionSpawns(TimeSpan elapsed)
        {

            // Get the total elapsed seconds
            var seconds = Math.Ceiling(elapsed.TotalSeconds);

            // Minions don't spawn till 1:30
            seconds -= MINION_SPAWN_TIME;
            seconds -= 1; // fix a off by 1 second issue

            if (0 > seconds)
            {
                return;
            }

            // First decide total waves since game start
            var waves = Math.Floor(1 + (seconds / MINION_SPAWN_INTERVAL));

            // Now figure out how many creeps this is equal to
            var creeps = (waves * 6);

            // Also add siege minions
            creeps += Math.Floor(waves / MINION_SPAWN_SIEGE_INTERVAL);

            // Update the text
            MinionSpawns.Text = creeps.ToString();
            
        }

        private void StateButton_Click(object sender, RoutedEventArgs e)
        {
            if (STATE_RUNNING == state)
            {

                // Set state to stopped and reset
                state = STATE_STOPPED;

                // Enable the text box
                GameTimeBox.IsReadOnly = false;

                // Set the button
                StateButton.Content = "Start";

                // Reset all values
                CrutchReset();

            }
            else if (STATE_STOPPED == state)
            {

                // Set the date time
                started = DateTime.Now;

                // Get the time span the user set for initial start
                TimeSpan initialOffset = GetTimeBoxValue();

                // Now set the new start time
                started = started.Subtract(initialOffset);

                // Disable the text box
                GameTimeBox.IsReadOnly = true;

                // Set the button
                StateButton.Content = "Stop";

                InitTimers();

                // Set state to running
                state = STATE_RUNNING;

            }
        }
        
        private TimeSpan GetTimeBoxValue()
        {
            TimeSpan ret;

            // Lets parse this text and set our initial time span, so we
            // can reset it to 0 if they inserted something malformed
            var currentTime = GameTimeBox.Text;

            // Our valid formats are:
            //   SS, where SS is the number of seconds
            //   MM:SS, where MM is the number of minutes
            Regex regSs = new Regex(@"^[0-9]{1,2}$");
            Regex regMm = new Regex(@"^[0-9]{1,2}:[0-9]{1,2}$");

            if (regSs.IsMatch(currentTime))
            {
                var seconds = Convert.ToInt32(currentTime);

                ret = new TimeSpan(0, 0, 0, seconds);
            }
            else if (regMm.IsMatch(currentTime))
            {
                var parts = currentTime.Split(':');
                var minutes = Convert.ToInt32(parts[0]);
                var seconds = Convert.ToInt32(parts[1]);

                ret = new TimeSpan(0, 0, minutes, seconds);
            }
            else
            {

                // bad format, set to 00:00
                GameTimeBox.Text = "00:00";

                ret = new TimeSpan(0, 0, 0, 0);
            }

            return ret;
        }

        private void GameTimeBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {

            // We only care about the enter key
            if (e.Key != System.Windows.Input.Key.Enter)
            {
                return;
            }

            GetTimeBoxValue();
        }
    }

    #region KeyboardHookHelper

    public static class HotKeyManager
    {
        public static event EventHandler<HotKeyEventArgs> HotKeyPressed;

        public static int RegisterHotKey(Keys key, KeyModifiers modifiers)
        {
            _windowReadyEvent.WaitOne();
            int id = System.Threading.Interlocked.Increment(ref _id);
            _wnd.Invoke(new RegisterHotKeyDelegate(RegisterHotKeyInternal), _hwnd, id, (uint)modifiers, (uint)key);
            return id;
        }

        public static void UnregisterHotKey(int id)
        {
            _wnd.Invoke(new UnRegisterHotKeyDelegate(UnRegisterHotKeyInternal), _hwnd, id);
        }

        delegate void RegisterHotKeyDelegate(IntPtr hwnd, int id, uint modifiers, uint key);
        delegate void UnRegisterHotKeyDelegate(IntPtr hwnd, int id);

        private static void RegisterHotKeyInternal(IntPtr hwnd, int id, uint modifiers, uint key)
        {
            RegisterHotKey(hwnd, id, modifiers, key);
        }

        private static void UnRegisterHotKeyInternal(IntPtr hwnd, int id)
        {
            UnregisterHotKey(_hwnd, id);
        }

        private static void OnHotKeyPressed(HotKeyEventArgs e)
        {
            if (HotKeyManager.HotKeyPressed != null)
            {
                HotKeyManager.HotKeyPressed(null, e);
            }
        }

        private static volatile MessageWindow _wnd;
        private static volatile IntPtr _hwnd;
        private static ManualResetEvent _windowReadyEvent = new ManualResetEvent(false);
        static HotKeyManager()
        {
            Thread messageLoop = new Thread(delegate()
            {
                System.Windows.Forms.Application.Run(new MessageWindow());
            });
            messageLoop.Name = "MessageLoopThread";
            messageLoop.IsBackground = true;
            messageLoop.Start();
        }

        private class MessageWindow : Form
        {
            public MessageWindow()
            {
                _wnd = this;
                _hwnd = this.Handle;
                _windowReadyEvent.Set();
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_HOTKEY)
                {
                    HotKeyEventArgs e = new HotKeyEventArgs(m.LParam);
                    HotKeyManager.OnHotKeyPressed(e);
                }

                base.WndProc(ref m);
            }

            protected override void SetVisibleCore(bool value)
            {
                // Ensure the window never becomes visible
                base.SetVisibleCore(false);
            }

            private const int WM_HOTKEY = 0x312;
        }

        [DllImport("user32", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private static int _id = 0;
    }


    public class HotKeyEventArgs : EventArgs
    {
        public readonly Keys Key;
        public readonly KeyModifiers Modifiers;

        public HotKeyEventArgs(Keys key, KeyModifiers modifiers)
        {
            this.Key = key;
            this.Modifiers = modifiers;
        }

        public HotKeyEventArgs(IntPtr hotKeyParam)
        {
            uint param = (uint)hotKeyParam.ToInt64();
            Key = (Keys)((param & 0xffff0000) >> 16);
            Modifiers = (KeyModifiers)(param & 0x0000ffff);
        }
    }

    [Flags]
    public enum KeyModifiers
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8,
        NoRepeat = 0x4000
    }

    #endregion

}
