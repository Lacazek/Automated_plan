/******************************************************************************
 * Nom du fichier : UserInterface_Check.xaml.cs & UserInterface_Check.xaml
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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace Structure_optimisation
{
    /// <summary>
    /// Logique d'interaction pour DoseCheck.xaml
    /// </summary>
    public partial class UserInterface_Check : Window
    {
        private UserInterfaceModel _model;
        private bool isDropDownOpen = false;

        internal UserInterface_Check(UserInterfaceModel model)
        {
            InitializeComponent();
            _model = model;
            DataContext = _model;

            try
            {
                Patient_Info.Text = $" Patient : {_model.GetContext.Patient.Name} {_model.GetContext.Patient.DateOfBirth}\n" +
                            $"Oncologue principal : {_model.GetContext.Patient.PrimaryOncologistName} {_model.GetContext.Patient.PrimaryOncologistId}\n" +
                            $"Intention du course : {_model.GetContext.Course.Intent}\n" +
                            $"Id du course : {_model.GetContext.Course.Id}\n" +
                            $"Statut du course : {_model.GetContext.Course.ClinicalStatus}\n" +
                            $"Nom du plan : {_model.GetContext.PlanSetup.Id}\n" +
                            $"Commentaire : {_model.GetContext.Patient.Comment}";

                OK_Button.Visibility = Visibility.Collapsed;

   
                foreach (var item in Directory.GetFiles(Path.Combine(model.GetCheckPath,@"Template_dosi")))
                {
                    _model.AddFile = System.IO.Path.GetFileNameWithoutExtension(item);
                }

                _model.GetList.Sort();

                foreach (var file in _model.GetList)
                {
                    Box_File.Items.Add(file);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Box_File.SelectedItem != null)
            {
                OK_Button.Visibility = Visibility.Visible;
            }
            else
                OK_Button.Visibility = Visibility.Collapsed;

        }
        private void Button_Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _model.UserFileCheck = (string)Box_File.SelectedItem;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.Close();
            }
        }
        private void ComboBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!isDropDownOpen)
            {
                e.Handled = true;
            }
        }
        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            isDropDownOpen = true;
        }
        private void ComboBox_DropDownClosed(object sender, EventArgs e)
        {
            isDropDownOpen = false;
        }
    }
}
