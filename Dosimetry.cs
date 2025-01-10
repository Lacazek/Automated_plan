/******************************************************************************
 * Nom du fichier : Dosimetry.cs
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

using Structure_optimisation;
using System;
using System.Linq;
using System.Windows;
using VMS.TPS.Common.Model.Types;

namespace Opti_Struct
{
    internal class Dosimetry
    {
        public Dosimetry()
        {
        }

        internal void LaunchDosimetry(UserInterfaceModel model)
        {
            model.Message = $"\n*************************Partie dosimétrie*************************\n";
            model.GetContext.PlanSetup.Course.Comment = "Dosimétrie réalisée automatiquement";
            model.GetContext.PlanSetup.Comment = "Dosimétrie réalisée automatiquement";
            string MyOptimizer, MyDoseCalculator, MLCID;

            if (model.UserSelection[3].ToUpper().Contains("HALCYON"))
            {
                // TBOX
                //MyOptimizer = "PO_18.0.0";
                //MyDoseCalculator = "AAA_18.0.1";
                MyOptimizer = "PO_18.0.1";
                MyDoseCalculator = "PHOTONS_AAA_18.0.1";
                MLCID = "SX2 MLC";
            }
            else
            {
                //TBOX
                MyOptimizer = "PO_15.6.06";
                MyDoseCalculator = "PHOTONS_AAA_15.6.06 V2";
                MLCID = "HHMA294";
            }
            model.GetContext.PlanSetup.SetCalculationModel(CalculationType.PhotonVolumeDose, MyDoseCalculator);
            model.GetContext.PlanSetup.SetCalculationModel(CalculationType.PhotonOptimization, MyOptimizer);

            // Dans un futur lointain, il sera possible de construire l'optimiseur avec les meilleures contraintes à utiliser pour le cas
            // reste à définir les paramètres pertinents (entrée) et les contraintes adaptés (sorties)
            ConstructMyOptimizer(model);

            model.Message = $"Les donnés d'optimisation de bases ont été chargées\n";

            if (MessageBox.Show("Les donnés d'optimisation de bases ont été chargées\nSouhaitez-vous lancer l'optimisation et le calcul de dose de manière automatique ?", "Information", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                model.Message = $"L'utilisateur à choisi de réaliser automatiquement l'optimisation et le calcul de la dose\n";
                try
                {
                    if (model.UserSelection[2].ToUpper().Equals("IMRT"))
                    {
                        model.Message = $"Dosimétrie IMRT en cours de réalisation ...\n";
                        model.GetContext.PlanSetup.SetCalculationOption(MyOptimizer, "General/GpuSettings/UseGPU", "Yes");
                        model.GetContext.ExternalPlanSetup.Optimize(300);
                        model.GetContext.ExternalPlanSetup.CalculateLeafMotionsAndDose();
                        model.GetContext.ExternalPlanSetup.Optimize(300, OptimizationOption.ContinueOptimizationWithPlanDoseAsIntermediateDose);
                        model.GetContext.ExternalPlanSetup.CalculateLeafMotionsAndDose();
                        //model.GetContext.ExternalPlanSetup.PlanNormalizationValue = model.GetContext.PlanSetup.GetDoseAtVolume(model.GetContext.StructureSet.Structures.First(x => x.Id.Equals(model.GetContext.PlanSetup.TargetVolumeID)), 99, VolumePresentation.Relative, DoseValuePresentation.Relative).Dose - 5;
                        model.Message = $"Dosimétrie réalisée avec succés\n";
                    }

                    else if (model.UserSelection[2].ToUpper().Equals("3D"))
                    {
                        model.Message = $"Dosimétrie 3D en cours de réalisation ...\n";
                        model.GetContext.ExternalPlanSetup.CalculateDose();
                        model.Message = $"Dosimétrie réalisée avec succés\n";
                    }

                    else
                    {
                        model.GetContext.PlanSetup.SetCalculationOption(MyOptimizer, "VMAT/ApertureShapeController", "Moderate");
                        model.Message = $"Dosimétrie En Arcthérapie en cours de réalisation ...\n";
                        model.GetContext.ExternalPlanSetup.OptimizeVMAT();
                        model.GetContext.ExternalPlanSetup.CalculateDose();
                        model.GetContext.ExternalPlanSetup.OptimizeVMAT(new OptimizationOptionsVMAT(OptimizationOption.ContinueOptimizationWithPlanDoseAsIntermediateDose, MLCID));
                        model.GetContext.ExternalPlanSetup.CalculateDose();
                        //model.GetContext.ExternalPlanSetup.PlanNormalizationValue = model.GetContext.PlanSetup.GetDoseAtVolume(model.GetContext.StructureSet.Structures.First(x=>x.Id.Equals(model.GetContext.PlanSetup.TargetVolumeID)),99.5,VolumePresentation.Relative,DoseValuePresentation.Relative).Dose - 0.5;
                        model.Message = $"Dosimétrie réalisée avec succés\n";
                    }
                }
                catch (Exception ex)
                {
                    model.Message = $"Erreur dans la réalisation de la dosimétrie";
                    model.Message = ex.Message + "\n";
                }
            }
        }

        internal void ConstructMyOptimizer(UserInterfaceModel model)
        {
            foreach (var objective in model.GetContext.ExternalPlanSetup.OptimizationSetup.Objectives)
                model.GetContext.ExternalPlanSetup.OptimizationSetup.RemoveObjective(objective);

            switch (model.UserSelection[2].ToUpper())
            {
                #region IMRT
                case "IMRT":
                    switch (model.UserSelection[0].Split('_').LastOrDefault().ToUpper())
                    {
                        #region SEIN
                        case "SEIN":

                            // Paramètres faisceaux
                            foreach (var beam in model.GetContext.PlanSetup.Beams)
                            {
                                if (!beam.IsSetupField)
                                    model.GetContext.PlanSetup.OptimizationSetup.AddBeamSpecificParameter(beam, 300, 300, false);
                            }

                            // NTO
                            model.GetContext.PlanSetup.OptimizationSetup.AddNormalTissueObjective(300, 3, 100, 30, 0.1);

                            // Cible
                            // CTV dosi
                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CTV_DOSI") || x.Id.ToUpper().Equals("CTV_DOSI_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.02, DoseValue.DoseUnit.Gy), 2, 400);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CTV_DOSI") || x.Id.ToUpper().Equals("CTV_DOSI_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 50, 400);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CTV_DOSI") || x.Id.ToUpper().Equals("CTV_DOSI_AUTO")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 50, 400);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CTV_DOSI") || x.Id.ToUpper().Equals("CTV_DOSI_AUTO")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 99, 400);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CTV_DOSI") || x.Id.ToUpper().Equals("CTV_DOSI_AUTO")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 100, 400);
                            }
                            catch { }

                            // PTV dosi
                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("PTV_DOSI") || x.Id.ToUpper().Equals("PTV_DOSI_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.02, DoseValue.DoseUnit.Gy), 2, 400);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("PTV_DOSI") || x.Id.ToUpper().Equals("PTV_DOSI_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 50, 400);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("PTV_DOSI") || x.Id.ToUpper().Equals("PTV_DOSI_AUTO")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 50, 400);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("PTV_DOSI") || x.Id.ToUpper().Equals("PTV_DOSI_AUTO")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 99, 400);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("PTV_DOSI") || x.Id.ToUpper().Equals("PTV_DOSI_AUTO")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 100, 400);
                            }
                            catch { }

                            // OAR
                            // Sein contro
                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("SEIN CONTRO") || x.Id.ToUpper().Equals("SEIN CONTRO_AUTO")).First(), new DoseValue(1, DoseValue.DoseUnit.Gy), 170);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("SEIN CONTRO") || x.Id.ToUpper().Equals("SEIN CONTRO_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(3, DoseValue.DoseUnit.Gy), 0, 250);
                            }
                            catch { }

                            // CE
                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("CONTOUR EXTERNE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.02, DoseValue.DoseUnit.Gy), 0, 1000);
                            }
                            catch { }

                            // Ring sein
                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_RING SEIN")).First(), new DoseValue(30, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_RING SEIN")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.95, DoseValue.DoseUnit.Gy), 0, 180);
                                model.GetContext.PlanSetup.OptimizationSetup.AddEUDObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_RING SEIN")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.9, DoseValue.DoseUnit.Gy), 40, 180);
                            }
                            catch { }

                            // CE - (PTV +1cm)
                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.Replace(" ", "").ToUpper().Contains("CE-(PTV+1CM)") || x.Id.ToUpper().Equals("CE-(PTV+1CM)_AUTO")).First(), new DoseValue(10, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.Replace(" ", "").ToUpper().Contains("CE-(PTV+1CM)") || x.Id.ToUpper().Equals("CE-(PTV+1CM)_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.9, DoseValue.DoseUnit.Gy), 0, 180);
                                model.GetContext.PlanSetup.OptimizationSetup.AddEUDObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.Replace(" ", "").ToUpper().Contains("CE-(PTV+1CM)") || x.Id.ToUpper().Equals("CE-(PTV+1CM)_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.72, DoseValue.DoseUnit.Gy), 40, 180);
                            }
                            catch { }

                            switch (model.UserSelection[1].ToUpper())
                            {
                                case "DROIT":

                                    // Coeur
                                    try
                                    {
                                        model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("COEUR")).First(), new DoseValue(0.5, DoseValue.DoseUnit.Gy), 200);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("COEUR")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(1, DoseValue.DoseUnit.Gy), 0, 200);
                                    }
                                    catch { }

                                    // Poumon D
                                    try
                                    {
                                        model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("POUMON_D")).First(), new DoseValue(3, DoseValue.DoseUnit.Gy), 250);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("POUMON_D")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.95, DoseValue.DoseUnit.Gy), 0, 200);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("POUMON_D")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.86, DoseValue.DoseUnit.Gy), 2, 200);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("POUMON_D")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.6, DoseValue.DoseUnit.Gy), 5, 200);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("POUMON_D")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.35, DoseValue.DoseUnit.Gy), 10, 200);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("POUMON_D")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.22, DoseValue.DoseUnit.Gy), 25, 200);
                                    }
                                    catch { }

                                    // Poumon G
                                    try
                                    {
                                        model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("POUMON_G")).First(), new DoseValue(1, DoseValue.DoseUnit.Gy), 150);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("POUMON_G")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(2, DoseValue.DoseUnit.Gy), 0, 150);
                                    }
                                    catch { }

                                    // Sein G
                                    try
                                    {
                                        // model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("SEIN_G")).First(), new DoseValue(1, DoseValue.DoseUnit.Gy), 170);
                                        //model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("SEIN_G")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(3, DoseValue.DoseUnit.Gy), 0, 250);
                                        //model.GetContext.PlanSetup.OptimizationSetup.AddEUDObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("SEIN_G")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(3, DoseValue.DoseUnit.Gy), 40, 200);
                                    }

                                    catch { }

                                    // Tete humérale D
                                    try
                                    {
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("TETE_HUMERUS_D")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 250);
                                    }
                                    catch { }

                                    // Foie
                                    try
                                    {
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("FOIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.5, DoseValue.DoseUnit.Gy), 0, 150);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("FOIE")).First(), new DoseValue(3, DoseValue.DoseUnit.Gy), 180);
                                    }

                                    catch { }
                                    break;

                                case "GAUCHE":

                                    // Coeur
                                    try
                                    {
                                        model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("COEUR")).First(), new DoseValue(1, DoseValue.DoseUnit.Gy), 150);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("COEUR")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 220);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddEUDObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("COEUR")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 40, 200);
                                    }
                                    catch { }

                                    try
                                    {
                                        // Poumon G
                                        model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("POUMON_G")).First(), new DoseValue(3, DoseValue.DoseUnit.Gy), 250);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("POUMON_G")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.95, DoseValue.DoseUnit.Gy), 0, 200);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("POUMON_G")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.90, DoseValue.DoseUnit.Gy), 2, 200);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("POUMON_G")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.7, DoseValue.DoseUnit.Gy), 5, 200);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("POUMON_G")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.40, DoseValue.DoseUnit.Gy), 10, 200);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("POUMON_G")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.2, DoseValue.DoseUnit.Gy), 25, 200);
                                    }
                                    catch { }

                                    // Poumon D
                                    try
                                    {
                                        model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("POUMON_D")).First(), new DoseValue(1, DoseValue.DoseUnit.Gy), 150);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("POUMON_D")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(2, DoseValue.DoseUnit.Gy), 0, 150);
                                    }
                                    catch { }

                                    // IVA
                                    try
                                    {
                                        model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("IVA")).First(), new DoseValue(1, DoseValue.DoseUnit.Gy), 150);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("IVA)")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(5, DoseValue.DoseUnit.Gy), 0, 200);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddEUDObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("IVA)")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(5, DoseValue.DoseUnit.Gy), 40, 200);
                                    }
                                    catch { }

                                    // Sein D
                                    try
                                    {
                                        model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("SEIN_D")).First(), new DoseValue(1, DoseValue.DoseUnit.Gy), 170);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("SEIN_D")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(3, DoseValue.DoseUnit.Gy), 0, 250);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddEUDObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("SEIN_D")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(4, DoseValue.DoseUnit.Gy), 40, 180);
                                    }
                                    catch { }

                                    // tete humérale G
                                    try
                                    {
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("TETE_HUMERUS_G")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 250);
                                    }

                                    catch { }
                                    break;
                            }
                            break;

                        default:
                            MessageBox.Show("Erreur lors de la construction de l'optimiseur");
                            break;
                            #endregion
                    }
                    break;
                #endregion
                #region ARC
                case "ARCTHÉRAPIE":

                    switch (model.UserSelection[0].Split('_').LastOrDefault().ToUpper())//+ " " + model.UserSelection[1].ToUpper())
                    {
                        case "PROSTATE":

                            break;

                        case "SEIN": // En cours

                            // NTO
                            model.GetContext.PlanSetup.OptimizationSetup.AddNormalTissueObjective(300, 3, 100, 30, 0.1);

                            // Cible
                            // PTV dosi
                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("PTV_DOSI") || x.Id.ToUpper().Equals("PTV_SEIN_AUTO")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("PTV_DOSI") || x.Id.ToUpper().Equals("PTV_SEIN_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("PTV_CMI") || x.Id.ToUpper().Equals("PTV_CMI_AUTO")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("PTV_CMI") || x.Id.ToUpper().Equals("PTV_CMI_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }


                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("PTV_CLAV") || x.Id.ToUpper().Equals("PTV_CLAV_AUTO")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("PTV_CLAV") || x.Id.ToUpper().Equals("PTV_CLAV_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("PTV_AXILLAIRE") || x.Id.ToUpper().Equals("PTV_AXILLAIRE_AUTO")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("PTV_AXILLAIRE") || x.Id.ToUpper().Equals("PTV_AXILLAIRE_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("PTV_BOOST") || x.Id.ToUpper().Equals("PTV_BOOST_AUTO")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.32, DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("PTV_BOOST") || x.Id.ToUpper().Equals("PTV_BOOST_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.34, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            break;

                        #region Pelvis gynéco + GG

                        case "PELVIS GYN + GG":

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV T")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV T")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV N")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(double.Parse(model.GetPrescription2[4]), DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV N")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(double.Parse(model.GetPrescription2[4]) * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            // OARs
                            try
                            {
                                //model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("RECTUM")).First(), new DoseValue(30, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("RECTUM")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(50, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), new DoseValue(30, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(45, DoseValue.DoseUnit.Gy), 0, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(40, DoseValue.DoseUnit.Gy), 15, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(30, DoseValue.DoseUnit.Gy), 30, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 70, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), new DoseValue(20, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(37, DoseValue.DoseUnit.Gy), 0, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(25, DoseValue.DoseUnit.Gy), 15, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(15, DoseValue.DoseUnit.Gy), 30, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(10, DoseValue.DoseUnit.Gy), 70, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("FEMUR D")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(25, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("FEMUR G")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(25, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("MOELLE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(15, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CANAL_MEDULLAIRE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {

                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CAVITE_PERITONEALE")).First(), new DoseValue(5, DoseValue.DoseUnit.Gy), 180);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CAVITE_PERITONEALE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {

                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("DIGESTIF")).First(), new DoseValue(5, DoseValue.DoseUnit.Gy), 180);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("DIGESTIF")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            break;
                        #endregion

                        #region Pelvis Gyneco
                        case "PELVIS GYN":

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV T")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV T")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            // OARs
                            try
                            {
                                //model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("RECTUM")).First(), new DoseValue(30, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("RECTUM")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(50, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), new DoseValue(30, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(45, DoseValue.DoseUnit.Gy), 0, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(40, DoseValue.DoseUnit.Gy), 15, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(30, DoseValue.DoseUnit.Gy), 30, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 70, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), new DoseValue(20, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(37, DoseValue.DoseUnit.Gy), 0, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(25, DoseValue.DoseUnit.Gy), 15, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(15, DoseValue.DoseUnit.Gy), 30, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(10, DoseValue.DoseUnit.Gy), 70, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("FEMUR D")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(25, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("FEMUR G")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(25, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("MOELLE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(15, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CANAL_MEDULLAIRE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {

                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CAVITE_PERITONEALE")).First(), new DoseValue(5, DoseValue.DoseUnit.Gy), 180);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CAVITE_PERITONEALE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {

                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("DIGESTIF")).First(), new DoseValue(5, DoseValue.DoseUnit.Gy), 180);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("DIGESTIF")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            break;
                        #endregion

                        #region Pelvis
                        case "PELVIS":

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV T")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV T")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            // OARs
                            try
                            {
                                //model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("RECTUM")).First(), new DoseValue(30, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("RECTUM")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(50, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), new DoseValue(30, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(45, DoseValue.DoseUnit.Gy), 0, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(40, DoseValue.DoseUnit.Gy), 15, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(30, DoseValue.DoseUnit.Gy), 30, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 70, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), new DoseValue(20, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(37, DoseValue.DoseUnit.Gy), 0, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(25, DoseValue.DoseUnit.Gy), 15, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(15, DoseValue.DoseUnit.Gy), 30, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(10, DoseValue.DoseUnit.Gy), 70, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("FEMUR D")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(25, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("FEMUR G")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(25, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("MOELLE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(15, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CANAL_MEDULLAIRE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {

                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CAVITE_PERITONEALE")).First(), new DoseValue(5, DoseValue.DoseUnit.Gy), 180);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CAVITE_PERITONEALE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {

                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("DIGESTIF")).First(), new DoseValue(5, DoseValue.DoseUnit.Gy), 180);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("DIGESTIF")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            break;
                        #endregion

                        #region pelvis + GG
                        case "PELVIS + GG":

                            // Cbles
                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV T")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV T")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV N")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(double.Parse(model.GetPrescription2[4]), DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV N")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(double.Parse(model.GetPrescription2[4]) * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            // OARs
                            try
                            {
                                //model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("RECTUM")).First(), new DoseValue(30, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("RECTUM")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(50, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), new DoseValue(30, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(45, DoseValue.DoseUnit.Gy), 0, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(40, DoseValue.DoseUnit.Gy), 15, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(30, DoseValue.DoseUnit.Gy), 30, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 70, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), new DoseValue(20, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(37, DoseValue.DoseUnit.Gy), 0, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(25, DoseValue.DoseUnit.Gy), 15, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(15, DoseValue.DoseUnit.Gy), 30, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(10, DoseValue.DoseUnit.Gy), 70, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("FEMUR D")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(25, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("FEMUR G")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(25, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("MOELLE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(15, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CANAL_MEDULLAIRE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {

                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CAVITE_PERITONEALE")).First(), new DoseValue(5, DoseValue.DoseUnit.Gy), 180);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CAVITE_PERITONEALE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {

                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("DIGESTIF")).First(), new DoseValue(5, DoseValue.DoseUnit.Gy), 180);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("DIGESTIF")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            break;
                        #endregion

                        #region Pelvis + GG + boost GG
                        case "PELVIS + GG + BOOST GG":

                            // Cbles
                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV T")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV T")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV GG")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(double.Parse(model.GetPrescription2[4]), DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV GG")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(double.Parse(model.GetPrescription2[4]) * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV N")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(double.Parse(model.GetPrescription2[6]), DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV N")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(double.Parse(model.GetPrescription2[6]) * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }


                            // OARs
                            try
                            {
                                //model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("RECTUM")).First(), new DoseValue(30, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("RECTUM")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(50, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), new DoseValue(30, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(45, DoseValue.DoseUnit.Gy), 0, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(40, DoseValue.DoseUnit.Gy), 15, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(30, DoseValue.DoseUnit.Gy), 30, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 70, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), new DoseValue(20, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(37, DoseValue.DoseUnit.Gy), 0, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(25, DoseValue.DoseUnit.Gy), 15, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(15, DoseValue.DoseUnit.Gy), 30, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(10, DoseValue.DoseUnit.Gy), 70, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("FEMUR D")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(25, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("FEMUR G")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(25, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("MOELLE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(15, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CANAL_MEDULLAIRE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {

                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CAVITE_PERITONEALE")).First(), new DoseValue(5, DoseValue.DoseUnit.Gy), 180);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CAVITE_PERITONEALE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {

                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("DIGESTIF")).First(), new DoseValue(5, DoseValue.DoseUnit.Gy), 180);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("DIGESTIF")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            break;
                        #endregion

                        #region Pelvis Gyn + GG + boost GG
                        case "PELVIS GYN + GG + BOOST GG":

                            // Cbles
                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV T")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV T")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV GG")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(double.Parse(model.GetPrescription2[4]), DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV GG")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(double.Parse(model.GetPrescription2[4]) * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV N")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(double.Parse(model.GetPrescription2[6]), DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV N")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(double.Parse(model.GetPrescription2[6]) * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }


                            // OARs
                            try
                            {
                                //model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("RECTUM")).First(), new DoseValue(30, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("RECTUM")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(50, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), new DoseValue(30, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(45, DoseValue.DoseUnit.Gy), 0, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(40, DoseValue.DoseUnit.Gy), 15, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(30, DoseValue.DoseUnit.Gy), 30, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("VESSIE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 70, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), new DoseValue(20, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(37, DoseValue.DoseUnit.Gy), 0, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(25, DoseValue.DoseUnit.Gy), 15, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(15, DoseValue.DoseUnit.Gy), 30, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT_AUTO")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(10, DoseValue.DoseUnit.Gy), 70, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("FEMUR D")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(25, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("FEMUR G")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(25, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("MOELLE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(15, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CANAL_MEDULLAIRE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {

                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CAVITE_PERITONEALE")).First(), new DoseValue(5, DoseValue.DoseUnit.Gy), 180);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("CAVITE_PERITONEALE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {

                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("DIGESTIF")).First(), new DoseValue(5, DoseValue.DoseUnit.Gy), 180);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("DIGESTIF")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            break;
                        #endregion

                        case "":


                            break;
                    }

                    model.GetContext.PlanSetup.OptimizationSetup.AddNormalTissueObjective(300, 3, 100, 30, 0.15);
                    break;
                #endregion
                #region Stéréotaxie
                case "STEREOTAXIE":
                    switch (model.UserSelection[0].Split('_').LastOrDefault().ToUpper() + " " + model.UserSelection[1].ToUpper())
                    {
                        #region Crane
                        case "CRANE":

                            break;
                        #endregion

                        #region Poumon
                        case "POUMON":

                            break;
                        #endregion

                        #region Poumon 4D
                        case "POUMON4D":

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_ITV")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.01, DoseValue.DoseUnit.Gy), 100, 400);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_ITV")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.02, DoseValue.DoseUnit.Gy), 0, 400);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.99, DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.01, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING1")).First(), new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.75, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING1")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.8, DoseValue.DoseUnit.Gy) * 0.95, 0, 180);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING2")).First(), new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.4, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING2")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.8, DoseValue.DoseUnit.Gy), 0, 180);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING3")).First(), new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.2, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING3")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.6, DoseValue.DoseUnit.Gy), 0, 180);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("MOELLE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.1, DoseValue.DoseUnit.Gy), 0, 180);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("PRV_MOELLE+1")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.15, DoseValue.DoseUnit.Gy), 0, 180);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("PRV_MOELLE+2")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.2, DoseValue.DoseUnit.Gy), 0, 180);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("PRV_MOELLE+3")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.25, DoseValue.DoseUnit.Gy), 0, 180);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("OESOPHAGE")).First(), new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.1, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("OESOPHAGE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.15, DoseValue.DoseUnit.Gy), 0, 0);
                            }
                            catch { }


                            break;
                        #endregion

                        #region Stéréo 1 loc
                        case "GENERAL/1LOC":
                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING1")).First(), new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.75, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING1")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.8, DoseValue.DoseUnit.Gy) * 0.95, 0, 180);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING2")).First(), new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.4, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING2")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.8, DoseValue.DoseUnit.Gy), 0, 180);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING3")).First(), new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.2, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING3")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.6, DoseValue.DoseUnit.Gy), 0, 180);
                            }
                            catch { }

                            break;
                        #endregion

                        #region Stéréo 2 locs
                        // a editer pour 2 cibles
                        case "GENERAL/2LOC":

                            // cible1
                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            // cible2
                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING1")).First(), new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.75, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING1")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.8, DoseValue.DoseUnit.Gy) * 0.95, 0, 180);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING2")).First(), new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.4, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING2")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.8, DoseValue.DoseUnit.Gy), 0, 180);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING3")).First(), new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.2, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING3")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.6, DoseValue.DoseUnit.Gy), 0, 180);
                            }

                            catch { }

                            break;
                        #endregion

                        #region Stéréo 3 locs
                        // a editer pour 3 cibles
                        case "GENERAL/3LOC":
                            // cible1
                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            // cible2
                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            // cible3
                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING1")).First(), new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.75, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING1")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.8, DoseValue.DoseUnit.Gy) * 0.95, 0, 180);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING2")).First(), new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.4, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING2")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.8, DoseValue.DoseUnit.Gy), 0, 180);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING3")).First(), new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.2, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_RING3")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.6, DoseValue.DoseUnit.Gy), 0, 180);
                            }

                            catch { }

                            break;
                        #endregion

                        case "":

                            model.GetContext.PlanSetup.OptimizationSetup.AddNormalTissueObjective(300, 3, 100, 30, 0.2);
                            break;
                    }
                    break;
            }
            #endregion
        }
    }
}
