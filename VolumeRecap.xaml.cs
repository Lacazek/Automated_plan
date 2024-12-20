/******************************************************************************
 * Nom du fichier : VolumeRecap.xaml.cs
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


using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Opti_Struct
{
    /// <summary>
    /// Logique d'interaction pour VolumeRecap.xaml
    /// </summary>
    public partial class VolumeRecap : Window
    {
        public VolumeRecap(string information, string erreur, string file)
        {
            InitializeComponent();
            recap_Title.Text = $"Protocole utilisé: {file}";

            // Effacer les anciennes inlines (si existantes)
            recap_Body.Inlines.Clear();

            // Ajouter le texte des informations (en vert)
            var infoRun = new Run(information)
            {
                Foreground = Brushes.Green
            };

            // Ajouter le texte des erreurs (en rouge)
            var errorRun = new Run("\n\n" + erreur)
            {
                Foreground = Brushes.Red
            };

            // Ajouter les deux au TextBlock
            recap_Body.Inlines.Add(infoRun);
            recap_Body.Inlines.Add(errorRun);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
