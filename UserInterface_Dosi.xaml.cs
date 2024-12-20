/******************************************************************************
 * Nom du fichier : UserInterface_Dosi.xaml.cs & UserInterface_Dosi.xaml
 * Auteur         : LACAZE Killian
 * Date de création : [02/10/2024]
 * Description    : [Brève description du contenu ou de l'objectif du code]
 *
 * Droits d'auteur © [2024], [LACAZE.K].
 * Tous droits réservés.
 * 
 * Ce code a été développé exclusivement par LACAZE Killian. Toute utilisation de ce code 
 * est soumise aux conditions suivantes :
 * 
 * 1. L'utilisation de ce code est autorisée uniquement à titre personnel ou professionnel, 
 *    mais sans modification de son contenu.
 * 2. Toute redistribution, copie, ou publication de ce code sans l'accord explicite 
 *    de l'auteur est strictement interdite.
 * 3. L'auteur assume la responsabilité de l'utilisation de ce code dans ses propres projets.
 * 
 * CE CODE EST FOURNI "EN L'ÉTAT", SANS AUCUNE GARANTIE, EXPRESSE OU IMPLICITE. 
 * L'AUTEUR DÉCLINE TOUTE RESPONSABILITÉ POUR TOUT DOMMAGE OU PERTE RÉSULTANT 
 * DE L'UTILISATION DE CE CODE.
 *
 * Toute utilisation non autorisée ou attribution incorrecte de ce code est interdite.
 ******************************************************************************/


using System;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Collections.Generic;
using System.Windows.Input;

namespace Structure_optimisation
{
    /// <summary>
    /// Logique d'interaction pour UserInterface.xaml
    /// </summary>
    public partial class UserInterface_Dosi : Window
    {
        private UserInterfaceModel _model;
        private bool isDropDownOpen = false;

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
                Box_Loc_machine.Items.Add(item.Id.Contains(":") ? item.Id.Split(':')[0] : item.Id);
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

        private void ComboBox_PreviewMouseWheel_prescription(object sender, MouseWheelEventArgs e)
        {
            if (!isDropDownOpen)
            {
                e.Handled = true;
            }
        }
        private void ComboBox_DropDownOpened_prescription(object sender, EventArgs e)
        {
            isDropDownOpen = true;
        }
        private void ComboBox_DropDownClosed_prescription(object sender, EventArgs e)
        {
            isDropDownOpen = false;
        }
        private void ComboBox_PreviewMouseWheel_cote(object sender, MouseWheelEventArgs e)
        {
            if (!isDropDownOpen)
            {
                e.Handled = true;
            }
        }
        private void ComboBox_DropDownOpened_cote(object sender, EventArgs e)
        {
            isDropDownOpen = true;
        }
        private void ComboBox_DropDownClosed_cote(object sender, EventArgs e)
        {
            isDropDownOpen = false;
        }
        private void ComboBox_PreviewMouseWheel_technique(object sender, MouseWheelEventArgs e)
        {
            if (!isDropDownOpen)
            {
                e.Handled = true;
            }
        }
        private void ComboBox_DropDownOpened_technique(object sender, EventArgs e)
        {
            isDropDownOpen = true;
        }
        private void ComboBox_DropDownClosed_technique(object sender, EventArgs e)
        {
            isDropDownOpen = false;
        }
        private void ComboBox_PreviewMouseWheel_machine(object sender, MouseWheelEventArgs e)
        {
            if (!isDropDownOpen)
            {
                e.Handled = true;
            }
        }
        private void ComboBox_DropDownOpened_machine(object sender, EventArgs e)
        {
            isDropDownOpen = true;
        }
        private void ComboBox_DropDownClosed_machine(object sender, EventArgs e)
        {
            isDropDownOpen = false;
        }
    }
}
