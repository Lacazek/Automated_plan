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
            string MyOptimizer, MyDoseCalculator;

            if (model.UserSelection[3].ToUpper().Contains("HALCYON"))
            {
                // TBOX
                MyOptimizer = "PO_18.0.0";
                MyDoseCalculator = "AAA_18.0.1";
                //MyOptimizer = "PO_18.0.1";
                //MyDoseCalculator = "PHOTONS_AAA_18.0.1";
                model.GetContext.PlanSetup.SetCalculationOption(MyOptimizer, "General/GpuSettings/UseGPU", "Yes");
                model.GetContext.PlanSetup.SetCalculationOption(MyOptimizer, "VMAT/ApertureShapeController", "Moderate");
            }
            else
            {
                //TBOX
                MyOptimizer = "PO_15.6.06";
                MyDoseCalculator = "PHOTONS_AAA_15.6.06 V2";
                model.GetContext.PlanSetup.SetCalculationOption(MyOptimizer, "PhotonOptCalculationOptions/UseGPU", "No");
                model.GetContext.PlanSetup.SetCalculationOption(MyDoseCalculator, "PhotonOptCalculationOptions/ApertureShapeController", "Moderate");
            }

            model.GetContext.PlanSetup.SetCalculationModel(CalculationType.PhotonOptimization, MyOptimizer);
            model.GetContext.PlanSetup.SetCalculationModel(CalculationType.PhotonVolumeDose, MyDoseCalculator);
            ConstructMyOptimizer(model);

            if (model.UserSelection[2].ToUpper().Equals("IMRT"))
            {
                model.GetContext.ExternalPlanSetup.Optimize(300);
                model.GetContext.ExternalPlanSetup.CalculateLeafMotionsAndDose();
                model.GetContext.ExternalPlanSetup.Optimize(300, OptimizationOption.ContinueOptimizationWithPlanDoseAsIntermediateDose);
                model.GetContext.ExternalPlanSetup.CalculateLeafMotionsAndDose();
            }

            else if (model.UserSelection[2].ToUpper().Equals("3D"))
            {
                model.GetContext.ExternalPlanSetup.CalculateDose();
            }

            else
            {
                model.GetContext.ExternalPlanSetup.OptimizeVMAT();
                model.GetContext.ExternalPlanSetup.CalculateDose();
                model.GetContext.ExternalPlanSetup.OptimizeVMAT(new OptimizationOptionsVMAT(OptimizationIntermediateDoseOption.UseIntermediateDose, "HHMA294"));
                model.GetContext.ExternalPlanSetup.CalculateDose();
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


                        case "PELVIS":

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("PTV T")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("PTV T")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            break;

                        case "PELVIS+GG":

                            // Cbles
                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("z_PTV T")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose, DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("z_PTV T")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 1.02, DoseValue.DoseUnit.Gy), 0, 300);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV N_OPT")).First(), OptimizationObjectiveOperator.Lower, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.9, DoseValue.DoseUnit.Gy), 100, 300);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("Z_PTV N_OPT")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(model.GetContext.PlanSetup.TotalDose.Dose * 0.95, DoseValue.DoseUnit.Gy), 0, 400);
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
                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT")).First(), new DoseValue(20, DoseValue.DoseUnit.Gy), 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(37, DoseValue.DoseUnit.Gy), 0, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(25, DoseValue.DoseUnit.Gy), 15, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(15, DoseValue.DoseUnit.Gy), 30, 150);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Equals("Z_VESSIE_OPT")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(10, DoseValue.DoseUnit.Gy), 70, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("FEMUR D")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(25, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("FEMUR G")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(25, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("MOELLE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(15, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("CANAL_MEDULLAIRE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            try
                            {

                                model.GetContext.PlanSetup.OptimizationSetup.AddMeanDoseObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("CAVITE_PERITONEALE")).First(), new DoseValue(5, DoseValue.DoseUnit.Gy), 180);
                                model.GetContext.PlanSetup.OptimizationSetup.AddPointObjective(model.GetContext.StructureSet.Structures.Where(x => x.Id.ToUpper().Contains("CAVITE_PERITONEALE")).First(), OptimizationObjectiveOperator.Upper, new DoseValue(20, DoseValue.DoseUnit.Gy), 0, 150);
                            }
                            catch { }

                            break;

                        case "":


                            break;
                    }

                    model.GetContext.PlanSetup.OptimizationSetup.AddNormalTissueObjective(300, 3, 100, 30, 0.1);
                    break;

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
            }

        }
    }
}
