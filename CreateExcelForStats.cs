/******************************************************************************
 * Nom du fichier : CreateExcelForStats.cs
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
using System.Collections.Generic;
using System.IO;
using System.Windows;
using VMS.TPS.Common.Model.API;

namespace Structure_optimisation
{

    // A améliorer : 
    // assurer que les valeurs recherchées apparaissent dans les colonnes en haut
    // et que les résultats sont bien associées aux résultats.
    // diminuer le nombre de chiffres significatifs

    internal class CreateExcelForStats
    {
        private StreamWriter _excelForStats;
        private Patient _patient;
        private Course _course;
        private PlanSetup _plan;

        internal CreateExcelForStats(UserInterfaceModel _model)
        {
            try
            {
                _patient = _model.GetContext.Patient;
                _course = _model.GetContext.Course;
                _plan = _model.GetContext.PlanSetup;
                if (!Directory.Exists(Path.Combine(_model.GetCheckPath,@"out")))
                    Directory.CreateDirectory(_model.GetCheckPath + @"out");
                _excelForStats = new StreamWriter(_model.GetCheckPath + @"\out\Data_" + _patient.LastName + "_" + _patient.FirstName + ".csv");
                _excelForStats.WriteLine("patientID;courseID;planID;TotalDose;Dose/#;Fractions;Structure;Dose max; Dose moyenne; Dose mediane; Dose min");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur dans la construction du fichier excel\n" + ex.Message);
            }
        }

        internal void Fill(Dictionary<string, string> _results)
        {

            string currentOrgan = "";
            try
            {
                foreach (var value in _results)
                {
                    string organ = value.Key.Split('/')[0];

                    if (!organ.Equals(currentOrgan))
                    {
                        if (!string.IsNullOrEmpty(currentOrgan))
                        {
                            _excelForStats.WriteLine();
                        }
                        _excelForStats.Write("{0};{1};{2};{3};{4};{5};{6}",
                            _patient.Id, _course.Id, _plan.Id, _plan.TotalDose, _plan.DosePerFraction, _plan.NumberOfFractions, organ);
                        _excelForStats.Write(";");
                        currentOrgan = organ;
                    }
                    else
                    {
                        _excelForStats.Write(";");
                    }
                    _excelForStats.Write("{0:0.00}", value.Value);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


            _excelForStats.WriteLine();
        }

        internal void Close()
        {
            _excelForStats.Close();
        }
    }
}

