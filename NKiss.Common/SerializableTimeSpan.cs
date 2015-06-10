
using System;
using System.Xml.Serialization;

namespace Adhesive.Common
{
    public struct SerializableTimeSpan
    {
        #region Constructor

        private TimeSpan _timeSpan;

        public SerializableTimeSpan(int hours, int minutes, int seconds)
        {
            _timeSpan = new TimeSpan(hours, minutes, seconds);

        }

        public SerializableTimeSpan(TimeSpan timeSpan)
        {
            _timeSpan = new TimeSpan();
            this._timeSpan = timeSpan;
        }

        #endregion

        [XmlText]
        public string XmlText
        {
            get
            {
                return _timeSpan.ToString();
            }
            set
            {
                TimeSpan.TryParse(value, out _timeSpan);
            }
        }

        #region Convertor

        public static implicit operator TimeSpan(SerializableTimeSpan t)
        {
            return t._timeSpan;
        }
        public static implicit operator SerializableTimeSpan(TimeSpan t)
        {
            return new SerializableTimeSpan(t);
        }

        #endregion

        #region Override
        public override string ToString()
        {
            return _timeSpan.ToString();
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (Object.ReferenceEquals(this, obj))
                return true;
            if (this.GetType() != obj.GetType())
                return false;
            SerializableTimeSpan timeSpanEx = (SerializableTimeSpan)obj;
            if (_timeSpan.Equals(timeSpanEx._timeSpan))
                return true;
            return false;
        }
        public override int GetHashCode()
        {
            return _timeSpan.GetHashCode();
        }
        #endregion
    }
}
