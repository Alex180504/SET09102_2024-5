using System;
using System.ComponentModel;

namespace SET09102_2024_5.Models
{
    public class IncidentModel : INotifyPropertyChanged
    {
        private int _id;
        private string _priority;
        private string _responderName;
        private string _responderComments;
        private string _status;
        private DateTime? _resolvedDate;

        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }
        }

        public string Priority
        {
            get => _priority;
            set
            {
                if (_priority != value)
                {
                    _priority = value;
                    OnPropertyChanged(nameof(Priority));
                }
            }
        }

        public string ResponderName
        {
            get => _responderName;
            set
            {
                if (_responderName != value)
                {
                    _responderName = value;
                    OnPropertyChanged(nameof(ResponderName));
                }
            }
        }

        public string ResponderComments
        {
            get => _responderComments;
            set
            {
                if (_responderComments != value)
                {
                    _responderComments = value;
                    OnPropertyChanged(nameof(ResponderComments));
                }
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public DateTime? ResolvedDate
        {
            get => _resolvedDate;
            set
            {
                if (_resolvedDate != value)
                {
                    _resolvedDate = value;
                    OnPropertyChanged(nameof(ResolvedDate));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
