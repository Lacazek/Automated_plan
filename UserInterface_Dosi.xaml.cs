using System;
using System.Windows;
using System.Windows.Controls;
using VMS.TPS.Common.Model.API;
using System.IO;
using Image = VMS.TPS.Common.Model.API.Image;
using static System.Net.WebRequestMethods;
using System.Collections.Generic;

namespace Structure_optimisation
{
    /// <summary>
    /// Logique d'interaction pour UserInterface.xaml
    /// </summary>
    public partial class UserInterface_Dosi : Window
    {
        private UserInterfaceModel _model;

        internal UserInterface_Dosi(UserInterfaceModel model)
        {
            InitializeComponent();
            _model = model;
            DataContext = _model;
            OK_Button.Visibility = Visibility.Collapsed;
            Box_Loc_machine.Visibility = Visibility.Collapsed;
            Box_Loc_cote.Visibility = Visibility.Collapsed;
            Box_Loc_technique.Visibility = Visibility.Collapsed;
            foreach (var item in Directory.GetFiles(_model.GetPrescription))
            {
                Box_Loc_prescription.Items.Add(Path.GetFileNameWithoutExtension(item));
            }
            Box_Loc_cote.Items.Add("Droit");
            Box_Loc_cote.Items.Add("Gauche");
            Box_Loc_cote.Items.Add(string.Empty);

            Box_Loc_technique.Items.Add("3D");
            Box_Loc_technique.Items.Add("IMRT");
            Box_Loc_technique.Items.Add("Arcthérapie");
            Box_Loc_technique.Items.Add("Dynamic Arc");
            Box_Loc_technique.Items.Add("Stéréotaxie");

            foreach (var item in _model.GetContext.Equipment.GetExternalBeamTreatmentUnits())
            {
                Box_Loc_machine.Items.Add(item);
            }
        }

        internal GetFile File
        {
            get { return _model.File; }
        }
        internal void IsOpened(bool test)
        {
            _model.IsOpened(test);
        }

        private void Button_Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<string> selections = new List<string> {
                Box_Loc_prescription.SelectedItem.ToString(),
                Box_Loc_cote.SelectedItem.ToString(),
                Box_Loc_technique.SelectedItem.ToString(),
                Box_Loc_machine.SelectedItem.ToString()
                };
                _model.UserSelection = selections;
                this.Close();
                _model.LaunchPlanning();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.Close();
            }
        }
        private void ComboBox_SelectionChanged_prescription(object sender, SelectionChangedEventArgs e)
        {
            if (Box_Loc_prescription.SelectedItem != null)
                Box_Loc_cote.Visibility = Visibility.Visible;
            else
                Box_Loc_cote.Visibility = Visibility.Collapsed;
        }

        private void ComboBox_SelectionChanged_cote(object sender, SelectionChangedEventArgs e)
        {
            if (Box_Loc_cote.SelectedItem != null)
                Box_Loc_technique.Visibility = Visibility.Visible;
            else
                Box_Loc_technique.Visibility = Visibility.Collapsed;
        }

        private void ComboBox_SelectionChanged_technique(object sender, SelectionChangedEventArgs e)
        {
            if (Box_Loc_technique.SelectedItem != null)
                Box_Loc_machine.Visibility = Visibility.Visible;
            else
                Box_Loc_machine.Visibility = Visibility.Collapsed;
        }

        private void ComboBox_SelectionChanged_machine(object sender, SelectionChangedEventArgs e)
        {
            if (Box_Loc_machine.SelectedItem != null)
                OK_Button.Visibility = Visibility.Visible;
            else
                OK_Button.Visibility = Visibility.Collapsed;
        }
    }
}
