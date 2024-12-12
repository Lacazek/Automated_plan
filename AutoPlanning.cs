/******************************************************************************
 * Nom du fichier : Autoplanning.cs
 * Auteur         : LACAZE Killian
 * Date de cr�ation : [02/10/2024]
 * Description    : [Br�ve description du contenu ou de l'objectif du code]
 *
 * Droits d'auteur � [2024], [LACAZE.K].
 * Tous droits r�serv�s.
 * 
 * Ce code a �t� d�velopp� exclusivement par LACAZE Killian. Toute utilisation de ce code 
 * est soumise aux conditions suivantes :
 * 
 * 1. L'utilisation de ce code est autoris�e uniquement � titre personnel ou professionnel, 
 *    mais sans modification de son contenu.
 * 2. Toute redistribution, copie, ou publication de ce code sans l'accord explicite 
 *    de l'auteur est strictement interdite.
 * 3. L'auteur assume la responsabilit� de l'utilisation de ce code dans ses propres projets.
 * 
 * CE CODE EST FOURNI "EN L'�TAT", SANS AUCUNE GARANTIE, EXPRESSE OU IMPLICITE. 
 * L'AUTEUR D�CLINE TOUTE RESPONSABILIT� POUR TOUT DOMMAGE OU PERTE R�SULTANT 
 * DE L'UTILISATION DE CE CODE.
 *
 * Toute utilisation non autoris�e ou attribution incorrecte de ce code est interdite.
 ******************************************************************************/



using System.Reflection;
using System.Runtime.CompilerServices;
using VMS.TPS.Common.Model.API;
using Structure_optimisation;
using System.Windows;

// This line is necessary to "write" in database
[assembly: ESAPIScript(IsWriteable = true)]
[assembly: AssemblyVersion("2.0.0.1")]

namespace VMS.TPS
{
    public class Script
    {
        public Script()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext context)
        {
            context.Patient.BeginModifications();
            UserInterfaceModel model = new UserInterfaceModel(context);

            try
            {
                model.IsOpened(true);
                MessageBox.Show($"AutoPlanning termin�, v�rifiez la dosim�trie", "Information", MessageBoxButton.OKCancel, MessageBoxImage.Information);
            }
            catch
            {
                model.IsOpened(true);
                MessageBox.Show($"Une erreur est survenue", "Erreur", MessageBoxButton.OKCancel, MessageBoxImage.Error);
            }
        }
    }
}

