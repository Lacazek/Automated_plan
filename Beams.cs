/******************************************************************************
 * Nom du fichier : Beams.cs
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
using System.IO;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Text.RegularExpressions;
using System.Net.Configuration;

namespace Opti_Struct
{
    internal class Beams
    {
        private VVector _isocenter;
        private double _gantryAngle = 30;
        private double _collimatorAngle = 330;
        private Structure _target;
        public Beams()
        {
            _isocenter = new VVector();
        }

        internal void CreateBeams(UserInterfaceModel model)
        {
            try
            {
                model.Message = $"\n*************************Partie faisceaux*************************\n";

                // Suppression des faisceaux pré-existents 
                if (model.GetContext.ExternalPlanSetup.Beams != null)
                {
                    foreach (var beam in model.GetContext.ExternalPlanSetup.Beams)
                        model.GetContext.ExternalPlanSetup.RemoveBeam(beam);
                    model.Message = $"La ballistique n'est as nulle; les faisceaux ont tous été supprimés\nLes nouveaux faisceaux sont ajoutés";
                }

                // Check table Halcyon
                IsThereCouch(model);
                model.Message = $"Table insérée";

                // Prescription
                AddPrescription(model);
                model.Message = $"Prescription complétée";

                // Point de ref
                try
                {
                    if (!model.GetContext.PlanSetup.ReferencePoints.Any(x => x.Id.ToUpper().Equals("AUTOPOINT")))
                    {
                        model.GetContext.PlanSetup.AddReferencePoint(true, null, "AutoPoint");
                        model.GetContext.PlanSetup.ReferencePoints.First(x => x.Id.Equals("AutoPoint")).TotalDoseLimit = model.GetContext.PlanSetup.TotalDose;
                        model.GetContext.PlanSetup.ReferencePoints.First(x => x.Id.Equals("AutoPoint")).DailyDoseLimit = model.GetContext.PlanSetup.DosePerFraction;
                        model.GetContext.PlanSetup.ReferencePoints.First(x => x.Id.Equals("AutoPoint")).SessionDoseLimit = model.GetContext.PlanSetup.DosePerFraction;
                        model.Message = $"Point de référence généré et remplie";
                    }
                    else
                    {
                        model.GetContext.PlanSetup.ReferencePoints.First(x => x.Id.Equals("AutoPoint")).TotalDoseLimit = model.GetContext.PlanSetup.TotalDose;
                        model.GetContext.PlanSetup.ReferencePoints.First(x => x.Id.Equals("AutoPoint")).DailyDoseLimit = model.GetContext.PlanSetup.DosePerFraction;
                        model.GetContext.PlanSetup.ReferencePoints.First(x => x.Id.Equals("AutoPoint")).SessionDoseLimit = model.GetContext.PlanSetup.DosePerFraction;
                        model.Message = $"Point de référence déja existant";
                    }
                }
                catch( Exception ex)
                {
                    model.Message = $"Le point de référence existe déjà, il est n'est peut être as actif pour le plan\n";
                    model.Message = ex.ToString();
                }

                //Imagerie
                ImagingBeamSetupParameters ImageParameters = new ImagingBeamSetupParameters(ImagingSetup.kVCBCT, 140, 140, 140, 140, 280, 280);
                model.Message = $"Paramètres d'imagerie générés'";

                // Paramètre DRR (taille [mm],pondération, fenetre scan de , fenêtre scan à, découpe de, découpe à)
                DRRCalculationParameters DRR = new DRRCalculationParameters(500, 1, -450, 150, 2, 6);
                model.Message = $"Paramètres de DRR générés";

                // Isocentre
                try
                {
                    _target = model.GetContext.StructureSet.Structures.Where(s => model.Targets.Any(target => target.Equals(s.Id))).OrderByDescending(s => s.Volume).FirstOrDefault();
                }
                catch
                {
                    _target = model.GetContext.StructureSet.Structures.Where(id => id.Id.Equals(model.GetContext.PlanSetup.TargetVolumeID)).First();
                }
                _isocenter = model.GetContext.StructureSet.Structures.First(x => x.Id.Equals(_target.Id)).CenterPoint;
                model.Message = $"L'isocentre est placé au barycentre du volume : {_target.Id}";

                // Paramètres faisceaux
                ExternalBeamMachineParameters BeamParameters = new ExternalBeamMachineParameters(
                        model.UserSelection[3].ToUpper().Contains("HALCYON") ? model.UserSelection[3].Split(':')[0] : model.UserSelection[3],
                        model.UserSelection[3].ToUpper().Contains("HALCYON") ? "6X" : model.GetContext.ExternalPlanSetup.DosePerFraction.Dose > 4 ? "6X-FFF" : "6X",
                        model.UserSelection[3].ToUpper().Contains("HALCYON") ? 740 : model.GetContext.ExternalPlanSetup.DosePerFraction.Dose > 4 ? 1400 : 600,
                        model.UserSelection[2].ToUpper().Contains("IMRT") || model.UserSelection[2].ToUpper().Contains("3D") ? "STATIC" : "ARC",
                        model.UserSelection[3].ToUpper().Contains("HALCYON") ? "FFF" : model.GetContext.ExternalPlanSetup.DosePerFraction.Dose > 4 ? "FFF" : null);

                // Angles
                SetAngles(model, BeamParameters);

                model.Message = $"Optimisation de l'angle du bras et du collimateur réalisée";

                double[] metersetWeight = new double[101];
                for (int i = 0; i < 101; i++)
                {
                    metersetWeight[i] = (1.0 / 100.0) * i;
                }

                // Beams
                #region IMRT
                if (model.UserSelection[2].Contains("IMRT") && model.UserSelection[0].ToUpper().Contains("SEIN"))
                {
                    // évaluer ici 
                    _gantryAngle = _gantryAngle + 10;

                    model.GetContext.ExternalPlanSetup.AddFixedSequenceBeam(BeamParameters, _collimatorAngle, _gantryAngle, _isocenter);
                    model.GetContext.ExternalPlanSetup.AddFixedSequenceBeam(BeamParameters, _collimatorAngle, _gantryAngle + 10, _isocenter);
                    model.GetContext.ExternalPlanSetup.AddFixedSequenceBeam(BeamParameters, _collimatorAngle, _gantryAngle - 10, _isocenter);

                    if (model.UserSelection[1].Contains("Droit"))
                    {
                        model.GetContext.ExternalPlanSetup.AddFixedSequenceBeam(BeamParameters, _collimatorAngle == 0 ? 0 : 360 - _collimatorAngle, _gantryAngle + 180, _isocenter);
                        model.GetContext.ExternalPlanSetup.AddFixedSequenceBeam(BeamParameters, _collimatorAngle == 0 ? 0 : 360 - _collimatorAngle, _gantryAngle + 190, _isocenter);
                    }
                    else
                    {
                        model.GetContext.ExternalPlanSetup.AddFixedSequenceBeam(BeamParameters, _collimatorAngle == 0 ? 0 : 360 - _collimatorAngle, _gantryAngle - 180, _isocenter);
                        model.GetContext.ExternalPlanSetup.AddFixedSequenceBeam(BeamParameters, _collimatorAngle == 0 ? 0 : 360 - _collimatorAngle, _gantryAngle - 190, _isocenter);
                    }

                    foreach (var (b, index) in model.GetContext.PlanSetup.Beams.Select((b, index) => (b, index)))
                    {
                        // ajouter tolérances table  b.set
                        if (!b.IsSetupField)
                        {
                            b.Id = index < 3 ? "TGI " + (11 + index) : "TGE " + (11 + index - 3);
                            b.FitMLCToStructure(_target);
                        }
                    }
                    model.Message = $"Balistique IMRT associée correctement";
                }
                #endregion

                #region ARC
                else if (model.UserSelection[2].Contains("Arcthérapie") || model.UserSelection[2].Contains("Dynamic Arc") || model.UserSelection[2].Contains("Stéréotaxie"))
                {

                    int gantryStart = model.UserSelection[1].Contains("Gauche") ? 179 : model.UserSelection[1].Contains("Droit") ? 181 : 181;
                    int gantryEnd = model.UserSelection[1].Contains("Gauche") ? 0 : model.UserSelection[1].Contains("Droit") ? 0 : 179;
                    var firstDirection = model.UserSelection[1].Contains("Gauche") ? GantryDirection.CounterClockwise : GantryDirection.Clockwise;
                    var secondDirection = model.UserSelection[1].Contains("Gauche") ? GantryDirection.Clockwise : GantryDirection.CounterClockwise;

                    if (model.UserSelection[3].ToUpper().Contains("HALCYON"))
                    {
                        model.GetContext.ExternalPlanSetup.AddVMATBeamForFixedJaws(BeamParameters, metersetWeight, _collimatorAngle, gantryStart, gantryEnd, firstDirection, 0, _isocenter);
                        model.GetContext.ExternalPlanSetup.AddVMATBeamForFixedJaws(BeamParameters, metersetWeight, 360 - _collimatorAngle, gantryEnd, gantryStart, secondDirection, 0, _isocenter);
                    }
                    else
                    {
                        model.GetContext.ExternalPlanSetup.AddConformalArcBeam(BeamParameters, _collimatorAngle, 180, gantryStart, gantryEnd, firstDirection, 0, _isocenter);
                        model.GetContext.ExternalPlanSetup.AddConformalArcBeam(BeamParameters, 360 - _collimatorAngle, 180, gantryEnd, gantryStart, secondDirection, 0, _isocenter);
                        model.GetContext.ExternalPlanSetup.Beams.ToList().ForEach(beam => beam.FitCollimatorToStructure(new FitToStructureMargins(5), _target, true, true, false));
                        model.GetContext.ExternalPlanSetup.OptimizationSetup.UseJawTracking = true;
                    }

                    foreach (var (b, index) in model.GetContext.PlanSetup.Beams.Select((b, index) => (b, index)))
                    {
                        // ajouter tolérances table  b.set
                        if (!b.IsSetupField)
                            b.Id = "Arc 1" + (index + 1);
                    }
                    model.Message = $"Balistique Arcthérapie associée correctement";
                }
                #endregion

                else
                {
                    //OK
                    //model.GetContext.ExternalPlanSetup.AddStaticBeam(BeamParameters, new VRect<double>(0, 0, 0, 0), _collimatorAngle, _gantryAngle, 1, _isocenter);
                    //model.GetContext.ExternalPlanSetup.AddStaticBeam(BeamParameters, new VRect<double>(0, 0, 0, 0), _collimatorAngle, _gantryAngle + 180, 1, _isocenter);


                    // A évaluer ici
                    float[,] leaves = new float[2, 60];

                    for (int i = 0; i < 60; i++)
                    {
                        leaves[0, i] = -10;
                        leaves[1, i] = 10;
                    }

                    model.GetContext.ExternalPlanSetup.AddMLCBeam(BeamParameters, leaves, new VRect<double>(0, 0, 0, 0), _collimatorAngle, _gantryAngle, 1, _isocenter);
                    model.GetContext.ExternalPlanSetup.AddMLCBeam(BeamParameters, leaves, new VRect<double>(0, 0, 0, 0), _collimatorAngle, _gantryAngle + 180, 1, _isocenter);
                    model.GetContext.ExternalPlanSetup.Beams.ToList().ForEach(beam => beam.FitCollimatorToStructure(new FitToStructureMargins(7), _target, true, true, true));
                    model.Message = $"Balistique 3D associée correctement";
                }

                model.GetContext.ExternalPlanSetup.AddImagingSetup(BeamParameters, ImageParameters, model.GetContext.StructureSet.Structures.First(st => st.Id.Equals(model.GetContext.PlanSetup.TargetVolumeID)));
                model.GetContext.PlanSetup.Beams.First(x => x.IsSetupField).CreateOrReplaceDRR(DRR);
                model.Message = $"KVCBCT mis à jour avec succés";

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur dans la création des faisceaux\n {ex.Message}");
            }
        }

        internal void SetAngles(UserInterfaceModel model, ExternalBeamMachineParameters BeamParameters)
        {

            #region Gantry puis collimateur
            if (model.UserSelection[2].ToUpper().Contains("IMRT") && model.UserSelection[0].ToUpper().Contains("SEIN"))
            {

                // Angle du gantry, à faire en priorité
                int[] gantryAngle = new int[61];

                for (int i = 0; i < 61; i++)
                {
                    if (model.UserSelection[1].Contains("Droit"))
                        gantryAngle[i] = 10 + i;
                    else
                        gantryAngle[i] = 280 + i;
                }

                // Angle du collimateur
                int[] colliAngle = new int[31 + 30];
                for (int i = 0; i < 30; i++)
                {
                    if (model.UserSelection[1].Contains("Droit"))
                        colliAngle[i] = 328 + i;
                    else
                        colliAngle[i] = 0 + i;
                }

                int index = 0;
                List<double> Areas = new List<double>();
                List<Dictionary<int, double>> mlc = new List<Dictionary<int, double>>();
                double Area = 0.0;

                foreach (var angle in gantryAngle)
                {
                    Area = 0.0;
                    var beam_colli = model.GetContext.ExternalPlanSetup.AddFixedSequenceBeam(BeamParameters, _collimatorAngle, angle, _isocenter);
                    model.GetContext.PlanSetup.Beams.First().FitMLCToStructure(_target);
                    mlc.Add(model.GetContext.ExternalPlanSetup.Beams.First().CalculateAverageLeafPairOpenings());

                    foreach (var key in mlc[index].Keys)
                    {
                        Area += mlc[index][key];
                    }
                    Areas.Add(Area);

                    if (index != 0 && Areas[index] < Areas[index - 1])
                    {
                        _gantryAngle = angle;
                    }
                    index++;
                    model.GetContext.ExternalPlanSetup.RemoveBeam(beam_colli);
                }

                index = 0;

                foreach (var angle in colliAngle)
                {
                    Area = 0.0;
                    var beam_colli = model.GetContext.ExternalPlanSetup.AddFixedSequenceBeam(BeamParameters, angle, _gantryAngle, _isocenter);
                    model.GetContext.PlanSetup.Beams.First().FitMLCToStructure(_target);
                    mlc.Add(model.GetContext.ExternalPlanSetup.Beams.First().CalculateAverageLeafPairOpenings());

                    foreach (var key in mlc[index].Keys)
                    {
                        Area += mlc[index][key];
                    }
                    Areas.Add(Area);

                    if (index != 0 && Areas[index] < Areas[index - 1])
                    {
                        _collimatorAngle = angle;
                    }
                    index++;
                    model.GetContext.ExternalPlanSetup.RemoveBeam(beam_colli);
                }
            }
            else if (model.UserSelection[0].ToUpper().Contains("POUMON"))
            {
                _collimatorAngle = 45;
            }
            else
            {
                _collimatorAngle = 30;
            }
            #endregion
        }

        internal void AddPrescription(UserInterfaceModel model)
        {
            using (StreamReader SelectedPrescription = new StreamReader(Path.Combine(model.GetPrescription, model.UserSelection[0] + ".txt")))
            {
                string firstLine = SelectedPrescription.ReadLine();
                string secondLine = SelectedPrescription.ReadLine();
                string thirdLine = SelectedPrescription.ReadLine();

                model.GetContext.PlanSetup.SetPrescription(
                    int.Parse(Regex.Match(secondLine.Split(':')[1], @"\d+").Value),
                    new DoseValue(double.Parse(Regex.Match(firstLine.Split(':')[1], @"\d+\.?\d*").Value), DoseValue.DoseUnit.Gy),
                    double.Parse(Regex.Match(thirdLine.Split(':')[1], @"\d+").Value) / 100);
            }
        }
        internal void IsThereCouch(UserInterfaceModel model)
        {
            IReadOnlyList<Structure> couchStructureList = model.GetContext.StructureSet.Structures.ToList();
            bool ImageResized = false;
            string error = "Erreur dans la création de la table";

            // Que pour l'Halcyon car le truebeam la table est ajouté sans problème par lecture du fichier txt
            // Pour l'Halcyon cela est dépendant s'il y a un faisceau ou non (l'ajout se fait par lecture de la machine dans les paramètres faisceau)
            if (!model.GetContext.StructureSet.Structures.Any(x => x.DicomType.ToUpper().Equals("SUPPORT")))
                model.GetContext.StructureSet.AddCouchStructures("RDS_Couch_Top", PatientOrientation.NoOrientation, RailPosition.In, RailPosition.In, null, null, null, out couchStructureList, out ImageResized, out error);
        }
    }
}
