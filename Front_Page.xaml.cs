/******************************************************************************
 * Nom du fichier : Front_Page.xaml.cs
 * Auteur         : LACAZE Killian
 * Date de création : [09/01/2025]
 * Description    : [Brève description du contenu ou de l'objectif du code]
 *
 * Droits d'auteur © [2025], [LACAZE.K].
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
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Opti_Struct
{
    /// <summary>
    /// Logique d'interaction pour Front_Page.xaml
    /// </summary>
    /// 
    public partial class Front_Page : Window
    {

        public event EventHandler<string> Signal;
        private bool _isExpanded = false;

        private double _defaultWidth_autocontour;
        private double _defaultHeight_autocontour;

        private double _defaultWidth_autoplanning;
        private double _defaultHeight_autoplanning;

        private double _defaultWidth_autoeval;
        private double _defaultHeight_autoeval;

        private double _defaultWidth_autotransfert;
        private double _defaultHeight_autotransfert;

        public Front_Page(string string_Log_Auto_Contour, string string_Log_Auto_Planning, string string_Log_Auto_Evaluation, string string_Log_Auto_Transfert)
        {
            InitializeComponent();
            Set_Log_Auto_Contour = string_Log_Auto_Contour;
            Set_Log_Auto_Planning = string_Log_Auto_Planning;
            Set_Log_Auto_Evaluation = string_Log_Auto_Evaluation;
            Set_Log_Auto_Transfert = string_Log_Auto_Transfert;

            this.Loaded += Front_Page_Loaded;
        }

        private void Front_Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Maintenant, les dimensions devraient être accessibles
            _defaultWidth_autocontour = Log_Auto_Contour.ActualWidth;
            _defaultHeight_autocontour = Log_Auto_Contour.ActualHeight;

            _defaultWidth_autoplanning = Log_Auto_Planning.ActualWidth;
            _defaultHeight_autoplanning = Log_Auto_Planning.ActualHeight;

            _defaultWidth_autoeval = Log_Auto_Evaluation.ActualWidth;
            _defaultHeight_autoeval = Log_Auto_Evaluation.ActualHeight;

            _defaultWidth_autotransfert = Log_Auto_Transfert.ActualWidth;
            _defaultHeight_autotransfert = Log_Auto_Transfert.ActualHeight;
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            Signal?.Invoke(this, "Close");
            this.Close();
        }

        private void Auto_Contour_Click(object sender, RoutedEventArgs e)
        {
            Signal?.Invoke(this, "0");
            this.Close();
        }

        private void Auto_Planning_Click(object sender, RoutedEventArgs e)
        {
            Signal?.Invoke(this, "1");
            this.Close();
        }

        private void Auto_Evaluation_Click(object sender, RoutedEventArgs e)
        {
            Signal?.Invoke(this, "2");
            this.Close();
        }

        private void Auto_Transfert_Click(object sender, RoutedEventArgs e)
        {
            Signal?.Invoke(this, "3");
            this.Close();
        }

        #region Get and Set
        internal string Set_Log_Auto_Contour
        {
            set
            {
                Log_Auto_Contour.Text = string.Empty;
                Log_Auto_Contour.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                Log_Auto_Contour.Text = value;
            }
        }
        internal string Set_Log_Auto_Planning
        {
            set
            {
                Log_Auto_Planning.Text = string.Empty;
                Log_Auto_Planning.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255));
                Log_Auto_Planning.Text = value;
            }
        }
        internal string Set_Log_Auto_Evaluation
        {
            set
            {
                Log_Auto_Evaluation.Text = string.Empty;
                Log_Auto_Evaluation.Foreground = new SolidColorBrush(Color.FromArgb(255, 5, 132, 5));
                Log_Auto_Evaluation.Text = value;
            }
        }
        internal string Set_Log_Auto_Transfert
        {
            set
            {
                Log_Auto_Transfert.Text = string.Empty;
                Log_Auto_Transfert.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                Log_Auto_Transfert.Text = value;
            }
        }

        private void Log_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

            var textBlock = (TextBlock)sender;
            // Si vous voulez agrandir le TextBlock, vous pouvez changer sa taille
            textBlock.TextWrapping = TextWrapping.NoWrap;
            //textBlock.Width = double.NaN;  // La largeur devient infinie pour s'ajuster au texte
            textBlock.TextTrimming = TextTrimming.None;  // Ne pas afficher les "..."

            double width, height;

            width = textBlock.Name == "Log_Auto_Contour" ? _defaultWidth_autocontour :
                    textBlock.Name == "Log_Auto_Planning" ? _defaultWidth_autoplanning :
                    textBlock.Name == "Log_Auto_Evaluation" ? _defaultWidth_autoeval : _defaultWidth_autotransfert;

            height = textBlock.Name == "Log_Auto_Contour" ? _defaultHeight_autocontour :
                     textBlock.Name == "Log_Auto_Planning" ? _defaultHeight_autoplanning :
                     textBlock.Name == "Log_Auto_Evaluation" ? _defaultHeight_autoeval : _defaultHeight_autotransfert;


            if (_isExpanded)
            {

                textBlock.Width = width;
                textBlock.Height = height;
                _isExpanded = false;
                Panel.SetZIndex(textBlock, 0);
            }
            else
            {
                textBlock.Width = width * 1;
                textBlock.Height = height * 2;
                _isExpanded = true;
                Panel.SetZIndex(textBlock, 1);
            }
        }
        #endregion



    }
}
