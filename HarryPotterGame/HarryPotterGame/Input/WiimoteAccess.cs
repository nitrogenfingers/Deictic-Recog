using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WiimoteLib;
using System.Windows.Forms;

using Microsoft.Xna.Framework;

namespace HarryPotterGame.Input {
    public class WiimoteAccess {
        //The object giving us access to WM state information
        private Wiimote wm;
        //The last wiimote state. Unlike the last sensor, this updates every time the wiimote does.
        private WiimoteState lastWMState;
        public WiimoteState GetState() { return lastWMState; }
        //The last time a dot was detected on the wiimote sensor. While the information being gathered is suitable for detecting IR "signals",
        private SensorTime lastSensor;

        //The last button state for B
        private bool lastAState = false;
        private bool currAState = false;

        //Gets whether or not the sensor is "signalling". Can be used to interpret the gesture.
        public bool PointDetected {
            get {
                return lastSensor.on || DateTime.Now.Subtract(lastSensor.date).TotalMilliseconds < 200;
            }
        }

        public bool SelectDetected {
            get {
                return currAState && !lastAState;
            }
        }

        public bool GrabDetected {
            get {
                return lastWMState.ButtonState.B;
            }
        }

        /// <summary>
        /// The sensor time struct should be constructed whenever the wiimote detects a change in the number of points being tracked. It stores:
        /// - Whether or not the sensor is on
        /// - The specific date-time that the change in the sensor occurred
        /// - The amount of time that has elapsed since the last change in the sensor state
        /// </summary>
        struct SensorTime {
            public bool on; public DateTime date; public TimeSpan time;
            public SensorTime(bool on, DateTime last) { this.on = on; this.date = DateTime.Now; this.time = DateTime.Now.Subtract(last); }
            public SensorTime(bool on, DateTime now, TimeSpan time) { this.on = on; this.date = now; this.time = time; }
        };

        /// <summary>
        /// Constructs a new WiiMoteAccess object. If there is no wiimote attached to the computer (and I guarantee this will happen at least once)
        /// </summary>
        public WiimoteAccess() {
            try {
                lastSensor = new SensorTime(false, DateTime.Now, TimeSpan.Zero);
                wm = new Wiimote();
                wm.SetLEDs(true, false, false, false);
                wm.WiimoteChanged += new EventHandler<WiimoteChangedEventArgs>(wm_WiimoteChanged);
                wm.WiimoteExtensionChanged += new EventHandler<WiimoteExtensionChangedEventArgs>(wm_WiimoteExtensionChanged);
                wm.Connect();
                wm.SetReportType(InputReport.IRAccel, true);
                lastWMState = wm.WiimoteState;
                lastAState = wm.WiimoteState.ButtonState.A;
            } catch (Exception) {
                DialogResult result = MessageBox.Show("A HIM Wiimote device was not found. Please ensure the WiiMote is connected to your computer" +
                    " by a Bluetooth connection.");
            }
        }

        public void Update(GameTime gameTime) {
            lastAState = currAState;
            currAState = wm.WiimoteState.ButtonState.A;

            if (wm.WiimoteState.ButtonState.A) {
                Console.Out.WriteLine("A detected!");
            }
        }

        private void wm_WiimoteExtensionChanged(object sender, WiimoteExtensionChangedEventArgs e) {
            //throw new NotImplementedException();
        }

        private void wm_WiimoteChanged(object sender, WiimoteChangedEventArgs e) {
            if (e.WiimoteState.IRState.IRSensors[0].Found != lastSensor.on) {
                lastSensor = new SensorTime(e.WiimoteState.IRState.IRSensors[0].Found, lastSensor.date);
            }

            lastWMState = e.WiimoteState;
        }
    }
}
