using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SET09102_2024_5.ViewModels;
using Microsoft.Maui.Controls;

namespace SET09102_2024_5.Views
{
    public partial class DataQualityPage : ContentPage
    {
        public DataQualityPage(DataQualityViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}
