using System;
using System.Collections.Generic;

namespace keyboard_unchatter_csharp
{
    class KeyStatusList
    {
        public enum PressStatus
        {
            Down = 0,
            Up = 1
        }

        public class KeyStatus
        {
            private int _keyCode;
            private DateTime _pressDownTime;
            private DateTime _blockTime;
            private bool _blocked;
            private PressStatus _lastPressStatus;

            public int KeyCode
            {
                get { return _keyCode; }
            }

            public DateTime PressDownTime
            {
                get { return _pressDownTime; }
            }

            public DateTime BlockTime
            {
                get { return _blockTime; }
            }

            public bool IsBlocked
            {
                get { return _blocked; }
            }

            internal PressStatus LastPressStatus
            {
                get { return _lastPressStatus; }
                set { _lastPressStatus = value; }
            }

            public KeyStatus(int code)
            {
                _keyCode = code;
                _pressDownTime = DateTime.MinValue;
                _blocked = false;
            }

            public double GetLastPressTimeSpan()
            {
                var timeSpan = (DateTime.Now - _pressDownTime);
                return timeSpan.TotalMilliseconds;
            }

            public double GetBlockTimeSpan()
            {
                var timeSpan = (DateTime.Now - _blockTime);
                return timeSpan.TotalMilliseconds;
            }

            public void Block()
            {
                _blocked = true;
                _blockTime = DateTime.Now;
            }

            public void Press()
            {
                _pressDownTime = DateTime.Now;
                _blocked = false;
            }
        }

        private Dictionary<int, KeyStatus> _keyStatus = new Dictionary<int, KeyStatus>();

        public KeyStatus GetKey(int keyCode)
        {
            KeyStatus status;

            if (_keyStatus.TryGetValue(keyCode, out status))
            {
                return status;
            }

            _keyStatus[keyCode] = new KeyStatus(keyCode);

            return _keyStatus[keyCode];
        }

        public void Clear()
        {
            _keyStatus.Clear();
        }
    }
}
