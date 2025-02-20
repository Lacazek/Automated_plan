/******************************************************************************
 * Nom du fichier : UserInterfaceModel.cs
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
using System.ComponentModel;
using VMS.TPS.Common.Model.API;
using System.IO;
using System.Reflection;
using System.Windows;
using Opti_Struct;
using DoseCheck;

namespace Structure_optimisation
{
    internal class UserInterfaceModel : INotifyPropertyChanged
    {
        private ScriptContext _context;
        private List<string> _userSelection;
        private List<string> _localisation;
        private List<string> _list;
        private List<string> _prescription;

        private string _userChoice;
        private string _rename;
        private string _log;
        private readonly string _fisherMan;

        private bool _alredayClosed;

        private GetFile _file;
        private StreamWriter _logFile;

        private Beams _beams;
        private Dosimetry _dosimetry;
        private GetMyData _getMyData;

        public event PropertyChangedEventHandler PropertyChanged;

        public UserInterfaceModel(ScriptContext context)
        {
            _userChoice = string.Empty;
            _rename = string.Empty;
            _alredayClosed = false;

            _context = context;
            _list = new List<string>();
            _localisation = new List<string>();

            _beams = new Beams();
            _dosimetry = new Dosimetry();


            _userSelection = new List<string>();
            _file = new GetFile(this);
            _fisherMan = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).ToString(), "fisherMan4.png");
            FillList();

            string LogFileName = $@"File\Log\LogFile_{Environment.UserName}.txt";
            FileInfo _fileinfo = new FileInfo(LogFileName);
            if (_fileinfo.Exists && _fileinfo.Length > 500 * 1000)
                _fileinfo.Delete();
            _logFile = new StreamWriter(LogFileName, true);

            _file.MessageChanged += MessageChanged;
            Message = $"\n**********************************";
            Message = $"Debut de programme : {DateTime.Now}";
            Message = $"Ordinateur utilisé : {Environment.MachineName}";
            Message = $"OS : {Environment.OSVersion}";
            Message = $"Domaine windows : {Environment.UserDomainName}";
            Message = $"Dossier de travail : {Environment.SystemDirectory}";
            Message = $"Taille du jeu de travail : {Environment.WorkingSet}";
            Message = $"User : {Environment.UserName}\n";
            Message = $"Fichier ouvert\n";


            bool Run = true;
            string Log_Auto_Contour = string.Empty, Log_Auto_Planning = string.Empty, Log_Auto_Evaluation = string.Empty, Log_Auto_Transfert = string.Empty;

            while (Run)
            {
                _log = string.Empty;
                Front_Page FP = new Front_Page(Log_Auto_Contour, Log_Auto_Planning, Log_Auto_Evaluation, Log_Auto_Transfert);
                FP.Signal += (s, result) =>
                {
                    switch (result)
                    {
                        case "0":
                            try
                            {
                                Message = $"\n\n**************************************************";
                                Message = $"L'utilisateur à choisi de réaliser l'autocontour\n";
                                UserInterface_Volume interfaceVolume = new UserInterface_Volume(this);
                                interfaceVolume.ShowDialog();
                                Log_Auto_Contour = _log;
                            }
                            catch
                            {
                                IsOpened(true);
                                _alredayClosed = true;
                            }
                            break;

                        case "1":
                            try
                            {
                                Message = $"\n\n**************************************************";
                                Message = $"L'utilisateur à choisi de réaliser l'autoplanning\n";
                                UserInterface_Dosi Interface_dosi = new UserInterface_Dosi(this);
                                Interface_dosi.ShowDialog();
                                Log_Auto_Planning = _log;
                            }
                            catch
                            {
                                IsOpened(true);
                                _alredayClosed = true;
                            }
                            break;

                        case "2":

                            try
                            {
                                Message = $"\n\n**************************************************";
                                Message = $"L'utilisateur à choisi de réaliser l'evaluation dosimétrique\n";
                                _getMyData = new GetMyData(this);
                                UserInterface_Check Interface_check = new UserInterface_Check(this);
                                Interface_check.ShowDialog();
                                Log_Auto_Evaluation = _log;
                            }
                            catch
                            {
                                IsOpened(true);
                                _alredayClosed = true;
                            }
                            break;

                        case "3":

                            try
                            {
                                Message = $"\n\n**************************************************";
                                Message = $"L'utilisateur à choisi de réaliser le transfert automatique\n";
                                Log_Auto_Transfert = $"Les transferts ne sont actuellement pas opérationnels";
                                //Log_Auto_Transfert = _log;
                            }
                            catch
                            {
                                IsOpened(true);
                                _alredayClosed = true;
                            }
                            break;

                        case "Close":
                            Run = false;
                            Message = $"L'utilisateur à mis fin au programme";
                            break;
                        default:
                            break;
                    }

                };
                FP.ShowDialog();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            try
            {
                context.ExternalPlanSetup.Id = _userSelection == null ?
                    (_userSelection[0] + " " + _userSelection[1] + " " + context.ExternalPlanSetup.TotalDose.ToString() + "Gy").Length < 16 ?
                     _userSelection[0] + " " + _userSelection[1] + " " + context.ExternalPlanSetup.TotalDose.ToString() + "Gy" : "Autom_plan" : "Auto_plan";
                Message = $"Modification du nom de plan réalisée avec succés\n";
            }
            catch (Exception ex)
            {
                Message = $"Impossibilité de modifier le nom du plan\n";
                Message += ex.Message;
            }
        }

        internal void LaunchPlanning()
        {
            // 1 Création des contours --> getFile-> CreateVolume
            _file.CreateUserDosimetryFile(this);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            // 2 Création des faisceaux --> Beams
            if (MessageBox.Show("Voulez-vous créer les faisceaux? ?", "Information", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Message = $"L'utilisateur à choisi de réaliser automatiquement les faisceaux\n";
                _beams.CreateBeams(this);
                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (MessageBox.Show("Voulez-vous lancer la dosimétrie ?", "Information", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    Message = $"L'utilisateur à choisi de réaliser automatiquement la dosimétrie, à minima le remplissage des objectifs d'optimisation\n";
                    //3 Paramétrages de la dosi --> Dosimetry
                    _dosimetry.LaunchDosimetry(this);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                else
                    Message = $"L'utilisateur n'a pas choisi de réaliser automatiquement ni la dosimétrie ni le remplissage automatique des pré-contraintes dosimétriques\n";
            }
            else
                Message = $"L'utilisateur n'a pas choisi de réaliser automatiquement la mise en place des faisceaux\n";
        }

        internal void FillList()
        {
            foreach (var item in Directory.GetFiles(_file.GetVolumePath))
            {
                _localisation.Add(Path.GetFileNameWithoutExtension(item));
            }
            _localisation.Sort();
        }
        internal void ClearList()
        {
            _localisation.Clear();
        }
        internal void CreateUserVolumeFile()
        {
            _file.CreateUserVolumeFile();
        }

        #region get and set
        internal ScriptContext GetContext
        {
            get { return _context; }
        }
        internal string GetPrescription
        {
            get { return _file.GetPrescription; }
        }
        internal List<string> GetPrescription2
        {
            get { return _prescription; }
            set { _prescription = value; }
        }
        internal GetFile File
        {
            get { return _file; }
        }
        internal string UserFile
        {
            get { return _file.UserFile; }
            set { _file.UserFile = value; }
        }
        internal string UserFileCheck
        {
            get { return _getMyData.UserFileCheck; }
            set { _getMyData.UserFileCheck = value; }
        }
        internal string UserPath
        {
            get { return _file.GetPath; }
            set { _file.GetPath = value; }
        }
        internal string GetVolumePath
        {
            get { return _file.GetVolumePath; }
            set { _file.GetVolumePath = value; }
        }
        internal string GetCheckPath
        {
            get { return _file.GetCheckPath; }
            set { _file.GetCheckPath = value; }
        }
        internal List<string> Localisation
        {
            get { return _localisation; }
        }
        internal List<string> Targets
        {
            get { return _file.Targets; }
            set { _file.Targets = value; }
        }
        internal List<string> UserSelection
        {
            get { return _userSelection; }
            set
            {
                _userSelection.AddRange(value);
                _logFile.WriteLine($"Prescription : {_userSelection[0]}\nCôté : {_userSelection[1]}\nTechnique : {_userSelection[2]}\nMachine : {_userSelection[3]}\n");
            }
        }
        internal List<string> GetList
        {
            get { return _list; }
        }
        internal string AddFile
        {
            set { _list.Add(value); }
        }
        internal string Set_Log
        {
            set { _log += value; }
        }
        #endregion

        #region update message
        internal string Message
        {
            get { return _file.Message; }
            set
            {
                _logFile.WriteLine(value);
                _logFile.Flush();
                OnPropertyChanged(nameof(_file.Message));
            }
        }
        internal void IsOpened(bool test)
        {
            if (test == true && _alredayClosed != true)
            {
                _logFile.WriteLine($"Fichier Log fermé");
                _logFile.WriteLine($"Fin du programme : {DateTime.Now}");
                _logFile.WriteLine($"***************************Script terminé***************************");
                _logFile.Close();
            }
        }

        private void MessageChanged(object sender, string e)
        {
            _logFile.WriteLine(_file.Message);
            _logFile.Flush();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}

