using System;
using System.Collections.Generic;
using System.ComponentModel;
using VMS.TPS.Common.Model.API;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Opti_Struct;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices;
using DoseCheck;

namespace Structure_optimisation
{
    internal class UserInterfaceModel : INotifyPropertyChanged
    {
        private ScriptContext _context;
        private List<string> _userSelection;
        private List<string> _localisation;
        private List<string> _list;

        private string _userChoice;
        private string _rename;
        private readonly string _fisherMan;

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
            _context = context;
            _list = new List<string>();
            _localisation = new List<string>();

            _beams = new Beams();
            _dosimetry = new Dosimetry();


            _userSelection = new List<string>();
            _file = new GetFile(this);
            _fisherMan = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).ToString(), "fisherMan4.png");
            FillList();

            FileInfo _fileinfo = new FileInfo(@"LogFile.txt");
            if (_fileinfo.Exists && _fileinfo.Length > 500 * 1000)
                _fileinfo.Delete();
            _logFile = new StreamWriter(@"LogFile.txt", true);

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

            if (MessageBox.Show("Voulez-vous lancer l'autocontour ?\n\nSi vous sélectionnez NO\nIl est possible que la suite du programme ne fonctionne pas correctement\n\n" +
                "Si vous sélectionnez YES\nLes contours d'optimisations seront réalisés automatiquement", "Information", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                UserInterface_Volume interfaceVolume = new UserInterface_Volume(this);
                interfaceVolume.ShowDialog();
            }

            if (MessageBox.Show("Voulez-vous lancer l'autoplanning ?", "Information", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Message = $"\n\n**************************************************";
                Message = $"L'utilisateur à choisi de réaliser l'autoplanning\n";
                UserInterface_Dosi Interface_dosi = new UserInterface_Dosi(this);
                Interface_dosi.ShowDialog();

                //Améliorer fichier log

                if (MessageBox.Show("Voulez-vous lancer l'évaluation de plan ?", "Information", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    Message = $"\n\n**************************************************";
                    Message = $"L'utilisateur à choisi de réaliser l'evaluation dosimétrique\n";
                    _getMyData = new GetMyData(this);
                    UserInterface_Check Interface_check = new UserInterface_Check(this);
                    Interface_check.ShowDialog();

                    //Améliorer fichier log

                    if (MessageBox.Show("Voulez-vous lancer le transfert des données ?", "Information", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {

                    };
                };
            };

            context.ExternalPlanSetup.Name = _userSelection[0]+" " + _userSelection[1] + " " + context.ExternalPlanSetup.TotalDose.ToString()+"Gy";
        }

        internal void LaunchPlanning()
        {
            // 1 Création des contours --> getFile-> CreateVolume
            _file.CreateUserDosimetryFile(this);
            // 2 Création des faisceaux --> Beams
            _beams.CreateBeams(this);
            //3 Paramétrages de la dosi --> Dosimetry
            _dosimetry.LaunchDosimetry(this);
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
            if (test == true)
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

