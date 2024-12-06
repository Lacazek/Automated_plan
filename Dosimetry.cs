using Structure_optimisation;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VMS.TPS.Common.Model.API;
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
            model.GetContext.PlanSetup.Course.Comment = "Dosimétrie réalisée automatiquement";
            model.GetContext.PlanSetup.Comment = "Dosimétrie réalisée automatiquement";

            if (model.UserSelection[3].ToUpper().Contains("HALCYON"))
            {
                ConstructMyOptimizer(model);
                //TBOX
                model.GetContext.PlanSetup.SetCalculationModel(CalculationType.PhotonVolumeDose, "AAA_18.0.1");
                model.GetContext.PlanSetup.SetCalculationModel(CalculationType.PhotonOptimization, "PO_18.0.0");
                //model.GetContext.PlanSetup.SetCalculationModel(CalculationType.PhotonVolumeDose, "PHOTONS_AAA_18.0.1");
                //model.GetContext.PlanSetup.SetCalculationModel(CalculationType.PhotonOptimization, "PO_18.0.1");
            }
            else
            {
                ConstructMyOptimizer(model);
                model.GetContext.PlanSetup.SetCalculationModel(CalculationType.PhotonVolumeDose, "PHOTONS_AAA_15.6.06 V2");
                model.GetContext.PlanSetup.SetCalculationModel(CalculationType.PhotonOptimization, "PO_15.6.06");

            }
            if (model.UserSelection[2].ToUpper().Equals("IMRT"))
            {
                model.GetContext.PlanSetup.SetCalculationOption("PO_18.0.1", "General/GpuSettings/UseGPU", "Yes");
                model.GetContext.ExternalPlanSetup.Optimize(300);
                model.GetContext.ExternalPlanSetup.CalculateLeafMotionsAndDose();
                model.GetContext.ExternalPlanSetup.Optimize(300, OptimizationOption.ContinueOptimizationWithPlanDoseAsIntermediateDose);
                model.GetContext.ExternalPlanSetup.CalculateLeafMotionsAndDose();
            }
            else
            {
                model.GetContext.ExternalPlanSetup.OptimizeVMAT();
                model.GetContext.ExternalPlanSetup.CalculateDose();
            }
        }

        internal void ConstructMyOptimizer(UserInterfaceModel model)
        {
            foreach (var objective in model.GetContext.ExternalPlanSetup.OptimizationSetup.Objectives)
                model.GetContext.ExternalPlanSetup.OptimizationSetup.RemoveObjective(objective);

            switch (model.UserSelection[2])
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
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_RING SEIN")).First(), new DoseValue(35, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_RING SEIN")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.95, DoseValue.DoseUnit.Gy), 0, 180);
                                model.GetContext.PlanSetup.OptimizationSetup.AddEUDObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_RING SEIN")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.9, DoseValue.DoseUnit.Gy), 40, 180);
                            }
                            catch { }

                            // CE - (PTV +1cm)
                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.Replace(" ", "").ToUpper().Contains("CE-(PTV+1CM)") || x.Id.ToUpper().Equals("CE-(PTV+1CM)_AUTO")).First(), new DoseValue(25, DoseValue.DoseUnit.Gy), 150);
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
                                        model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("COEUR")).First(), new DoseValue(0.5, DoseValue.DoseUnit.Gy), 150);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("COEUR")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(1, DoseValue.DoseUnit.Gy), 0, 150);
                                    }
                                    catch { }

                                    // Poumon D
                                    try
                                    {
                                        model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("POUMON_D")).First(), new DoseValue(3, DoseValue.DoseUnit.Gy), 250);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("POUMON_D")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.95, DoseValue.DoseUnit.Gy), 0, 200);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("POUMON_D")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.90, DoseValue.DoseUnit.Gy), 2, 200);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("POUMON_D")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.7, DoseValue.DoseUnit.Gy), 5, 200);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("POUMON_D")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.40, DoseValue.DoseUnit.Gy), 10, 200);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("POUMON_D")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.2, DoseValue.DoseUnit.Gy), 25, 200);
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
                                        model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("SEIN_G")).First(), new DoseValue(1, DoseValue.DoseUnit.Gy), 170);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("SEIN_G")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(3, DoseValue.DoseUnit.Gy), 0, 250);
                                        model.GetContext.PlanSetup.OptimizationSetup.AddEUDObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("SEIN_G")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(3, DoseValue.DoseUnit.Gy), 40, 200);
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

                case "ARCTHERAPIE":

                    switch (model.UserSelection[0].Split('_').LastOrDefault().ToUpper() + " " + model.UserSelection[1].ToUpper())
                    {
                        case "PROSTATE":

                        case "PELVIS":


                            break;


                        case "":

                            model.GetContext.PlanSetup.OptimizationSetup.AddNormalTissueObjective(300, 3, 100, 30, 0.1);
                            break;
                    }
                    break;

                // pas d'opti en 3D
                /* case "3D":
                       switch (model.UserSelection[0].Split('_').LastOrDefault().ToUpper() + " " + model.UserSelection[1].ToUpper())
                       {
                           case "SEIN":

                               break;
                       }
                       break; */

                case "STEREOTAXIE":
                    switch (model.UserSelection[0].Split('_').LastOrDefault().ToUpper() + " " + model.UserSelection[1].ToUpper())
                    {
                        case "PROSTATE":

                            break;


                        case "":

                            model.GetContext.PlanSetup.OptimizationSetup.AddNormalTissueObjective(300, 3, 100, 30, 0.1);
                            break;
                    }
                    break;
                    // Pas d'opti en DCA
                    /*case "DYNAMIC ARC":

                        switch (model.UserSelection[0].Split('_').LastOrDefault().ToUpper() + " " + model.UserSelection[1].ToUpper())
                        {
                            case "PROSTATE":

                                break;
                        }
                        break;
                    */
            }

        }
    }
}
