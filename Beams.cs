using Structure_optimisation;
using System;
using System.IO;
using System.Windows;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Text.RegularExpressions;
using System.Windows.Media.Media3D;
using System.Reflection;
using System.Windows.Input;
using System.Security.Cryptography;

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
            //try
            //{
            // Check table Halcyon
            IsThereCouch(model);

            // Prescription
            AddPrescription(model);

            // Point de ref
            model.GetContext.PlanSetup.AddReferencePoint(true, null, "AutoPoint");
            model.GetContext.PlanSetup.ReferencePoints.First(x => x.Id.Equals("AutoPoint")).TotalDoseLimit = model.GetContext.PlanSetup.TotalDose;
            model.GetContext.PlanSetup.ReferencePoints.First(x => x.Id.Equals("AutoPoint")).DailyDoseLimit = model.GetContext.PlanSetup.DosePerFraction;
            model.GetContext.PlanSetup.ReferencePoints.First(x => x.Id.Equals("AutoPoint")).SessionDoseLimit = model.GetContext.PlanSetup.DosePerFraction;

            //Imagerie
            ImagingBeamSetupParameters ImageParameters = new ImagingBeamSetupParameters(ImagingSetup.kVCBCT, 140, 140, 140, 140, 280, 280);

            // Paramètre DRR (taille [mm],pondération, fenetre scan de , fenêtre scan à, découpe de, découpe à)
            DRRCalculationParameters DRR = new DRRCalculationParameters(500, 1, -450, 150, 2, 6);

            // Isocentre
            // Demander utilisateur la target ID ????

            _target = model.GetContext.StructureSet.Structures.Where(id => id.Id.Equals(model.GetContext.PlanSetup.TargetVolumeID)).First();
            _isocenter = model.GetContext.StructureSet.Structures.First(x => x.Id.Equals(_target.Id)).CenterPoint;

            // Paramètres faisceaux
            ExternalBeamMachineParameters BeamParameters = new ExternalBeamMachineParameters(
                    model.UserSelection[3].ToUpper().Contains("HALCYON") ? model.UserSelection[3].Split(':')[0] : model.UserSelection[3],
                    model.UserSelection[3].ToUpper().Contains("HALCYON") ? "6X-FFF" : "6X",
                    model.UserSelection[3].ToUpper().Contains("HALCYON") ? 740 : 600,
                    model.UserSelection[2].ToUpper().Contains("IMRT") || model.UserSelection[2].ToUpper().Contains("3D") ? "STATIC" : "ARC",
                    model.UserSelection[3].ToUpper().Contains("HALCYON") ? "FFF" : "WFF");

            // Angles
            SetAngles(model, BeamParameters);

            // Beams
            if (model.UserSelection[2].Contains("IMRT") && model.UserSelection[0].ToUpper().Contains("SEIN"))
            {
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
                    model.GetContext.ExternalPlanSetup.AddFixedSequenceBeam(BeamParameters, Math.Abs(_collimatorAngle - 360), _gantryAngle - 180, _isocenter);
                    model.GetContext.ExternalPlanSetup.AddFixedSequenceBeam(BeamParameters, Math.Abs(_collimatorAngle - 360), _gantryAngle - 190, _isocenter);
                }

                foreach (var (b, index) in model.GetContext.PlanSetup.Beams.Select((b, index) => (b, index)))
                {
                    // ajouter tolérances table  b.set
                    if (!b.IsSetupField)
                        b.Id = index < 3 ? "TGI " + (11 + index) : "TGE " + (11 + index - 3);
                }
            }

            else if (model.UserSelection[2].Contains("Arcthérapie") || model.UserSelection[2].Contains("Dynamic Arc") || model.UserSelection[2].Contains("Stéréotaxie"))
            {
                // Checker si c'est bine conformal arc beam pour le VMAT
                model.GetContext.ExternalPlanSetup.AddConformalArcBeam(BeamParameters, _collimatorAngle, 180, 179, 181, GantryDirection.CounterClockwise, 0, _isocenter);
                model.GetContext.ExternalPlanSetup.AddConformalArcBeam(BeamParameters, 360 - _collimatorAngle, 180, 181, 179, GantryDirection.Clockwise, 0, _isocenter);
            }

            else if (model.UserSelection[2].Contains("3D"))
            {

                model.GetContext.ExternalPlanSetup.AddStaticBeam(BeamParameters, new VRect<double>(100, 100, 100, 100), _collimatorAngle, _gantryAngle, 1, _isocenter);
                model.GetContext.ExternalPlanSetup.AddStaticBeam(BeamParameters, new VRect<double>(100, 100, 100, 100), _collimatorAngle, _gantryAngle + 180, 1, _isocenter);
            }

            model.GetContext.ExternalPlanSetup.AddImagingSetup(BeamParameters, ImageParameters, model.GetContext.StructureSet.Structures.First(st => st.Id.Equals(model.GetContext.PlanSetup.TargetVolumeID)));
            model.GetContext.PlanSetup.Beams.First(x => x.IsSetupField).CreateOrReplaceDRR(DRR);

            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"Erreur dans la création des faisceaux\n {ex.Message}");
            //}
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
