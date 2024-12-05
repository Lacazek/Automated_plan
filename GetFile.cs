using System;
using System.IO;
using System.Windows;
using System.ComponentModel;
using VMS.TPS.Common.Model.API;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;
using System.Runtime.InteropServices;
using static System.Net.WebRequestMethods;

namespace Structure_optimisation
{

    internal class GetFile : INotifyPropertyChanged
    {
        private string _userFileChoice;
        private CreateVolume _createVolume;
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<string> MessageChanged;
        private string _path;
        private string _prescriptionPath;
        private string _volumePath;
        private string _checkPath;
        private string _message;
        private List<string> _targets;

        public GetFile(UserInterfaceModel model)
        {
            _userFileChoice = string.Empty;
            _createVolume = new CreateVolume(model);
            _path = Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).ToString(), @"File");
            _prescriptionPath = Path.Combine(_path, @"Prescription");
            _checkPath = Path.Combine(_path, @"Check_Dosi");
            try
            {
                _volumePath = Path.Combine(_path, $@"Volume\{Environment.UserName}");
            }
            catch
            {
                _volumePath = Path.Combine(_path, $@"Volume");
            }
            _createVolume.MessageChanged += VolumeMessageChanged;
        }

        internal void CreateUserVolumeFile ()
        {
            string workingfile = System.IO.Path.Combine(_volumePath, _userFileChoice + ".txt");
            Message = $"Fichier choisi : {_userFileChoice}";
            Message = $"Chemin du fichier : {workingfile}\n";
            try
            {
                for (int target = 0; target < _targets.Count(); target++)
                {
                    Message = $"La cible n°{target + 1} choisie est : \n{_targets[target]}";
                }
                _createVolume.CreationVolume(workingfile, _targets);
            }
            catch (Exception ex)
            {

                MessageBox.Show("Une erreur est survenue dans la création des volumes : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                Message = $"Une erreur est survenue dans la création des volumes : {ex.Message} \n";
            }
        }

        internal void CreateUserDosimetryFile(UserInterfaceModel model)
        {
           
          try
            {
                string treatmentType = model.UserSelection[2].ToUpper();
                if (treatmentType == "ARCTHÉRAPIE" || treatmentType == "DYNAMIC ARC" || treatmentType == "STÉRÉOTAXIE")
                {
                    treatmentType = "VMAT"; // Standardiser en "VMAT" pour ces choix
                }
                string[] parts = {model.UserSelection[0].Split('_').LastOrDefault().ToUpper(),
                model.UserSelection[1].ToUpper(),
                treatmentType,
                model.UserSelection[3].ToUpper()
                };


                string CorrectStructureFile = Directory.GetFiles(_volumePath, "*.txt")
        .Select(file => Path.GetFileNameWithoutExtension(file)).FirstOrDefault(fileNameWithoutExtension =>
        {
            var segments = fileNameWithoutExtension.Split('_');

            return segments.Any(segment => segment.ToUpper().Equals(parts[0], StringComparison.OrdinalIgnoreCase)) &&
                   fileNameWithoutExtension.ToUpper().Contains(parts[1]) &&
                   fileNameWithoutExtension.ToUpper().Contains(parts[2]);
        });

            }
            catch (Exception ex)
            {

                MessageBox.Show("Une erreur est survenue : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                Message = $"Une erreur est survenue : {ex.Message} \n";
            }
        }

        #region Get and Set
        internal string UserFile
        {
            get { return _userFileChoice; }
            set
            {
                _userFileChoice = value;
                OnPropertyChanged(nameof(UserFile));
            }
        }

        internal string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                OnMessageChanged();
            }
        }
        internal string GetPath
        {
            get { return _path; }
            set { _path = value; }
        }
        internal string GetPrescription
        {
            get { return _prescriptionPath; }
        }
        internal string GetVolumePath
        {
            get { return _volumePath; }
            set { _volumePath = value; }
        }
        internal string GetCheckPath
        {
            get { return _checkPath; }
            set { _checkPath = value; }
        }

        internal List<string> Targets
        {
            set { _targets = value; }
        }
        internal CreateVolume GetVolume
        {
            get { return _createVolume; }
        }

        #region Update log file
        private void VolumeMessageChanged(object sender, string e)
        {
            Message = e;
        }
        protected virtual void OnMessageChanged()
        {
            MessageChanged?.Invoke(this, _message);
        }
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_createVolume.Message));
        }
        #endregion
        #endregion
    }
}
