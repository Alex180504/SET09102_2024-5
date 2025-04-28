using System;
using System.ComponentModel;

namespace SET09102_2024_5.Models
{
    public class SensorOperationalModel : INotifyPropertyChanged
    {
        private int _id;
        private string _type;
        private string _status;
        private string _measurand;
        private DateTime? _deploymentDate;
        private int _incidentCount;

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

        public string Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged(nameof(Type));
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

        public string Measurand
        {
            get => _measurand;
            set
            {
                if (_measurand != value)
                {
                    _measurand = value;
                    OnPropertyChanged(nameof(Measurand));
                }
            }
        }

        public DateTime? DeploymentDate
        {
            get => _deploymentDate;
            set
            {
                if (_deploymentDate != value)
                {
                    _deploymentDate = value;
                    OnPropertyChanged(nameof(DeploymentDate));
                }
            }
        }

        public int IncidentCount
        {
            get => _incidentCount;
            set
            {
                if (_incidentCount != value)
                {
                    _incidentCount = value;
                    OnPropertyChanged(nameof(IncidentCount));
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
